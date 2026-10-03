import { STATUS } from '../engine/types.js';
import { normalizePersian } from '../persian/normalize.js';

/** Rules 18–20: SEO title length, meta description length, canonical */
export function metaLengthRules(ctx) {
  const seoTitle = normalizePersian(ctx.metaTitle || ctx.title || '');
  const metaDesc = normalizePersian(ctx.metaDescription || '');
  const canonical = String(ctx.canonical || '').trim();
  const results = [];

  // 18. SEO title length — FA: good ~35–65
  const titleLen = seoTitle.length;
  let tStatus = STATUS.GOOD;
  let tScore = 100;
  let tDesc = `طول عنوان SEO: ${titleLen} نویسه.`;
  let tFix = '';

  if (!seoTitle) {
    tStatus = STATUS.PROBLEM;
    tScore = 0;
    tDesc = 'عنوان SEO خالی است.';
    tFix = 'عنوانی بین حدود ۳۵ تا ۶۵ نویسه بنویسید.';
  } else if (titleLen < 35) {
    tStatus = STATUS.NEEDS_IMPROVEMENT;
    tScore = 50;
    tDesc += ' کمی کوتاه است.';
    tFix = 'عنوان را کمی کامل‌تر کنید (حدود ۳۵–۶۵ نویسه).';
  } else if (titleLen > 65) {
    tStatus = STATUS.NEEDS_IMPROVEMENT;
    tScore = 45;
    tDesc += ' ممکن است در نتایج جستجو بریده شود.';
    tFix = 'عنوان را کوتاه‌تر کنید تا حدود ۶۵ نویسه.';
  } else {
    tDesc += ' در محدوده مناسب است.';
  }

  results.push({
    id: 'seo-title-length',
    category: 'meta',
    status: tStatus,
    score: tScore,
    title: 'طول عنوان SEO',
    description: tDesc,
    fix: tFix,
  });

  // 19. Meta description length — good ~70–160
  const dLen = metaDesc.length;
  let dStatus = STATUS.GOOD;
  let dScore = 100;
  let dDesc = `طول توضیحات متا: ${dLen} نویسه.`;
  let dFix = '';

  if (!metaDesc) {
    dStatus = STATUS.PROBLEM;
    dScore = 0;
    dDesc = 'توضیحات متا خالی است.';
    dFix = 'توضیحی حدود ۷۰ تا ۱۶۰ نویسه بنویسید که شامل کلمه کلیدی باشد.';
  } else if (dLen < 70) {
    dStatus = STATUS.NEEDS_IMPROVEMENT;
    dScore = 50;
    dDesc += ' کوتاه است.';
    dFix = 'توضیحات را کامل‌تر کنید (حدود ۷۰–۱۶۰ نویسه).';
  } else if (dLen > 160) {
    dStatus = STATUS.NEEDS_IMPROVEMENT;
    dScore = 45;
    dDesc += ' ممکن است در SERP بریده شود.';
    dFix = 'توضیحات را کوتاه‌تر کنید.';
  } else {
    dDesc += ' در محدوده مناسب است.';
  }

  results.push({
    id: 'meta-description-length',
    category: 'meta',
    status: dStatus,
    score: dScore,
    title: 'طول توضیحات متا',
    description: dDesc,
    fix: dFix,
  });

  // 20. Canonical URL format
  if (!canonical) {
    results.push({
      id: 'canonical-url',
      category: 'meta',
      status: STATUS.NEEDS_IMPROVEMENT,
      score: 70,
      title: 'نشانی Canonical',
      description: 'Canonical تنظیم نشده است (در بسیاری موارد اختیاری است).',
      fix: 'در صورت وجود نسخه‌های چندگانه از URL، یک آدرس canonical مطلق تنظیم کنید.',
    });
  } else {
    let ok = false;
    let reason = '';
    try {
      const u = new URL(canonical);
      ok = u.protocol === 'http:' || u.protocol === 'https:';
      if (!ok) reason = 'پروتکل باید http یا https باشد.';
    } catch {
      ok = false;
      reason = 'فرمت URL معتبر نیست.';
    }

    results.push({
      id: 'canonical-url',
      category: 'meta',
      status: ok ? STATUS.GOOD : STATUS.PROBLEM,
      score: ok ? 100 : 20,
      title: 'نشانی Canonical',
      description: ok
        ? `Canonical معتبر به نظر می‌رسد: ${canonical}`
        : `Canonical نامعتبر است. ${reason}`,
      fix: ok ? '' : 'یک URL مطلق معتبر وارد کنید، مثلاً https://example.com/path',
    });
  }

  return results;
}
