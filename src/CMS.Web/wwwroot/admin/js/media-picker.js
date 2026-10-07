(function () {
  const modal = document.getElementById("media-library-picker");
  if (!modal) return;

  const PAGE_SIZE = 24;
  const SCROLL_THRESHOLD_PX = 96;

  const grid = modal.querySelector("[data-media-picker-grid]");
  const empty = modal.querySelector("[data-media-picker-empty]");
  const loading = modal.querySelector("[data-media-picker-loading]");
  const loadingMore = modal.querySelector("[data-media-picker-more]");
  const uploadForm = modal.querySelector("[data-media-picker-upload-form]");
  const uploadError = modal.querySelector("[data-media-picker-upload-error]");
  const uploadSubmit = modal.querySelector("[data-media-picker-upload-submit]");
  const uploadStatus = modal.querySelector("[data-media-picker-upload-status]");
  const uploadProgress = modal.querySelector("[data-media-picker-upload-progress]");
  const uploadIcon = uploadSubmit?.querySelector("[data-upload-icon]");
  const uploadLabel = uploadSubmit?.querySelector("[data-upload-label]");
  const fileInput = uploadForm?.querySelector('input[name="file"]');
  const tabButtons = modal.querySelectorAll("[data-media-picker-tab]");
  const panels = modal.querySelectorAll("[data-media-picker-panel]");
  const uploadingText = modal.getAttribute("data-uploading-label") || "در حال آپلود…";
  const idleUploadText =
    modal.getAttribute("data-upload-label") || uploadLabel?.textContent || "آپلود و انتخاب";
  const clearLabel = modal.getAttribute("data-clear-label") || "حذف تصویر";
  const editLabel = modal.getAttribute("data-edit-label") || "ویرایش تصویر";
  const editMissingLabel =
    modal.getAttribute("data-edit-missing-label") ||
    "این تصویر در کتابخانه رسانه یافت نشد و قابل ویرایش نیست.";
  const emptyDefaultText = empty?.textContent || "کتابخانه خالی است.";

  let targetSelector = null;
  let onSelectCallback = null;
  let uploading = false;
  let nextPage = 1;
  let hasNext = false;
  let loadingPage = false;

  function token() {
    return (
      uploadForm?.querySelector('input[name="__RequestVerificationToken"]')?.value ||
      document.querySelector('meta[name="request-verification-token"]')?.getAttribute("content") ||
      ""
    );
  }

  function showUploadError(message) {
    if (!uploadError) return;
    uploadError.hidden = false;
    uploadError.textContent = message || "آپلود ناموفق بود.";
  }

  function clearUploadError() {
    if (!uploadError) return;
    uploadError.hidden = true;
    uploadError.textContent = "";
  }

  function setUploading(isUploading) {
    uploading = isUploading;
    uploadForm?.classList.toggle("is-uploading", isUploading);
    uploadForm?.setAttribute("aria-busy", isUploading ? "true" : "false");

    if (uploadSubmit) {
      uploadSubmit.disabled = isUploading;
      uploadSubmit.classList.toggle("is-loading", isUploading);
    }

    if (uploadIcon) {
      uploadIcon.classList.toggle("fa-upload", !isUploading);
      uploadIcon.classList.toggle("fa-spinner", isUploading);
      uploadIcon.classList.toggle("fa-spin", isUploading);
    }

    if (uploadLabel) {
      uploadLabel.textContent = isUploading ? uploadingText : idleUploadText;
    }

    if (uploadStatus) uploadStatus.hidden = !isUploading;
    if (uploadProgress) {
      uploadProgress.hidden = !isUploading;
      uploadProgress.setAttribute("aria-hidden", isUploading ? "false" : "true");
    }

    // Keep the file input enabled — disabling it clears the chosen file in some browsers.
    uploadForm?.querySelectorAll("input, button").forEach((el) => {
      if (el === uploadSubmit) return;
      if (el === fileInput) return;
      if (el.name === "__RequestVerificationToken") return;
      if (el.type === "file") return;
      el.disabled = isUploading;
    });
  }

  function resetPaging() {
    nextPage = 1;
    hasNext = false;
    loadingPage = false;
    if (grid) grid.innerHTML = "";
    if (empty) {
      empty.hidden = true;
      empty.textContent = emptyDefaultText;
    }
    if (loadingMore) loadingMore.hidden = true;
  }

  function resetState() {
    targetSelector = null;
    onSelectCallback = null;
    setTab("library");
    if (uploadForm) uploadForm.reset();
    clearUploadError();
    setUploading(false);
    resetPaging();
  }

  function close() {
    if (uploading) return;
    modal.hidden = true;
    resetState();
  }

  function setTab(name) {
    if (uploading && name !== "upload") return;
    tabButtons.forEach((btn) => {
      btn.classList.toggle("is-active", btn.getAttribute("data-media-picker-tab") === name);
      btn.disabled = uploading && btn.getAttribute("data-media-picker-tab") !== "upload";
    });
    panels.forEach((panel) => {
      panel.hidden = panel.getAttribute("data-media-picker-panel") !== name;
    });
  }

  function findPreview(selector) {
    return document.querySelector(`[data-media-preview-for="${CSS.escape(selector)}"]`);
  }

  function findClearButtons(selector) {
    return document.querySelectorAll(
      `[data-media-clear][data-target="${CSS.escape(selector)}"]`
    );
  }

  function findEditButtons(selector) {
    return document.querySelectorAll(
      `[data-media-edit][data-target="${CSS.escape(selector)}"]`
    );
  }

  function hasValue(selector) {
    const input = document.querySelector(selector);
    return !!(input && String(input.value || "").trim());
  }

  function setMediaId(input, id) {
    if (!input) return;
    if (id) input.dataset.mediaId = String(id);
    else delete input.dataset.mediaId;
  }

  function syncActionButtons(selector) {
    const visible = hasValue(selector);
    findClearButtons(selector).forEach((btn) => {
      btn.hidden = !visible;
    });
    findEditButtons(selector).forEach((btn) => {
      btn.hidden = !visible;
    });
  }

  function syncClearButton(selector) {
    syncActionButtons(selector);
  }

  function findAltTarget(selector) {
    if (!selector) return null;
    const pickerBtn = document.querySelector(
      `[data-media-picker][data-target="${CSS.escape(selector)}"]`
    );
    const altSelector = pickerBtn?.getAttribute("data-alt-target");
    if (!altSelector) return null;
    return document.querySelector(altSelector);
  }

  function clearTarget(selector) {
    if (!selector) return;

    const input = document.querySelector(selector);
    if (input) {
      input.value = "";
      setMediaId(input, null);
      input.dispatchEvent(new Event("input", { bubbles: true }));
      input.dispatchEvent(new Event("change", { bubbles: true }));
    }

    const altInput = findAltTarget(selector);
    if (altInput) {
      altInput.value = "";
      altInput.dispatchEvent(new Event("input", { bubbles: true }));
      altInput.dispatchEvent(new Event("change", { bubbles: true }));
    }

    const preview = findPreview(selector);
    if (preview) {
      const fallback = preview.getAttribute("data-avatar-fallback");
      if (fallback) {
        preview.innerHTML = `<img src="${fallback}" alt="" />`;
      } else {
        preview.innerHTML = "";
      }
    }

    syncActionButtons(selector);
  }

  function applyToTarget(item, selector) {
    const input = document.querySelector(selector);
    if (input) {
      input.value = item.publicUrl;
      setMediaId(input, item.id || null);
      input.dispatchEvent(new Event("input", { bubbles: true }));
      input.dispatchEvent(new Event("change", { bubbles: true }));
    }

    const altInput = findAltTarget(selector);
    if (altInput) {
      const mediaAlt = (item.altText || "").trim();
      if (mediaAlt && !altInput.value.trim()) {
        altInput.value = mediaAlt;
        altInput.dispatchEvent(new Event("input", { bubbles: true }));
        altInput.dispatchEvent(new Event("change", { bubbles: true }));
      }
    }

    const preview = findPreview(selector);
    if (preview) {
      const alt = (altInput?.value || item.altText || "").trim();
      preview.innerHTML = `<img src="${item.publicUrl}" alt="${alt}" />`;
    }
    syncActionButtons(selector);
  }

  function ensureClearButton(pickerBtn) {
    const selector = pickerBtn.getAttribute("data-target");
    if (!selector) return;
    if (pickerBtn.hasAttribute("data-media-picker-no-clear")) return;

    const host =
      pickerBtn.closest(".admin-row-actions") ||
      pickerBtn.parentElement;
    if (!host) return;

    if (
      host.querySelector(`[data-media-clear][data-target="${CSS.escape(selector)}"]`) ||
      host.querySelector(`[data-avatar-clear][data-target="${CSS.escape(selector)}"]`)
    ) {
      syncActionButtons(selector);
      return;
    }

    const clearBtn = document.createElement("button");
    clearBtn.type = "button";
    clearBtn.className = "btn btn-ghost";
    clearBtn.setAttribute("data-media-clear", "");
    clearBtn.setAttribute("data-target", selector);
    clearBtn.innerHTML = `<i class="fas fa-trash-alt" aria-hidden="true"></i><span>${clearLabel}</span>`;
    host.appendChild(clearBtn);
    syncActionButtons(selector);
  }

  function ensureEditButton(pickerBtn) {
    const selector = pickerBtn.getAttribute("data-target");
    if (!selector) return;
    if (pickerBtn.hasAttribute("data-media-picker-no-edit")) return;
    if (pickerBtn.hasAttribute("data-media-picker-no-clear")) return;

    const host =
      pickerBtn.closest(".admin-row-actions") ||
      pickerBtn.parentElement;
    if (!host) return;

    if (host.querySelector(`[data-media-edit][data-target="${CSS.escape(selector)}"]`)) {
      syncActionButtons(selector);
      return;
    }

    const editBtn = document.createElement("button");
    editBtn.type = "button";
    editBtn.className = "btn btn-secondary";
    editBtn.setAttribute("data-media-edit", "");
    editBtn.setAttribute("data-target", selector);
    editBtn.innerHTML = `<i class="fas fa-crop-alt" aria-hidden="true"></i><span>${editLabel}</span>`;
    editBtn.hidden = true;

    const clearBtn = host.querySelector(
      `[data-media-clear][data-target="${CSS.escape(selector)}"], [data-avatar-clear][data-target="${CSS.escape(selector)}"]`
    );
    if (clearBtn) host.insertBefore(editBtn, clearBtn);
    else host.appendChild(editBtn);

    syncActionButtons(selector);
  }

  async function editTarget(selector) {
    const input = document.querySelector(selector);
    if (!input) return;

    const url = String(input.value || "").trim();
    if (!url) return;

    if (!window.AdminMediaImageEditor) {
      window.alert(editMissingLabel);
      return;
    }

    try {
      let asset = null;
      let id = input.dataset.mediaId || null;
      if (id) {
        asset = null;
      } else {
        asset = await window.AdminMediaImageEditor.resolveByUrl(url);
        id = asset?.id;
        if (id) setMediaId(input, id);
      }

      if (!id) {
        window.alert(editMissingLabel);
        return;
      }

      await window.AdminMediaImageEditor.open({
        id,
        asset: asset || undefined,
        onSaved: (data) => {
          if (!data?.publicUrl) return;
          input.value = data.publicUrl;
          setMediaId(input, data.id || id);
          input.dispatchEvent(new Event("input", { bubbles: true }));
          input.dispatchEvent(new Event("change", { bubbles: true }));
          const preview = findPreview(selector);
          if (preview) {
            const bust = data.publicUrl.includes("?")
              ? `${data.publicUrl}&v=${Date.now()}`
              : `${data.publicUrl}?v=${Date.now()}`;
            preview.innerHTML = `<img src="${bust}" alt="" />`;
          }
          syncActionButtons(selector);
        }
      });
    } catch (err) {
      window.alert(err?.message || editMissingLabel);
    }
  }

  function wireClearControls() {
    document.querySelectorAll("[data-media-picker][data-target]").forEach((btn) => {
      ensureClearButton(btn);
      ensureEditButton(btn);
    });

    document.querySelectorAll("[data-media-clear][data-target], [data-media-edit][data-target]").forEach((btn) => {
      const selector = btn.getAttribute("data-target");
      if (!selector) return;
      syncActionButtons(selector);
      const input = document.querySelector(selector);
      if (!input || input.dataset.mediaClearBound === "1") return;
      input.dataset.mediaClearBound = "1";
      input.addEventListener("input", () => syncActionButtons(selector));
      input.addEventListener("change", () => syncActionButtons(selector));
    });
  }

  function applySelection(item) {
    if (!item || !item.publicUrl) {
      showUploadError("آپلود انجام شد ولی آدرس تصویر برنگشت.");
      setUploading(false);
      return;
    }

    const callback = onSelectCallback;
    const selector = targetSelector;
    uploading = false;
    modal.hidden = true;
    resetState();

    if (typeof callback === "function") {
      callback(item);
      return;
    }

    if (selector) applyToTarget(item, selector);
  }

  function appendItems(items) {
    if (!grid) return;

    for (const item of items) {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "admin-media-picker-item";
      button.innerHTML = `<img loading="lazy" src="${item.thumbnailUrl || item.publicUrl}" alt="${item.altText || item.title || item.fileName || ""}" /><span>${item.title || item.fileName}</span>`;
      button.addEventListener("click", () => applySelection(item));
      grid.appendChild(button);
    }
  }

  function nearBottom() {
    if (!grid) return false;
    return grid.scrollTop + grid.clientHeight >= grid.scrollHeight - SCROLL_THRESHOLD_PX;
  }

  function needsMoreToFill() {
    if (!grid || !hasNext) return false;
    return grid.scrollHeight <= grid.clientHeight + 8;
  }

  async function loadPage(reset) {
    if (!grid) return;
    if (loadingPage) return;
    if (!reset && !hasNext) return;

    loadingPage = true;
    const pageToLoad = reset ? 1 : nextPage;

    if (reset) {
      resetPaging();
      if (loading) loading.hidden = false;
    } else if (loadingMore) {
      loadingMore.hidden = false;
    }

    try {
      const response = await fetch(
        `/admin/media/picker?page=${pageToLoad}&pageSize=${PAGE_SIZE}`,
        {
          headers: { Accept: "application/json" },
          credentials: "same-origin"
        }
      );

      if (!response.ok) {
        if (reset) {
          empty.hidden = false;
          empty.textContent = "بارگذاری کتابخانه رسانه ممکن نشد.";
        }
        hasNext = false;
        return;
      }

      const data = await response.json();
      const items = Array.isArray(data.items) ? data.items : Array.isArray(data) ? data : [];
      hasNext = !!data.hasNext;
      nextPage = (data.page || pageToLoad) + 1;

      if (reset && !items.length) {
        empty.hidden = false;
        return;
      }

      empty.hidden = true;
      appendItems(items);

      if (needsMoreToFill()) {
        loadingPage = false;
        if (loading) loading.hidden = true;
        if (loadingMore) loadingMore.hidden = true;
        await loadPage(false);
        return;
      }
    } catch {
      if (reset) {
        empty.hidden = false;
        empty.textContent = "بارگذاری کتابخانه رسانه ممکن نشد.";
      }
      hasNext = false;
    } finally {
      loadingPage = false;
      if (loading) loading.hidden = true;
      if (loadingMore) loadingMore.hidden = true;
    }
  }

  /**
   * @param {string|{target?: string, onSelect?: function}} options
   *   string → CSS selector for an input (legacy)
   *   object → { target, onSelect } for inputs and/or callbacks (CKEditor)
   */
  async function open(options) {
    targetSelector = null;
    onSelectCallback = null;

    if (typeof options === "string") {
      targetSelector = options;
    } else if (options && typeof options === "object") {
      if (typeof options.target === "string") targetSelector = options.target;
      if (typeof options.onSelect === "function") onSelectCallback = options.onSelect;
    }

    modal.hidden = false;
    setUploading(false);
    clearUploadError();
    setTab("library");
    await loadPage(true);
  }

  function buildUploadFormData() {
    const formData = new FormData();
    const file = fileInput?.files?.[0];
    if (!file) return null;

    formData.append("file", file, file.name);

    const title = uploadForm?.querySelector('input[name="title"]')?.value?.trim();
    if (title) formData.append("title", title);

    const maxWidth = uploadForm?.querySelector('input[name="maxWidth"]')?.value?.trim();
    if (maxWidth) formData.append("maxWidth", maxWidth);

    const quality = uploadForm?.querySelector('input[name="quality"]')?.value?.trim();
    if (quality) formData.append("quality", quality);

    const antiforgery = token();
    if (antiforgery) formData.append("__RequestVerificationToken", antiforgery);

    return formData;
  }

  async function readErrorMessage(response) {
    const contentType = response.headers.get("content-type") || "";
    try {
      if (contentType.includes("application/json") || contentType.includes("problem+json")) {
        const data = await response.json();
        return (
          data.message ||
          data.detail ||
          data.title ||
          (data.errors && Object.values(data.errors).flat().find(Boolean)) ||
          null
        );
      }

      const text = (await response.text()).trim();
      if (text && text.length < 300 && !text.startsWith("<")) return text;
    } catch {
      /* ignore parse errors */
    }

    if (response.status === 403) return "اجازهٔ آپلود رسانه را ندارید.";
    if (response.status === 401) return "نشست شما منقضی شده است. دوباره وارد شوید.";
    if (response.status === 413) return "حجم فایل بیش از حد مجاز است.";
    if (response.status === 429) return "تعداد درخواست‌ها بیش از حد مجاز است. کمی بعد تلاش کنید.";
    return null;
  }

  grid?.addEventListener("scroll", () => {
    if (loadingPage || !hasNext) return;
    if (nearBottom()) loadPage(false);
  });

  tabButtons.forEach((btn) => {
    btn.addEventListener("click", () => {
      const name = btn.getAttribute("data-media-picker-tab");
      if (name) setTab(name);
    });
  });

  uploadForm?.addEventListener("submit", async (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (!uploadForm || uploading) return;

    clearUploadError();

    const formData = buildUploadFormData();
    if (!formData) {
      showUploadError("لطفاً یک فایل تصویر انتخاب کنید.");
      setTab("upload");
      fileInput?.focus();
      return;
    }

    setUploading(true);

    try {
      const response = await fetch("/admin/media/uploadpicker", {
        method: "POST",
        headers: {
          RequestVerificationToken: token(),
          "X-Requested-With": "XMLHttpRequest",
          Accept: "application/json"
        },
        credentials: "same-origin",
        body: formData
      });

      if (!response.ok) {
        showUploadError((await readErrorMessage(response)) || "آپلود ناموفق بود.");
        setUploading(false);
        return;
      }

      const data = await response.json().catch(() => null);
      if (!data || !data.publicUrl) {
        showUploadError("پاسخ سرور نامعتبر بود.");
        setUploading(false);
        return;
      }

      applySelection(data);
    } catch {
      showUploadError("آپلود ناموفق بود. اتصال شبکه را بررسی کنید.");
      setUploading(false);
    }
  });

  document.addEventListener("click", (event) => {
    const pickerBtn = event.target.closest("[data-media-picker][data-target]");
    if (pickerBtn && !modal.contains(pickerBtn)) {
      const pickerSelector = pickerBtn.getAttribute("data-target");
      if (pickerSelector) {
        open(pickerSelector);
        return;
      }
    }

    const clearBtn = event.target.closest("[data-media-clear]");
    if (clearBtn) {
      const selector = clearBtn.getAttribute("data-target");
      if (selector) clearTarget(selector);
      return;
    }

    const editBtn = event.target.closest("[data-media-edit]");
    if (editBtn) {
      const selector = editBtn.getAttribute("data-target");
      if (selector) editTarget(selector);
    }
  });

  modal.querySelectorAll("[data-media-picker-close]").forEach((el) => {
    el.addEventListener("click", close);
  });

  wireClearControls();

  window.AdminMediaPicker = {
    open,
    close,
    clear: clearTarget,
    refreshClearButtons: wireClearControls
  };
})();
