import { STATUS } from '../engine/types.js';
import { tokenize, splitSentences } from '../persian/tokenize.js';
import {
  averageSentenceLength,
  averageWordLength,
  estimatePassiveRatio,
} from '../persian/passive.js';

/** Rules 10–13: sentence/paragraph length, readability, passive voice */
export function readabilityRules(ctx) {
  const plain = ctx.plainText || '';
  const words = ctx.words || [];
  const paragraphs = ctx.content?.paragraphs || [];
  const results = [];

  const sentences = splitSentences(plain);
  const avgSentence = averageSentenceLength(plain);
  const sentenceCount = sentences.filter((s) => tokenize(s).length > 0).length;

  let sentStatus = STATUS.GOOD;
  let sentScore = 100;
  let sentDesc = `میانگین طول جمله: ${avgSentence.toFixed(1)} واژه (از ${sentenceCount} جمله).`;
  let sentFix = '';

  if (!plain.trim()) {
    sentStatus = STATUS.PROBLEM;
    sentScore = 0;
    sentDesc = 'متنی برای ارزیابی طول جمله وجود ندارد.';
    sentFix = 'محتوای مطلب را بنویسید.';
  } else if (avgSentence > 35) {
    sentStatus = STATUS.PROBLEM;
    sentScore = 25;
    sentDesc += ' جملات خیلی طولانی‌اند.';
    sentFix = 'جملات را با نقطه یا پاراگراف کوتاه‌تر کنید؛ هدف حدود ۱۰ تا ۲۵ واژه است.';
  } else if (avgSentence > 28) {
    sentStatus = STATUS.NEEDS_IMPROVEMENT;
    sentScore = 55;
    sentDesc += ' کمی طولانی است.';
    sentFix = 'برخی جملات را بشکنید تا خوانایی بهتر شود.';
  } else if (avgSentence < 5 && words.length > 80 && sentenceCount > 3) {
    sentStatus = STATUS.NEEDS_IMPROVEMENT;
    sentScore = 60;
    sentDesc += ' جملات بسیار کوتاه‌اند و ممکن است بریده به نظر برسند.';
    sentFix = 'جملات را کمی روان‌تر و کامل‌تر بنویسید.';
  } else {
    sentDesc += ' در محدوده مناسب است.';
  }

  results.push({
    id: 'sentence-length',
    category: 'readability',
    status: sentStatus,
    score: sentScore,
    title: 'طول جملات',
    description: sentDesc,
    fix: sentFix,
  });

  const paraWordCounts = paragraphs.map((p) => tokenize(p).length);
  const longParas = paraWordCounts.filter((n) => n > 150).length;
  const avgPara =
    paraWordCounts.length > 0
      ? paraWordCounts.reduce((a, b) => a + b, 0) / paraWordCounts.length
      : words.length;

  let paraStatus = STATUS.GOOD;
  let paraScore = 100;
  let paraDesc =
    paragraphs.length > 0
      ? `تعداد پاراگراف: ${paragraphs.length}؛ میانگین واژه‌ها در هر پاراگراف: ${avgPara.toFixed(0)}.`
      : 'پاراگراف مشخصی یافت نشد؛ کل متن به‌عنوان یک بلوک در نظر گرفته شد.';
  let paraFix = '';

  if (words.length === 0) {
    paraStatus = STATUS.PROBLEM;
    paraScore = 0;
    paraDesc = 'متنی برای ارزیابی پاراگراف وجود ندارد.';
    paraFix = 'متن مطلب را وارد کنید.';
  } else if (longParas > 0 || avgPara > 150) {
    paraStatus = STATUS.NEEDS_IMPROVEMENT;
    paraScore = 45;
    paraDesc += ' برخی پاراگراف‌ها خیلی طولانی‌اند.';
    paraFix = 'پاراگراف‌های بلند را به بخش‌های کوتاه‌تر تقسیم کنید (حدود ۴۰–۱۲۰ واژه).';
  } else if (paragraphs.length <= 1 && words.length > 200) {
    paraStatus = STATUS.NEEDS_IMPROVEMENT;
    paraScore = 50;
    paraDesc += ' ساختار پاراگراف‌بندی ضعیف است.';
    paraFix = 'متن را با تگ‌های <p> به پاراگراف‌های خوانا تقسیم کنید.';
  } else {
    paraDesc += ' ساختار پاراگراف مناسب به نظر می‌رسد.';
  }

  results.push({
    id: 'paragraph-length',
    category: 'readability',
    status: paraStatus,
    score: paraScore,
    title: 'طول پاراگراف‌ها',
    description: paraDesc,
    fix: paraFix,
  });

  const avgWordLen = averageWordLength(words);
  let readability = 100;
  if (avgSentence > 0) {
    readability -= Math.max(0, (avgSentence - 20) * 2.2);
  }
  if (avgWordLen > 0) {
    readability -= Math.max(0, (avgWordLen - 6) * 4);
  }
  if (words.length < 50) {
    readability = Math.min(readability, 40);
  }
  readability = Math.max(0, Math.min(100, Math.round(readability)));

  let readStatus = STATUS.GOOD;
  let readFix = '';
  if (readability < 40) {
    readStatus = STATUS.PROBLEM;
    readFix = 'جملات و واژه‌ها را ساده‌تر و کوتاه‌تر کنید.';
  } else if (readability < 65) {
    readStatus = STATUS.NEEDS_IMPROVEMENT;
    readFix = 'با کوتاه کردن جملات و واژه‌های پیچیده، خوانایی را بالا ببرید.';
  }

  results.push({
    id: 'readability-score',
    category: 'readability',
    status: readStatus,
    score: readability,
    title: 'امتیاز خوانایی',
    description: `امتیاز خوانایی تقریبی (فارسی‌محور): ${readability} از ۱۰۰. میانگین طول واژه: ${avgWordLen.toFixed(1)}.`,
    fix: readFix,
  });

  const passive = estimatePassiveRatio(plain);
  const ratioPct = Math.round(passive.ratio * 100);
  let passStatus = STATUS.GOOD;
  let passScore = 100;
  let passDesc = `تقریباً ${ratioPct}٪ جملات (${passive.passiveSentences} از ${passive.totalSentences}) الگوی مجهول دارند.`;
  let passFix = '';

  if (passive.totalSentences === 0) {
    passStatus = STATUS.PROBLEM;
    passScore = 0;
    passDesc = 'جمله‌ای برای بررسی معلوم/مجهول یافت نشد.';
    passFix = 'متن مطلب را بنویسید.';
  } else if (passive.ratio > 0.35) {
    passStatus = STATUS.PROBLEM;
    passScore = 25;
    passDesc += ' نسبت مجهول بالاست.';
    passFix = 'تا حد ممکن جملات را به حالت معلوم بازنویسی کنید (فاعل مشخص).';
  } else if (passive.ratio > 0.2) {
    passStatus = STATUS.NEEDS_IMPROVEMENT;
    passScore = 55;
    passDesc += ' کمی بیش از حد توصیه‌شده است.';
    passFix = 'برخی جملات مجهول را معلوم کنید.';
  } else {
    passDesc += ' در محدوده قابل قبول است.';
  }

  results.push({
    id: 'passive-voice',
    category: 'readability',
    status: passStatus,
    score: passScore,
    title: 'نسبت مجهول (تخمینی)',
    description: passDesc,
    fix: passFix,
  });

  return results;
}
