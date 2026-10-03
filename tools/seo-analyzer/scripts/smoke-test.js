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

function assert(cond, msg) {
  if (!cond) throw new Error(msg);
}

// Parallel engines: shared rules fused, uniques kept
{
  const r = M.analyze({
    title: 'خرید گوشی‌هوشمند',
    slug: 'kharid-gooshi',
    metaTitle: 'گوشی هوشمند: راهنمای خرید',
    metaDescription: 'راهنمای کامل خرید گوشی هوشمند برای انتخاب بهتر مدل مناسب شما در بازار ایران.',
    focusKeyword: 'گوشی هوشمند',
    bodyHtml:
      '<p>برای خرید گوشی‌هوشمند باید به باتری توجه کنید. گوشی هوشمند خوب عمر باتری بالایی دارد.</p>' +
      '<h2>قیمت گوشی هوشمند</h2><p>قیمت گوشی هوشمند به برند و سخت‌افزار بستگی دارد و باید بودجه را مشخص کنید.</p>',
    robotsIndex: true,
    robotsFollow: true,
    ogTitle: 'og',
    ogDescription: 'og desc',
  });
  const byId = Object.fromEntries(r.results.map((x) => [x.id, x]));

  assert(r.engines && r.engines.mode === 'parallel', 'mode should be parallel');
  assert(r.engines.yoast === true, 'yoast engine should run');
  assert(r.engines.mirka === true, 'mirka engine should run');

  assert(byId['keyword-in-title'], 'title rule missing');
  assert(byId['keyword-in-title'].source === 'both', 'shared title should be both');
  assert(byId['keyword-in-title'].engines?.mirka && byId['keyword-in-title'].engines?.yoast, 'title needs both engine payloads');
  assert(
    byId['keyword-in-title'].status === 'good' ||
      byId['keyword-in-title'].status === 'needs_improvement',
    `title match failed: ${byId['keyword-in-title'].status}`
  );

  assert(byId['keyword-in-first-paragraph'].source === 'both', 'intro should be both');
  assert(byId['keyword-in-first-paragraph'].status === 'good', 'first para ZWNJ match failed');
  assert(byId['keyword-in-headings'].source === 'both', 'headings should be both');
  assert(byId['keyword-in-headings'].status === 'good', 'heading match failed');

  assert(byId['keyword-in-slug'].status === 'good', 'latin slug should not fail FA keyword');
  assert(byId['keyword-in-slug'].source === 'mirka', 'slug rule should stay Mirka-only');
  assert(byId['robots-index'].status === 'good', 'robots should be good when index true');
  assert(byId['robots-index'].source === 'mirka', 'robots should stay Mirka-only');

  assert(byId['transition-words'], 'transition-words from Yoast missing');
  assert(byId['transition-words'].source === 'yoast', 'transition-words should be Yoast-only');

  assert(byId['passive-voice'].source === 'both', 'passive should be fused');
  assert(byId['sentence-length'].source === 'both', 'sentence-length should be fused');
  assert(byId['readability-score'].source === 'both', 'readability-score should be fused');

  // No duplicate ids
  const ids = r.results.map((x) => x.id);
  assert(ids.length === new Set(ids).size, 'duplicate rule ids in panel results');

  console.log('parallel/zwnj/slug/robots ok', r.overall);
}

// Density fused, not problem for natural text
{
  const filler =
    'در این بخش نکات عملی، مثال‌های واقعی و اشتباهات رایج را مرور می‌کنیم تا خواننده تصمیم بهتری بگیرد. ';
  const paras = Array.from({ length: 10 }, (_, i) => {
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
    bodyHtml: `<h2>مقدمه</h2><p>بازاریابی محتوا به جذب مخاطب کمک می‌کند.</p>${paras}`,
    robotsIndex: true,
    robotsFollow: true,
  });
  const dens = r.results.find((x) => x.id === 'keyword-density');
  assert(dens, 'density rule missing');
  assert(dens.source === 'both', 'density should be fused both');
  assert(dens.status !== 'problem', `density wrongly problem: ${dens.description}`);
  console.log('density ok', dens.status, dens.source);
}

// Mirka technical + no Yoast keyphrase without focus keyword
{
  const r = M.analyze({
    title: 'تست',
    slug: 'test',
    focusKeyword: '',
    bodyHtml: '<p>متن کوتاه</p>',
    robotsIndex: false,
  });
  const byId = Object.fromEntries(r.results.map((x) => [x.id, x]));
  assert(byId['focus-keyword-present'].status === 'problem', 'missing keyword should problem');
  assert(byId['focus-keyword-present'].source === 'mirka', 'focus keyword is Mirka-only');
  assert(byId['heading-structure'], 'heading-structure should remain');
  assert(byId['image-alt'], 'image-alt should remain');
  assert(byId['canonical-url'], 'canonical should remain');
  assert(byId['seo-title-length'], 'seo-title-length should remain');
  assert(
    !r.results.some(
      (x) =>
        (x.source === 'yoast' || x.source === 'both') && x.category === 'keyword'
    ),
    'Yoast keyphrase rules should not run without focus keyword'
  );
  console.log('mirka technical fallback ok');
}

// Highlight-in-text payloads for readability + keyword issues
{
  const r = M.analyze({
    title: 'تست خوانایی',
    slug: 'readability-test',
    metaTitle: 'تست خوانایی سئو',
    metaDescription: 'توضیحات متا بدون کلمه کلیدی هدف برای بررسی هایلایت.',
    focusKeyword: 'کلمه کلیدی کانونی',
    bodyHtml:
      '<p>این یک جمله بسیار بسیار طولانی است که عمداً نوشته شده تا قانون طول جمله را فعال کند و شامل واژه‌های زیادی باشد تا میانگین طول جمله از حد مجاز عبور کند و هشدار خوانایی نمایش داده شود.</p>' +
      '<p>گزارش نهایی نوشته شده است و نتایج اعلام شده است تا الگوی مجهول تشخیص داده شود.</p>',
    robotsIndex: true,
    robotsFollow: true,
  });
  const byId = Object.fromEntries(r.results.map((x) => [x.id, x]));

  assert(byId['passive-voice'], 'passive-voice missing');
  if (byId['passive-voice'].status !== 'good') {
    assert(
      byId['passive-voice'].highlightable === true,
      'passive should be highlightable when not good'
    );
    assert(
      Array.isArray(byId['passive-voice'].highlights) &&
        byId['passive-voice'].highlights.length > 0,
      'passive highlights missing'
    );
    assert(
      byId['passive-voice'].highlights.every((h) => h.field === 'body' && h.kind === 'passive'),
      'passive highlight kind/field wrong'
    );
  }

  assert(byId['sentence-length'], 'sentence-length missing');
  if (byId['sentence-length'].status !== 'good') {
    assert(byId['sentence-length'].highlightable, 'long sentences should highlight');
    assert(
      byId['sentence-length'].highlights.some((h) => h.kind === 'long-sentence'),
      'long-sentence kind missing'
    );
  }

  assert(byId['keyword-in-meta'], 'keyword-in-meta missing');
  assert(byId['keyword-in-meta'].highlightable, 'meta keyword should highlight field');
  assert(
    byId['keyword-in-meta'].highlights.some((h) => h.field === 'metaDescription'),
    'meta highlight should target metaDescription'
  );

  assert(byId['focus-keyword-present'].highlightable !== true, 'present keyword needs no highlight');

  console.log('highlight payloads ok');
}

console.log('all smoke checks passed');
