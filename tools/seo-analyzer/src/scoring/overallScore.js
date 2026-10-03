import { STATUS } from '../engine/types.js';

/** Category weights for overall SEO score (rule 23) */
const CATEGORY_WEIGHT = {
  keyword: 1.25,
  content: 1.1,
  readability: 0.9,
  structure: 1.0,
  links: 0.85,
  media: 0.8,
  meta: 1.15,
  robots: 0.7,
  social: 0.75,
  system: 0,
};

const STATUS_FLOOR = {
  [STATUS.GOOD]: 1,
  [STATUS.NEEDS_IMPROVEMENT]: 0.55,
  [STATUS.PROBLEM]: 0.15,
};

/**
 * Aggregate rule results into 0–100 overall score + label.
 * @param {import('../engine/types.js').SeoRuleResult[]} results
 */
export function computeOverallScore(results) {
  const list = (results || []).filter((r) => r && r.id !== 'rule-error');
  if (!list.length) {
    return { score: 0, label: 'ضعیف', status: STATUS.PROBLEM };
  }

  let weighted = 0;
  let totalWeight = 0;

  for (const r of list) {
    const catW = CATEGORY_WEIGHT[r.category] ?? 1;
    const floor = STATUS_FLOOR[r.status] ?? 0.5;
    const ruleScore = Math.max(0, Math.min(100, Number(r.score) || 0));
    // Blend explicit score with status floor so problems hurt more
    const blended = ruleScore * 0.7 + floor * 100 * 0.3;
    weighted += blended * catW;
    totalWeight += catW;
  }

  const score = Math.round(Math.max(0, Math.min(100, weighted / Math.max(totalWeight, 1))));

  let label = 'عالی';
  let status = STATUS.GOOD;
  if (score < 50) {
    label = 'ضعیف';
    status = STATUS.PROBLEM;
  } else if (score < 75) {
    label = 'نیاز به بهبود';
    status = STATUS.NEEDS_IMPROVEMENT;
  }

  return { score, label, status };
}
