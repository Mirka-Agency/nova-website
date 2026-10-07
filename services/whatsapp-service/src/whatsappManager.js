'use strict';

const fs = require('fs');
const path = require('path');
const QRCode = require('qrcode');
const { Client, LocalAuth } = require('whatsapp-web.js');
const { config } = require('./config');

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
    });

    client.on('auth_failure', (msg) => {
      console.error('[whatsapp] auth_failure');
      this.status = Status.AuthenticationFailed;
      this.lastError = typeof msg === 'string' ? msg : 'Authentication failed';
      this.qrRaw = null;
      this.qrDataUrl = null;
      this.phoneNumber = null;
    });

    client.on('disconnected', (reason) => {
      console.warn('[whatsapp] disconnected:', String(reason || ''));
      this.status = Status.Disconnected;
      this.phoneNumber = null;
      this.pushName = null;
      this.qrRaw = null;
      this.qrDataUrl = null;
      this.lastError = reason ? String(reason) : null;
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

  async listGroups() {
    this.ensureConnected();

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

    // Primary: WhatsApp Web module loader (window.Store is often missing on new WA builds)
    try {
      const fromRequire = await this.client.pupPage.evaluate(() => {
        const out = [];
        const col = window.require('WAWebCollections');
        const chatCol = col && col.Chat;
        let models = [];
        if (chatCol) {
          if (typeof chatCol.getModelsArray === 'function') {
            models = chatCol.getModelsArray() || [];
          } else if (Array.isArray(chatCol._models)) {
            models = chatCol._models;
          } else if (Array.isArray(chatCol.models)) {
            models = chatCol.models;
          } else if (chatCol._models && typeof chatCol._models === 'object') {
            models = Object.values(chatCol._models);
          }
        }

        for (const c of models) {
          const id = c?.id?._serialized;
          if (!id) continue;
          const isGroup = c?.isGroup === true || c?.id?.server === 'g.us' || String(id).endsWith('@g.us');
          if (!isGroup) continue;
          out.push({
            id: String(id),
            name: c.name || c.formattedTitle || String(id),
          });
        }
        return out;
      });

      for (const g of fromRequire || []) add(g.id, g.name);
      console.info('[whatsapp] WAWebCollections groups:', fromRequire?.length || 0);
    } catch (err) {
      console.warn('[whatsapp] WAWebCollections list failed:', safeError(err));
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

    const groups = [...byId.values()].sort((a, b) =>
      String(a.name).localeCompare(String(b.name), 'fa')
    );
    console.info('[whatsapp] returning groups:', groups.length);
    return groups;
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
    const result = await this.client.sendMessage(chatId, message.trim());
    return {
      ok: true,
      messageId: result?.id?._serialized || result?.id?.id || null,
      groupId: chatId,
      timestamp: new Date().toISOString(),
    };
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
      return;
    }

    this.destroying = true;
    const client = this.client;
    this.client = null;
    try {
      await client.destroy();
    } catch (err) {
      console.warn('[whatsapp] destroy warning:', safeError(err));
    } finally {
      this.destroying = false;
      this.qrRaw = null;
      this.qrDataUrl = null;
      if (resetStatus) {
        this.status = Status.Disconnected;
      }
    }
  }
}

function normalizeGroupId(groupId) {
  const trimmed = groupId.trim();
  if (trimmed.includes('@')) {
    return trimmed;
  }
  return `${trimmed}@g.us`;
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
