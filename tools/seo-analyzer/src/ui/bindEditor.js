import { debounce } from '../utils/debounce.js';
import { setApiMessage } from './renderPanel.js';

function q(form, selectors) {
  for (const sel of selectors) {
    const el = form.querySelector(sel);
    if (el) return el;
  }
  return null;
}

function fieldValue(el) {
  if (!el) return '';
  if (el.type === 'checkbox') return el.checked;
  return el.value != null ? String(el.value) : '';
}

function readCheckbox(form, dataSelector, names) {
  if (dataSelector) {
    const byData = form.querySelector(dataSelector);
    if (byData && byData.type === 'checkbox') return !!byData.checked;
  }
  // Prefer real checkbox — ASP.NET renders a hidden "false" sibling first
  for (const name of names) {
    const el = form.querySelector(`input[type="checkbox"][name="${name}"]`);
    if (el) return !!el.checked;
  }
  return null;
}

/**
 * Collect field values from the closest form around the SEO panel.
 */
export function collectFields(root) {
  const form = root.closest('form') || document.querySelector('form') || document;

  const titleEl = q(form, ['[name="Title"]', '#Title', '[data-seo-title]']);
  const slugEl = q(form, ['[name="Slug"]', '#Slug', '[data-seo-slug]']);
  const metaTitleEl = q(form, [
    '[name="MetaTitle"]',
    '[name="Seo.MetaTitle"]',
    '#MetaTitle',
    '[data-seo-meta-title]',
  ]);
  const metaDescEl = q(form, [
    '[name="MetaDescription"]',
    '[name="Seo.MetaDescription"]',
    '#MetaDescription',
    '[data-seo-meta-description]',
  ]);
  const canonicalEl = q(form, [
    '[name="CanonicalUrl"]',
    '[name="Seo.CanonicalUrl"]',
    '[name="Canonical"]',
    '[data-seo-canonical]',
  ]);
  const ogTitleEl = q(form, [
    '[name="OgTitle"]',
    '[name="Seo.OgTitle"]',
    '[data-seo-og-title]',
  ]);
  const ogDescEl = q(form, [
    '[name="OgDescription"]',
    '[name="Seo.OgDescription"]',
    '[data-seo-og-description]',
  ]);
  const ogImageEl = q(form, [
    '[name="OgImageUrl"]',
    '[name="OgImage"]',
    '[name="Seo.OgImage"]',
    '#OgImageUrl',
    '[data-seo-og-image]',
  ]);
  const focusEl = q(form, [
    '[name="Seo.FocusKeyword"]',
    '[name="FocusKeyword"]',
    '[data-seo-focus-keyword]',
    '#FocusKeyword',
  ]);

  const bodyEl =
    q(form, ['textarea.admin-ckeditor-source', '[data-seo-body]', '[name="Body"]', '#Body']) ||
    null;

  let bodyHtml = fieldValue(bodyEl);
  if (bodyEl && bodyEl.ckEditorInstance && typeof bodyEl.ckEditorInstance.getData === 'function') {
    try {
      bodyHtml = bodyEl.ckEditorInstance.getData() || bodyHtml;
    } catch {
      /* keep textarea */
    }
  } else if (typeof window !== 'undefined' && window.MirkaCkEditorInstances) {
    // optional global map
    const id = bodyEl && bodyEl.id;
    const ed = id && window.MirkaCkEditorInstances[id];
    if (ed && typeof ed.getData === 'function') {
      try {
        bodyHtml = ed.getData() || bodyHtml;
      } catch {
        /* ignore */
      }
    }
  }

  return {
    title: String(fieldValue(titleEl) || ''),
    slug: String(fieldValue(slugEl) || ''),
    bodyHtml: String(bodyHtml || ''),
    metaTitle: String(fieldValue(metaTitleEl) || ''),
    metaDescription: String(fieldValue(metaDescEl) || ''),
    canonical: String(fieldValue(canonicalEl) || ''),
    ogTitle: String(fieldValue(ogTitleEl) || ''),
    ogDescription: String(fieldValue(ogDescEl) || ''),
    ogImage: String(fieldValue(ogImageEl) || ''),
    focusKeyword: String(fieldValue(focusEl) || ''),
    robotsIndex: readCheckbox(form, '[data-seo-robots-index]', [
      'Seo.RobotsIndex',
      'RobotsIndex',
    ]),
    robotsFollow: readCheckbox(form, '[data-seo-robots-follow]', [
      'Seo.RobotsFollow',
      'RobotsFollow',
    ]),
    _els: {
      form,
      titleEl,
      slugEl,
      bodyEl,
      metaTitleEl,
      metaDescEl,
      canonicalEl,
      ogTitleEl,
      ogDescEl,
      ogImageEl,
      focusEl,
    },
  };
}

function getAntiforgeryToken(form) {
  const fromMeta = document.querySelector('meta[name="request-verification-token"]')?.content;
  if (fromMeta) return fromMeta;
  const input =
    form?.querySelector('input[name="__RequestVerificationToken"]') ||
    document.querySelector('input[name="__RequestVerificationToken"]');
  return input?.value || '';
}

/**
 * Extract absolute/relative hrefs from HTML for broken-link check.
 */
export function extractUrls(html) {
  const urls = [];
  const re = /href=["']([^"']+)["']/gi;
  let m;
  while ((m = re.exec(String(html || ''))) ) {
    const href = m[1].trim();
    if (!href || href.startsWith('#') || /^mailto:|^tel:|^javascript:/i.test(href)) continue;
    urls.push(href);
  }
  return [...new Set(urls)];
}

async function postJson(url, body, form) {
  const token = getAntiforgeryToken(form);
  const headers = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    'X-Requested-With': 'XMLHttpRequest',
  };
  if (token) headers.RequestVerificationToken = token;

  const res = await fetch(url, {
    method: 'POST',
    headers,
    credentials: 'same-origin',
    body: JSON.stringify(body),
  });

  let data = null;
  const text = await res.text();
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = { raw: text };
  }

  if (!res.ok) {
    const msg =
      (data && (data.message || data.title || data.error)) ||
      `خطای سرور (${res.status})`;
    const err = new Error(msg);
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}

/**
 * Bind live analysis + API action buttons.
 * @param {HTMLElement} root
 * @param {(fields: object) => void} onAnalyze
 */
export function bindEditor(root, onAnalyze) {
  const run = () => {
    const fields = collectFields(root);
    onAnalyze(fields);
  };

  const debounced = debounce(run, 450);
  const form = root.closest('form') || document;

  form.addEventListener('input', debounced);
  form.addEventListener('change', debounced);

  // CKEditor change:data (+ ready re-analyze so body is not empty)
  const bodyEl = form.querySelector('textarea.admin-ckeditor-source');
  const tryBindCk = () => {
    const editor = bodyEl && bodyEl.ckEditorInstance;
    if (editor && editor.model && editor.model.document) {
      editor.model.document.on('change:data', debounced);
      debounced.flush();
      return true;
    }
    return false;
  };
  if (bodyEl) {
    bodyEl.addEventListener('ckeditor:ready', () => {
      tryBindCk();
      debounced.flush();
    });
  }
  if (!tryBindCk()) {
    let attempts = 0;
    const iv = setInterval(() => {
      attempts += 1;
      if (tryBindCk() || attempts > 60) clearInterval(iv);
    }, 250);
  }

  // Action: suggest internal links
  root.addEventListener('click', async (ev) => {
    const suggestBtn = ev.target.closest('[data-seo-suggest-links]');
    const checkBtn = ev.target.closest('[data-seo-check-links]');
    if (!suggestBtn && !checkBtn) return;
    ev.preventDefault();

    const fields = collectFields(root);
    const formEl = fields._els.form;

    const apiBase =
      root.getAttribute('data-seo-api-base') || '/api/v1/admin/seo';
    const contentType = root.getAttribute('data-seo-content-type') || null;
    const contentIdRaw = root.getAttribute('data-seo-content-id') || '';
    const excludeId = contentIdRaw && /^[0-9a-f-]{36}$/i.test(contentIdRaw)
      ? contentIdRaw
      : null;

    if (suggestBtn) {
      setApiMessage(root, 'در حال دریافت پیشنهاد لینک…');
      try {
        const data = await postJson(
          `${apiBase}/tools/suggest-links`,
          {
            keyword: fields.focusKeyword || fields.title || '',
            excludeContentType: contentType,
            excludeId,
            take: 10,
          },
          formEl
        );
        const items = Array.isArray(data) ? data : data?.suggestions || data?.items || [];
        if (Array.isArray(items) && items.length) {
          const lines = items
            .slice(0, 8)
            .map((it) => {
              if (typeof it === 'string') return it;
              const title = it.title || it.Title || '';
              const path = it.relativePath || it.RelativePath || it.url || '';
              return path ? `${title} (${path})` : title || JSON.stringify(it);
            })
            .join(' · ');
          setApiMessage(root, `پیشنهادها: ${lines}`, false);
        } else {
          setApiMessage(root, 'پیشنهادی دریافت نشد. کلمه کلیدی یا عنوان را تکمیل کنید.', false);
        }
      } catch (err) {
        setApiMessage(
          root,
          `پیشنهاد لینک در دسترس نیست: ${err.message || 'خطای شبکه'}`,
          true
        );
      }
      return;
    }

    if (checkBtn) {
      setApiMessage(root, 'در حال بررسی لینک‌ها…');
      try {
        const urls = extractUrls(fields.bodyHtml);
        if (!urls.length) {
          setApiMessage(root, 'لینکی در محتوا یافت نشد.', false);
          return;
        }
        const data = await postJson(
          `${apiBase}/tools/check-links`,
          { urls },
          formEl
        );
        const list = Array.isArray(data) ? data : data?.results || [];
        const broken = list.filter((b) => b && (b.isOk === false || b.IsOk === false));
        if (broken.length) {
          setApiMessage(
            root,
            `لینک‌های مشکل‌دار: ${broken
              .map((b) => b.url || b.Url || b)
              .join('، ')}`,
            true
          );
        } else {
          setApiMessage(
            root,
            `بررسی انجام شد (${urls.length} لینک). مورد شکسته‌ای گزارش نشد.`,
            false
          );
        }
      } catch (err) {
        setApiMessage(
          root,
          `بررسی لینک در دسترس نیست: ${err.message || 'خطای شبکه'}`,
          true
        );
      }
    }
  });

  // Initial run
  run();

  return {
    refresh: run,
    refreshDebounced: debounced,
    destroy() {
      debounced.cancel();
      form.removeEventListener('input', debounced);
      form.removeEventListener('change', debounced);
    },
  };
}
