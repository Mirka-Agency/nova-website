(() => {
  const modal = document.querySelector("[data-variation-modal]");
  if (!modal) return;

  const form = modal.querySelector("[data-variation-form]");
  const title = modal.querySelector("#variation-form-title");
  const createAction = modal.getAttribute("data-create-action") || "";
  const indexUrl = modal.getAttribute("data-index-url") || "";
  const createTitle = modal.getAttribute("data-create-title") || "";

  const open = () => {
    modal.hidden = false;
  };

  const resetCreateForm = () => {
    if (!form) return;
    if (createAction) form.setAttribute("action", createAction);
    const idInput = form.querySelector('[name="Form.Id"]');
    if (idInput) idInput.value = "";

    form.querySelectorAll("input, textarea, select").forEach((el) => {
      if (el.name === "__RequestVerificationToken") return;
      if (el.name === "Form.Id") {
        el.value = "";
        return;
      }
      if (el.type === "checkbox") {
        el.checked = false;
        return;
      }
      if (el.tagName === "SELECT") {
        if (el.name === "Form.Status") el.value = "Active";
        return;
      }
      if (el.type === "hidden" && el.name !== "Form.ImageUrl") return;
      el.value = "";
    });

    const preview = form.querySelector("[data-media-preview-for='#Form_ImageUrl']");
    if (preview) preview.innerHTML = "";
    const imageUrl = form.querySelector("#Form_ImageUrl");
    if (imageUrl) {
      imageUrl.value = "";
      imageUrl.dispatchEvent(new Event("input", { bubbles: true }));
      imageUrl.dispatchEvent(new Event("change", { bubbles: true }));
    }
    window.AdminMediaPicker?.refreshClearButtons?.();
    form.querySelectorAll(".admin-field-error").forEach((el) => {
      el.textContent = "";
    });
    const summary = form.querySelector(".admin-validation");
    if (summary) summary.innerHTML = "";
    if (title && createTitle) title.textContent = createTitle;
    modal.setAttribute("data-is-editing", "false");

    const unlimited = form.querySelector("[data-unlimited-stock]");
    if (unlimited) unlimited.dispatchEvent(new Event("change", { bubbles: true }));
  };

  const close = () => {
    if (modal.getAttribute("data-is-editing") === "true") {
      if (indexUrl) window.location.href = indexUrl;
      return;
    }
    modal.hidden = true;
    resetCreateForm();
  };

  document.querySelectorAll("[data-variation-modal-open]").forEach((btn) => {
    btn.addEventListener("click", () => {
      if (modal.getAttribute("data-is-editing") === "true") {
        if (indexUrl) {
          window.location.href = indexUrl + (indexUrl.includes("?") ? "&" : "?") + "openAdd=1";
        }
        return;
      }
      resetCreateForm();
      open();
    });
  });

  modal.querySelectorAll("[data-variation-modal-close]").forEach((el) => {
    el.addEventListener("click", close);
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && !modal.hidden) close();
  });

  const params = new URLSearchParams(window.location.search);
  if (params.get("openAdd") === "1") {
    resetCreateForm();
    open();
    params.delete("openAdd");
    const next = window.location.pathname + (params.toString() ? `?${params}` : "");
    window.history.replaceState({}, "", next);
  }
})();
