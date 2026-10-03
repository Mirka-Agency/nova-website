import { STATUS } from '../engine/types.js';

/** Rule 22: Open Graph title/description presence */
export function openGraphRules(ctx) {
  const ogTitle = String(ctx.ogTitle || '').trim();
  const ogDesc = String(ctx.ogDescription || '').trim();
  const hasTitle = Boolean(ogTitle || ctx.metaTitle || ctx.title);
  const hasDesc = Boolean(ogDesc || ctx.metaDescription);

  if (!ogTitle && !ogDesc) {
    const fallbackOk = hasTitle && hasDesc;
    return {
      id: 'open-graph',
      category: 'social',
      status: fallbackOk ? STATUS.GOOD : STATUS.NEEDS_IMPROVEMENT,
      score: fallbackOk ? 85 : 35,
      title: 'Open Graph',
      description: fallbackOk
        ? 'فیلدهای اختصاصی OG خالی‌اند، اما عنوان/توضیحات متا به‌عنوان جایگزین استفاده می‌شوند.'
        : 'عنوان و توضیحات Open Graph (و جایگزین متا) کامل نیستند.',
      fix: fallbackOk
        ? ''
        : 'عنوان و توضیحات متا یا فیلدهای OG را تکمیل کنید.',
    };
  }

  if (!ogTitle || !ogDesc) {
    return {
      id: 'open-graph',
      category: 'social',
      status: STATUS.NEEDS_IMPROVEMENT,
      score: 60,
      title: 'Open Graph',
      description: `یکی از فیلدهای OG ناقص است (${!ogTitle ? 'عنوان' : 'توضیحات'}).`,
      fix: 'هر دو فیلد عنوان و توضیحات OG را تکمیل کنید.',
    };
  }

  return {
    id: 'open-graph',
    category: 'social',
    status: STATUS.GOOD,
    score: 100,
    title: 'Open Graph',
    description: 'عنوان و توضیحات Open Graph تنظیم شده‌اند.',
    fix: '',
  };
}
