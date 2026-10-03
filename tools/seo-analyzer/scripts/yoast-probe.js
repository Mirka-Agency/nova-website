/**
 * Quick probe: hybrid analyze() with Yoast FA engine.
 * Run after build: node scripts/yoast-probe.js
 */
const fs = require('fs');
const vm = require('vm');
const path = require('path');

const code = fs.readFileSync(
  path.resolve(__dirname, '../../../src/CMS.Web/wwwroot/admin/js/seo-analysis.bundle.js'),
  'utf8'
);

const sandbox = {
  setTimeout,
  clearTimeout,
  setInterval,
  clearInterval,
  console,
  URL,
  Buffer,
  document: {
    readyState: 'complete',
    querySelectorAll: () => [],
    querySelector: () => null,
    addEventListener: () => {},
  },
};
sandbox.window = sandbox;
sandbox.self = sandbox;
sandbox.globalThis = sandbox;
sandbox.global = sandbox;

vm.runInNewContext(code + '\nthis.__out = MirkaSeoAnalysis;', sandbox);
const M = sandbox.__out;

const filler =
  'در این بخش نکات عملی، مثال‌های واقعی و اشتباهات رایج را مرور می‌کنیم تا خواننده تصمیم بهتری بگیرد. ';
const paras = Array.from({ length: 12 }, (_, i) => {
  const kw = i % 3 === 0 ? 'بازاریابی محتوا ابزار قدرتمندی است. ' : '';
  return `<p>${kw}${filler}${filler}${filler}</p>`;
}).join('');

const r = M.analyze({
  title: 'بازاریابی محتوا چیست',
  slug: 'content-marketing',
  metaTitle: 'بازاریابی محتوا چیست و چرا مهم است',
  metaDescription:
    'در این مطلب با مفهوم بازاریابی محتوا و روش‌های اجرای آن برای رشد کسب‌وکار آشنا می‌شوید.',
  focusKeyword: 'بازاریابی محتوا',
  bodyHtml: `<h2>مقدمه بازاریابی محتوا</h2><p>بازاریابی محتوا به جذب مخاطب کمک می‌کند. بنابراین باید استراتژی داشته باشید. همچنین کیفیت محتوا مهم است.</p>${paras}<h2>جمع‌بندی</h2><p>بازاریابی محتوا را در انتهای مطلب هم مرور می‌کنیم.</p><img src="/a.jpg" alt="بازاریابی محتوا" />`,
  robotsIndex: true,
  robotsFollow: true,
  ogTitle: 'og',
  ogDescription: 'og desc',
});

const byId = Object.fromEntries(r.results.map((x) => [x.id, x]));
console.log('overall', r.overall);
console.log('engines', r.engines);
console.log(
  'sources',
  r.results.reduce((acc, x) => {
    acc[x.source || '?'] = (acc[x.source || '?'] || 0) + 1;
    return acc;
  }, {})
);
console.log('---');
for (const id of [
  'keyword-in-title',
  'keyword-in-meta',
  'keyword-in-first-paragraph',
  'keyword-density',
  'keyword-distribution',
  'transition-words',
  'passive-voice',
  'sentence-length',
  'keyword-in-slug',
  'heading-structure',
  'image-alt',
  'robots-index',
  'canonical-url',
]) {
  const x = byId[id];
  console.log(
    id,
    x ? `${x.source} ${x.status} ${x.score}` : 'MISSING',
    x ? x.title : ''
  );
}
