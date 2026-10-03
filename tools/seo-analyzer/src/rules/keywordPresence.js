import { STATUS } from '../engine/types.js';
import { includesKeyword, hasArabicScript, isLatinSlug } from '../persian/normalize.js';

/** Rules 1–6: focus keyword presence in key locations */
export function keywordPresenceRules(ctx) {
  const focus = String(ctx.focusKeyword || '').trim();
  const kw = focus;
  const hasKw = Boolean(kw);
  const results = [];

  results.push(
    hasKw
      ? {
          id: 'focus-keyword-present',
          category: 'keyword',
          status: STATUS.GOOD,
          score: 100,
          title: 'کلمه کلیدی کانونی تنظیم شده',
          description: `کلمه کلیدی کانونی: «${focus}»`,
          fix: '',
        }
      : {
          id: 'focus-keyword-present',
          category: 'keyword',
          status: STATUS.PROBLEM,
          score: 0,
          title: 'کلمه کلیدی کانونی مشخص نیست',
          description: 'بدون کلمه کلیدی کانونی نمی‌توان تحلیل معناداری انجام داد.',
          fix: 'یک کلمه یا عبارت کلیدی اصلی در فیلد کلمه کلیدی کانونی وارد کنید.',
        }
  );

  if (!hasKw) {
    const skip = (id, title) => ({
      id,
      category: 'keyword',
      status: STATUS.PROBLEM,
      score: 0,
      title,
      description: 'ابتدا کلمه کلیدی کانونی را مشخص کنید.',
      fix: 'کلمه کلیدی کانونی را وارد کنید.',
    });
    results.push(
      skip('keyword-in-title', 'کلمه کلیدی در عنوان SEO'),
      skip('keyword-in-meta', 'کلمه کلیدی در توضیحات متا'),
      skip('keyword-in-slug', 'کلمه کلیدی در نشانی/اسلاگ'),
      skip('keyword-in-first-paragraph', 'کلمه کلیدی در پاراگراف اول'),
      skip('keyword-in-headings', 'کلمه کلیدی در عناوین')
    );
    return results;
  }

  const seoTitle = (ctx.metaTitle || ctx.title || '').trim();
  const metaDesc = (ctx.metaDescription || '').trim();
  const slug = (ctx.slug || '').trim();
  const firstPara = ctx.content?.firstParagraph || '';
  const headings = ctx.content?.headings || [];

  const inTitle = includesKeyword(seoTitle, kw);
  results.push({
    id: 'keyword-in-title',
    category: 'keyword',
    status: inTitle ? STATUS.GOOD : STATUS.PROBLEM,
    score: inTitle ? 100 : 0,
    title: 'کلمه کلیدی در عنوان SEO',
    description: inTitle
      ? 'کلمه کلیدی کانونی در عنوان SEO (یا عنوان مطلب) وجود دارد.'
      : 'کلمه کلیدی کانونی در عنوان SEO دیده نمی‌شود.',
    fix: inTitle ? '' : 'عنوان SEO را طوری بنویسید که شامل کلمه کلیدی کانونی باشد.',
  });

  const inMeta = includesKeyword(metaDesc, kw);
  results.push({
    id: 'keyword-in-meta',
    category: 'keyword',
    status: !metaDesc
      ? STATUS.PROBLEM
      : inMeta
        ? STATUS.GOOD
        : STATUS.NEEDS_IMPROVEMENT,
    score: !metaDesc ? 0 : inMeta ? 100 : 40,
    title: 'کلمه کلیدی در توضیحات متا',
    description: !metaDesc
      ? 'توضیحات متا خالی است.'
      : inMeta
        ? 'کلمه کلیدی در توضیحات متا آمده است.'
        : 'توضیحات متا کلمه کلیدی کانونی را ندارد.',
    fix: !metaDesc
      ? 'یک توضیحات متا بنویسید که شامل کلمه کلیدی باشد.'
      : inMeta
        ? ''
        : 'کلمه کلیدی کانونی را به‌صورت طبیعی در توضیحات متا بگنجانید.',
  });

  // Slug: Persian sites often use Latin transliteration — don't mark as problem
  const slugLatinWithFaKw = hasArabicScript(kw) && isLatinSlug(slug);
  const inSlug =
    includesKeyword(slug.replace(/[-_/]+/g, ' '), kw) || includesKeyword(slug, kw);
  if (!slug) {
    results.push({
      id: 'keyword-in-slug',
      category: 'keyword',
      status: STATUS.PROBLEM,
      score: 0,
      title: 'کلمه کلیدی در نشانی (اسلاگ)',
      description: 'اسلاگ خالی است.',
      fix: 'اسلاگ را تنظیم کنید.',
    });
  } else if (slugLatinWithFaKw) {
    results.push({
      id: 'keyword-in-slug',
      category: 'keyword',
      status: STATUS.GOOD,
      score: 90,
      title: 'کلمه کلیدی در نشانی (اسلاگ)',
      description:
        'اسلاگ لاتین است و کلمه کلیدی فارسی؛ در سایت‌های فارسی این الگوی رایج است و به‌عنوان خطا در نظر گرفته نمی‌شود.',
      fix: '',
    });
  } else {
    results.push({
      id: 'keyword-in-slug',
      category: 'keyword',
      status: inSlug ? STATUS.GOOD : STATUS.NEEDS_IMPROVEMENT,
      score: inSlug ? 100 : 45,
      title: 'کلمه کلیدی در نشانی (اسلاگ)',
      description: inSlug
        ? 'اسلاگ شامل کلمه کلیدی کانونی است.'
        : 'اسلاگ کلمه کلیدی کانونی را ندارد.',
      fix: inSlug ? '' : 'در صورت امکان اسلاگ را نزدیک به کلمه کلیدی نگه دارید.',
    });
  }

  const inFirst = includesKeyword(firstPara, kw);
  results.push({
    id: 'keyword-in-first-paragraph',
    category: 'keyword',
    status: inFirst ? STATUS.GOOD : STATUS.NEEDS_IMPROVEMENT,
    score: inFirst ? 100 : 30,
    title: 'کلمه کلیدی در پاراگراف اول',
    description: inFirst
      ? 'کلمه کلیدی در ابتدای متن آمده است.'
      : 'پاراگراف اول کلمه کلیدی کانونی را ندارد.',
    fix: inFirst ? '' : 'در جملات ابتدایی مطلب، کلمه کلیدی را به‌صورت طبیعی بیاورید.',
  });

  const h1h2 = headings.filter((h) => h.level === 1 || h.level === 2);
  const inHeading = h1h2.some((h) => includesKeyword(h.text, kw));
  const titleAsH1 = includesKeyword(ctx.title || '', kw);
  const ok = inHeading || titleAsH1;
  results.push({
    id: 'keyword-in-headings',
    category: 'keyword',
    status: ok ? STATUS.GOOD : STATUS.NEEDS_IMPROVEMENT,
    score: ok ? 100 : 35,
    title: 'کلمه کلیدی در عناوین (H1/H2)',
    description: ok
      ? 'کلمه کلیدی در عنوان مطلب یا یکی از H1/H2 آمده است.'
      : 'در H1 یا H2 کلمه کلیدی دیده نمی‌شود.',
    fix: ok ? '' : 'حداقل یکی از عناوین H1 یا H2 را حول کلمه کلیدی بنویسید.',
  });

  return results;
}
