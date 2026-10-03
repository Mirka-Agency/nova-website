import { STATUS } from '../engine/types.js';

/**
 * Parallel merge: Mirka and Yoast both run; shared rule IDs fuse into one row.
 * Unique rules from either engine are kept. Overall score counts each id once.
 */

const STATUS_RANK = {
  [STATUS.PROBLEM]: 0,
  [STATUS.NEEDS_IMPROVEMENT]: 1,
  [STATUS.GOOD]: 2,
};

function worseStatus(a, b) {
  const ra = STATUS_RANK[a] ?? 1;
  const rb = STATUS_RANK[b] ?? 1;
  return ra <= rb ? a : b;
}

function sourceLabel(source) {
  if (source === 'both') return 'Mirka + Yoast';
  if (source === 'yoast') return 'Yoast';
  return 'Mirka';
}

/**
 * Fuse two assessments that share the same panel rule id.
 * Score is averaged so the CMS overall does not double-count the rule.
 * @param {any} mirka
 * @param {any} yoast
 */
export function combineSharedRule(mirka, yoast) {
  const mScore = Math.max(0, Math.min(100, Number(mirka.score) || 0));
  const yScore = Math.max(0, Math.min(100, Number(yoast.score) || 0));
  const score = Math.round((mScore + yScore) / 2);
  const status = worseStatus(mirka.status, yoast.status);

  const mDesc = String(mirka.description || '').trim();
  const yDesc = String(yoast.description || '').trim();
  const description = [mDesc && `Mirka: ${mDesc}`, yDesc && `Yoast: ${yDesc}`]
    .filter(Boolean)
    .join(' ');

  const mFix = String(mirka.fix || '').trim();
  const yFix = String(yoast.fix || '').trim();
  let fix = '';
  if (STATUS_RANK[mirka.status] <= STATUS_RANK[yoast.status]) {
    fix = mFix || yFix;
  } else {
    fix = yFix || mFix;
  }
  if (mFix && yFix && mFix !== yFix) {
    fix = `Mirka: ${mFix} | Yoast: ${yFix}`;
  }

  const highlights =
    (Array.isArray(mirka.highlights) && mirka.highlights.length
      ? mirka.highlights
      : null) ||
    (Array.isArray(yoast.highlights) && yoast.highlights.length
      ? yoast.highlights
      : null) ||
    [];

  return {
    id: mirka.id || yoast.id,
    category: mirka.category || yoast.category,
    status,
    score,
    title: mirka.title || yoast.title,
    description,
    fix,
    highlights,
    highlightable: highlights.length > 0,
    source: 'both',
    sourceLabel: sourceLabel('both'),
    engines: {
      mirka: {
        status: mirka.status,
        score: mScore,
        description: mDesc,
        fix: mFix,
      },
      yoast: {
        status: yoast.status,
        score: yScore,
        description: yDesc,
        fix: yFix,
        yoastIdentifier: yoast.yoastIdentifier,
        yoastScore: yoast.yoastScore,
        yoastRating: yoast.yoastRating,
      },
    },
  };
}

function normalizeEngineResult(r, fallbackSource) {
  const source = r.source || fallbackSource;
  return {
    ...r,
    source,
    sourceLabel: r.sourceLabel || sourceLabel(source),
  };
}

/**
 * @param {any[]} yoastResults
 * @param {any[]} mirkaResults
 * @param {{ yoastError?: string|null }} [opts]
 */
export function mergeAnalysisResults(yoastResults, mirkaResults, opts = {}) {
  const yoast = Array.isArray(yoastResults) ? yoastResults : [];
  const mirka = Array.isArray(mirkaResults) ? mirkaResults : [];

  if (opts.yoastError && yoast.length === 0) {
    return mirka.map((r) => normalizeEngineResult(r, 'mirka'));
  }

  /** @type {Map<string, any>} */
  const mirkaById = new Map();
  for (const r of mirka) {
    if (r && r.id) mirkaById.set(r.id, normalizeEngineResult(r, 'mirka'));
  }

  /** @type {Map<string, any>} */
  const yoastById = new Map();
  for (const r of yoast) {
    if (r && r.id) yoastById.set(r.id, normalizeEngineResult(r, 'yoast'));
  }

  const allIds = new Set([...mirkaById.keys(), ...yoastById.keys()]);
  const out = [];

  for (const id of allIds) {
    const m = mirkaById.get(id);
    const y = yoastById.get(id);
    if (m && y) {
      out.push(combineSharedRule(m, y));
    } else if (m) {
      out.push(m);
    } else if (y) {
      out.push(y);
    }
  }

  return out;
}
