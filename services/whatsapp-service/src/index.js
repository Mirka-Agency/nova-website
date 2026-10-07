'use strict';

const express = require('express');
const { config } = require('./config');
const { createRouter } = require('./routes');
const { manager } = require('./whatsappManager');

if (!config.apiKey) {
  console.warn('[whatsapp] WHATSAPP_API_KEY is empty — protected routes will return 503.');
}

const app = express();
app.disable('x-powered-by');
app.use(express.json({ limit: '64kb' }));

app.use((req, res, next) => {
  // Internal service only — do not expose CORS to browsers.
  res.setHeader('X-Content-Type-Options', 'nosniff');
  next();
});

app.use(createRouter());

app.use((err, _req, res, _next) => {
  const status = Number(err.statusCode) || 500;
  const message = err.message ? String(err.message).slice(0, 500) : 'Internal error';
  // Never log QR / session secrets.
  console.error('[whatsapp] request error:', message, err?.stack ? String(err.stack).slice(0, 400) : '');
  res.status(status).json({
    ok: false,
    error: message,
    status: manager.getSnapshot().status,
  });
});

const server = app.listen(config.port, '0.0.0.0', async () => {
  console.info(`[whatsapp] listening on :${config.port}`);
  try {
    await manager.start();
  } catch (err) {
    console.error('[whatsapp] initial start failed:', err?.message || err);
  }
});

async function shutdown(signal) {
  console.info(`[whatsapp] shutting down (${signal})`);
  try {
    await manager.destroyClient(true);
  } catch {
    // ignore
  }
  server.close(() => process.exit(0));
  setTimeout(() => process.exit(0), 8000).unref();
}

process.on('SIGINT', () => shutdown('SIGINT'));
process.on('SIGTERM', () => shutdown('SIGTERM'));
