(function () {
  const root = document.querySelector("[data-article-attachment]");
  if (!root) return;

  const fileInput = root.querySelector("[data-attachment-upload-input]");
  const uploadBtn = root.querySelector("[data-attachment-upload-btn]");
  const clearBtn = root.querySelector("[data-attachment-clear-btn]");
  const urlInput = root.querySelector("[data-attachment-url]");
  const nameInput = root.querySelector("[data-attachment-name]");
  const preview = root.querySelector("[data-attachment-preview]");
  const errorEl = root.querySelector("[data-attachment-upload-error]");
  const uploadUrl = root.getAttribute("data-upload-url") || "/admin/newsmedia/uploadattachment";

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

  function renderPreview(url, fileName) {
    if (!preview) return;
    preview.innerHTML = "";
    if (!url) {
      if (clearBtn) clearBtn.hidden = true;
      return;
    }

    const link = document.createElement("a");
    link.href = url;
    link.target = "_blank";
    link.rel = "noopener noreferrer";
    link.setAttribute("data-attachment-link", "");

    const icon = document.createElement("i");
    icon.className = "fas fa-file-alt";
    icon.setAttribute("aria-hidden", "true");

    const label = document.createElement("span");
    label.setAttribute("data-attachment-label", "");
    label.textContent = fileName || "فایل پیوست";

    link.appendChild(icon);
    link.appendChild(document.createTextNode(" "));
    link.appendChild(label);
    preview.appendChild(link);
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
      const fileName = (data.fileName || file.name || "").trim();
      if (!url) throw new Error("آدرس فایل دریافت نشد.");

      if (urlInput) {
        urlInput.value = url;
        urlInput.dispatchEvent(new Event("change", { bubbles: true }));
      }
      if (nameInput) {
        nameInput.value = fileName;
        nameInput.dispatchEvent(new Event("change", { bubbles: true }));
      }
      renderPreview(url, fileName);
    } catch (err) {
      showError(err?.message || "آپلود ناموفق بود.");
    } finally {
      setBusy(false);
      if (fileInput) fileInput.value = "";
    }
  }

  function clearAttachment() {
    clearError();
    if (urlInput) {
      urlInput.value = "";
      urlInput.dispatchEvent(new Event("change", { bubbles: true }));
    }
    if (nameInput) {
      nameInput.value = "";
      nameInput.dispatchEvent(new Event("change", { bubbles: true }));
    }
    renderPreview("", "");
  }

  uploadBtn?.addEventListener("click", () => fileInput?.click());
  clearBtn?.addEventListener("click", clearAttachment);
  fileInput?.addEventListener("change", () => {
    const file = fileInput.files && fileInput.files[0];
    if (file) uploadFile(file);
  });
})();
