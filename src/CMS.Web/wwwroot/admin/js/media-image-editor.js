(function () {
  const editor = document.getElementById("media-image-editor");
  if (!editor || window.AdminMediaImageEditor) return;

  const canvas = editor.querySelector("[data-editor-canvas]");
  const stage = editor.querySelector("[data-editor-stage]");
  const cropEl = editor.querySelector("[data-editor-crop]");
  const widthInput = editor.querySelector("[data-editor-width]");
  const heightInput = editor.querySelector("[data-editor-height]");
  const lockRatio = editor.querySelector("[data-editor-lock-ratio]");
  const editorStatus = editor.querySelector("[data-editor-status]");
  const sizeBadge = editor.querySelector("[data-editor-size-badge]");
  const ctx = canvas?.getContext("2d");

  const contentUrlBase = editor.getAttribute("data-content-url") || "/Admin/Media/Content";
  const detailsUrlBase = editor.getAttribute("data-details-url") || "/Admin/Media/Details";
  const replaceUrlBase = editor.getAttribute("data-replace-url") || "/Admin/Media/ReplaceImage";
  const resolveUrlBase = editor.getAttribute("data-resolve-url") || "/Admin/Media/Resolve";

  let sourceImage = null;
  let workingCanvas = null;
  let naturalAspect = 1;
  let crop = null;
  let dragging = false;
  let dragStart = null;
  let currentAsset = null;
  let selectedId = null;
  let onSavedCallback = null;

  function token() {
    return (
      document.querySelector('meta[name="request-verification-token"]')?.getAttribute("content") ||
      document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
      ""
    );
  }

  function setEditorStatus(message, isError) {
    if (!editorStatus) return;
    editorStatus.hidden = !message;
    editorStatus.textContent = message || "";
    editorStatus.classList.toggle("admin-field-error", !!isError);
  }

  function updateSizeBadge() {
    if (!sizeBadge || !workingCanvas) return;
    sizeBadge.textContent = `${workingCanvas.width} × ${workingCanvas.height} px`;
  }

  function fitCanvasDisplay() {
    if (!canvas || !stage || !workingCanvas) return;
    const maxW = Math.max(120, stage.clientWidth - 16);
    const maxH = Math.min(window.innerHeight * 0.5, 420);
    const scale = Math.min(1, maxW / workingCanvas.width, maxH / workingCanvas.height);
    const displayW = Math.max(1, Math.round(workingCanvas.width * scale));
    const displayH = Math.max(1, Math.round(workingCanvas.height * scale));
    canvas.style.width = `${displayW}px`;
    canvas.style.height = `${displayH}px`;
  }

  function drawWorking() {
    if (!workingCanvas || !canvas || !ctx) return;
    canvas.width = workingCanvas.width;
    canvas.height = workingCanvas.height;
    ctx.imageSmoothingEnabled = true;
    ctx.imageSmoothingQuality = "high";
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(workingCanvas, 0, 0);
    if (widthInput) widthInput.value = String(workingCanvas.width);
    if (heightInput) heightInput.value = String(workingCanvas.height);
    naturalAspect = workingCanvas.width / Math.max(1, workingCanvas.height);
    updateSizeBadge();
    fitCanvasDisplay();
    resetCropOverlay();
  }

  function createWorkingFromImage(img) {
    workingCanvas = document.createElement("canvas");
    workingCanvas.width = img.naturalWidth || img.width;
    workingCanvas.height = img.naturalHeight || img.height;
    const wctx = workingCanvas.getContext("2d");
    wctx.imageSmoothingEnabled = true;
    wctx.imageSmoothingQuality = "high";
    wctx.drawImage(img, 0, 0);
    drawWorking();
  }

  function applyResizeFromInputs() {
    if (!workingCanvas || !widthInput || !heightInput) return false;
    const w = Math.max(1, Math.round(Number(widthInput.value) || 0));
    const h = Math.max(1, Math.round(Number(heightInput.value) || 0));
    if (w < 1 || h < 1) {
      setEditorStatus("عرض و ارتفاع باید عدد مثبت باشند.", true);
      return false;
    }
    if (w === workingCanvas.width && h === workingCanvas.height) {
      return true;
    }
    if (w > 8000 || h > 8000) {
      setEditorStatus("حداکثر ابعاد ۸۰۰۰ پیکسل است.", true);
      return false;
    }

    const next = document.createElement("canvas");
    next.width = w;
    next.height = h;
    const nctx = next.getContext("2d");
    nctx.imageSmoothingEnabled = true;
    nctx.imageSmoothingQuality = "high";
    nctx.drawImage(workingCanvas, 0, 0, w, h);
    workingCanvas = next;
    drawWorking();
    setEditorStatus(`ابعاد به ${w} × ${h} تغییر کرد.`, false);
    return true;
  }

  function resetCropOverlay() {
    crop = null;
    if (!cropEl || !canvas) return;
    cropEl.hidden = true;
    cropEl.style.left = "0";
    cropEl.style.top = "0";
    cropEl.style.width = "0";
    cropEl.style.height = "0";
  }

  function canvasPoint(evt) {
    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / Math.max(1, rect.width);
    const scaleY = canvas.height / Math.max(1, rect.height);
    return {
      x: Math.max(0, Math.min(canvas.width, (evt.clientX - rect.left) * scaleX)),
      y: Math.max(0, Math.min(canvas.height, (evt.clientY - rect.top) * scaleY))
    };
  }

  function updateCropUi() {
    if (!crop || !cropEl || !canvas || !stage) return;
    const rect = canvas.getBoundingClientRect();
    const stageRect = stage.getBoundingClientRect();
    const scaleX = rect.width / Math.max(1, canvas.width);
    const scaleY = rect.height / Math.max(1, canvas.height);
    cropEl.hidden = false;
    cropEl.style.left = `${rect.left - stageRect.left + crop.x * scaleX}px`;
    cropEl.style.top = `${rect.top - stageRect.top + crop.y * scaleY}px`;
    cropEl.style.width = `${Math.max(0, crop.w * scaleX)}px`;
    cropEl.style.height = `${Math.max(0, crop.h * scaleY)}px`;
  }

  function transformRotate(degrees) {
    if (!workingCanvas) return;
    const src = workingCanvas;
    const next = document.createElement("canvas");
    const rad = (degrees * Math.PI) / 180;
    const cos = Math.abs(Math.cos(rad));
    const sin = Math.abs(Math.sin(rad));
    next.width = Math.max(1, Math.round(src.width * cos + src.height * sin));
    next.height = Math.max(1, Math.round(src.width * sin + src.height * cos));
    const nctx = next.getContext("2d");
    nctx.translate(next.width / 2, next.height / 2);
    nctx.rotate(rad);
    nctx.drawImage(src, -src.width / 2, -src.height / 2);
    workingCanvas = next;
    drawWorking();
    setEditorStatus(`چرخش ${degrees > 0 ? "راست" : "چپ"} اعمال شد.`, false);
  }

  function transformFlip(horizontal, vertical) {
    if (!workingCanvas) return;
    const src = workingCanvas;
    const next = document.createElement("canvas");
    next.width = src.width;
    next.height = src.height;
    const nctx = next.getContext("2d");
    nctx.translate(horizontal ? next.width : 0, vertical ? next.height : 0);
    nctx.scale(horizontal ? -1 : 1, vertical ? -1 : 1);
    nctx.drawImage(src, 0, 0);
    workingCanvas = next;
    drawWorking();
    setEditorStatus(horizontal ? "آینه افقی اعمال شد." : "آینه عمودی اعمال شد.", false);
  }

  function canvasToBlob(source, mime, quality) {
    return new Promise((resolve) => {
      const type = mime === "image/png" ? "image/png" : "image/jpeg";
      if (source.toBlob) {
        source.toBlob((blob) => resolve(blob), type, quality);
        return;
      }
      try {
        const dataUrl = source.toDataURL(type, quality);
        const parts = dataUrl.split(",");
        const bin = atob(parts[1] || "");
        const arr = new Uint8Array(bin.length);
        for (let i = 0; i < bin.length; i++) arr[i] = bin.charCodeAt(i);
        resolve(new Blob([arr], { type }));
      } catch {
        resolve(null);
      }
    });
  }

  async function loadAsset(id) {
    const response = await fetch(`${detailsUrlBase}/${id}`, {
      headers: { Accept: "application/json" },
      credentials: "same-origin"
    });
    if (!response.ok) throw new Error("details fetch failed");
    return response.json();
  }

  async function openEditor(options) {
    const id = options?.id;
    if (!id || !editor || !canvas) return false;

    selectedId = id;
    onSavedCallback = typeof options.onSaved === "function" ? options.onSaved : null;
    setEditorStatus("در حال بارگذاری تصویر…", false);
    editor.hidden = false;
    workingCanvas = null;
    sourceImage = null;
    resetCropOverlay();

    try {
      currentAsset = options.asset || (await loadAsset(id));
      const response = await fetch(`${contentUrlBase}/${id}?source=original&t=${Date.now()}`, {
        credentials: "same-origin"
      });
      if (!response.ok) throw new Error("content fetch failed");
      const blob = await response.blob();
      const objectUrl = URL.createObjectURL(blob);
      await new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => {
          sourceImage = img;
          createWorkingFromImage(img);
          URL.revokeObjectURL(objectUrl);
          setEditorStatus("");
          resolve();
        };
        img.onerror = () => {
          URL.revokeObjectURL(objectUrl);
          reject(new Error("image decode failed"));
        };
        img.src = objectUrl;
      });
      return true;
    } catch {
      setEditorStatus("بارگذاری تصویر برای ویرایش ناموفق بود.", true);
      return false;
    }
  }

  function closeEditor() {
    editor.hidden = true;
    sourceImage = null;
    workingCanvas = null;
    selectedId = null;
    currentAsset = null;
    onSavedCallback = null;
    resetCropOverlay();
    setEditorStatus("");
  }

  async function resolveByUrl(url) {
    if (!url) return null;
    const response = await fetch(`${resolveUrlBase}?url=${encodeURIComponent(url)}`, {
      headers: { Accept: "application/json" },
      credentials: "same-origin"
    });
    if (response.status === 404) {
      const data = await response.json().catch(() => ({}));
      throw new Error(data.message || "این تصویر در کتابخانه رسانه یافت نشد و قابل ویرایش نیست.");
    }
    if (!response.ok) throw new Error("resolve failed");
    return response.json();
  }

  editor.querySelectorAll("[data-media-editor-close]").forEach((el) => {
    el.addEventListener("click", closeEditor);
  });

  editor.querySelector("[data-editor-rotate-left]")?.addEventListener("click", () => transformRotate(-90));
  editor.querySelector("[data-editor-rotate-right]")?.addEventListener("click", () => transformRotate(90));
  editor.querySelector("[data-editor-flip-h]")?.addEventListener("click", () => transformFlip(true, false));
  editor.querySelector("[data-editor-flip-v]")?.addEventListener("click", () => transformFlip(false, true));
  editor.querySelector("[data-editor-reset]")?.addEventListener("click", () => {
    if (sourceImage) {
      createWorkingFromImage(sourceImage);
      setEditorStatus("به حالت اولیه برگشت.", false);
    }
  });

  canvas?.addEventListener("mousedown", (evt) => {
    if (!workingCanvas) return;
    evt.preventDefault();
    dragging = true;
    const p = canvasPoint(evt);
    dragStart = { x: p.x, y: p.y };
    crop = { x: p.x, y: p.y, w: 0, h: 0 };
    updateCropUi();
  });

  window.addEventListener("mousemove", (evt) => {
    if (!dragging || !dragStart) return;
    const p = canvasPoint(evt);
    crop = {
      x: Math.min(dragStart.x, p.x),
      y: Math.min(dragStart.y, p.y),
      w: Math.abs(p.x - dragStart.x),
      h: Math.abs(p.y - dragStart.y)
    };
    updateCropUi();
  });

  window.addEventListener("mouseup", () => {
    dragging = false;
  });

  editor.querySelector("[data-editor-crop-apply]")?.addEventListener("click", () => {
    if (!workingCanvas || !crop || crop.w < 2 || crop.h < 2) {
      setEditorStatus("ابتدا یک محدوده برش روی تصویر بکشید.", true);
      return;
    }
    const sx = Math.max(0, Math.floor(crop.x));
    const sy = Math.max(0, Math.floor(crop.y));
    const sw = Math.min(workingCanvas.width - sx, Math.floor(crop.w));
    const sh = Math.min(workingCanvas.height - sy, Math.floor(crop.h));
    if (sw < 2 || sh < 2) {
      setEditorStatus("محدوده برش خیلی کوچک است.", true);
      return;
    }
    const next = document.createElement("canvas");
    next.width = sw;
    next.height = sh;
    next.getContext("2d").drawImage(workingCanvas, sx, sy, sw, sh, 0, 0, sw, sh);
    workingCanvas = next;
    drawWorking();
    setEditorStatus(`برش اعمال شد: ${sw} × ${sh}`, false);
  });

  widthInput?.addEventListener("input", () => {
    if (!lockRatio?.checked || !heightInput) return;
    const w = Math.max(1, Math.round(Number(widthInput.value) || 1));
    heightInput.value = String(Math.max(1, Math.round(w / naturalAspect)));
  });

  heightInput?.addEventListener("input", () => {
    if (!lockRatio?.checked || !widthInput) return;
    const h = Math.max(1, Math.round(Number(heightInput.value) || 1));
    widthInput.value = String(Math.max(1, Math.round(h * naturalAspect)));
  });

  editor.querySelector("[data-editor-resize-apply]")?.addEventListener("click", () => {
    applyResizeFromInputs();
  });

  editor.querySelector("[data-editor-save]")?.addEventListener("click", async () => {
    if (!workingCanvas || !selectedId || !currentAsset) return;
    if (!applyResizeFromInputs()) return;

    setEditorStatus("در حال ذخیره…", false);
    const mime = currentAsset.contentType === "image/png" ? "image/png" : "image/jpeg";
    const blob = await canvasToBlob(workingCanvas, mime, 0.92);
    if (!blob || blob.size === 0) {
      setEditorStatus("ساخت فایل تصویر ناموفق بود.", true);
      return;
    }

    const ext = mime === "image/png" ? "png" : "jpg";
    const baseName = (currentAsset.fileName || "image").replace(/\.[^.]+$/, "");
    const body = new FormData();
    body.set("file", blob, `${baseName}-edited.${ext}`);
    body.set("__RequestVerificationToken", token());

    try {
      const response = await fetch(`${replaceUrlBase}/${selectedId}`, {
        method: "POST",
        credentials: "same-origin",
        headers: { RequestVerificationToken: token(), "X-Requested-With": "XMLHttpRequest" },
        body
      });
      const data = await response.json().catch(() => ({}));
      if (!response.ok) {
        setEditorStatus(data.message || "ذخیره ناموفق بود.", true);
        return;
      }

      const saved = onSavedCallback;
      const payload = { ...data, id: selectedId };
      closeEditor();
      if (saved) saved(payload);
    } catch {
      setEditorStatus("خطای شبکه هنگام ذخیره.", true);
    }
  });

  window.addEventListener("resize", () => {
    if (!editor.hidden) fitCanvasDisplay();
  });

  window.AdminMediaImageEditor = {
    open: openEditor,
    close: closeEditor,
    resolveByUrl,
    get isOpen() {
      return !editor.hidden;
    }
  };
})();
