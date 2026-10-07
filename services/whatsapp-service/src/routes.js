'use strict';

const express = require('express');
const { requireApiKey } = require('./auth');
const { manager } = require('./whatsappManager');

function createRouter() {
  const router = express.Router();

  router.get('/health', (_req, res) => {
    const snapshot = manager.getSnapshot();
    res.json({
      ok: true,
      service: 'whatsapp',
      status: snapshot.status,
      connected: snapshot.connected,
    });
  });

  router.use(requireApiKey);

  router.get('/status', (_req, res) => {
    res.json({ ok: true, ...manager.getSnapshot() });
  });

  router.get('/qr', async (_req, res, next) => {
    try {
      const payload = await manager.getQrPayload();
      res.json({ ok: true, ...payload });
    } catch (err) {
      next(err);
    }
  });

  router.post('/reconnect', async (_req, res, next) => {
    try {
      const snapshot = await manager.reconnect();
      res.json({ ok: true, ...snapshot });
    } catch (err) {
      next(err);
    }
  });

  router.post('/logout', async (_req, res, next) => {
    try {
      const snapshot = await manager.logout();
      res.json({ ok: true, ...snapshot });
    } catch (err) {
      next(err);
    }
  });

  router.post('/session/clear', async (_req, res, next) => {
    try {
      const snapshot = await manager.clearSession();
      res.json({ ok: true, ...snapshot });
    } catch (err) {
      next(err);
    }
  });

  router.post('/qr/regenerate', async (_req, res, next) => {
    try {
      const snapshot = await manager.regenerateQr();
      res.json({ ok: true, ...snapshot });
    } catch (err) {
      next(err);
    }
  });

  router.get('/groups', async (_req, res, next) => {
    try {
      const groups = await manager.listGroups();
      res.json({ ok: true, groups });
    } catch (err) {
      next(err);
    }
  });

  router.post('/send', async (req, res, next) => {
    try {
      const groupId = req.body?.groupId;
      const message = req.body?.message;
      const result = await manager.sendMessage(groupId, message);
      res.json({ ok: true, ...result });
    } catch (err) {
      next(err);
    }
  });

  return router;
}

module.exports = { createRouter };
