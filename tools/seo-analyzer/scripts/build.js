const esbuild = require('esbuild');
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const outfile = path.resolve(
  root,
  '../../src/CMS.Web/wwwroot/admin/js/seo-analysis.bundle.js'
);
const cssSrc = path.resolve(root, 'src/styles/seo-analysis.css');
const cssOut = path.resolve(
  root,
  '../../src/CMS.Web/wwwroot/admin/css/seo-analysis.css'
);

const shimDir = path.resolve(root, 'scripts/shims');
fs.mkdirSync(shimDir, { recursive: true });

// Ensure Buffer global for htmlparser2 / safe-buffer when bundled for browser
const bufferShim = path.join(shimDir, 'buffer-global.js');
fs.writeFileSync(
  bufferShim,
  `import { Buffer } from 'buffer';\nexport { Buffer };\n`
);

async function main() {
  fs.mkdirSync(path.dirname(outfile), { recursive: true });
  fs.mkdirSync(path.dirname(cssOut), { recursive: true });

  await esbuild.build({
    entryPoints: [path.resolve(root, 'src/index.js')],
    bundle: true,
    minify: true,
    format: 'iife',
    globalName: 'MirkaSeoAnalysis',
    outfile,
    target: ['es2018'],
    platform: 'browser',
    logLevel: 'info',
    define: {
      'process.env.NODE_ENV': '"production"',
      global: 'window',
    },
    mainFields: ['browser', 'module', 'main'],
    inject: [bufferShim],
    alias: {
      // Node builtins used by yoastseo / htmlparser2
      buffer: require.resolve('buffer/'),
      events: require.resolve('events/'),
      url: require.resolve('url/'),
    },
  });

  fs.copyFileSync(cssSrc, cssOut);

  const jsStat = fs.statSync(outfile);
  const cssStat = fs.statSync(cssOut);
  console.log(`JS bundle: ${outfile} (${jsStat.size} bytes)`);
  console.log(`CSS:       ${cssOut} (${cssStat.size} bytes)`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
