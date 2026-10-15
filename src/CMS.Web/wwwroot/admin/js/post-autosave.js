(function () {
  "use strict";

  const form = document.querySelector("form[data-post-autosave]");
  if (!form) return;

  const postId = form.getAttribute("data-post-id");
  const url = form.getAttribute("data-autosave-url");
  if (!postId || !url) return;

  const intervalMs = Number(form.getAttribute("data-autosave-interval") || "20000");
  const statusEl = form.querySelector("[data-post-autosave-status]");
  const draftTitle = form.getAttribute("data-draft-title") || "پیش‌نویس";
  const msgSaving = form.getAttribute("data-msg-saving") || "";
  const msgSaved = form.getAttribute("data-msg-saved") || "";
  const msgFailed = form.getAttribute("data-msg-failed") || "";
  const msgPaused = form.getAttribute("data-msg-paused") || "";

  const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
  const bodyTextareaEarly = form.querySelector("textarea.admin-ckeditor-source");
  // Snapshot server-rendered Body before CKEditor replaces the textarea.
  const loadedBodyHtml = bodyTextareaEarly
    ? String(
        bodyTextareaEarly.getAttribute("data-ck-initial-html") ||
          bodyTextareaEarly.value ||
          ""
      )
    : "";
  let userEditedBody = false;
  let lastPayload = "";
  let saving = false;
  let dirty = true;
  let hold = form.getAttribute("data-autosave-hold") === "true";
  let abortController = null;

  function isEffectivelyEmptyHtml(html) {
    return !String(html || "")
      .replace(/<p>(&nbsp;|\u00a0|\s)*<\/p>/gi, "")
      .replace(/&nbsp;|\u00a0/gi, "")
      .replace(/<br\s*\/?>/gi, "")
      .replace(/<[^>]+>/g, "")
      .trim();
  }

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
      const data = textarea.ckEditorInstance.getData();
      // Avoid persisting a blank body when the editor failed to hydrate loaded HTML.
      if (
        isEffectivelyEmptyHtml(data) &&
        !isEffectivelyEmptyHtml(loadedBodyHtml) &&
        !userEditedBody
      ) {
        return loadedBodyHtml;
      }
      return data;
    }
    const raw = fieldValue("Body") || "";
    if (
      isEffectivelyEmptyHtml(raw) &&
      !isEffectivelyEmptyHtml(loadedBodyHtml) &&
      !userEditedBody
    ) {
      return loadedBodyHtml;
    }
    return raw;
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

  function sourcesJsonValue() {
    const rows = Array.from(form.querySelectorAll("[data-scientific-sources-row]"));
    const items = [];
    for (const row of rows) {
      const title = (row.querySelector("[name$='.Title']")?.value || "").trim();
      const url = (row.querySelector("[name$='.Url']")?.value || "").trim();
      if (!title && !url) continue;
      if (!title || !url) continue;
      items.push({ title, url });
    }
    return items.length > 0 ? JSON.stringify(items) : null;
  }

  function videoSchemaJsonValue() {
    const rows = Array.from(form.querySelectorAll("[data-video-schema-row]"));
    const items = [];
    for (const row of rows) {
      const title = (row.querySelector("[name$='.Title']")?.value || "").trim();
      const contentUrl = (row.querySelector("[name$='.ContentUrl']")?.value || "").trim();
      const thumbnailUrl = (row.querySelector("[name$='.ThumbnailUrl']")?.value || "").trim() || null;
      const uploadDate = (row.querySelector("[name$='.UploadDate']")?.value || "").trim() || null;
      const durationMinutes = Number.parseInt(
        row.querySelector("[name$='.DurationMinutes']")?.value || "0",
        10
      );
      const durationSeconds = Number.parseInt(
        row.querySelector("[name$='.DurationSeconds']")?.value || "0",
        10
      );
      const description = (row.querySelector("[name$='.Description']")?.value || "").trim() || null;
      if (!title && !contentUrl && !thumbnailUrl && !uploadDate && !description
          && (!durationMinutes || durationMinutes === 0)
          && (!durationSeconds || durationSeconds === 0)) {
        continue;
      }
      if (!title || !contentUrl) continue;
      items.push({
        title,
        contentUrl,
        thumbnailUrl,
        uploadDate,
        durationMinutes: Number.isFinite(durationMinutes) ? durationMinutes : 0,
        durationSeconds: Number.isFinite(durationSeconds) ? durationSeconds : 0,
        description
      });
    }
    return items.length > 0 ? JSON.stringify(items) : null;
  }

  function contentGuideJsonValue() {
    const guideRoot = form.querySelector("[data-post-content-guide]");
    if (!guideRoot) return null;

    const enabled = !!guideRoot.querySelector("[data-content-guide-toggle]")?.checked;
    const title = (form.querySelector("[name='ContentGuideTitle']")?.value || "").trim() || null;
    const rows = Array.from(guideRoot.querySelectorAll("[data-content-guide-row]"));
    const items = [];
    for (const row of rows) {
      const itemTitle = (row.querySelector("[name$='.Title']")?.value || "").trim();
      const description = (row.querySelector("[name$='.Description']")?.value || "").trim();
      if (!itemTitle && !description) continue;
      if (!itemTitle || !description) continue;
      items.push({ title: itemTitle, description });
    }

    if (!enabled && !title && items.length === 0) return null;
    return JSON.stringify({ enabled, title, items });
  }

  function specialtyPathJsonValue() {
    const rows = Array.from(form.querySelectorAll("[data-specialty-path-row]"));
    const items = [];
    for (const row of rows) {
      const title = (row.querySelector("[name$='.Title']")?.value || "").trim();
      const text = (row.querySelector("[name$='.Text']")?.value || "").trim();
      const iconUrl = (row.querySelector("[name$='.IconUrl']")?.value || "").trim() || null;
      if (!title && !text && !iconUrl) continue;
      if (!title) continue;
      items.push({ title, text: text || "", iconUrl });
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

  function scientificActivityJsonValue() {
    const rows = Array.from(form.querySelectorAll("[data-scientific-activity-row]"));
    const items = [];
    for (const row of rows) {
      const text = (row.querySelector("[name$='.Text']")?.value || "").trim();
      if (!text) continue;
      items.push({ text });
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

  function eventInfoJsonValue() {
    const rows = Array.from(form.querySelectorAll("[data-event-info-item]"));
    const items = [];
    for (const row of rows) {
      const label = (row.querySelector("[data-event-info-label]")?.value || "").trim();
      const value = (row.querySelector("[data-event-info-value]")?.value || "").trim();
      if (!label || !value) continue;
      items.push({ label, value });
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
      scientificActivityJson: scientificActivityJsonValue(),
      coverImageUrl: (fieldValue("CoverImageUrl") || "").trim() || null,
      coverImageAlt: (fieldValue("CoverImageAlt") || "").trim() || null,
      avatarImageUrl: (fieldValue("AvatarImageUrl") || "").trim() || null,
      coverVideoUrl: (fieldValue("CoverVideoUrl") || "").trim() || null,
      videoUrl: (fieldValue("VideoUrl") || "").trim() || null,
      attachmentUrl: (fieldValue("AttachmentUrl") || "").trim() || null,
      attachmentFileName: (fieldValue("AttachmentFileName") || "").trim() || null,
      galleryJson: galleryJsonValue(),
      kind: kindValue(),
      categoryId,
      authorUserId: (fieldValue("AuthorUserId") || "").trim() || null,
      // Never touch publish state from autosave — only explicit Save may publish/unpublish.
      publish: null,
      publishedAtUtc: null,
      eventStartAtUtc: eventUtcValue("[data-autosave-event-start]"),
      eventEndAtUtc: eventUtcValue("[data-autosave-event-end]"),
      location: (fieldValue("Location") || "").trim() || null,
      eventInfoJson: eventInfoJsonValue(),
      metaTitle: (fieldValue("MetaTitle") || "").trim() || null,
      metaDescription: (fieldValue("MetaDescription") || "").trim() || null,
      seoKeywords: (fieldValue("SeoKeywords") || "").trim() || null,
      canonicalUrl: (fieldValue("CanonicalUrl") || "").trim() || null,
      ogTitle: (fieldValue("OgTitle") || "").trim() || null,
      ogDescription: (fieldValue("OgDescription") || "").trim() || null,
      ogImageUrl: (fieldValue("OgImageUrl") || "").trim() || null,
      faqJson: faqJsonValue(),
      sourcesJson: sourcesJsonValue(),
      videoSchemaJson: videoSchemaJsonValue(),
      contentGuideJson: contentGuideJsonValue(),
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

    // Only persist when the editor marked real edits (or force). Do not PUT solely
    // because a derived SEO score field changed in the payload snapshot.
    if (!force && !dirty) return;

    const payload = buildPayload();
    const serialized = JSON.stringify(payload);
    if (!force && serialized === lastPayload) {
      dirty = false;
      return;
    }

    saving = true;
    setStatus(msgSaving, false);
    abortController?.abort();
    abortController = new AbortController();
    const { signal } = abortController;

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
        signal,
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
      // Keep dirty if the form changed while this request was in flight
      // (e.g. video upload finished after the payload was snapshotted).
      const current = JSON.stringify(buildPayload());
      dirty = current !== serialized;
      const time = new Date().toLocaleTimeString("fa-IR", {
        hour: "2-digit",
        minute: "2-digit",
      });
      setStatus(`${msgSaved} — ${time}`, false);
      if (dirty) {
        window.setTimeout(function () {
          saveIfNeeded(false);
        }, 250);
      }
    } catch (err) {
      if (err?.name === "AbortError") return;
      setStatus(msgFailed, true);
    } finally {
      saving = false;
    }
  }

  form.addEventListener("submit", function () {
    hold = true;
    form.setAttribute("data-autosave-hold", "true");
    abortController?.abort();
  });

  function isAutosaveIgnoredTarget(target) {
    if (!(target instanceof Element)) return false;
    // SEO score is derived client-side; must not drive draft PUTs by itself.
    if (target.closest("[data-seo-score-input], [data-seo-score], [data-seo-summary], [data-seo-tabs]")) {
      return true;
    }
    return false;
  }

  function markDirty(ev) {
    if (ev && isAutosaveIgnoredTarget(ev.target)) return;
    dirty = true;
  }

  form.addEventListener("post-autosave:now", function () {
    dirty = true;
    saveIfNeeded(true);
  });

  form.addEventListener("input", markDirty);
  form.addEventListener("change", markDirty);

  const bodyTextarea = form.querySelector("textarea.admin-ckeditor-source");
  if (bodyTextarea) {
    function bindBodyEditor(editor) {
      if (!editor) return;
      // Re-hydrate if create() left the editable empty despite server HTML.
      if (
        !isEffectivelyEmptyHtml(loadedBodyHtml) &&
        isEffectivelyEmptyHtml(editor.getData())
      ) {
        try {
          editor.setData(loadedBodyHtml);
        } catch (err) {
          console.warn("[post-autosave] restore body failed", err);
        }
      }
      editor.model.document.on("change:data", function () {
        userEditedBody = true;
        markDirty();
      });
      dirty = true;
    }

    bodyTextarea.addEventListener("ckeditor:ready", function () {
      bindBodyEditor(bodyTextarea.ckEditorInstance);
    });
    if (bodyTextarea.ckEditorInstance) {
      bindBodyEditor(bodyTextarea.ckEditorInstance);
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
  }, Math.max(intervalMs, 10000));

  // First autosave after editor has had time to boot; avoid competing with CKEditor parse.
  window.setTimeout(function () {
    saveIfNeeded(true);
  }, 4000);
})();
