(function () {
  "use strict";

  const form = document.querySelector("form[data-post-autosave]");
  if (!form) return;

  const postId = form.getAttribute("data-post-id");
  const url = form.getAttribute("data-autosave-url");
  if (!postId || !url) return;

  const intervalMs = Number(form.getAttribute("data-autosave-interval") || "5000");
  const statusEl = form.querySelector("[data-post-autosave-status]");
  const draftTitle = form.getAttribute("data-draft-title") || "پیش‌نویس";
  const msgSaving = form.getAttribute("data-msg-saving") || "";
  const msgSaved = form.getAttribute("data-msg-saved") || "";
  const msgFailed = form.getAttribute("data-msg-failed") || "";
  const msgPaused = form.getAttribute("data-msg-paused") || "";

  const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
  let lastPayload = "";
  let saving = false;
  let dirty = true;
  let hold = form.getAttribute("data-autosave-hold") === "true";

  function token() {
    return (
      tokenInput?.value
      || document.querySelector('meta[name="request-verification-token"]')?.getAttribute("content")
      || ""
    );
  }

  function isHeld() {
    return hold || form.getAttribute("data-autosave-hold") === "true";
  }

  function setStatus(text, isError) {
    if (!statusEl) return;
    if (!text) {
      statusEl.hidden = true;
      statusEl.textContent = "";
      statusEl.classList.remove("is-error");
      return;
    }
    statusEl.hidden = false;
    statusEl.textContent = text;
    statusEl.classList.toggle("is-error", !!isError);
  }

  function fieldValue(name) {
    const el = form.elements.namedItem(name);
    if (!el) return null;
    if (el instanceof RadioNodeList) {
      const checked = Array.from(el).find((x) => x.checked);
      return checked ? checked.value : "";
    }
    if (el.type === "checkbox") return el.checked;
    return el.value;
  }

  function bodyValue() {
    const textarea = form.querySelector("textarea.admin-ckeditor-source");
    if (textarea?.ckEditorInstance) {
      return textarea.ckEditorInstance.getData();
    }
    return fieldValue("Body") || "";
  }

  function faqJsonValue() {
    const rows = Array.from(form.querySelectorAll("[data-faq-row]"));
    const items = [];
    for (const row of rows) {
      const question = (row.querySelector("[name$='.Question']")?.value || "").trim();
      const answer = (row.querySelector("[name$='.Answer']")?.value || "").trim();
      if (!question && !answer) continue;
      if (!question || !answer) continue;
      items.push({ question, answer });
    }
    return items.length > 0 ? JSON.stringify(items) : null;
  }

  function specialtyPathJsonValue() {
    const rows = Array.from(form.querySelectorAll("[data-specialty-path-row]"));
    const items = [];
    for (const row of rows) {
      const title = (row.querySelector("[name$='.Title']")?.value || "").trim();
      const text = (row.querySelector("[name$='.Text']")?.value || "").trim();
      if (!title && !text) continue;
      if (!title || !text) continue;
      items.push({ title, text });
    }
    return items.length > 0 ? JSON.stringify(items) : null;
  }

  function educationPathJsonValue() {
    const rows = Array.from(form.querySelectorAll("[data-education-path-row]"));
    const items = [];
    for (const row of rows) {
      const year = (row.querySelector("[name$='.Year']")?.value || "").trim();
      const title = (row.querySelector("[name$='.Title']")?.value || "").trim();
      const place = (row.querySelector("[name$='.Place']")?.value || "").trim();
      if (!year && !title && !place) continue;
      if (!year || !title || !place) continue;
      items.push({ year, title, place });
    }
    return items.length > 0 ? JSON.stringify(items) : null;
  }

  function galleryJsonValue() {
    const rows = Array.from(form.querySelectorAll("[data-article-gallery-item]"));
    const items = [];
    for (const row of rows) {
      const url = (row.querySelector("[data-gallery-url]")?.value || "").trim();
      if (!url) continue;
      const altText = (row.querySelector("[data-gallery-alt]")?.value || "").trim() || null;
      items.push({ url, altText });
    }
    return items.length > 0 ? JSON.stringify(items) : null;
  }

  function kindValue() {
    return fieldValue("Kind") || null;
  }

  function eventUtcValue(selector) {
    const el = form.querySelector(selector);
    const value = (el?.value || "").trim();
    return value || null;
  }

  function isPublishChecked() {
    const el = form.elements.namedItem("Publish");
    return !!(el && el.type === "checkbox" && el.checked);
  }

  function buildPayload() {
    const title = (fieldValue("Title") || "").trim();
    const categoryRaw = fieldValue("CategoryId");
    let categoryId = null;
    if (categoryRaw) {
      categoryId = categoryRaw;
    }

    return {
      title: title || draftTitle,
      subtitle: (fieldValue("Subtitle") || "").trim() || null,
      slug: (fieldValue("Slug") || "").trim() || null,
      body: bodyValue(),
      excerpt: (fieldValue("Excerpt") || "").trim() || null,
      highlights: (fieldValue("Highlights") || "").trim() || null,
      specialtyPathJson: specialtyPathJsonValue(),
      educationPathJson: educationPathJsonValue(),
      coverImageUrl: (fieldValue("CoverImageUrl") || "").trim() || null,
      avatarImageUrl: (fieldValue("AvatarImageUrl") || "").trim() || null,
      coverVideoUrl: (fieldValue("CoverVideoUrl") || "").trim() || null,
      galleryJson: galleryJsonValue(),
      kind: kindValue(),
      categoryId,
      authorUserId: (fieldValue("AuthorUserId") || "").trim() || null,
      publish: false,
      publishedAtUtc: null,
      eventStartAtUtc: eventUtcValue("[data-autosave-event-start]"),
      eventEndAtUtc: eventUtcValue("[data-autosave-event-end]"),
      location: (fieldValue("Location") || "").trim() || null,
      metaTitle: (fieldValue("MetaTitle") || "").trim() || null,
      metaDescription: (fieldValue("MetaDescription") || "").trim() || null,
      seoKeywords: (fieldValue("SeoKeywords") || "").trim() || null,
      canonicalUrl: (fieldValue("CanonicalUrl") || "").trim() || null,
      ogTitle: (fieldValue("OgTitle") || "").trim() || null,
      ogDescription: (fieldValue("OgDescription") || "").trim() || null,
      ogImageUrl: (fieldValue("OgImageUrl") || "").trim() || null,
      faqJson: faqJsonValue(),
      focusKeyword: seoFieldValue("FocusKeyword"),
      robotsIndex: seoCheckboxValue("RobotsIndex", true),
      robotsFollow: seoCheckboxValue("RobotsFollow", true),
      schemaType: seoFieldValue("SchemaType"),
      seoScore: seoScoreValue(),
    };
  }

  function seoPanel() {
    return form.querySelector("[data-seo-panel]");
  }

  function seoFieldValue(name) {
    const panel = seoPanel();
    if (!panel) return null;
    if (name === "FocusKeyword") {
      const input = panel.querySelector("[data-seo-focus-keyword]");
      return input ? (input.value || "").trim() || null : null;
    }
    if (name === "SchemaType") {
      const input = panel.querySelector("[data-seo-schema-type]");
      return input ? (input.value || "").trim() || null : null;
    }
    return null;
  }

  function seoCheckboxValue(name, defaultValue) {
    const panel = seoPanel();
    if (!panel) return defaultValue;
    const selector = name === "RobotsIndex" ? "[data-seo-robots-index]" : "[data-seo-robots-follow]";
    const input = panel.querySelector(selector);
    if (!input) return defaultValue;
    return !!input.checked;
  }

  function seoScoreValue() {
    const panel = seoPanel();
    if (!panel) return null;
    const input = panel.querySelector("[data-seo-score-input]");
    if (!input || !input.value) return null;
    const n = parseInt(input.value, 10);
    return Number.isFinite(n) ? n : null;
  }

  async function saveIfNeeded(force) {
    if (saving || isHeld()) return;
    if (isPublishChecked()) {
      setStatus(msgPaused, false);
      return;
    }

    const payload = buildPayload();
    const serialized = JSON.stringify(payload);
    if (!force && serialized === lastPayload && !dirty) return;

    saving = true;
    setStatus(msgSaving, false);

    try {
      const response = await fetch(url, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
          RequestVerificationToken: token(),
          "X-Requested-With": "XMLHttpRequest",
        },
        credentials: "same-origin",
        body: serialized,
      });

      if (!response.ok) {
        throw new Error("autosave failed");
      }

      const data = await response.json().catch(() => null);
      if (data?.slug) {
        const slugInput = form.elements.namedItem("Slug");
        if (
          slugInput &&
          document.activeElement !== slugInput &&
          slugInput.value !== data.slug
        ) {
          slugInput.value = data.slug;
        }
      }

      lastPayload = serialized;
      dirty = false;
      const time = new Date().toLocaleTimeString("fa-IR", {
        hour: "2-digit",
        minute: "2-digit",
      });
      setStatus(`${msgSaved} — ${time}`, false);
    } catch {
      setStatus(msgFailed, true);
    } finally {
      saving = false;
    }
  }

  function markDirty() {
    dirty = true;
  }

  form.addEventListener("input", markDirty);
  form.addEventListener("change", markDirty);

  const bodyTextarea = form.querySelector("textarea.admin-ckeditor-source");
  if (bodyTextarea) {
    bodyTextarea.addEventListener("ckeditor:ready", function () {
      const editor = bodyTextarea.ckEditorInstance;
      if (!editor) return;
      editor.model.document.on("change:data", markDirty);
      dirty = true;
    });
    if (bodyTextarea.ckEditorInstance) {
      bodyTextarea.ckEditorInstance.model.document.on("change:data", markDirty);
    }
  }

  form.addEventListener("post-autosave:resume", function () {
    hold = false;
    form.removeAttribute("data-autosave-hold");
    dirty = true;
    saveIfNeeded(true);
  });

  window.setInterval(function () {
    saveIfNeeded(false);
  }, Math.max(intervalMs, 2000));

  // First autosave shortly after load so the draft reflects opened editor state.
  window.setTimeout(function () {
    saveIfNeeded(true);
  }, 1200);
})();
