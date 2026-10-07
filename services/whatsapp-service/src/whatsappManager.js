'use strict';

const fs = require('fs');
const path = require('path');
const { execFile } = require('child_process');
const { promisify } = require('util');
const QRCode = require('qrcode');
const { Client, LocalAuth } = require('whatsapp-web.js');
const { config } = require('./config');

const execFileAsync = promisify(execFile);

const Status = Object.freeze({
  Connected: 'Connected',
  Disconnected: 'Disconnected',
  Connecting: 'Connecting',
  QrRequired: 'QR Required',
  AuthenticationFailed: 'Authentication Failed',
});

class WhatsAppManager {
  constructor() {
    this.client = null;
    this.status = Status.Disconnected;
    this.qrDataUrl = null;
    this.qrRaw = null;
    this.phoneNumber = null;
    this.pushName = null;
    this.lastConnectedAt = null;
    this.lastError = null;
    this.starting = false;
    this.destroying = false;
    this.cachedGroups = [];
    this.cachedGroupsAt = null;
  }

  getSnapshot() {
    return {
      status: this.status,
      phoneNumber: this.phoneNumber,
      pushName: this.pushName,
      lastConnectedAt: this.lastConnectedAt,
      lastError: this.lastError,
      hasQr: Boolean(this.qrDataUrl),
      connected: this.status === Status.Connected,
    };
  }

  async getQrPayload() {
    if (this.status === Status.Connected) {
      return { status: this.status, qr: null, qrDataUrl: null };
    }

    return {
      status: this.status,
      qr: this.qrRaw,
      qrDataUrl: this.qrDataUrl,
    };
  }

  async start() {
    if (this.starting || (this.client && this.status === Status.Connected)) {
      return this.getSnapshot();
    }

    this.starting = true;
    this.status = Status.Connecting;
    this.lastError = null;

    try {
      await this.ensureAuthDir();
      await this.destroyClient(false);
      await this.killOrphanSessionBrowsers();

      try {
        await this.initializeClient();
      } catch (err) {
        const msg = safeError(err);
        if (!/browser is already running|userDataDir/i.test(msg)) throw err;

        console.warn('[whatsapp] session browser lock held; killing orphans and retrying once');
        await this.destroyClient(false);
        await this.killOrphanSessionBrowsers();
        await sleep(1500);
        await this.initializeClient();
      }

      return this.getSnapshot();
    } catch (err) {
      this.status = Status.AuthenticationFailed;
      this.lastError = safeError(err);
      console.error('[whatsapp] start failed:', this.lastError);
      throw err;
    } finally {
      this.starting = false;
    }
  }

  async initializeClient() {
    // On Windows local, headed Chrome is more reliable for WWebJS Store injection.
    // In Docker/Linux keep headless for servers.
    const isWin = process.platform === 'win32';
    const puppeteer = {
      headless: isWin ? false : true,
      args: [
        '--no-sandbox',
        '--disable-setuid-sandbox',
        '--disable-dev-shm-usage',
        '--disable-accelerated-2d-canvas',
        '--no-first-run',
        '--no-zygote',
        '--disable-gpu',
      ],
    };

    if (config.puppeteerExecutablePath) {
      puppeteer.executablePath = config.puppeteerExecutablePath;
    }

    this.client = new Client({
      authStrategy: new LocalAuth({
        clientId: config.clientId,
        dataPath: config.authPath,
      }),
      puppeteer,
    });

    this.bindEvents(this.client);
    await this.client.initialize();
  }

  bindEvents(client) {
    client.on('qr', async (qr) => {
      // Never log QR contents — only emit a high-level event.
      console.info('[whatsapp] QR required (scan via Linked Devices)');
      this.status = Status.QrRequired;
      this.qrRaw = qr;
      this.phoneNumber = null;
      this.pushName = null;
      try {
        this.qrDataUrl = await QRCode.toDataURL(qr, {
          errorCorrectionLevel: 'M',
          margin: 1,
          width: 320,
        });
      } catch (err) {
        this.qrDataUrl = null;
        this.lastError = safeError(err);
        console.error('[whatsapp] QR encode failed:', this.lastError);
      }
    });

    client.on('authenticated', () => {
      console.info('[whatsapp] authenticated');
      this.status = Status.Connecting;
      this.qrRaw = null;
      this.qrDataUrl = null;
      this.lastError = null;
    });

    client.on('ready', async () => {
      console.info('[whatsapp] ready');
      this.status = Status.Connected;
      this.qrRaw = null;
      this.qrDataUrl = null;
      this.lastConnectedAt = new Date().toISOString();
      this.lastError = null;
      try {
        const info = client.info;
        this.phoneNumber = info?.wid?.user ? `+${info.wid.user}` : null;
        this.pushName = info?.pushname || null;
      } catch {
        // ignore
      }
      // Warm group cache while the page is fresh (avoids later detached-frame empties).
      try {
        await this.listGroups({ allowCacheOnly: false });
      } catch (err) {
        console.warn('[whatsapp] warm groups failed:', safeError(err));
      }
    });

    client.on('auth_failure', (msg) => {
      console.error('[whatsapp] auth_failure');
      this.status = Status.AuthenticationFailed;
      this.lastError = typeof msg === 'string' ? msg : 'Authentication failed';
      this.qrRaw = null;
      this.qrDataUrl = null;
      this.phoneNumber = null;
      this.cachedGroups = [];
      this.cachedGroupsAt = null;
    });

    client.on('disconnected', (reason) => {
      console.warn('[whatsapp] disconnected:', String(reason || ''));
      this.status = Status.Disconnected;
      this.phoneNumber = null;
      this.pushName = null;
      this.qrRaw = null;
      this.qrDataUrl = null;
      this.lastError = reason ? String(reason) : null;
      this.cachedGroups = [];
      this.cachedGroupsAt = null;
    });
  }

  async reconnect() {
    await this.destroyClient(false);
    return this.start();
  }

  async regenerateQr() {
    await this.destroyClient(false);
    return this.start();
  }

  async logout() {
    try {
      if (this.client) {
        await this.client.logout();
      }
    } catch (err) {
      console.warn('[whatsapp] logout warning:', safeError(err));
    }

    await this.destroyClient(false);
    this.status = Status.Disconnected;
    this.phoneNumber = null;
    this.pushName = null;
    this.qrRaw = null;
    this.qrDataUrl = null;
    return this.getSnapshot();
  }

  async clearSession() {
    await this.destroyClient(false);
    await this.removeAuthDir();
    this.status = Status.Disconnected;
    this.phoneNumber = null;
    this.pushName = null;
    this.qrRaw = null;
    this.qrDataUrl = null;
    this.lastError = null;
    return this.getSnapshot();
  }

  async listGroups(options = {}) {
    this.ensureConnected();
    const allowCacheOnly = options.allowCacheOnly !== false;

    const byId = new Map();
    const add = (id, name) => {
      if (!id || typeof id !== 'string') return;
      const clean = id.trim();
      if (!clean.endsWith('@g.us')) return;
      if (!byId.has(clean)) {
        byId.set(clean, {
          id: clean,
          name: (name && String(name).trim()) || clean,
          participantsCount: null,
        });
      }
    };

    // Collect groups from a live page/frame (pupPage often detaches after WA reloads).
    try {
      const fromPage = await this.collectGroupsFromLivePage();
      for (const g of fromPage || []) add(g.id, g.name);
      console.info('[whatsapp] live page groups:', fromPage?.length || 0);
    } catch (err) {
      console.warn('[whatsapp] live page groups failed:', safeError(err));
    }

    // Fallback: classic getChats (may throw opaque "r" on some WA versions)
    if (byId.size === 0) {
      try {
        const chats = await this.client.getChats();
        for (const c of chats) {
          const id = c?.id?._serialized || '';
          if (c?.isGroup === true || id.endsWith('@g.us')) add(id, c?.name);
        }
      } catch (err) {
        console.warn('[whatsapp] getChats failed:', safeError(err));
      }
    }

    let groups = [...byId.values()].sort((a, b) =>
      String(a.name).localeCompare(String(b.name), 'fa')
    );

    if (groups.length > 0) {
      this.cachedGroups = groups;
      this.cachedGroupsAt = new Date().toISOString();
    } else if (allowCacheOnly && this.cachedGroups.length > 0) {
      console.warn(
        '[whatsapp] live groups empty; returning cache (%s @ %s)',
        this.cachedGroups.length,
        this.cachedGroupsAt || 'unknown'
      );
      groups = this.cachedGroups;
    }

    console.info('[whatsapp] returning groups:', groups.length);
    return groups;
  }

  async collectGroupsFromLivePage() {
    const targets = await this.getEvaluationTargets();
    let lastError = null;

    for (const target of targets) {
      try {
        const groups = await target.evaluate(extractGroupsInPage);
        if (Array.isArray(groups) && groups.length > 0) {
          return groups;
        }
        // Keep trying other frames even if this one returned [].
        if (Array.isArray(groups)) lastError = null;
      } catch (err) {
        lastError = err;
        const msg = safeError(err);
        if (/detached frame/i.test(msg)) {
          console.warn('[whatsapp] skipping detached frame');
          continue;
        }
        console.warn('[whatsapp] evaluate failed:', msg);
      }
    }

    if (lastError) throw lastError;
    return [];
  }

  async getEvaluationTargets() {
    const targets = [];
    const seen = new WeakSet();

    const push = (frameOrPage) => {
      if (!frameOrPage || typeof frameOrPage.evaluate !== 'function') return;
      if (seen.has(frameOrPage)) return;
      seen.add(frameOrPage);
      targets.push(frameOrPage);
    };

    // Prefer current pupPage + child frames
    try {
      const page = this.client?.pupPage;
      if (page && !page.isClosed?.()) {
        push(page);
        for (const frame of page.frames?.() || []) push(frame);
      }
    } catch {
      // ignore
    }

    // Recover after reload: scan all browser pages/frames
    try {
      const browser = this.client?.pupBrowser
        || (typeof this.client?.pupPage?.browser === 'function' ? this.client.pupPage.browser() : null);
      if (browser) {
        const pages = await browser.pages();
        for (const page of pages) {
          if (page.isClosed?.()) continue;
          push(page);
          for (const frame of page.frames?.() || []) push(frame);
        }
      }
    } catch (err) {
      console.warn('[whatsapp] browser page scan failed:', safeError(err));
    }

    return targets;
  }

  async sendMessage(groupId, message) {
    if (!groupId || typeof groupId !== 'string') {
      const error = new Error('groupId is required');
      error.statusCode = 400;
      throw error;
    }
    if (!message || typeof message !== 'string' || !message.trim()) {
      const error = new Error('message is required');
      error.statusCode = 400;
      throw error;
    }

    this.ensureConnected();

    const chatId = normalizeGroupId(groupId);
    const text = message.trim();

    try {
      return await this.sendMessageOnce(chatId, text);
    } catch (err) {
      const msg = safeError(err);
      if (!/detached frame/i.test(msg)) throw err;

      console.warn('[whatsapp] send hit detached frame; trying live-page fallback then reconnect');
      try {
        const viaPage = await this.sendViaLivePage(chatId, text);
        if (viaPage) return viaPage;
      } catch (pageErr) {
        console.warn('[whatsapp] live-page send failed:', safeError(pageErr));
      }

      await this.reconnect();
      await this.waitUntilConnected(45000);
      return this.sendMessageOnce(chatId, text);
    }
  }

  async sendMessageOnce(chatId, text) {
    const result = await this.client.sendMessage(chatId, text);
    return {
      ok: true,
      messageId: result?.id?._serialized || result?.id?.id || null,
      groupId: chatId,
      timestamp: new Date().toISOString(),
    };
  }

  async sendViaLivePage(chatId, text) {
    const targets = await this.getEvaluationTargets();
    let lastError = null;

    for (const target of targets) {
      try {
        const messageId = await target.evaluate(async (id, message) => {
          const wweb = window.WWebJS;
          if (!wweb || typeof wweb.sendMessage !== 'function') {
            throw new Error('WWebJS.sendMessage unavailable');
          }
          const chat = typeof wweb.getChat === 'function'
            ? await wweb.getChat(id)
            : null;
          if (!chat) throw new Error('chat not found on page');
          const sent = await wweb.sendMessage(chat, message);
          return sent?.id?._serialized || sent?.id?.id || null;
        }, chatId, text);

        return {
          ok: true,
          messageId: messageId || null,
          groupId: chatId,
          timestamp: new Date().toISOString(),
        };
      } catch (err) {
        lastError = err;
        if (/detached frame/i.test(safeError(err))) continue;
      }
    }

    if (lastError) throw lastError;
    return null;
  }

  async waitUntilConnected(timeoutMs = 30000) {
    const started = Date.now();
    while (Date.now() - started < timeoutMs) {
      if (this.status === Status.Connected && this.client) return;
      if (this.status === Status.AuthenticationFailed || this.status === Status.QrRequired) {
        const error = new Error(`WhatsApp reconnect needs attention (status: ${this.status})`);
        error.statusCode = 409;
        throw error;
      }
      await new Promise((r) => setTimeout(r, 500));
    }
    const error = new Error(`WhatsApp reconnect timed out (status: ${this.status})`);
    error.statusCode = 409;
    throw error;
  }

  ensureConnected() {
    if (!this.client || this.status !== Status.Connected) {
      const error = new Error(`WhatsApp is not connected (status: ${this.status})`);
      error.statusCode = 409;
      throw error;
    }
  }

  async ensureAuthDir() {
    await fs.promises.mkdir(config.authPath, { recursive: true });
  }

  async removeAuthDir() {
    try {
      await fs.promises.rm(config.authPath, { recursive: true, force: true });
      await this.ensureAuthDir();
    } catch (err) {
      console.warn('[whatsapp] clear session warning:', safeError(err));
    }
  }

  async destroyClient(resetStatus) {
    if (!this.client || this.destroying) {
      if (resetStatus) {
        this.status = Status.Disconnected;
      }
      // Still try to clear orphan Chromes holding the session lock.
      await this.killOrphanSessionBrowsers();
      return;
    }

    this.destroying = true;
    const client = this.client;
    this.client = null;
    try {
      try {
        const browser = client.pupBrowser;
        if (browser) await browser.close();
      } catch (err) {
        console.warn('[whatsapp] browser.close warning:', safeError(err));
      }
      try {
        await client.destroy();
      } catch (err) {
        console.warn('[whatsapp] destroy warning:', safeError(err));
      }
    } finally {
      await this.killOrphanSessionBrowsers();
      this.destroying = false;
      this.qrRaw = null;
      this.qrDataUrl = null;
      this.cachedGroups = [];
      this.cachedGroupsAt = null;
      if (resetStatus) {
        this.status = Status.Disconnected;
      }
    }
  }

  sessionUserDataDir() {
    return path.join(config.authPath, `session-${config.clientId}`);
  }

  async killOrphanSessionBrowsers() {
    const marker = this.sessionUserDataDir();
    if (process.platform !== 'win32') {
      // Best-effort SingletonLock cleanup for non-Windows if browser died uncleanly.
      await this.clearSingletonLocks(marker);
      return;
    }

    try {
      const { stdout } = await execFileAsync(
        'powershell.exe',
        [
          '-NoProfile',
          '-Command',
          [
            "$marker = $env:WA_SESSION_MARKER",
            "Get-CimInstance Win32_Process -Filter \"Name='chrome.exe'\" |",
            '  Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -ge 0 } |',
            '  ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue; $_.ProcessId }',
          ].join(' '),
        ],
        {
          env: { ...process.env, WA_SESSION_MARKER: marker },
          windowsHide: true,
          timeout: 15000,
        }
      );
      const killed = String(stdout || '')
        .split(/\r?\n/)
        .map((s) => s.trim())
        .filter(Boolean);
      if (killed.length) {
        console.warn('[whatsapp] killed orphan session chrome pids:', killed.join(', '));
        await sleep(800);
      }
    } catch (err) {
      console.warn('[whatsapp] orphan chrome cleanup warning:', safeError(err));
    }

    await this.clearSingletonLocks(marker);
  }

  async clearSingletonLocks(sessionDir) {
    for (const name of ['SingletonLock', 'SingletonCookie', 'SingletonSocket']) {
      try {
        await fs.promises.rm(path.join(sessionDir, name), { force: true });
      } catch {
        // ignore
      }
    }
  }
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function normalizeGroupId(groupId) {
  const trimmed = groupId.trim();
  if (trimmed.includes('@')) {
    return trimmed;
  }
  return `${trimmed}@g.us`;
}

/**
 * Runs inside WhatsApp Web page/frame. Must stay a plain function for puppeteer.evaluate.
 */
function extractGroupsInPage() {
  const out = [];
  const seen = Object.create(null);

  const push = (id, name) => {
    if (!id) return;
    const sid = String(id);
    if (!sid.endsWith('@g.us') || seen[sid]) return;
    seen[sid] = true;
    out.push({
      id: sid,
      name: (name && String(name).trim()) || sid,
    });
  };

  const readChatModels = (chatCol) => {
    if (!chatCol) return [];
    if (typeof chatCol.getModelsArray === 'function') return chatCol.getModelsArray() || [];
    if (Array.isArray(chatCol._models)) return chatCol._models;
    if (Array.isArray(chatCol.models)) return chatCol.models;
    if (chatCol._models && typeof chatCol._models === 'object') return Object.values(chatCol._models);
    return [];
  };

  try {
    if (typeof window.require === 'function') {
      const col = window.require('WAWebCollections');
      for (const c of readChatModels(col && col.Chat)) {
        const id = c?.id?._serialized;
        const isGroup = c?.isGroup === true || c?.id?.server === 'g.us' || String(id || '').endsWith('@g.us');
        if (isGroup) push(id, c.name || c.formattedTitle);
      }
    }
  } catch {
    // ignore require failures
  }

  try {
    const models =
      (window.Store && window.Store.Chat && typeof window.Store.Chat.getModelsArray === 'function'
        ? window.Store.Chat.getModelsArray()
        : []) || [];
    for (const c of models) {
      const id = c?.id?._serialized;
      const isGroup = c?.isGroup === true || c?.id?.server === 'g.us' || String(id || '').endsWith('@g.us');
      if (isGroup) push(id, c.name || c.formattedTitle);
    }
  } catch {
    // ignore Store failures
  }

  return out;
}

function safeError(err) {
  if (!err) return 'Unknown error';
  if (typeof err === 'string') return err.slice(0, 500);
  if (err.message) return String(err.message).slice(0, 500);
  return 'Unknown error';
}

const manager = new WhatsAppManager();

module.exports = {
  Status,
  manager,
  authSessionPath: () => path.join(config.authPath),
};
