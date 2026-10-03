/**
 * Maps Yoast assessment identifiers → Mirka panel rule IDs.
 *
 * Shared IDs are fused (Mirka + Yoast) in mergeResults — neither engine replaces the other.
 * Yoast-only assessments use ids that Mirka does not emit.
 */

/** Yoast SEO assessments skipped because Mirka already covers the same technical concern distinctly. */
export const YOAST_SEO_SKIP = new Set([
  'metaDescriptionLength', // Mirka FA character ranges (Yoast still not fused — different metric)
  'titleWidth', // Mirka FA title length
  'slugKeyword', // Mirka Latin-slug policy
  'urlKeyword',
  'internalLinks', // Mirka link counts + admin APIs
  'externalLinks',
  'images', // Mirka missing-alt checklist
  'singleH1', // Mirka heading-structure (skip levels)
]);

/**
 * @typedef {Object} YoastRuleMapping
 * @property {string} id Panel rule id (shared with Mirka when same concern)
 * @property {string} category
 */

/** @type {Record<string, YoastRuleMapping>} */
export const YOAST_RULE_MAP = {
  // SEO / keyphrase (FA function words + stemmer) — shared with Mirka when same id
  introductionKeyword: {
    id: 'keyword-in-first-paragraph',
    category: 'keyword',
  },
  keyphraseInSEOTitle: {
    id: 'keyword-in-title',
    category: 'keyword',
  },
  metaDescriptionKeyword: {
    id: 'keyword-in-meta',
    category: 'keyword',
  },
  subheadingsKeyword: {
    id: 'keyword-in-headings',
    category: 'keyword',
  },
  keyphraseDensity: {
    id: 'keyword-density',
    category: 'keyword',
  },
  keyphraseDistribution: {
    id: 'keyword-distribution',
    category: 'keyword',
  },
  keyphraseLength: {
    id: 'keyphrase-length',
    category: 'keyword',
  },
  functionWordsInKeyphrase: {
    id: 'function-words-in-keyphrase',
    category: 'keyword',
  },
  textLength: {
    id: 'word-count',
    category: 'content',
  },
  textCompetingLinks: {
    id: 'competing-links',
    category: 'links',
  },
  imageKeyphrase: {
    id: 'image-keyphrase',
    category: 'media',
  },

  // Readability (FA)
  textSentenceLength: {
    id: 'sentence-length',
    category: 'readability',
  },
  textParagraphTooLong: {
    id: 'paragraph-length',
    category: 'readability',
  },
  passiveVoice: {
    id: 'passive-voice',
    category: 'readability',
  },
  textTransitionWords: {
    id: 'transition-words',
    category: 'readability',
  },
  sentenceBeginnings: {
    id: 'sentence-beginnings',
    category: 'readability',
  },
  subheadingsTooLong: {
    id: 'subheading-distribution',
    category: 'readability',
  },
  textPresence: {
    id: 'text-presence',
    category: 'content',
  },
};

/** Shared id for Mirka synthetic + Yoast aggregate readability */
export const MIRKA_READABILITY_SCORE_ID = 'readability-score';
