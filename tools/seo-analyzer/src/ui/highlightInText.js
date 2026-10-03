import { findNormalizedMatches } from '../highlights/normalizeWithMap.js';

const MARKER_GROUP = 'mirkaSeoHl';
const FIELD_CLASS = 'seo-field-is-highlighted';
const FIELD_FLASH = 'seo-field-flash';
const ACTIVE_ITEM = 'seo-item--highlight-active';
const CONVERTED = '__mirkaSeoHlReady';

/**
 * Create a Yoast-like highlight controller bound to a SEO panel root.
 * @param {HTMLElement} root
 */
export function createHighlightController(root) {
  /** @type {{ ruleId: string, highlights: any[], index: number } | null} */
  let session = null;
  /** @type {HTMLElement | null} */
  let toolbarEl = null;
  /** @type {Set<HTMLElement>} */
  const highlightedFields = new Set();

  function getForm() {
    return root.closest('form') || document;
  }

  function getBodyEditor() {
    const form = getForm();
    const bodyEl =
      form.querySelector?.('textarea.admin-ckeditor-source') ||
      document.querySelector('textarea.admin-ckeditor-source');
    if (!bodyEl) return { bodyEl: null, editor: null };
    const editor =
      bodyEl.ckEditorInstance ||
      (bodyEl.id && window.MirkaCkEditorInstances?.[bodyEl.id]) ||
      null;
    return { bodyEl, editor };
  }

  function fieldSelectors(field) {
    const map = {
      title: ['[name="Title"]', '#Title', '[data-seo-title]'],
      slug: ['[name="Slug"]', '#Slug', '[data-seo-slug]'],
      metaTitle: [
        '[name="MetaTitle"]',
        '[name="Seo.MetaTitle"]',
        '#MetaTitle',
        '[data-seo-meta-title]',
      ],
      metaDescription: [
        '[name="MetaDescription"]',
        '[name="Seo.MetaDescription"]',
        '#MetaDescription',
        '[data-seo-meta-description]',
      ],
      focusKeyword: [
        '[name="Seo.FocusKeyword"]',
        '[name="FocusKeyword"]',
        '[data-seo-focus-keyword]',
        '#FocusKeyword',
      ],
      canonical: [
        '[name="CanonicalUrl"]',
        '[name="Seo.CanonicalUrl"]',
        '[name="Canonical"]',
        '[data-seo-canonical]',
      ],
      ogTitle: ['[name="OgTitle"]', '[name="Seo.OgTitle"]', '[data-seo-og-title]'],
      ogDescription: [
        '[name="OgDescription"]',
        '[name="Seo.OgDescription"]',
        '[data-seo-og-description]',
      ],
      ogImage: [
        '[name="OgImageUrl"]',
        '[name="OgImage"]',
        '[name="Seo.OgImage"]',
        '#OgImageUrl',
        '[data-seo-og-image]',
      ],
    };
    return map[field] || [];
  }

  function resolveFieldEl(field) {
    const form = getForm();
    for (const sel of fieldSelectors(field)) {
      const el = form.querySelector?.(sel);
      if (el) return el;
    }
    return null;
  }

  function ensureConversion(editor) {
    if (!editor || editor[CONVERTED]) return;
    try {
      editor.conversion.for('editingDowncast').markerToHighlight({
        model: MARKER_GROUP,
        view: {
          classes: 'mirka-seo-hl',
        },
      });
      editor[CONVERTED] = true;
    } catch (err) {
      console.warn('[MirkaSeoAnalysis] highlight conversion failed', err);
    }
  }

  function clearBodyMarkers(editor) {
    if (!editor?.model?.markers) return;
    const toRemove = [];
    for (const marker of editor.model.markers) {
      if (marker.name.startsWith(`${MARKER_GROUP}:`)) {
        toRemove.push(marker.name);
      }
    }
    if (toRemove.length) {
      editor.model.change((writer) => {
        for (const name of toRemove) {
          try {
            writer.removeMarker(name);
          } catch {
            /* ignore */
          }
        }
      });
    }
    try {
      const editable = editor.ui?.getEditableElement?.();
      editable?.querySelectorAll?.('.mirka-seo-img-hl').forEach((el) => {
        el.classList.remove('mirka-seo-img-hl');
      });
    } catch {
      /* ignore */
    }
  }

  function clearFieldHighlights() {
    highlightedFields.forEach((el) => {
      el.classList.remove(FIELD_CLASS, FIELD_FLASH);
    });
    highlightedFields.clear();
  }

  function clearActiveItem() {
    root.querySelectorAll(`.${ACTIVE_ITEM}`).forEach((el) => {
      el.classList.remove(ACTIVE_ITEM);
    });
  }

  function ensureToolbar() {
    if (toolbarEl) return toolbarEl;
    toolbarEl = document.createElement('div');
    toolbarEl.className = 'seo-highlight-toolbar';
    toolbarEl.setAttribute('data-seo-highlight-toolbar', '');
    toolbarEl.hidden = true;
    toolbarEl.innerHTML = `
      <span class="seo-highlight-toolbar__label" data-seo-hl-label>هایلایت در متن</span>
      <span class="seo-highlight-toolbar__count" data-seo-hl-count></span>
      <div class="seo-highlight-toolbar__nav">
        <button type="button" class="seo-highlight-toolbar__btn" data-seo-hl-prev title="قبلی" aria-label="هایلایت قبلی">‹</button>
        <button type="button" class="seo-highlight-toolbar__btn" data-seo-hl-next title="بعدی" aria-label="هایلایت بعدی">›</button>
        <button type="button" class="seo-highlight-toolbar__btn seo-highlight-toolbar__btn--close" data-seo-hl-close title="بستن" aria-label="بستن هایلایت">×</button>
      </div>
    `;
    const header = root.querySelector('.admin-seo-panel__header') || root;
    header.after(toolbarEl);

    toolbarEl.addEventListener('click', (ev) => {
      const t = ev.target;
      if (!(t instanceof Element)) return;
      if (t.closest('[data-seo-hl-prev]')) {
        ev.preventDefault();
        navigate(-1);
      } else if (t.closest('[data-seo-hl-next]')) {
        ev.preventDefault();
        navigate(1);
      } else if (t.closest('[data-seo-hl-close]')) {
        ev.preventDefault();
        clear();
      }
    });

    return toolbarEl;
  }

  function updateToolbar() {
    const bar = ensureToolbar();
    if (!session || !session.highlights.length) {
      bar.hidden = true;
      return;
    }
    bar.hidden = false;
    const count = bar.querySelector('[data-seo-hl-count]');
    if (count) {
      count.textContent = `${session.index + 1} از ${session.highlights.length}`;
    }
  }

  /**
   * Collect model text + per-character positions.
   */
  function collectModelText(editor) {
    const model = editor.model;
    const rootEl = model.document.getRoot();
    if (!rootEl) return { text: '', positions: [] };

    let text = '';
    /** @type {any[]} */
    const positions = [];
    const range = model.createRangeIn(rootEl);

    for (const item of range.getItems()) {
      if (!item.is || !item.is('$textProxy')) continue;
      const data = item.data || '';
      const parent = item.parent;
      const startOffset = item.startOffset;
      if (parent == null || startOffset == null) continue;

      for (let i = 0; i < data.length; i++) {
        positions.push(model.createPositionAt(parent, startOffset + i));
        text += data[i];
      }
    }

    return { text, positions };
  }

  function highlightMissingImages(editor, highlights, focusIndex) {
    const editable = editor.ui?.getEditableElement?.();
    if (!editable) return false;

    // Clear previous image outline classes
    editable.querySelectorAll('.mirka-seo-img-hl').forEach((el) => {
      el.classList.remove('mirka-seo-img-hl');
    });

    const imgHighlights = highlights
      .map((h, idx) => ({ h, idx }))
      .filter(({ h }) => h.kind === 'image-alt');
    if (!imgHighlights.length) return false;

    const imgs = Array.from(editable.querySelectorAll('img'));
    const missing = imgs.filter((img) => !String(img.getAttribute('alt') || '').trim());
    if (!missing.length) return false;

    missing.forEach((img) => img.classList.add('mirka-seo-img-hl'));

    const focusHl = highlights[focusIndex];
    let target = missing[0];
    if (focusHl && focusHl.kind === 'image-alt') {
      const bySrc = missing.find((img) => {
        const src = img.getAttribute('src') || '';
        return src && focusHl.text && src.includes(String(focusHl.text).slice(0, 40));
      });
      target = bySrc || missing[Math.min(focusHl.occurrence || 0, missing.length - 1)] || missing[0];
    }

    if (target) {
      target.scrollIntoView({ behavior: 'smooth', block: 'center' });
      try {
        editor.editing.view.focus();
      } catch {
        /* ignore */
      }
    }
    return true;
  }

  function applyBodyHighlights(editor, highlights, focusIndex) {
    ensureConversion(editor);
    clearBodyMarkers(editor);

    const current = highlights[focusIndex];
    if (current?.kind === 'image-alt' || highlights.some((h) => h.kind === 'image-alt')) {
      const handled = highlightMissingImages(editor, highlights, focusIndex);
      if (handled && current?.kind === 'image-alt') return;
    }

    const bodyItems = highlights
      .map((h, idx) => ({ h, idx }))
      .filter(({ h }) => h.field === 'body' && h.kind !== 'image-alt');

    if (!bodyItems.length) return;

    const { text, positions } = collectModelText(editor);
    if (!text || !positions.length) return;

    /** Track how many times we've used each search text */
    const usedCount = new Map();
    /** @type {{ name: string, range: any, highlightIndex: number }[]} */
    const placed = [];

    editor.model.change((writer) => {
      for (const { h, idx } of bodyItems) {
        const needle = String(h.text || '').trim();
        if (!needle) continue;

        const matches = findNormalizedMatches(text, needle, 40);
        if (!matches.length) continue;

        const key = `${h.kind}|${needle}`;
        const occ = typeof h.occurrence === 'number' ? h.occurrence : usedCount.get(key) || 0;
        usedCount.set(key, occ + 1);
        const match = matches[Math.min(occ, matches.length - 1)] || matches[0];
        if (!match) continue;

        const startPos = positions[match.start];
        const endPos = positions[Math.min(match.end, positions.length) - 1];
        if (!startPos || !endPos) continue;

        try {
          const endExclusive = endPos.getShiftedBy
            ? endPos.getShiftedBy(1)
            : editor.model.createPositionAt(endPos.parent, endPos.offset + 1);
          const range = writer.createRange(startPos, endExclusive);
          const name = `${MARKER_GROUP}:${placed.length}`;
          writer.addMarker(name, {
            range,
            usingOperation: false,
            affectsData: false,
          });
          placed.push({ name, range, highlightIndex: idx });
        } catch (err) {
          console.warn('[MirkaSeoAnalysis] marker place failed', err);
        }
      }
    });

    // Scroll focused marker into view
    const focus = placed.find((p) => p.highlightIndex === focusIndex) || placed[0];
    if (focus) {
      try {
        editor.editing.view.focus();
        editor.model.change((writer) => {
          writer.setSelection(focus.range);
        });
        editor.editing.view.scrollToTheSelection?.();
        const editable = editor.ui.getEditableElement?.();
        if (editable) {
          const sel = window.getSelection?.();
          if (sel && sel.rangeCount) {
            const rect = sel.getRangeAt(0).getBoundingClientRect();
            if (rect && (rect.top < 80 || rect.bottom > window.innerHeight - 40)) {
              const absoluteTop = window.scrollY + rect.top - window.innerHeight * 0.35;
              window.scrollTo({ top: Math.max(0, absoluteTop), behavior: 'smooth' });
            }
          } else {
            editable.scrollIntoView({ behavior: 'smooth', block: 'center' });
          }
        }
      } catch {
        /* ignore scroll errors */
      }
    }
  }

  function flashField(el) {
    if (!el) return;
    el.classList.add(FIELD_CLASS, FIELD_FLASH);
    highlightedFields.add(el);
    try {
      el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      if (typeof el.focus === 'function') el.focus({ preventScroll: true });
    } catch {
      try {
        el.focus();
      } catch {
        /* ignore */
      }
    }
    window.setTimeout(() => {
      el.classList.remove(FIELD_FLASH);
    }, 1200);
  }

  function applyFieldHighlight(highlight) {
    if (!highlight || highlight.field === 'body') return;
    const el = resolveFieldEl(highlight.field);
    if (el) flashField(el);
  }

  function markListItem(ruleId) {
    clearActiveItem();
    const safe =
      typeof CSS !== 'undefined' && typeof CSS.escape === 'function'
        ? CSS.escape(ruleId)
        : String(ruleId).replace(/["\\]/g, '\\$&');
    const item = root.querySelector(`.seo-item[data-seo-rule-id="${safe}"]`);
    if (item) item.classList.add(ACTIVE_ITEM);
  }

  function applyCurrent() {
    if (!session || !session.highlights.length) return;
    const { editor } = getBodyEditor();
    const current = session.highlights[session.index];

    clearFieldHighlights();

    if (editor) {
      applyBodyHighlights(editor, session.highlights, session.index);
    }

    if (current && current.field !== 'body') {
      applyFieldHighlight(current);
    } else if (current && current.field === 'body' && !editor) {
      // Fallback: scroll textarea
      const { bodyEl } = getBodyEditor();
      if (bodyEl) flashField(bodyEl);
    }

    markListItem(session.ruleId);
    updateToolbar();
  }

  function navigate(delta) {
    if (!session || !session.highlights.length) return;
    const n = session.highlights.length;
    session.index = (session.index + delta + n) % n;
    applyCurrent();
  }

  /**
   * Start highlighting for a rule.
   * @param {string} ruleId
   * @param {any[]} highlights
   * @param {number} [startIndex]
   */
  function show(ruleId, highlights, startIndex = 0) {
    const list = Array.isArray(highlights) ? highlights.filter((h) => h && (h.text || h.field)) : [];
    if (!list.length) {
      // Still try to focus a sensible field for empty-text field highlights
      const fieldOnly = Array.isArray(highlights)
        ? highlights.filter((h) => h && h.field && h.field !== 'body')
        : [];
      if (fieldOnly.length) {
        session = { ruleId, highlights: fieldOnly, index: 0 };
        applyCurrent();
        return;
      }
      clear();
      return;
    }

    session = {
      ruleId,
      highlights: list,
      index: Math.max(0, Math.min(startIndex, list.length - 1)),
    };
    applyCurrent();
  }

  function clear() {
    const { editor } = getBodyEditor();
    if (editor) clearBodyMarkers(editor);
    clearFieldHighlights();
    clearActiveItem();
    session = null;
    if (toolbarEl) toolbarEl.hidden = true;
  }

  /**
   * Re-apply active session after content re-analysis (updated highlight list).
   * @param {any[]} results
   */
  function syncFromResults(results) {
    if (!session) return;
    const rule = (results || []).find((r) => r && r.id === session.ruleId);
    if (!rule || !rule.highlightable || !rule.highlights?.length) {
      clear();
      return;
    }
    const prevIndex = session.index;
    session.highlights = rule.highlights;
    session.index = Math.min(prevIndex, rule.highlights.length - 1);
    applyCurrent();
  }

  function getSession() {
    return session;
  }

  return {
    show,
    clear,
    navigate,
    syncFromResults,
    getSession,
    destroy() {
      clear();
      toolbarEl?.remove();
      toolbarEl = null;
    },
  };
}
