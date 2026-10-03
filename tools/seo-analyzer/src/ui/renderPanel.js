const STATUS_ICON = {
  good: '✓',
  needs_improvement: '!',
  problem: '✕',
};

const STATUS_CLASS = {
  good: 'seo-status-good',
  needs_improvement: 'seo-status-warn',
  problem: 'seo-status-error',
};

const TAB_CATEGORIES = {
  content: new Set(['keyword', 'content', 'structure', 'links', 'media', 'meta']),
  readability: new Set(['readability']),
  social: new Set(['social']),
  advanced: new Set(['robots', 'schema']),
};

function escapeHtml(str) {
  return String(str || '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function truncate(str, max) {
  const s = String(str || '');
  if (s.length <= max) return s;
  return s.slice(0, max - 1) + '…';
}

function ensureActions(root) {
  if (root.querySelector('[data-seo-suggest-links]')) return;

  const actions = document.createElement('div');
  actions.className = 'seo-analysis__actions admin-seo-actions';
  actions.innerHTML = `
    <button type="button" class="btn btn-secondary seo-btn" data-seo-suggest-links>پیشنهاد لینک داخلی</button>
    <button type="button" class="btn btn-secondary seo-btn" data-seo-check-links>بررسی لینک‌های شکسته</button>
  `;

  const msg = document.createElement('div');
  msg.className = 'seo-analysis__api-msg admin-seo-api-msg';
  msg.setAttribute('data-seo-api-msg', '');
  msg.hidden = true;

  const tabs = root.querySelector('[data-seo-tabs]');
  if (tabs) {
    tabs.before(actions);
    actions.after(msg);
  } else {
    root.appendChild(actions);
    root.appendChild(msg);
  }
}

function ensureSummary(root) {
  let el = root.querySelector('[data-seo-summary]');
  if (el) return el;

  el = document.createElement('div');
  el.className = 'admin-seo-summary';
  el.setAttribute('data-seo-summary', '');

  const serp = root.querySelector('[data-seo-serp]');
  const keywordField = root.querySelector('[data-seo-focus-keyword]')?.closest('.admin-field');
  const insertAfter = keywordField || root.querySelector('.admin-seo-panel__header');
  if (insertAfter && insertAfter.parentNode) {
    insertAfter.after(el);
  } else if (serp) {
    serp.before(el);
  } else {
    root.prepend(el);
  }
  return el;
}

function ensureScoreCaption(scoreWrap) {
  if (!scoreWrap) return null;
  let caption = scoreWrap.querySelector('[data-seo-score-caption]');
  if (caption) return caption;
  caption = document.createElement('span');
  caption.className = 'admin-seo-score__caption';
  caption.setAttribute('data-seo-score-caption', '');
  scoreWrap.appendChild(caption);
  return caption;
}

function ensureSerpChrome(root) {
  const serp = root.querySelector('[data-seo-serp]');
  if (!serp || serp.querySelector('[data-seo-serp-favicon]')) return;

  const url = serp.querySelector('[data-seo-serp-url]');
  if (!url) return;

  const row = document.createElement('div');
  row.className = 'admin-seo-serp__breadcrumb';
  const fav = document.createElement('span');
  fav.className = 'admin-seo-serp__favicon';
  fav.setAttribute('data-seo-serp-favicon', '');
  fav.setAttribute('aria-hidden', 'true');
  fav.textContent = '◆';
  url.replaceWith(row);
  row.appendChild(fav);
  row.appendChild(url);
}

function countByStatus(results) {
  const counts = { good: 0, needs_improvement: 0, problem: 0 };
  for (const r of results) {
    if (r && counts[r.status] != null) counts[r.status] += 1;
  }
  return counts;
}

function renderSummary(root, results) {
  const el = ensureSummary(root);
  const c = countByStatus(results);
  el.innerHTML = `
    <span class="admin-seo-summary__chip admin-seo-summary__chip--good"><strong>${c.good}</strong> خوب</span>
    <span class="admin-seo-summary__chip admin-seo-summary__chip--warn"><strong>${c.needs_improvement}</strong> نیاز به بهبود</span>
    <span class="admin-seo-summary__chip admin-seo-summary__chip--error"><strong>${c.problem}</strong> مشکل</span>
  `;
}

function updateTabCounts(root, byTab) {
  const tabs = root.querySelectorAll('[data-seo-tab]');
  tabs.forEach((tab) => {
    const key = tab.getAttribute('data-seo-tab');
    const n = (byTab[key] || []).length;
    let badge = tab.querySelector('.admin-seo-tab__count');
    if (!badge) {
      badge = document.createElement('span');
      badge.className = 'admin-seo-tab__count';
      tab.appendChild(badge);
    }
    badge.textContent = String(n);
    badge.hidden = n === 0 && key !== 'advanced';
  });
}

function renderHighlightActions(r) {
  if (!r.highlightable || !Array.isArray(r.highlights) || !r.highlights.length) {
    return '';
  }
  const count = r.highlights.length;
  const countLabel = count > 1 ? ` (${count})` : '';
  return `
    <div class="seo-item__actions">
      <button type="button"
              class="seo-item__highlight-btn"
              data-seo-highlight-btn
              data-seo-rule-id="${escapeHtml(r.id)}"
              title="مشاهده محل این مورد در متن">
        <span class="seo-item__highlight-icon" aria-hidden="true">◎</span>
        مشاهده در متن${countLabel}
      </button>
    </div>`;
}

function renderResultList(container, items) {
  if (!container) return;
  if (!items.length) {
    container.innerHTML = '<p class="seo-empty">موردی برای این بخش نیست.</p>';
    return;
  }

  const order = { problem: 0, needs_improvement: 1, good: 2 };
  const sorted = [...items].sort(
    (a, b) => (order[a.status] ?? 9) - (order[b.status] ?? 9)
  );

  container.innerHTML = `<ul class="seo-checklist">${sorted
    .map((r) => {
      const cls = STATUS_CLASS[r.status] || STATUS_CLASS.needs_improvement;
      const icon = STATUS_ICON[r.status] || '•';
      const source =
        r.sourceLabel ||
        (r.source === 'both' ? 'Mirka + Yoast' : r.source === 'yoast' ? 'Yoast' : 'Mirka');
      const fix = r.fix
        ? `<p class="seo-item__fix"><span class="seo-item__fix-label">راهنما:</span> ${escapeHtml(r.fix)}</p>`
        : '';
      const highlightable = r.highlightable ? ' seo-item--highlightable' : '';
      return `
        <li class="seo-item ${cls}${highlightable}"
            data-seo-rule-id="${escapeHtml(r.id)}"
            ${r.highlightable ? 'data-seo-highlightable="1"' : ''}>
          <span class="seo-item__icon" aria-hidden="true">${icon}</span>
          <div class="seo-item__body">
            <div class="seo-item__title">
              ${escapeHtml(r.title)}
              <span class="seo-item__source">${escapeHtml(source)}</span>
            </div>
            <p class="seo-item__desc">${escapeHtml(r.description)}</p>
            ${fix}
            ${renderHighlightActions(r)}
          </div>
        </li>`;
    })
    .join('')}</ul>`;
}

/**
 * Update the server-rendered SEO panel without wiping form fields.
 * @param {HTMLElement} root
 * @param {{ results: any[], overall: {score:number,label:string,status:string}, fields: Record<string,string> }} payload
 */
export function renderPanel(root, payload) {
  if (!root) return;

  ensureActions(root);
  ensureSerpChrome(root);

  const results = payload.results || [];
  const overall = payload.overall || { score: 0, label: 'ضعیف', status: 'problem' };
  const fields = payload.fields || {};

  const scoreLabel = root.querySelector('[data-seo-score-label]');
  const scoreInput = root.querySelector('[data-seo-score-input]');
  const scoreWrap = root.querySelector('[data-seo-score]');
  const scoreRing = root.querySelector('[data-seo-score-ring]');
  const scoreCaption = ensureScoreCaption(scoreWrap);

  if (scoreLabel) scoreLabel.textContent = String(overall.score);
  if (scoreCaption) scoreCaption.textContent = overall.label || '';
  if (scoreInput) {
    scoreInput.value = String(overall.score);
    scoreInput.dispatchEvent(new Event('change', { bubbles: true }));
  }
  if (scoreWrap) {
    scoreWrap.classList.remove('seo-score--good', 'seo-score--warn', 'seo-score--error');
    if (overall.status === 'good') scoreWrap.classList.add('seo-score--good');
    else if (overall.status === 'needs_improvement') scoreWrap.classList.add('seo-score--warn');
    else scoreWrap.classList.add('seo-score--error');
    scoreWrap.setAttribute('data-seo-score-value', String(overall.score));
    scoreWrap.setAttribute('title', overall.label || '');
  }
  if (scoreRing) {
    const deg = Math.round((Number(overall.score) / 100) * 360);
    scoreRing.style.background = `conic-gradient(currentColor ${deg}deg, #ececec 0)`;
  }

  renderSummary(root, results);

  const title = truncate(fields.metaTitle || fields.title || 'عنوان صفحه', 60);
  const desc = truncate(fields.metaDescription || 'توضیحات متا اینجا نمایش داده می‌شود…', 160);
  let url = fields.canonical || '';
  if (!url && fields.slug) {
    const origin = typeof window !== 'undefined' ? window.location.origin : 'https://example.com';
    url = `${origin}/${String(fields.slug).replace(/^\//, '')}`;
  }
  if (!url) url = 'https://example.com/…';

  const serpUrl = root.querySelector('[data-seo-serp-url]');
  const serpTitle = root.querySelector('[data-seo-serp-title]');
  const serpDesc = root.querySelector('[data-seo-serp-desc]');
  if (serpUrl) serpUrl.textContent = url;
  if (serpTitle) serpTitle.textContent = title;
  if (serpDesc) serpDesc.textContent = desc;

  const byTab = {
    content: [],
    readability: [],
    social: [],
    advanced: [],
  };

  for (const r of results) {
    const cat = r.category || 'content';
    if (TAB_CATEGORIES.readability.has(cat)) byTab.readability.push(r);
    else if (TAB_CATEGORIES.social.has(cat)) byTab.social.push(r);
    else if (TAB_CATEGORIES.advanced.has(cat)) byTab.advanced.push(r);
    else byTab.content.push(r);
  }

  updateTabCounts(root, byTab);

  renderResultList(root.querySelector('[data-seo-results-content]'), byTab.content);
  renderResultList(root.querySelector('[data-seo-results-readability]'), byTab.readability);
  renderResultList(root.querySelector('[data-seo-results-social]'), byTab.social);
  renderResultList(root.querySelector('[data-seo-results-advanced]'), byTab.advanced);

  const generic = root.querySelector('[data-seo-results]:not([data-seo-results-content])');
  if (generic && !root.querySelector('[data-seo-results-content]')) {
    renderResultList(generic, results);
  }
}

/**
 * Show API action feedback message.
 */
export function setApiMessage(root, message, isError = false) {
  const el = root?.querySelector('[data-seo-api-msg]');
  if (!el) return;
  if (!message) {
    el.hidden = true;
    el.textContent = '';
    el.classList.remove('seo-api-msg--error', 'seo-api-msg--ok');
    return;
  }
  el.hidden = false;
  el.textContent = message;
  el.classList.toggle('seo-api-msg--error', isError);
  el.classList.toggle('seo-api-msg--ok', !isError);
}
