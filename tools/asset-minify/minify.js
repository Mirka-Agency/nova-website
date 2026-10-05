const esbuild = require('esbuild');
const fs = require('fs');
const path = require('path');

const wwwroot = path.resolve(
  process.env.ASSET_WWWROOT ||
    process.argv[2] ||
    path.join(__dirname, '../../src/CMS.Web/wwwroot')
);

/** Path segments / name patterns that must not be re-minified. */
const SKIP_DIR_NAMES = new Set(['vendor', 'lib', 'node_modules']);

function shouldSkip(filePath) {
  const relative = path.relative(wwwroot, filePath);
  const parts = relative.split(path.sep);
  if (parts.some((p) => SKIP_DIR_NAMES.has(p.toLowerCase()))) {
    return true;
  }

  const base = path.basename(filePath).toLowerCase();
  if (base.endsWith('.min.js') || base.endsWith('.min.css')) {
    return true;
  }
  // Already produced by a dedicated bundler (e.g. tools/seo-analyzer).
  if (base.endsWith('.bundle.js') || base.endsWith('.bundle.css')) {
    return true;
  }
  // UMD / prebuilt library dumps outside vendor (defensive).
  if (base.endsWith('.umd.js') || base.endsWith('.umd.css')) {
    return true;
  }

  return false;
}

function collectAssets(dir, out = []) {
  if (!fs.existsSync(dir)) {
    throw new Error(`wwwroot not found: ${dir}`);
  }

  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (SKIP_DIR_NAMES.has(entry.name.toLowerCase())) {
        continue;
      }
      collectAssets(full, out);
      continue;
    }
    if (!entry.isFile()) continue;

    const ext = path.extname(entry.name).toLowerCase();
    if (ext !== '.css' && ext !== '.js') continue;
    if (shouldSkip(full)) continue;
    out.push(full);
  }

  return out;
}

async function minifyFile(filePath) {
  const ext = path.extname(filePath).toLowerCase();
  const loader = ext === '.css' ? 'css' : 'js';
  const source = fs.readFileSync(filePath, 'utf8');
  const result = await esbuild.transform(source, {
    loader,
    minify: true,
    target: ['es2018'],
    // Keep legal comments out; fail hard on syntax errors.
    logLevel: 'silent',
  });
  fs.writeFileSync(filePath, result.code, 'utf8');
  return { before: source.length, after: result.code.length };
}

async function main() {
  const files = collectAssets(wwwroot);
  if (files.length === 0) {
    console.error('No CSS/JS assets found to minify under', wwwroot);
    process.exit(1);
  }

  console.log(`Minifying ${files.length} asset(s) in place under wwwroot…`);

  let totalBefore = 0;
  let totalAfter = 0;

  for (const file of files) {
    const rel = path.relative(wwwroot, file);
    try {
      const { before, after } = await minifyFile(file);
      totalBefore += before;
      totalAfter += after;
      const pct = before === 0 ? 0 : Math.round((1 - after / before) * 100);
      console.log(`  OK  ${rel}  (${before} → ${after} bytes, -${pct}%)`);
    } catch (err) {
      console.error(`  FAIL ${rel}`);
      console.error(err && err.message ? err.message : err);
      process.exit(1);
    }
  }

  const saved = totalBefore - totalAfter;
  const pct =
    totalBefore === 0 ? 0 : Math.round((saved / totalBefore) * 100);
  console.log(
    `Done. ${files.length} files, ${totalBefore} → ${totalAfter} bytes (−${saved}, −${pct}%).`
  );
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
