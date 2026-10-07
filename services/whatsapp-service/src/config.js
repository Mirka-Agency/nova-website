'use strict';

function required(name, fallback = '') {
  const value = process.env[name];
  if (value === undefined || value === null || String(value).trim() === '') {
    return fallback;
  }
  return String(value).trim();
}

const config = {
  port: Number(required('PORT', '3100')),
  apiKey: required('WHATSAPP_API_KEY', ''),
  authPath: required('WHATSAPP_AUTH_PATH', '/data/wwebjs_auth'),
  clientId: required('WHATSAPP_CLIENT_ID', 'cms-whatsapp'),
  puppeteerExecutablePath: required(
    'PUPPETEER_EXECUTABLE_PATH',
    process.platform === 'win32' ? '' : '/usr/bin/chromium'
  ),
};

module.exports = { config };
