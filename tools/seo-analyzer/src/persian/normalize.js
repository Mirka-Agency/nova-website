import { toPersianChars, digitsFaToEn, digitsArToEn } from '@persian-tools/persian-tools';

const ARABIC_YEH = /\u064A/g; // ي
const ARABIC_KAF = /\u0643/g; // ك
const TATWEEL = /\u0640/g;
const MULTI_SPACE = /\s+/g;
const ZWNJ = /\u200C+/g;
const LETTER_CLASS = '\\u0600-\\u06FF\\u0750-\\u077F\\u08A0-\\u08FFa-zA-Z0-9';

/**
 * Visual/display normalize (keeps meaningful ZWNJ where possible).
 */
export function normalizePersian(input) {
  if (input == null) return '';
  let text = String(input);

  try {
    const converted = toPersianChars(text);
    if (converted != null) text = String(converted);
  } catch {
    text = text.replace(ARABIC_YEH, 'ی').replace(ARABIC_KAF, 'ک');
  }

  try {
    const digits = digitsFaToEn(text);
    if (digits != null) text = String(digits);
  } catch {
    /* ignore */
  }
  try {
    const digits = digitsArToEn(text);
    if (digits != null) text = String(digits);
  } catch {
    /* ignore */
  }

  text = String(text || '');
  text = text.replace(TATWEEL, '');
  return text.replace(MULTI_SPACE, ' ').trim();
}

/**
 * Matching normalize: unify ZWNJ ↔ space so «گوشی هوشمند» == «گوشی‌هوشمند».
 */
export function normalizeForMatch(input) {
  return normalizePersian(input)
    .replace(ZWNJ, ' ')
    .replace(MULTI_SPACE, ' ')
    .trim()
    .toLowerCase();
}

export function escapeRegExp(str) {
  return String(str).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function keywordMatchRegex(keyword) {
  const k = normalizeForMatch(keyword);
  if (!k) return null;
  const tokens = k.split(/\s+/).filter(Boolean);
  if (!tokens.length) return null;

  // Allow flexible whitespace/ZWNJ between phrase tokens
  const body = tokens.map(escapeRegExp).join('[\\s\\u200C]+');

  if (tokens.length === 1 && tokens[0].length <= 2) {
    // Very short tokens: require letter boundaries to avoid noise
    return new RegExp(`(?<![${LETTER_CLASS}])${body}(?![${LETTER_CLASS}])`, 'g');
  }

  if (tokens.length === 1) {
    return new RegExp(`(?<![${LETTER_CLASS}])${body}(?![${LETTER_CLASS}])`, 'g');
  }

  return new RegExp(body, 'g');
}

/**
 * Count focus-keyword occurrences (phrase-aware, ZWNJ-safe, word-boundary for singles).
 */
export function countKeyword(haystack, keyword) {
  const h = normalizeForMatch(haystack);
  const re = keywordMatchRegex(keyword);
  if (!re || !h) return 0;
  const matches = h.match(re);
  return matches ? matches.length : 0;
}

/**
 * True if normalized keyword appears in haystack.
 */
export function includesKeyword(haystack, keyword) {
  return countKeyword(haystack, keyword) > 0;
}

export function hasArabicScript(text) {
  return /[\u0600-\u06FF]/.test(String(text || ''));
}

export function isLatinSlug(slug) {
  const s = String(slug || '').trim();
  if (!s) return false;
  return /^[a-z0-9\-\/_~.]+$/i.test(s) && !hasArabicScript(s);
}
