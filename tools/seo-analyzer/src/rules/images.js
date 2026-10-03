import { STATUS } from '../engine/types.js';

/** Rule 17: images missing alt */
export function imageAltRules(ctx) {
  const images = ctx.content?.images || [];
  const missing = images.filter((img) => !String(img.alt || '').trim());

  if (images.length === 0) {
    return {
      id: 'image-alt',
      category: 'media',
      status: STATUS.NEEDS_IMPROVEMENT,
      score: 70,
      title: 'متن جایگزین تصاویر',
      description: 'تصویری در محتوا یافت نشد.',
      fix: 'در صورت امکان تصویر مرتبط با alt توصیفی اضافه کنید.',
    };
  }

  if (missing.length === 0) {
    return {
      id: 'image-alt',
      category: 'media',
      status: STATUS.GOOD,
      score: 100,
      title: 'متن جایگزین تصاویر',
      description: `همه ${images.length} تصویر دارای alt هستند.`,
      fix: '',
    };
  }

  const status = missing.length === images.length ? STATUS.PROBLEM : STATUS.NEEDS_IMPROVEMENT;
  return {
    id: 'image-alt',
    category: 'media',
    status,
    score: status === STATUS.PROBLEM ? 15 : 45,
    title: 'متن جایگزین تصاویر',
    description: `${missing.length} از ${images.length} تصویر بدون alt هستند.`,
    fix: 'برای هر تصویر یک متن جایگزین کوتاه و توصیفی بنویسید.',
  };
}
