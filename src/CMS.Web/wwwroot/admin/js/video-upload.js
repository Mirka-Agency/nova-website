(() => {
  const root = document.querySelector("[data-video-field]");
  if (!root) return;

  const urlInput = root.querySelector("[data-video-url-input]");
  const fileInput = root.querySelector("[data-video-upload-input]");
  const uploadBtn = root.querySelector("[data-video-upload-btn]");
  const clearBtn = root.querySelector("[data-video-clear-btn]");
  const errorEl = root.querySelector("[data-video-upload-error]");
  const progressEl = root.querySelector("[data-video-upload-progress]");
  const progressBar = root.querySelector("[data-video-upload-progress-bar]");
  const percentEl = root.querySelector("[data-video-upload-percent]");
  const previewEl = root.querySelector("[data-video-preview]");
  const progressWrap = root.querySelector("[data-video-upload-progress-wrap]");
  const uploadUrl = root.getAttribute("data-upload-url") || "/admin/videosmedia/uploadvideo";
  const initUrl = root.getAttribute("data-chunk-init-url") || "/admin/videosmedia/initvideoupload";
  const chunkUrl = root.getAttribute("data-chunk-url") || "/admin/videosmedia/uploadvideochunk";
  const completeUrl =
    root.getAttribute("data-chunk-complete-url") || "/admin/videosmedia/completevideoupload";
  const abortUrl = root.getAttribute("data-chunk-abort-url") || "/admin/videosmedia/abortvideoupload";
  const chunkThreshold = Number(root.getAttribute("data-chunk-threshold") || 5 * 1024 * 1024);
  const chunkSize = Number(root.getAttribute("data-chunk-size") || 5 * 1024 * 1024);

  let activeUploadId = null;

  function token() {
    return (
      document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
      ""
    );
  }

  function toFaDigits(value) {
    return String(value).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[d]);
  }

  function showError(message) {
    if (!errorEl) return;
    errorEl.hidden = !message;
    errorEl.textContent = message || "";
  }

  function setProgress(percent) {
    const clamped = Math.max(0, Math.min(100, Math.round(percent || 0)));
    if (progressWrap) progressWrap.hidden = false;
    if (progressEl) {
      progressEl.hidden = false;
      progressEl.setAttribute("aria-hidden", "false");
      progressEl.classList.add("is-determinate");
    }
    if (progressBar) {
      progressBar.style.width = `${clamped}%`;
    }
    if (percentEl) {
      percentEl.hidden = false;
      percentEl.textContent = `${toFaDigits(clamped)}٪`;
    }
  }

  function clearProgress() {
    if (progressWrap) progressWrap.hidden = true;
    if (progressEl) {
      progressEl.hidden = true;
      progressEl.setAttribute("aria-hidden", "true");
      progressEl.classList.remove("is-determinate");
    }
    if (progressBar) progressBar.style.width = "0%";
    if (percentEl) {
      percentEl.hidden = true;
      percentEl.textContent = "";
    }
  }

  function resolveContentType(file) {
    const declared = (file.type || "").trim().toLowerCase();
    if (declared.startsWith("video/")) return declared;
    const name = (file.name || "").toLowerCase();
    if (name.endsWith(".webm")) return "video/webm";
    if (name.endsWith(".ogg") || name.endsWith(".ogv")) return "video/ogg";
    if (name.endsWith(".mov") || name.endsWith(".qt")) return "video/quicktime";
    return "video/mp4";
  }

  function setBusy(busy) {
    if (uploadBtn) uploadBtn.disabled = busy;
    if (fileInput) fileInput.disabled = busy;
    if (!busy) clearProgress();
  }

  function resolveEmbedUrl(url) {
    const trimmed = (url || "").trim();
    if (!trimmed) return null;

    const iframeSrc = trimmed.match(/src\s*=\s*["'](https?:\/\/[^"']+)["']/i);
    const raw = iframeSrc ? iframeSrc[1].trim() : trimmed;

    if (/aparat\.com\/video\/video\/embed\//i.test(raw)
      || /youtube\.com\/embed\//i.test(raw)
      || /player\.vimeo\.com\/video\//i.test(raw)) {
      return raw.split("#")[0];
    }

    const aparat = raw.match(/aparat\.com\/(?:v\/|embed\/|video\/video\/embed(?:_box)?\/videohash\/)([A-Za-z0-9_-]+)/i);
    if (aparat) {
      return `https://www.aparat.com/video/video/embed/videohash/${aparat[1]}/vt/frame`;
    }

    const yt = raw.match(/(?:youtube\.com\/watch\?(?:[^#]*&)?v=|youtube\.com\/embed\/|youtu\.be\/)([A-Za-z0-9_-]{6,})/i);
    if (yt) return `https://www.youtube.com/embed/${yt[1]}`;

    const vimeo = raw.match(/vimeo\.com\/(?:video\/)?(\d+)/i);
    if (vimeo) return `https://player.vimeo.com/video/${vimeo[1]}`;

    return null;
  }

  function isHostedPage(url) {
    const raw = (url || "").trim();
    return /aparat\.com|youtube\.com|youtu\.be|vimeo\.com/i.test(raw);
  }

  function renderPreview(url) {
    if (!previewEl) return;
    const trimmed = (url || "").trim();
    if (!trimmed) {
      previewEl.innerHTML = "";
      return;
    }

    const embedUrl = resolveEmbedUrl(trimmed);
    if (embedUrl) {
      previewEl.innerHTML =
        `<iframe src="${embedUrl}" title="پیش‌نمایش ویدیو" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; fullscreen" allowfullscreen loading="lazy"></iframe>`;
      return;
    }

    if (isHostedPage(trimmed)) {
      previewEl.innerHTML =
        `<p class="admin-hint">لینک ویدیو قابل پخش توکار نیست. بعد از ذخیره، لینک منبع نمایش داده می‌شود.</p>`;
      return;
    }

    previewEl.innerHTML = `<video controls preload="metadata" src="${trimmed}"></video>`;
  }

  function syncClear() {
    if (!clearBtn || !urlInput) return;
    clearBtn.hidden = !urlInput.value.trim();
  }

  async function readErrorMessage(response) {
    try {
      const data = await response.json();
      return (
        data.message ||
        data.error?.message ||
        data.detail ||
        data.title ||
        null
      );
    } catch {
      return null;
    }
  }

  function applyUploadedUrl(url) {
    if (urlInput) {
      urlInput.value = url;
      urlInput.dispatchEvent(new Event("input", { bubbles: true }));
      urlInput.dispatchEvent(new Event("change", { bubbles: true }));
    }
    renderPreview(url);
    syncClear();
  }

  function postJson(url, body) {
    const antiforgery = token();
    return fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: antiforgery,
        "X-Requested-With": "XMLHttpRequest",
        Accept: "application/json"
      },
      credentials: "same-origin",
      body: JSON.stringify(body)
    });
  }

  function uploadBlobWithProgress(url, formData, onProgress) {
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      xhr.open("POST", url, true);
      xhr.withCredentials = true;
      xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");
      xhr.setRequestHeader("Accept", "application/json");
      const antiforgery = token();
      if (antiforgery) xhr.setRequestHeader("RequestVerificationToken", antiforgery);

      xhr.upload.onprogress = (event) => {
        if (!event.lengthComputable || typeof onProgress !== "function") return;
        onProgress(event.loaded, event.total);
      };

      xhr.onload = () => {
        resolve({
          ok: xhr.status >= 200 && xhr.status < 300,
          status: xhr.status,
          async json() {
            try {
              return JSON.parse(xhr.responseText || "{}");
            } catch {
              return {};
            }
          },
          async text() {
            return xhr.responseText || "";
          }
        });
      };

      xhr.onerror = () => reject(new Error("network"));
      xhr.onabort = () => reject(new Error("abort"));
      xhr.send(formData);
    });
  }

  async function abortActiveUpload() {
    if (!activeUploadId) return;
    const id = activeUploadId;
    activeUploadId = null;
    try {
      await postJson(abortUrl, { uploadId: id });
    } catch {
      // ignore
    }
  }

  async function uploadSingle(file) {
    const formData = new FormData();
    formData.append("file", file, file.name);
    const antiforgery = token();
    if (antiforgery) formData.append("__RequestVerificationToken", antiforgery);

    const response = await uploadBlobWithProgress(uploadUrl, formData, (loaded, total) => {
      setProgress((loaded / Math.max(total, 1)) * 100);
    });

    if (!response.ok) {
      showError((await readErrorMessage(response)) || "آپلود ویدیو ناموفق بود.");
      return;
    }

    const data = await response.json();
    const url = data.url || data.publicUrl;
    if (!url) {
      showError("آپلود انجام شد ولی آدرس ویدیو برنگشت.");
      return;
    }

    setProgress(100);
    applyUploadedUrl(url);
  }

  async function uploadChunked(file) {
    const initResponse = await postJson(initUrl, {
      fileName: file.name,
      contentType: resolveContentType(file),
      totalBytes: file.size
    });

    if (!initResponse.ok) {
      showError((await readErrorMessage(initResponse)) || "شروع آپلود تکه‌تکه ناموفق بود.");
      return;
    }

    const initData = await initResponse.json();
    const uploadId = initData.uploadId;
    const totalChunks = Number(initData.totalChunks || 0);
    const size = Number(initData.chunkSize || chunkSize);
    if (!uploadId || totalChunks < 1) {
      showError("پاسخ شروع آپلود نامعتبر بود.");
      return;
    }

    activeUploadId = uploadId;
    setProgress(0);

    for (let index = 0; index < totalChunks; index++) {
      const start = index * size;
      const end = Math.min(file.size, start + size);
      const blob = file.slice(start, end);

      const formData = new FormData();
      formData.append("uploadId", uploadId);
      formData.append("chunkIndex", String(index));
      formData.append("chunk", blob, `${file.name}.part${index}`);
      const antiforgery = token();
      if (antiforgery) formData.append("__RequestVerificationToken", antiforgery);

      const response = await uploadBlobWithProgress(chunkUrl, formData, (loaded, total) => {
        const chunkFraction = loaded / Math.max(total, 1);
        const overall = ((index + chunkFraction) / totalChunks) * 100;
        setProgress(overall);
      });

      if (!response.ok) {
        showError((await readErrorMessage(response)) || `آپلود تکه ${index + 1} ناموفق بود.`);
        await abortActiveUpload();
        return;
      }

      setProgress(((index + 1) / totalChunks) * 95);
    }

    const completeResponse = await postJson(completeUrl, { uploadId });
    activeUploadId = null;

    if (!completeResponse.ok) {
      showError((await readErrorMessage(completeResponse)) || "اتمام آپلود ویدیو ناموفق بود.");
      return;
    }

    const data = await completeResponse.json();
    const url = data.url || data.publicUrl;
    if (!url) {
      showError("آپلود انجام شد ولی آدرس ویدیو برنگشت.");
      return;
    }

    setProgress(100);
    applyUploadedUrl(url);
  }

  async function uploadFile(file) {
    showError("");
    setBusy(true);
    setProgress(0);

    try {
      if (file.size > chunkThreshold) {
        await uploadChunked(file);
      } else {
        await uploadSingle(file);
      }
    } catch {
      await abortActiveUpload();
      showError("ارتباط با سرور برقرار نشد.");
    } finally {
      setBusy(false);
      if (fileInput) fileInput.value = "";
    }
  }

  uploadBtn?.addEventListener("click", () => fileInput?.click());

  fileInput?.addEventListener("change", () => {
    const file = fileInput.files?.[0];
    if (!file) return;
    uploadFile(file);
  });

  clearBtn?.addEventListener("click", () => {
    if (urlInput) {
      urlInput.value = "";
      urlInput.dispatchEvent(new Event("input", { bubbles: true }));
      urlInput.dispatchEvent(new Event("change", { bubbles: true }));
    }
    renderPreview("");
    showError("");
    syncClear();
  });

  urlInput?.addEventListener("input", () => {
    renderPreview(urlInput.value);
    syncClear();
  });
  urlInput?.addEventListener("change", () => {
    renderPreview(urlInput.value);
    syncClear();
  });

  renderPreview(urlInput?.value || "");
  syncClear();
})();
