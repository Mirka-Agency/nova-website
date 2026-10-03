const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const cssSrc = path.resolve(root, 'src/styles/seo-analysis.css');
const cssOut = path.resolve(
  root,
  '../../src/CMS.Web/wwwroot/admin/css/seo-analysis.css'
);

fs.mkdirSync(path.dirname(cssOut), { recursive: true });
fs.copyFileSync(cssSrc, cssOut);
console.log(`Copied CSS → ${cssOut}`);
