import { STATUS } from '../engine/types.js';

/** Rules 15–16: internal / external links */
export function linkRules(ctx) {
  const links = (ctx.content?.links || []).filter(
    (l) => l.href && !l.href.startsWith('#') && !/^mailto:|^tel:|^javascript:/i.test(l.href)
  );
  const internal = links.filter((l) => l.internal);
  const external = links.filter((l) => !l.internal);

  const wordCount = (ctx.words || []).length;
  const expectLinks = wordCount >= 300;

  const internalResult = (() => {
    let status = STATUS.GOOD;
    let score = 100;
    let description = `تعداد لینک داخلی: ${internal.length}.`;
    let fix = '';

    if (internal.length === 0 && expectLinks) {
      status = STATUS.NEEDS_IMPROVEMENT;
      score = 35;
      description += ' لینک داخلی وجود ندارد.';
      fix = 'حداقل ۱–۲ لینک به صفحات مرتبط سایت اضافه کنید.';
    } else if (internal.length === 0) {
      status = STATUS.NEEDS_IMPROVEMENT;
      score = 50;
      description += ' هنوز لینک داخلی نیست.';
      fix = 'با رشد متن، لینک‌های داخلی مرتبط اضافه کنید.';
    } else {
      description += ' خوب است.';
    }

    return {
      id: 'internal-links',
      category: 'links',
      status,
      score,
      title: 'لینک‌های داخلی',
      description,
      fix,
    };
  })();

  const externalResult = (() => {
    let status = STATUS.GOOD;
    let score = 100;
    let description = `تعداد لینک خارجی: ${external.length}.`;
    let fix = '';

    if (external.length === 0 && wordCount >= 600) {
      status = STATUS.NEEDS_IMPROVEMENT;
      score = 60;
      description += ' برای مطالب بلند، ارجاع خارجی معتبر می‌تواند مفید باشد.';
      fix = 'در صورت نیاز به منبع معتبر، یک لینک خارجی با rel مناسب اضافه کنید.';
    } else if (external.length === 0) {
      description += ' اجباری نیست؛ در صورت وجود منبع معتبر اضافه کنید.';
      score = 85;
    } else {
      description += ' وجود دارد.';
    }

    return {
      id: 'external-links',
      category: 'links',
      status,
      score,
      title: 'لینک‌های خارجی',
      description,
      fix,
    };
  })();

  return [internalResult, externalResult];
}
