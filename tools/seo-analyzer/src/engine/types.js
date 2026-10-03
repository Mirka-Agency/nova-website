/**
 * Shared result status constants.
 * @typedef {'good' | 'needs_improvement' | 'problem'} SeoStatus
 */

export const STATUS = {
  GOOD: 'good',
  NEEDS_IMPROVEMENT: 'needs_improvement',
  PROBLEM: 'problem',
};

/**
 * @typedef {Object} SeoHighlight
 * @property {string} field
 * @property {string} text
 * @property {string} kind
 * @property {number} [occurrence]
 */

/**
 * @typedef {Object} SeoRuleResult
 * @property {string} id
 * @property {string} category
 * @property {SeoStatus} status
 * @property {number} score 0–100 contribution weight preference
 * @property {string} title
 * @property {string} description
 * @property {string} fix
 * @property {SeoHighlight[]} [highlights]
 * @property {boolean} [highlightable]
 */

/**
 * @typedef {Object} AnalyzeContext
 * @property {string} title
 * @property {string} slug
 * @property {string} bodyHtml
 * @property {string} metaTitle
 * @property {string} metaDescription
 * @property {string} canonical
 * @property {string} ogTitle
 * @property {string} ogDescription
 * @property {string} ogImage
 * @property {string} focusKeyword
 * @property {boolean|null} robotsIndex
 * @property {boolean|null} robotsFollow
 * @property {import('../content/parseHtml.js').ParsedContent} content
 * @property {string} plainText
 * @property {string[]} words
 * @property {string} normalizedKeyword
 */
