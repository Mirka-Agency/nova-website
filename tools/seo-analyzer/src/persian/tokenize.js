import { normalizePersian } from './normalize.js';

/**
 * Tokenize Persian + English text into words.
 * Splits on whitespace and common punctuation; keeps ZWNJ inside words.
 */
export function tokenize(text) {
  const normalized = normalizePersian(text || '');
  if (!normalized) return [];

  const cleaned = normalized
    .replace(/[«»""''`]/g, ' ')
    .replace(/[،,.؛;:!؟?。…۔()\[\]{}<>\/\\|@#$%^&*_+=~\-–—]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();

  if (!cleaned) return [];

  return cleaned
    .split(/\s+/)
    .map((w) => w.replace(/^[\u200C]+|[\u200C]+$/g, ''))
    .filter((w) => w.length > 0);
}

/**
 * Sentence split for Persian/English.
 * IMPORTANT: normalize per-block so newlines (paragraph boundaries) are not collapsed.
 */
export function splitSentences(text) {
  if (text == null || !String(text).trim()) return [];

  // Split on paragraph/newline BEFORE normalizePersian (which collapses \s → space)
  const rawBlocks = String(text)
    .replace(/\r\n/g, '\n')
    .replace(/\r/g, '\n')
    .split(/\n+/)
    .map((s) => s.trim())
    .filter((s) => s.length > 0);

  const sentences = [];
  for (const raw of rawBlocks) {
    const block = normalizePersian(raw);
    if (!block) continue;

    const parts = block.split(/(?<=[.!?؟。…۔])\s*|(?<=[؛;])\s+/);
    for (const part of parts) {
      const t = part.trim();
      if (!t) continue;
      if (/^[.!?؟。…۔؛;]+$/.test(t)) continue;
      sentences.push(t);
    }
  }

  if (sentences.length) return sentences;

  const fallback = normalizePersian(text);
  return fallback ? [fallback] : [];
}
