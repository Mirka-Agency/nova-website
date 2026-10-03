import { STATUS } from '../engine/types.js';

/** Rule 21: robots index/nofollow awareness (informational when index off) */
export function robotsRules(ctx) {
  const index = ctx.robotsIndex;
  const follow = ctx.robotsFollow;

  // null = field not present / unknown → treat as default index+follow
  const indexOff = index === false;
  const followOff = follow === false;

  if (indexOff) {
    return {
      id: 'robots-index',
      category: 'robots',
      status: STATUS.NEEDS_IMPROVEMENT,
      score: 40,
      title: 'نمایه شدن در موتور جستجو',
      description:
        'گزینه نمایه‌سازی (index) خاموش است؛ این صفحه احتمالاً در نتایج جستجو نشان داده نمی‌شود. این یک هشدار اطلاعاتی است.',
      fix: 'اگر قصد دارید صفحه در گوگل دیده شود، گزینه «ایندکس» را فعال کنید.',
    };
  }

  if (followOff) {
    return {
      id: 'robots-index',
      category: 'robots',
      status: STATUS.NEEDS_IMPROVEMENT,
      score: 70,
      title: 'دنبال کردن لینک‌ها (follow)',
      description:
        'گزینه follow خاموش است (nofollow). لینک‌های این صفحه ممکن است اعتبار انتقال ندهند.',
      fix: 'فقط اگر عمداً nofollow می‌خواهید این حالت را نگه دارید؛ در غیر این صورت follow را فعال کنید.',
    };
  }

  return {
    id: 'robots-index',
    category: 'robots',
    status: STATUS.GOOD,
    score: 100,
    title: 'تنظیمات ربات‌ها',
    description: 'صفحه برای نمایه‌سازی و دنبال کردن لینک‌ها باز به نظر می‌رسد (یا فیلدها پیش‌فرض‌اند).',
    fix: '',
  };
}
