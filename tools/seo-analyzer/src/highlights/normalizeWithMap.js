/**
 * Normalize text while keeping a map from normalized index → original index.
 * Mirrors normalizeForMatch rules closely enough for highlight search.
 */

const ARABIC_YEH = '\u064A';
const PERSIAN_YEH = '\u06CC';
const ARABIC_KAF = '\u0643';
const PERSIAN_KAF = '\u06A9';
const TATWEEL = '\u0640';
const ZWNJ = '\u200C';

/**
 * @param {string} input
 * @returns {{ normalized: string, indexMap: number[] }}
 */
export function normalizeWithMap(input) {
  const s = String(input || '');
  let normalized = '';
  /** @type {number[]} */
  const indexMap = [];

  for (let i = 0; i < s.length; i++) {
    let ch = s[i];

    if (ch === TATWEEL) continue;
    if (ch === ARABIC_YEH) ch = PERSIAN_YEH;
    if (ch === ARABIC_KAF) ch = PERSIAN_KAF;
    if (ch === ZWNJ) ch = ' ';

    if (/\s/.test(ch)) {
      if (normalized.endsWith(' ')) continue;
      ch = ' ';
    }

    ch = ch.toLowerCase();
    indexMap.push(i);
    normalized += ch;
  }

  // Trim leading/trailing spaces from normalized + map
  let start = 0;
  let end = normalized.length;
  while (start < end && normalized[start] === ' ') start += 1;
  while (end > start && normalized[end - 1] === ' ') end -= 1;

  return {
    normalized: normalized.slice(start, end),
    indexMap: indexMap.slice(start, end),
  };
}

/**
 * Find all occurrences of needle in haystack (normalized).
 * @returns {{ start: number, end: number, text: string }[]}
 *   start/end are indices in the *original* haystack string.
 */
export function findNormalizedMatches(haystack, needle, limit = 40) {
  const { normalized, indexMap } = normalizeWithMap(haystack);
  const needleNorm = normalizeWithMap(needle).normalized;
  if (!normalized || !needleNorm) return [];

  const tokens = needleNorm.split(/\s+/).filter(Boolean);
  if (!tokens.length) return [];

  const escape = (str) => String(str).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const body = tokens.map(escape).join('\\s+');
  const re = new RegExp(body, 'g');
  /** @type {{ start: number, end: number, text: string }[]} */
  const out = [];
  let m;
  while ((m = re.exec(normalized)) && out.length < limit) {
    const nStart = m.index;
    const nEnd = m.index + m[0].length - 1;
    if (nStart >= indexMap.length || nEnd >= indexMap.length) break;
    const start = indexMap[nStart];
    const end = indexMap[nEnd] + 1;
    out.push({
      start,
      end,
      text: haystack.slice(start, end),
    });
    if (m.index === re.lastIndex) re.lastIndex += 1;
  }
  return out;
}
