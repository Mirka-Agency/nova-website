import { RuleEngine } from './engine/RuleEngine.js';
import { parseHtml } from './content/parseHtml.js';
import { tokenize } from './persian/tokenize.js';
import { normalizePersian, normalizeForMatch } from './persian/normalize.js';
import { keywordPresenceRules } from './rules/keywordPresence.js';
import { keywordDensityRules } from './rules/keywordDensity.js';
import { wordCountRules } from './rules/wordCount.js';
import { readabilityRules } from './rules/readability.js';
import { headingStructureRules } from './rules/headings.js';
import { linkRules } from './rules/links.js';
import { imageAltRules } from './rules/images.js';
import { metaLengthRules } from './rules/meta.js';
import { robotsRules } from './rules/robots.js';
import { openGraphRules } from './rules/openGraph.js';
import { computeOverallScore } from './scoring/overallScore.js';
import { renderPanel } from './ui/renderPanel.js';
import { bindEditor, collectFields } from './ui/bindEditor.js';
import { runYoastAnalysis } from './yoast/runYoast.js';
import { mergeAnalysisResults } from './yoast/mergeResults.js';
import { attachHighlights } from './highlights/collectHighlights.js';
import { createHighlightController } from './ui/highlightInText.js';

function createEngine() {
  const engine = new RuleEngine();
  engine
    .register(keywordPresenceRules)
    .register(keywordDensityRules)
    .register(wordCountRules)
    .register(readabilityRules)
    .register(headingStructureRules)
    .register(linkRules)
    .register(imageAltRules)
    .register(metaLengthRules)
    .register(robotsRules)
    .register(openGraphRules);
  return engine;
}

const engine = createEngine();

/**
 * Analyze content fields and return results + overall score.
 * Parallel: Mirka and Yoast (Farsi) both run; shared rule ids are fused once for scoring/UI.
 * @param {object} fields
 */
export function analyze(fields = {}) {
  const bodyHtml = fields.bodyHtml || fields.body || '';
  const content = parseHtml(bodyHtml);
  const plainText = content.plainText || '';
  const words = tokenize(plainText);
  const focusKeyword = String(fields.focusKeyword || '').trim();
  const normalizedKeyword = focusKeyword ? normalizeForMatch(focusKeyword) : '';

  const context = {
    title: fields.title || '',
    slug: fields.slug || '',
    bodyHtml,
    metaTitle: fields.metaTitle || '',
    metaDescription: fields.metaDescription || '',
    canonical: fields.canonical || '',
    ogTitle: fields.ogTitle || '',
    ogDescription: fields.ogDescription || '',
    ogImage: fields.ogImage || '',
    focusKeyword,
    normalizedKeyword: focusKeyword ? normalizePersian(focusKeyword) : '',
    robotsIndex: fields.robotsIndex ?? null,
    robotsFollow: fields.robotsFollow ?? null,
    content,
    plainText,
    words,
  };

  context.normalizedKeyword = normalizedKeyword || context.normalizedKeyword;

  const mirkaResults = engine.run(context);

  const yoast = runYoastAnalysis({
    ...fields,
    bodyHtml,
    focusKeyword,
    title: context.title,
    slug: context.slug,
    metaTitle: context.metaTitle,
    metaDescription: context.metaDescription,
  });

  const results = mergeAnalysisResults(yoast.results, mirkaResults, {
    yoastError: yoast.meta?.error || null,
  });

  attachHighlights(results, context);

  const overall = computeOverallScore(results);

  return {
    results,
    overall,
    engines: {
      mode: 'parallel',
      mirka: true,
      yoast: !yoast.meta?.error,
      yoastMeta: yoast.meta || null,
    },
    context: {
      wordCount: words.length,
      focusKeyword,
    },
    fields: {
      title: context.title,
      slug: context.slug,
      metaTitle: context.metaTitle,
      metaDescription: context.metaDescription,
      canonical: context.canonical,
    },
  };
}

/**
 * Initialize SEO panel on an element with [data-seo-panel].
 * @param {HTMLElement} root
 */
export function init(root) {
  if (!root) {
    console.warn('[MirkaSeoAnalysis] init: root element missing');
    return null;
  }

  if (root.dataset.seoBound === '1') {
    return root.__mirkaSeo || null;
  }

  const highlighter = createHighlightController(root);
  /** @type {any} */
  let lastPayload = null;

  const runAnalyze = (fields) => {
    const clean = { ...fields };
    delete clean._els;
    const payload = analyze(clean);
    lastPayload = payload;
    renderPanel(root, payload);
    highlighter.syncFromResults(payload.results);
    return payload;
  };

  const binding = bindEditor(root, runAnalyze);
  root.dataset.seoBound = '1';

  root.addEventListener('click', (ev) => {
    const target = ev.target;
    if (!(target instanceof Element)) return;

    const btn = target.closest('[data-seo-highlight-btn]');
    const item = target.closest('.seo-item[data-seo-highlightable]');
    if (!btn && !item) return;

    // Avoid stealing clicks from other controls inside the item
    if (!btn && target.closest('button, a, input, select, textarea, label')) return;

    ev.preventDefault();
    const ruleId =
      (btn && btn.getAttribute('data-seo-rule-id')) ||
      (item && item.getAttribute('data-seo-rule-id'));
    if (!ruleId || !lastPayload) return;

    const rule = (lastPayload.results || []).find((r) => r && r.id === ruleId);
    if (!rule || !rule.highlightable) return;

    const session = highlighter.getSession();
    if (session && session.ruleId === ruleId) {
      highlighter.navigate(1);
      return;
    }

    highlighter.show(ruleId, rule.highlights || [], 0);
  });

  const api = {
    analyze: () => runAnalyze(collectFields(root)),
    refresh: binding.refresh,
    destroy() {
      highlighter.destroy();
      binding.destroy();
      delete root.dataset.seoBound;
      delete root.__mirkaSeo;
    },
    highlight: highlighter,
    root,
  };
  root.__mirkaSeo = api;
  return api;
}

/**
 * Auto-init all [data-seo-panel] nodes when DOM is ready.
 */
export function autoInit() {
  const panels = document.querySelectorAll('[data-seo-panel]');
  const instances = [];
  panels.forEach((el) => {
    instances.push(init(el));
  });
  return instances;
}

if (typeof document !== 'undefined') {
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => autoInit());
  } else {
    // Defer so late-mounted panels / CKEditor can appear
    setTimeout(() => autoInit(), 0);
  }
}
