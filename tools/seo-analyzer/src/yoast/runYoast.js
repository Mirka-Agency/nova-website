/**
 * Run YoastSEO.js (official) with Farsi researcher and map to Mirka rule results.
 */

import { Paper, SeoAssessor, ContentAssessor, assessments, interpreters } from 'yoastseo';
// Deep import keeps only FA language data path; package root still resolves AbstractResearcher.
import FarsiResearcher from 'yoastseo/build/languageProcessing/languages/fa/Researcher.js';

import { YOAST_RULE_MAP, YOAST_SEO_SKIP, MIRKA_READABILITY_SCORE_ID } from './ruleMap.js';
import { yoastRating, ratingToStatus, yoastScoreToPercent, stripHtml } from './score.js';
import { copyFor } from './persianCopy.js';
import { STATUS } from '../engine/types.js';

const scoreToRating = interpreters.scoreToRating;

/**
 * Estimate title width in pixels for Yoast PageTitleWidth (we skip that assessment,
 * but Paper still accepts titleWidth for completeness).
 * @param {string} title
 */
function estimateTitleWidth(title) {
  // Rough average glyph width; unused for skipped titleWidth assessment
  return Math.round(String(title || '').length * 10);
}

/**
 * @param {object} fields
 * @returns {import('yoastseo').Paper}
 */
export function createPaper(fields = {}) {
  const bodyHtml = fields.bodyHtml || fields.body || '';
  const seoTitle = (fields.metaTitle || fields.title || '').trim();
  const keyword = String(fields.focusKeyword || '').trim();
  const slug = String(fields.slug || '').trim();
  const permalink = fields.permalink || (slug ? `https://local.test/${slug}` : 'https://local.test/');

  return new Paper(bodyHtml, {
    keyword,
    // synonyms left empty — CMS has a single focus keyword today
    description: String(fields.metaDescription || ''),
    title: seoTitle,
    titleWidth: estimateTitleWidth(seoTitle),
    slug,
    permalink,
    locale: 'fa_IR',
  });
}

/**
 * @param {import('yoastseo').AssessmentResult} result
 * @param {string} identifier
 */
function mapOne(result, identifier) {
  const mapping = YOAST_RULE_MAP[identifier];
  if (!mapping) return null;

  const rawScore = result.getScore();
  const rating = yoastRating(rawScore, scoreToRating);
  const status = ratingToStatus(rating);
  const copy = copyFor(identifier, rating);
  const yoastText = stripHtml(result.getText());

  return {
    id: mapping.id,
    category: mapping.category,
    status,
    score: yoastScoreToPercent(rawScore),
    title: copy.title,
    description: copy.description || yoastText,
    fix: copy.fix,
    source: 'yoast',
    yoastIdentifier: identifier,
    yoastScore: rawScore,
    yoastRating: rating,
  };
}

/**
 * Build a Mirka readability-score summary from Yoast readability results.
 * @param {ReturnType<mapOne>[]} readabilityResults
 * @param {number} yoastOverall  Yoast content assessor traffic-light score (≈30/60/90)
 */
function buildReadabilitySummary(readabilityResults, yoastOverall) {
  if (!readabilityResults.length) return null;

  const avg =
    readabilityResults.reduce((s, r) => s + (Number(r.score) || 0), 0) /
    readabilityResults.length;

  // Blend Yoast overall (30/60/90) into 0–100 with per-rule average
  const yoastMapped =
    yoastOverall >= 80 ? 95 : yoastOverall >= 50 ? 65 : yoastOverall > 0 ? 35 : avg;
  const score = Math.round(avg * 0.7 + yoastMapped * 0.3);

  let status = STATUS.GOOD;
  let description = `امتیاز خوانایی ترکیبی (Yoast فارسی): ${score} از ۱۰۰.`;
  let fix = '';
  if (score < 40) {
    status = STATUS.PROBLEM;
    fix = 'جملات، پاراگراف‌ها و نسبت مجهول را بهبود دهید.';
  } else if (score < 65) {
    status = STATUS.NEEDS_IMPROVEMENT;
    fix = 'با کوتاه کردن جملات و افزودن کلمات ربط، خوانایی را بالا ببرید.';
  } else {
    description += ' وضعیت کلی خوب است.';
  }

  return {
    id: MIRKA_READABILITY_SCORE_ID,
    category: 'readability',
    status,
    score,
    title: 'امتیاز خوانایی',
    description,
    fix,
    source: 'yoast',
  };
}

/**
 * @param {object} fields analyze() fields
 * @returns {{ results: any[], meta: { engine: string, locale: string, seoOverall: number|null, contentOverall: number|null, error?: string } }}
 */
export function runYoastAnalysis(fields = {}) {
  const meta = {
    engine: 'yoastseo',
    locale: 'fa_IR',
    seoOverall: null,
    contentOverall: null,
  };

  try {
    const paper = createPaper(fields);
    const researcher = new FarsiResearcher(paper);
    const hasKeyword = Boolean(String(fields.focusKeyword || '').trim());
    const mapped = [];

    // Without a focus keyphrase, Yoast SEO assessments are noisy reds —
    // Mirka's focus-keyword-present + keywordPresenceRules own that UX.
    if (hasKeyword) {
      const seoAssessor = new SeoAssessor(researcher);
      for (const id of YOAST_SEO_SKIP) {
        seoAssessor.removeAssessment(id);
      }

      // Shipped in the free package and works for FA.
      const Dist =
        assessments.seo.KeyphraseDistributionAssessment ||
        assessments.seo.KeyphraseDistribution;
      if (Dist) {
        seoAssessor.addAssessment('keyphraseDistribution', new Dist());
      }

      seoAssessor.assess(paper);
      meta.seoOverall = seoAssessor.calculateOverallScore();

      for (const result of seoAssessor.getValidResults()) {
        const id = result.getIdentifier();
        if (!id || YOAST_SEO_SKIP.has(id)) continue;
        if (!YOAST_RULE_MAP[id]) continue;
        const item = mapOne(result, id);
        if (item) mapped.push(item);
      }
    }

    const contentAssessor = new ContentAssessor(researcher);
    contentAssessor.assess(paper);
    meta.contentOverall = contentAssessor.calculateOverallScore();

    const readabilityMapped = [];
    for (const result of contentAssessor.getValidResults()) {
      const id = result.getIdentifier();
      if (!id || !YOAST_RULE_MAP[id]) continue;
      const item = mapOne(result, id);
      if (item) {
        mapped.push(item);
        readabilityMapped.push(item);
      }
    }

    const summary = buildReadabilitySummary(
      readabilityMapped,
      meta.contentOverall || 0
    );
    if (summary) mapped.push(summary);

    return { results: mapped, meta };
  } catch (err) {
    meta.error = err && err.message ? String(err.message) : 'yoast-error';
    return { results: [], meta };
  }
}
