import { normalizePersian } from './normalize.js';
import { splitSentences, tokenize } from './tokenize.js';

/**
 * Heuristic Persian passive-voice detection.
 * Looks for patterns like: شده است، می‌شود، گردیده، شده‌اند، خواهد شد, etc.
 * This is approximate — Persian passive is morphological and context-heavy.
 */
const PASSIVE_PATTERNS = [
  /شده\s*است/g,
  /شده\s*اند/g,
  /شده‌اند/g,
  /شده‌ام/g,
  /شده‌ای/g,
  /شده‌ایم/g,
  /شده‌اید/g,
  /می‌شود/g,
  /مي‌شود/g,
  /خواهد\s*شد/g,
  /گردیده\s*است/g,
  /گردیده‌اند/g,
  /ساخته\s*شده/g,
  /نوشته\s*شده/g,
  /انجام\s*شده/g,
  /گفته\s*شده/g,
  /دیده\s*شده/g,
  /شده\s+است/g,
];

/**
 * @param {string} sentence
 * @returns {boolean}
 */
export function isPassiveSentence(sentence) {
  const n = normalizePersian(sentence);
  if (!n) return false;
  return PASSIVE_PATTERNS.some((re) => {
    re.lastIndex = 0;
    return re.test(n);
  });
}

/**
 * @returns {{
 *   passiveSentences: number,
 *   totalSentences: number,
 *   ratio: number,
 *   sentences: string[],
 * }}
 */
export function estimatePassiveRatio(text) {
  const sentences = splitSentences(text);
  if (!sentences.length) {
    return { passiveSentences: 0, totalSentences: 0, ratio: 0, sentences: [] };
  }

  /** @type {string[]} */
  const passiveList = [];
  for (const sentence of sentences) {
    if (isPassiveSentence(sentence)) passiveList.push(sentence);
  }

  return {
    passiveSentences: passiveList.length,
    totalSentences: sentences.length,
    ratio: passiveList.length / sentences.length,
    sentences: passiveList,
  };
}

/**
 * Average word length in characters (Persian-friendly).
 */
export function averageWordLength(words) {
  if (!words || !words.length) return 0;
  const total = words.reduce((sum, w) => sum + w.replace(/\u200C/g, '').length, 0);
  return total / words.length;
}

/**
 * Average sentence length in words.
 */
export function averageSentenceLength(text) {
  const sentences = splitSentences(text);
  if (!sentences.length) return 0;
  const lengths = sentences.map((s) => tokenize(s).length).filter((n) => n > 0);
  if (!lengths.length) return 0;
  return lengths.reduce((a, b) => a + b, 0) / lengths.length;
}
