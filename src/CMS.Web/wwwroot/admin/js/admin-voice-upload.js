(function () {
  const root = document.querySelector("[data-voice-audio]");
  if (!root) return;

  const fileInput = root.querySelector("[data-voice-upload-input]");
  const uploadBtn = root.querySelector("[data-voice-upload-btn]");
  const clearBtn = root.querySelector("[data-voice-clear-btn]");
  const urlInput = root.querySelector("[data-voice-url]");
  const preview = root.querySelector("[data-voice-preview]");
  const errorEl = root.querySelector("[data-voice-upload-error]");
  const uploadUrl = root.getAttribute("data-upload-url") || "/admin/voicesmedia/uploadaudio";

  function token() {
    return (
      document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
      ""
    );
  }

  function showError(message) {
    if (!errorEl) return;
    errorEl.textContent = message || "آپلود ناموفق بود.";
    errorEl.hidden = false;
  }

  function clearError() {
    if (!errorEl) return;
    errorEl.textContent = "";
    errorEl.hidden = true;
  }

  function setBusy(busy) {
    if (uploadBtn) uploadBtn.disabled = busy;
    if (clearBtn) clearBtn.disabled = busy;
  }

  function renderPreview(url) {
    if (!preview) return;
    preview.innerHTML = "";
    if (!url) {
      if (clearBtn) clearBtn.hidden = true;
      return;
    }

    const audio = document.createElement("audio");
    audio.controls = true;
    audio.preload = "metadata";
    audio.src = url;
    audio.setAttribute("data-voice-player", "");
    preview.appendChild(audio);
    if (clearBtn) clearBtn.hidden = false;
  }

  async function uploadFile(file) {
    clearError();
    setBusy(true);
    try {
      const formData = new FormData();
      formData.append("file", file);
      const antiforgery = token();
      if (antiforgery) formData.append("__RequestVerificationToken", antiforgery);

      const response = await fetch(uploadUrl, {
        method: "POST",
        headers: {
          RequestVerificationToken: antiforgery,
          Accept: "application/json",
          "X-Requested-With": "XMLHttpRequest",
        },
        body: formData,
        credentials: "same-origin",
      });

      const data = await response.json().catch(() => ({}));
      if (!response.ok) {
        const message =
          data?.error?.message || data?.detail || "آپلود ناموفق بود.";
        throw new Error(message);
      }

      const url = (data.url || "").trim();
      if (!url) throw new Error("آدرس فایل دریافت نشد.");

      if (urlInput) {
        urlInput.value = url;
        urlInput.dispatchEvent(new Event("change", { bubbles: true }));
      }
      renderPreview(url);
    } catch (err) {
      showError(err?.message || "آپلود ناموفق بود.");
    } finally {
      setBusy(false);
      if (fileInput) fileInput.value = "";
    }
  }

  function clearAudio() {
    clearError();
    if (urlInput) {
      urlInput.value = "";
      urlInput.dispatchEvent(new Event("change", { bubbles: true }));
    }
    renderPreview("");
  }

  uploadBtn?.addEventListener("click", () => fileInput?.click());
  clearBtn?.addEventListener("click", clearAudio);
  fileInput?.addEventListener("change", () => {
    const file = fileInput.files && fileInput.files[0];
    if (file) uploadFile(file);
  });
})();
