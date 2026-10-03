import { STATUS } from '../engine/types.js';

/**
 * Yoast uses roughly −∞…9 with traffic-light ratings.
 * @param {number} score
 * @returns {'good'|'ok'|'bad'|'feedback'|'error'|''}
 */
export function yoastRating(score, scoreToRating) {
  if (typeof scoreToRating === 'function') {
    return scoreToRating(score) || '';
  }
  if (score === -1) return 'error';
  if (score === 0) return 'feedback';
  if (score <= 4) return 'bad';
  if (score <= 7) return 'ok';
  if (score > 7) return 'good';
  return '';
}

/**
 * @param {string} rating
 * @returns {import('../engine/types.js').SeoStatus}
 */
export function ratingToStatus(rating) {
  switch (rating) {
    case 'good':
      return STATUS.GOOD;
    case 'ok':
    case 'feedback':
      return STATUS.NEEDS_IMPROVEMENT;
    case 'bad':
    case 'error':
    default:
      return STATUS.PROBLEM;
  }
}

/**
 * Map Yoast assessment score onto Mirka's 0–100 contribution.
 * @param {number} score
 */
export function yoastScoreToPercent(score) {
  const n = Number(score);
  if (!Number.isFinite(n) || n <= 0) return 0;
  // Typical max is 9; clamp so odd high values don't break the CMS score
  return Math.round(Math.max(0, Math.min(100, (Math.min(n, 9) / 9) * 100)));
}

/**
 * Strip HTML tags from Yoast assessment text (often contains <a>).
 * @param {string} html
 */
export function stripHtml(html) {
  return String(html || '')
    .replace(/<[^>]+>/g, '')
    .replace(/\s+/g, ' ')
    .trim();
}
