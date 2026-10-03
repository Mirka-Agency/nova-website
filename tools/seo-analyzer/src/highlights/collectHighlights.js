import { tokenize, splitSentences } from '../persian/tokenize.js';
import { estimatePassiveRatio } from '../persian/passive.js';
import { countKeyword } from '../persian/normalize.js';
import { STATUS } from '../engine/types.js';

/**
 * @typedef {Object} SeoHighlight
 * @property {string} field  body | title | metaTitle | metaDescription | slug | focusKeyword | ogTitle | ogDescription | ogImage
 * @property {string} text   snippet to locate in the field / editor
 * @property {string} kind   keyword | passive | long-sentence | long-paragraph | heading | image-alt | field
 * @property {number} [occurrence] 0-based match index among identical texts
 */

const LONG_SENTENCE_WORDS = 25;
const LONG_PARAGRAPH_WORDS = 150;
const MAX_PER_RULE = 25;

/**
 * @param {string} field
 * @param {string} text
 * @param {string} kind
 * @param {number} [occurrence]
 * @returns {SeoHighlight | null}
 */
function hl(field, text, kind, occurrence = 0) {
  const t = String(text || '').trim();
  if (!t) return null;
  return { field, text: t, kind, occurrence };
}

/**
 * Deduplicate by field+text+occurrence while capping count.
 * @param {(SeoHighlight|null|undefined)[]} items
 * @param {number} [limit]
 */
function pack(items, limit = MAX_PER_RULE) {
  const out = [];
  const seen = new Set();
  for (const item of items) {
    if (!item || !item.field) continue;
    // Field-only markers (empty text) are valid — scroll/focus the input
    if (!item.text && item.kind !== 'field') continue;
    const key = `${item.field}|${item.kind}|${item.text || ''}|${item.occurrence || 0}`;
    if (seen.has(key)) continue;
    seen.add(key);
    out.push(item);
    if (out.length >= limit) break;
  }
  return out;
}

/**
 * Attach `highlights` arrays onto analysis results (mutates results).
 * Only non-good (or keyword-location) rules get actionable markers.
 * @param {any[]} results
 * @param {any} context analyze() context
 */
export function attachHighlights(results, context) {
  if (!Array.isArray(results) || !context) return results;

  const plain = context.plainText || '';
  const focus = String(context.focusKeyword || '').trim();
  const paragraphs = context.content?.paragraphs || [];
  const headings = context.content?.headings || [];
  const images = context.content?.images || [];
  const sentences = splitSentences(plain);
  const passive = estimatePassiveRatio(plain);

  const longSentences = sentences.filter((s) => tokenize(s).length > LONG_SENTENCE_WORDS);
  const longParagraphs = paragraphs.filter((p) => tokenize(p).length > LONG_PARAGRAPH_WORDS);

  /** @type {Map<string, any>} */
  const byId = new Map();
  for (const r of results) {
    if (r && r.id) byId.set(r.id, r);
  }

  for (const r of results) {
    if (!r || !r.id) continue;
    r.highlights = buildForRule(r, {
      focus,
      plain,
      longSentences,
      longParagraphs,
      passiveSentences: passive.sentences || [],
      headings,
      images,
      firstParagraph: context.content?.firstParagraph || '',
      title: context.title || '',
      metaTitle: context.metaTitle || '',
      metaDescription: context.metaDescription || '',
      slug: context.slug || '',
    });
    r.highlightable = Array.isArray(r.highlights) && r.highlights.length > 0;
  }

  return results;
}

/**
 * @param {any} rule
 * @param {any} data
 * @returns {SeoHighlight[]}
 */
function buildForRule(rule, data) {
  const id = rule.id;
  const bad = rule.status !== STATUS.GOOD;

  switch (id) {
    case 'passive-voice': {
      if (!bad && !(data.passiveSentences || []).length) return [];
      if (!bad) return [];
      return pack(
        (data.passiveSentences || []).map((s, i) => hl('body', s, 'passive', i))
      );
    }

    case 'sentence-length': {
      if (!bad) return [];
      return pack(
        (data.longSentences || []).map((s, i) => hl('body', s, 'long-sentence', i))
      );
    }

    case 'paragraph-length': {
      if (!bad) return [];
      return pack(
        (data.longParagraphs || []).map((p, i) => hl('body', p, 'long-paragraph', i))
      );
    }

    case 'keyword-density':
    case 'keyword-distribution': {
      if (!data.focus) return [];
      // Highlight keyphrase occurrences whenever present (Yoast-style eye on density)
      const count = countKeyword(data.plain, data.focus);
      if (count === 0) {
        if (!bad) return [];
        const target = data.firstParagraph || data.plain.slice(0, 80);
        return pack([hl('body', target || data.focus, 'field', 0)]);
      }
      return pack(
        Array.from({ length: Math.min(count, MAX_PER_RULE) }, (_, i) =>
          hl('body', data.focus, 'keyword', i)
        )
      );
    }

    case 'keyword-in-first-paragraph': {
      if (!data.focus) return [];
      if (rule.status === STATUS.GOOD) {
        return pack([hl('body', data.focus, 'keyword', 0)]);
      }
      // Missing: jump to start of body / first paragraph
      const target = data.firstParagraph || data.plain.slice(0, 60);
      return pack([hl('body', target, 'field', 0)]);
    }

    case 'keyword-in-headings': {
      if (!data.focus) return [];
      if (rule.status === STATUS.GOOD) {
        const match = (data.headings || []).find((h) =>
          countKeyword(h.text, data.focus) > 0
        );
        if (match) return pack([hl('body', match.text, 'heading', 0)]);
        if (countKeyword(data.title, data.focus) > 0) {
          return pack([hl('title', data.title, 'field', 0)]);
        }
        return pack([hl('body', data.focus, 'keyword', 0)]);
      }
      const firstHeading = (data.headings || [])[0];
      if (firstHeading) return pack([hl('body', firstHeading.text, 'heading', 0)]);
      return pack([hl('title', data.title || data.focus, 'field', 0)]);
    }

    case 'keyword-in-title': {
      const field = data.metaTitle ? 'metaTitle' : 'title';
      const value = data.metaTitle || data.title;
      if (rule.status === STATUS.GOOD && data.focus) {
        return pack([hl(field, data.focus, 'keyword', 0)]);
      }
      return pack([hl(field, value || data.focus || '', 'field', 0)]);
    }

    case 'keyword-in-meta': {
      if (rule.status === STATUS.GOOD && data.focus) {
        return pack([hl('metaDescription', data.focus, 'keyword', 0)]);
      }
      return pack([
        hl('metaDescription', data.metaDescription || data.focus || '', 'field', 0),
      ]);
    }

    case 'keyword-in-slug': {
      return pack([hl('slug', data.slug || data.focus || '', 'field', 0)]);
    }

    case 'focus-keyword-present': {
      if (rule.status === STATUS.GOOD) return [];
      return pack([hl('focusKeyword', '', 'field', 0)]);
    }

    case 'heading-structure': {
      if (!bad) return [];
      const items = (data.headings || []).map((h, i) => hl('body', h.text, 'heading', i));
      if (!items.length && data.title) {
        return pack([hl('title', data.title, 'field', 0)]);
      }
      return pack(items);
    }

    case 'image-alt': {
      if (!bad) return [];
      const missing = (data.images || []).filter((img) => !String(img.alt || '').trim());
      return pack(
        missing.map((img, i) =>
          hl('body', img.src || `image-${i}`, 'image-alt', i)
        )
      );
    }

    case 'seo-title-length': {
      const field = data.metaTitle ? 'metaTitle' : 'title';
      return pack([hl(field, data.metaTitle || data.title || '', 'field', 0)]);
    }

    case 'meta-description-length': {
      return pack([hl('metaDescription', data.metaDescription || '', 'field', 0)]);
    }

    case 'open-graph': {
      if (!bad) return [];
      return pack([
        hl('ogTitle', '', 'field', 0),
        hl('ogDescription', '', 'field', 0),
        hl('ogImage', '', 'field', 0),
      ]);
    }

    case 'readability-score': {
      if (!bad) return [];
      return pack([
        ...(data.longSentences || []).slice(0, 8).map((s, i) =>
          hl('body', s, 'long-sentence', i)
        ),
        ...(data.passiveSentences || []).slice(0, 8).map((s, i) =>
          hl('body', s, 'passive', i)
        ),
      ]);
    }

    default:
      return [];
  }
}
