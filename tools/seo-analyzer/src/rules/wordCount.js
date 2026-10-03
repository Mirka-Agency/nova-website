import { STATUS } from '../engine/types.js';

/** Rule 9: word count for Persian tokens */
export function wordCountRules(ctx) {
  const count = (ctx.words || []).length;

  let status = STATUS.GOOD;
  let score = 100;
  let description = `تعداد واژه‌ها: ${count}.`;
  let fix = '';

  if (count < 300) {
    status = STATUS.PROBLEM;
    score = Math.round((count / 300) * 40);
    description += ' متن کوتاه‌تر از حد توصیه‌شده است.';
    fix = 'مطلب را به حداقل ۳۰۰ واژه برسانید؛ برای سئوی بهتر ۶۰۰ واژه یا بیشتر هدف بگیرید.';
  } else if (count < 600) {
    status = STATUS.NEEDS_IMPROVEMENT;
    score = 55 + Math.round(((count - 300) / 300) * 30);
    description += ' طول متوسط است؛ می‌توانید عمیق‌تر بنویسید.';
    fix = 'با افزودن جزئیات، مثال و زیرعنوان‌ها طول متن را به ۶۰۰ واژه یا بیشتر برسانید.';
  } else {
    description += ' طول متن مناسب است.';
  }

  return {
    id: 'word-count',
    category: 'content',
    status,
    score,
    title: 'تعداد واژه‌ها',
    description,
    fix,
  };
}
