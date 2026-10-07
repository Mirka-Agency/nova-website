'use strict';

const { config } = require('./config');

function extractApiKey(req) {
  const header = req.get('x-api-key');
  if (header && header.trim()) {
    return header.trim();
  }

  const auth = req.get('authorization');
  if (auth && auth.toLowerCase().startsWith('bearer ')) {
    return auth.slice(7).trim();
  }

  return '';
}

function requireApiKey(req, res, next) {
  if (!config.apiKey) {
    return res.status(503).json({
      ok: false,
      error: 'WHATSAPP_API_KEY is not configured on the WhatsApp service.',
    });
  }

  const provided = extractApiKey(req);
  if (!provided || provided !== config.apiKey) {
    return res.status(401).json({
      ok: false,
      error: 'Unauthorized',
    });
  }

  return next();
}

module.exports = { requireApiKey };
