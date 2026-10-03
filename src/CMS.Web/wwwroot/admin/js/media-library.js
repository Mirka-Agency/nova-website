(function () {
  const root = document.querySelector("[data-media-library]");
  if (!root || !window.MediaLibraryConfig) return;

  const cfg = window.MediaLibraryConfig;
  const canManage = root.getAttribute("data-can-manage") === "true";
  const panel = root.querySelector("[data-media-details]");
  const form = root.querySelector("[data-media-details-form]");
  const statusEl = root.querySelector("[data-media-status]");
  const preview = root.querySelector("[data-media-preview]");
  const tokenInput = form?.querySelector('input[name="__RequestVerificationToken"]');

  let selectedId = null;
  let currentAsset = null;

  function token() {
    return tokenInput?.value
      || document.querySelector('meta[name="request-verification-token"]')?.getAttribute("content")
      || "";
  }

  function setStatus(message, isError) {
    if (!statusEl) return;
    statusEl.hidden = !message;
    statusEl.textContent = message || "";
    statusEl.classList.toggle("admin-field-error", !!isError);
  }

  function field(name) {
    return form?.querySelector(`[data-media-field="${name}"]`);
  }

  function withCacheBust(url) {
    if (!url) return url;
    const sep = url.includes("?") ? "&" : "?";
    return `${url}${sep}v=${Date.now()}`;
  }

  function fillDetails(asset) {
    currentAsset = asset;
    selectedId = asset.id;
    form.querySelector("[data-media-id-field]").value = asset.id;
    preview.src = withCacheBust(asset.publicUrl);
    preview.alt = asset.altText || asset.title || asset.fileName;
    root.querySelector("[data-media-filename]").textContent = asset.fileName;
    root.querySelector("[data-media-type]").textContent = asset.contentType;
    const dimensionsEl = root.querySelector("[data-media-dimensions]");
    if (dimensionsEl) {
      dimensionsEl.textContent = asset.width && asset.height
        ? `${asset.width} × ${asset.height}`
        : "—";
    }
    root.querySelector("[data-media-size]").textContent = asset.sizeDisplay;
    const originalSizeRow = root.querySelector("[data-media-original-size-row]");
    const originalSizeEl = root.querySelector("[data-media-original-size]");
    if (originalSizeRow && originalSizeEl) {
      const showOriginalSize = !!(asset.hasSeparateOriginal && asset.originalSizeDisplay);
      originalSizeRow.hidden = !showOriginalSize;
      originalSizeEl.textContent = asset.originalSizeDisplay || "—";
    }
    root.querySelector("[data-media-created]").textContent = new Date(asset.createdAtUtc).toLocaleString();
    field("title").value = asset.title || "";
    field("altText").value = asset.altText || "";
    field("caption").value = asset.caption || "";
    field("description").value = asset.description || "";
    field("publicUrl").value = asset.publicUrl;
    const originalUrlField = field("originalPublicUrl");
    const originalUrlRow = root.querySelector("[data-media-original-url-row]");
    if (originalUrlField) originalUrlField.value = asset.originalPublicUrl || "";
    if (originalUrlRow) originalUrlRow.hidden = !asset.hasSeparateOriginal;
    const restoreBtn = form.querySelector("[data-media-restore-original]");
    if (restoreBtn) restoreBtn.hidden = !canManage || !asset.hasSeparateOriginal;
    panel.hidden = false;
    setStatus("");
  }

  function applyFileUpdate(data) {
    if (!currentAsset) return;
    currentAsset.publicUrl = data.publicUrl ?? currentAsset.publicUrl;
    currentAsset.fileName = data.fileName ?? currentAsset.fileName;
    currentAsset.contentType = data.contentType ?? currentAsset.contentType;
    currentAsset.sizeDisplay = data.sizeDisplay ?? currentAsset.sizeDisplay;
    currentAsset.thumbnailPublicUrl = data.thumbnailPublicUrl ?? data.publicUrl ?? currentAsset.thumbnailPublicUrl;
    currentAsset.width = data.width ?? currentAsset.width;
    currentAsset.height = data.height ?? currentAsset.height;
    currentAsset.originalPublicUrl = data.originalPublicUrl ?? currentAsset.originalPublicUrl;
    currentAsset.originalSizeDisplay = data.originalSizeDisplay ?? currentAsset.originalSizeDisplay;
    currentAsset.hasSeparateOriginal = data.hasSeparateOriginal ?? currentAsset.hasSeparateOriginal;
    fillDetails(currentAsset);
    const item = root.querySelector(`.media-library-item[data-media-id="${selectedId}"]`);
    if (item) {
      item.setAttribute("data-media-url", currentAsset.publicUrl);
      const img = item.querySelector("img");
      if (img) img.src = withCacheBust(currentAsset.thumbnailPublicUrl || currentAsset.publicUrl);
    }
  }

  async function openDetails(id) {
    root.querySelectorAll(".media-library-item").forEach((el) => {
      el.classList.toggle("is-selected", el.getAttribute("data-media-id") === id);
    });
    const response = await fetch(`${cfg.detailsUrl}/${id}`, {
      headers: { Accept: "application/json" },
      credentials: "same-origin"
    });
    if (!response.ok) {
      setStatus("بارگذاری جزئیات ناموفق بود.", true);
      return;
    }
    fillDetails(await response.json());
  }

  root.querySelectorAll("[data-media-id]").forEach((btn) => {
    btn.addEventListener("click", () => openDetails(btn.getAttribute("data-media-id")));
  });

  panel?.querySelector("[data-media-details-close]")?.addEventListener("click", () => {
    panel.hidden = true;
    selectedId = null;
    root.querySelectorAll(".media-library-item.is-selected").forEach((el) => el.classList.remove("is-selected"));
  });

  form?.querySelector("[data-media-copy-url]")?.addEventListener("click", async () => {
    const url = field("publicUrl")?.value;
    if (!url) return;
    try {
      await navigator.clipboard.writeText(url);
      setStatus(cfg.copied, false);
    } catch {
      field("publicUrl").select();
      document.execCommand("copy");
      setStatus(cfg.copied, false);
    }
  });

  form?.querySelector("[data-media-copy-original-url]")?.addEventListener("click", async () => {
    const url = field("originalPublicUrl")?.value;
    if (!url) return;
    try {
      await navigator.clipboard.writeText(url);
      setStatus(cfg.copied, false);
    } catch {
      field("originalPublicUrl").select();
      document.execCommand("copy");
      setStatus(cfg.copied, false);
    }
  });

  form?.addEventListener("submit", async (e) => {
    e.preventDefault();
    if (!canManage || !selectedId) return;
    const body = new FormData(form);
    body.set("Title", field("title").value);
    body.set("AltText", field("altText").value);
    body.set("Caption", field("caption").value);
    body.set("Description", field("description").value);

    const response = await fetch(`${cfg.updateUrl}/${selectedId}`, {
      method: "POST",
      credentials: "same-origin",
      headers: { RequestVerificationToken: token(), "X-Requested-With": "XMLHttpRequest" },
      body
    });
    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
      setStatus(data.message || "ذخیره ناموفق بود.", true);
      return;
    }
    setStatus(data.message || cfg.saved, false);
    const item = root.querySelector(`.media-library-item[data-media-id="${selectedId}"]`);
    if (item) {
      const title = field("title").value || currentAsset?.fileName || "";
      item.querySelector(".media-library-caption").textContent = title;
      item.querySelector("img").alt = field("altText").value || title;
    }
  });

  form?.querySelector("[data-media-delete]")?.addEventListener("click", async () => {
    if (!canManage || !selectedId || !confirm(cfg.confirmDelete)) return;
    const body = new FormData();
    body.set("id", selectedId);
    body.set("__RequestVerificationToken", token());
    const response = await fetch(cfg.deleteUrl, {
      method: "POST",
      credentials: "same-origin",
      headers: { RequestVerificationToken: token(), "X-Requested-With": "XMLHttpRequest", Accept: "application/json" },
      body
    });
    if (!response.ok) {
      setStatus("حذف ناموفق بود.", true);
      return;
    }
    location.reload();
  });

  form?.querySelector("[data-media-restore-original]")?.addEventListener("click", async () => {
    if (!canManage || !selectedId || !confirm(cfg.confirmRestore)) return;
    const body = new FormData();
    body.set("id", selectedId);
    body.set("__RequestVerificationToken", token());
    const response = await fetch(`${cfg.restoreUrl}/${selectedId}`, {
      method: "POST",
      credentials: "same-origin",
      headers: { RequestVerificationToken: token(), "X-Requested-With": "XMLHttpRequest", Accept: "application/json" },
      body
    });
    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
      setStatus(data.message || "بازگردانی ناموفق بود.", true);
      return;
    }
    applyFileUpdate(data);
    setStatus(data.message || cfg.saved, false);
  });

  form?.querySelector("[data-media-edit-image]")?.addEventListener("click", async () => {
    if (!canManage || !currentAsset || !selectedId || !window.AdminMediaImageEditor) return;
    await window.AdminMediaImageEditor.open({
      id: selectedId,
      asset: currentAsset,
      onSaved: (data) => {
        applyFileUpdate(data);
        setStatus(data.message || cfg.saved, false);
      }
    });
  });
})();
