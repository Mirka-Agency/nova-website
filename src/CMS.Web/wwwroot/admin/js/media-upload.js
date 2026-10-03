(function () {
  const form = document.querySelector("[data-media-upload-form]");
  if (!form) return;

  const submit = form.querySelector("[data-media-upload-submit]");
  const status = form.querySelector("[data-media-upload-status]");
  const progress = form.querySelector("[data-media-upload-progress]");
  const cancel = form.querySelector("[data-media-upload-cancel]");
  const icon = submit?.querySelector("[data-upload-icon]");
  const label = submit?.querySelector("[data-upload-label]");
  const uploadingText = form.getAttribute("data-uploading-label") || "در حال آپلود…";
  const idleText = form.getAttribute("data-upload-label") || label?.textContent || "آپلود";
  let busy = false;

  form.addEventListener("submit", () => {
    if (busy) return;
    busy = true;
    form.setAttribute("aria-busy", "true");
    form.classList.add("is-uploading");
    if (submit) {
      submit.disabled = true;
      submit.classList.add("is-loading");
    }
    if (cancel) cancel.setAttribute("aria-disabled", "true");
    if (icon) {
      icon.classList.remove("fa-upload");
      icon.classList.add("fa-spinner", "fa-spin");
    }
    if (label) label.textContent = uploadingText;
    if (status) status.hidden = false;
    if (progress) {
      progress.hidden = false;
      progress.setAttribute("aria-hidden", "false");
    }
  });
})();
