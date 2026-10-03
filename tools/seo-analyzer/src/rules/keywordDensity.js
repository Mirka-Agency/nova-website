import { STATUS } from '../engine/types.js';
import { countKeyword, normalizeForMatch, escapeRegExp } from '../persian/normalize.js';

/** Rules 7–8: keyword density & distribution */
export function keywordDensityRules(ctx) {
  const kw = String(ctx.focusKeyword || ctx.normalizedKeyword || '').trim();
  const words = ctx.words || [];
  const plain = ctx.plainText || '';
  const results = [];

  if (!kw) {
    return [
      {
        id: 'keyword-density',
        category: 'keyword',
        status: STATUS.PROBLEM,
        score: 0,
        title: 'چگالی کلمه کلیدی',
        description: 'بدون کلمه کلیدی قابل محاسبه نیست.',
        fix: 'کلمه کلیدی کانونی را وارد کنید.',
      },
      {
        id: 'keyword-distribution',
        category: 'keyword',
        status: STATUS.PROBLEM,
        score: 0,
        title: 'توزیع کلمه کلیدی',
        description: 'بدون کلمه کلیدی قابل محاسبه نیست.',
        fix: 'کلمه کلیدی کانونی را وارد کنید.',
      },
    ];
  }

  const occurrences = countKeyword(plain, kw);
  const totalWords = Math.max(words.length, 1);
  // Yoast-style: keyphrase count / word count (do NOT multiply by token length again)
  const density = (occurrences * 100) / totalWords;

  let densityStatus = STATUS.GOOD;
  let densityScore = 100;
  let densityDesc = `چگالی تقریبی: ${density.toFixed(2)}٪ (${occurrences} بار در حدود ${totalWords} واژه).`;
  let densityFix = '';

  if (words.length < 50) {
    densityStatus = STATUS.NEEDS_IMPROVEMENT;
    densityScore = 50;
    densityDesc = `متن خیلی کوتاه است (${totalWords} واژه)؛ چگالی هنوز قابل اتکا نیست. تکرار: ${occurrences}.`;
    densityFix = 'ابتدا متن را کامل‌تر کنید، سپس چگالی را دوباره بررسی کنید.';
  } else if (occurrences === 0) {
    densityStatus = STATUS.PROBLEM;
    densityScore = 0;
    densityDesc = 'کلمه کلیدی در متن مطلب ظاهر نشده است.';
    densityFix = 'کلمه کلیدی را چند بار به‌صورت طبیعی در متن به‌کار ببرید.';
  } else if (density > 3.5) {
    densityStatus = STATUS.PROBLEM;
    densityScore = 15;
    densityDesc += ' چگالی بیش از حد (Keyword Stuffing) است.';
    densityFix = 'تکرار کلمه کلیدی را کم کنید؛ هدف حدود ۰٫۵ تا ۲٫۵٪ است.';
  } else if (density < 0.5) {
    densityStatus = STATUS.NEEDS_IMPROVEMENT;
    densityScore = 45;
    densityDesc += ' چگالی کمی پایین است.';
    densityFix = 'چند بار دیگر کلمه کلیدی را در بخش‌های مختلف متن بیاورید.';
  } else if (density > 2.5) {
    densityStatus = STATUS.NEEDS_IMPROVEMENT;
    densityScore = 55;
    densityDesc += ' کمی بالاتر از محدوده ایده‌آل است.';
    densityFix = 'تکرار را کمی کاهش دهید تا به بازه ۰٫۵–۲٫۵٪ نزدیک شود.';
  } else {
    densityDesc += ' در محدوده مناسب است.';
  }

  results.push({
    id: 'keyword-density',
    category: 'keyword',
    status: densityStatus,
    score: densityScore,
    title: 'چگالی کلمه کلیدی',
    description: densityDesc,
    fix: densityFix,
  });

  const h = normalizeForMatch(plain);
  const k = normalizeForMatch(kw);
  let distStatus = STATUS.GOOD;
  let distScore = 100;
  let distDesc = 'توزیع کلمه کلیدی در متن متعادل به نظر می‌رسد.';
  let distFix = '';

  if (occurrences === 0) {
    distStatus = STATUS.PROBLEM;
    distScore = 0;
    distDesc = 'توزیعی برای ارزیابی وجود ندارد.';
    distFix = 'ابتدا کلمه کلیدی را در متن استفاده کنید.';
  } else if (h.length > 0 && k) {
    const tokens = k.split(/\s+/).filter(Boolean);
    const body = tokens.map(escapeRegExp).join('[\\s\\u200C]+');
    const re = new RegExp(body, 'g');
    const positions = [];
    let match;
    while ((match = re.exec(h))) {
      positions.push(match.index / h.length);
      if (match.index === re.lastIndex) re.lastIndex += 1;
    }
    const early = positions.filter((p) => p < 0.25).length;
    if (occurrences >= 3 && early === occurrences) {
      distStatus = STATUS.NEEDS_IMPROVEMENT;
      distScore = 40;
      distDesc = 'تقریباً همه تکرارهای کلمه کلیدی در ابتدای متن متمرکز شده‌اند.';
      distFix = 'کلمه کلیدی را در میانه و انتهای مطلب هم به‌کار ببرید.';
    } else if (occurrences >= 2) {
      const midLate = positions.filter((p) => p >= 0.33).length;
      if (midLate === 0) {
        distStatus = STATUS.NEEDS_IMPROVEMENT;
        distScore = 50;
        distDesc = 'توزیع کلمه کلیدی بیشتر در ابتدای متن است.';
        distFix = 'در بخش‌های بعدی مطلب نیز از کلمه کلیدی استفاده کنید.';
      }
    }
  }

  results.push({
    id: 'keyword-distribution',
    category: 'keyword',
    status: distStatus,
    score: distScore,
    title: 'توزیع کلمه کلیدی',
    description: distDesc,
    fix: distFix,
  });

  return results;
}
