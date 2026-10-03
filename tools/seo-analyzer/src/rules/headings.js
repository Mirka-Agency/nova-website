import { STATUS } from '../engine/types.js';

/** Rule 14: heading structure */
export function headingStructureRules(ctx) {
  const headings = ctx.content?.headings || [];
  const title = (ctx.title || '').trim();
  const h1Count = headings.filter((h) => h.level === 1).length;
  const effectiveH1 = h1Count + (title ? 1 : 0);

  let status = STATUS.GOOD;
  let score = 100;
  let description = '';
  let fix = '';

  // Detect skips e.g. H2 → H4
  let hasSkip = false;
  let prev = null;
  for (const h of headings) {
    if (prev != null && h.level > prev + 1) {
      hasSkip = true;
      break;
    }
    prev = h.level;
  }

  if (headings.length === 0 && !title) {
    status = STATUS.PROBLEM;
    score = 0;
    description = 'هیچ عنوانی (H1–H6) در محتوا یافت نشد.';
    fix = 'عنوان مطلب و زیرعنوان‌های H2/H3 را اضافه کنید.';
  } else if (h1Count > 1) {
    status = STATUS.NEEDS_IMPROVEMENT;
    score = 45;
    description = `در محتوا ${h1Count} تگ H1 وجود دارد؛ ترجیحاً یک H1 کافی است.`;
    fix = 'فقط یک H1 نگه دارید و بقیه را به H2/H3 تبدیل کنید. عنوان صفحه می‌تواند نقش H1 را داشته باشد.';
  } else if (hasSkip) {
    status = STATUS.NEEDS_IMPROVEMENT;
    score = 50;
    description = 'سلسله‌مراتب عناوین پرش دارد (مثلاً از H2 مستقیم به H4).';
    fix = 'ترتیب منطقی عناوین را رعایت کنید: H2 سپس H3 و الی آخر.';
  } else if (headings.length === 0 && title) {
    status = STATUS.NEEDS_IMPROVEMENT;
    score = 55;
    description = 'عنوان مطلب وجود دارد اما در بدنه زیرعنوانی نیست.';
    fix = 'با H2/H3 محتوا را بخش‌بندی کنید.';
  } else if (effectiveH1 === 0) {
    status = STATUS.NEEDS_IMPROVEMENT;
    score = 40;
    description = 'H1 مشخصی دیده نمی‌شود.';
    fix = 'یک H1 واضح (یا عنوان صفحه قوی) تنظیم کنید.';
  } else {
    description = `ساختار عناوین مناسب است (${headings.length} عنوان در محتوا).`;
  }

  return {
    id: 'heading-structure',
    category: 'structure',
    status,
    score,
    title: 'ساختار عناوین',
    description,
    fix,
  };
}
