# Mirka SEO Content Analyzer

Hybrid client-side SEO analyzer for the Mirka CMS admin (Persian-first):

- **YoastSEO.js** (`yoastseo@3.6.0`, GPL-3.0) — official assessments with the **Farsi** researcher
- **Mirka rules** — full custom checklist (technical + Persian policies)

**Mode: parallel** — both engines always analyze; shared rule ids are fused into one panel row
(average score, worse status) so nothing is replaced and the CMS 0–100 score never double-counts.

## Important note (repo state)

Before this package, the repository had **no frontend `package.json`**. Admin JS/CSS lived as static files under `src/CMS.Web/wwwroot/admin/`. This tool is the first dedicated NPM package for admin frontend bundling.

## Build

```bash
cd tools/seo-analyzer
npm install
npm run build
npm run smoke
```

Outputs:

- `src/CMS.Web/wwwroot/admin/js/seo-analysis.bundle.js` (IIFE global `MirkaSeoAnalysis`, ~1.1 MB with Yoast FA)
- `src/CMS.Web/wwwroot/admin/css/seo-analysis.css`

## Libraries

| Package | Role |
|---------|------|
| `yoastseo` | Official Yoast analysis engine + Farsi researcher |
| `@persian-tools/persian-tools` | Mirka FA normalize (`toPersianChars`, digits, …) |
| `buffer` / `events` / `url` | Browser polyfills required to bundle Yoast |
| `esbuild` (dev) | Browser IIFE bundle |

## Parallel analysis map

### From both (fused — one UI row, one score slot)

| Panel rule id | Mirka | Yoast |
|---------------|-------|-------|
| `keyword-in-first-paragraph` | ✓ | `introductionKeyword` |
| `keyword-in-title` | ✓ | `keyphraseInSEOTitle` |
| `keyword-in-meta` | ✓ | `metaDescriptionKeyword` |
| `keyword-in-headings` | ✓ | `subheadingsKeyword` |
| `keyword-density` | ✓ | `keyphraseDensity` |
| `keyword-distribution` | ✓ | `keyphraseDistribution` |
| `word-count` | ✓ | `textLength` |
| `sentence-length` | ✓ | `textSentenceLength` |
| `paragraph-length` | ✓ | `textParagraphTooLong` |
| `passive-voice` | ✓ | `passiveVoice` |
| `readability-score` | ✓ synthetic | ✓ FA aggregate |

### Yoast-only (Farsi researcher)

| Panel rule id | Yoast identifier |
|---------------|------------------|
| `keyphrase-length` | `keyphraseLength` |
| `function-words-in-keyphrase` | `functionWordsInKeyphrase` |
| `transition-words` | `textTransitionWords` |
| `sentence-beginnings` | `sentenceBeginnings` |
| `subheading-distribution` | `subheadingsTooLong` |
| `text-presence` | `textPresence` |
| `competing-links` | `textCompetingLinks` |
| `image-keyphrase` | `imageKeyphrase` |

### Mirka-only

| Panel rule id | Why |
|---------------|-----|
| `focus-keyword-present` | Empty-keyphrase UX |
| `keyword-in-slug` | Latin slug + FA keyword policy |
| `heading-structure` | Skip-level detection |
| `internal-links` / `external-links` | Counts + admin suggest/check APIs |
| `image-alt` | Missing-alt checklist |
| `seo-title-length` / `meta-description-length` | FA character ranges |
| `canonical-url` / `robots-index` / `open-graph` | Technical / social |

### Yoast not used for FA / deferred

| Assessment | Reason |
|------------|--------|
| Flesch reading ease | Not available for Farsi |
| Inclusive language | English only |
| `slugKeyword` / `titleWidth` / `metaDescriptionLength` | Mirka FA policies preferred (not fused) |
| `internalLinks` / `externalLinks` / `images` / `singleH1` | Mirka twins kept as Mirka-only |

### Final 0–100 score (CMS)

1. After merge, each rule **id appears once** (`source`: `mirka` | `yoast` | `both`).
2. For `both`, score = average of Mirka & Yoast percents; status = worse of the two.
3. `computeOverallScore` weights categories as before — no double counting of shared rules.
4. Yoast traffic-light totals stay in `engines.yoastMeta` only.

## Usage

```html
<link rel="stylesheet" href="~/admin/css/seo-analysis.css" />
<div data-seo-panel></div>
<script src="~/admin/js/seo-analysis.bundle.js"></script>
<script>
  MirkaSeoAnalysis.init(document.querySelector('[data-seo-panel]'));
  MirkaSeoAnalysis.analyze({ title, slug, bodyHtml, focusKeyword, ... });
</script>
```

### Field selectors (within closest form)

- Title: `[name=Title]`, `#Title`
- Slug: `[name=Slug]`
- Body: `textarea.admin-ckeditor-source` (prefers `ckEditorInstance.getData()`)
- Meta: `MetaTitle`, `MetaDescription`, canonical, OG fields
- Focus keyword: `[name="Seo.FocusKeyword"]` or `[data-seo-focus-keyword]`
- Robots: `Seo.RobotsIndex` / `Seo.RobotsFollow` checkboxes

### Admin API buttons (antiforgery)

- `POST /api/v1/admin/seo/suggest-links`
- `POST /api/v1/admin/seo/check-links`

## License note

`yoastseo` is **GPL-3.0**. Bundling it into the admin analyzer means this tool’s JS distribution inherits GPL obligations for that combined work. Review with your compliance process before shipping.
