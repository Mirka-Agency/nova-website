(function () {
  const form = document.querySelector("[data-admin-form-steps]");
  if (!form) return;

  const total = Number(form.getAttribute("data-admin-form-steps-count") || "1");
  const panels = Array.from(form.querySelectorAll("[data-admin-form-step]"));
  const indicators = Array.from(
    document.querySelectorAll("[data-admin-form-step-indicator]")
  );
  const prevBtn = form.querySelector("[data-admin-form-step-prev]");
  const nextBtn = form.querySelector("[data-admin-form-step-next]");
  const submitBtn = form.querySelector("[data-admin-form-step-submit]");

  const saveAlways = form.getAttribute("data-admin-form-steps-save-always") === "true";
  let current = 1;

  function panelFor(step) {
    return panels.find((p) => Number(p.getAttribute("data-admin-form-step")) === step);
  }

  function fieldHasError(el) {
    if (!el) return false;
    if (el.classList.contains("input-validation-error") || el.classList.contains("field-validation-error")) {
      return true;
    }
    if (el.classList.contains("admin-field-error") && el.textContent.trim()) {
      return true;
    }
    return false;
  }

  function stepHasErrors(step) {
    const panel = panelFor(step);
    if (!panel) return false;
    return Array.from(
      panel.querySelectorAll(".input-validation-error, .field-validation-error, .admin-field-error")
    ).some(fieldHasError);
  }

  function firstInvalidStep() {
    for (let step = 1; step <= total; step++) {
      if (stepHasErrors(step)) return step;
    }
    return 1;
  }

  function validateStep(step) {
    const panel = panelFor(step);
    if (!panel) return true;

    const fields = panel.querySelectorAll("input, select, textarea");
    for (const field of fields) {
      if (typeof field.checkValidity === "function" && !field.checkValidity()) {
        setStep(step);
        field.reportValidity();
        field.focus();
        return false;
      }
    }
    return true;
  }

  function validateCurrentStep() {
    return validateStep(current);
  }

  function validateAllSteps() {
    for (let step = 1; step <= total; step++) {
      if (!validateStep(step)) return false;
    }
    return true;
  }

  function setStep(step) {
    current = Math.min(Math.max(step, 1), total);

    panels.forEach((panel) => {
      const n = Number(panel.getAttribute("data-admin-form-step"));
      const active = n === current;
      panel.hidden = !active;
      panel.classList.toggle("is-active", active);
    });

    indicators.forEach((item) => {
      const n = Number(item.getAttribute("data-admin-form-step-indicator"));
      item.classList.toggle("is-active", n === current);
      item.classList.toggle("is-complete", n < current);
      if (n === current) {
        item.setAttribute("aria-current", "step");
      } else {
        item.removeAttribute("aria-current");
      }
    });

    if (prevBtn) prevBtn.hidden = current === 1;
    if (nextBtn) nextBtn.hidden = current === total;
    if (submitBtn) submitBtn.hidden = saveAlways ? false : current !== total;
  }

  prevBtn?.addEventListener("click", () => setStep(current - 1));
  nextBtn?.addEventListener("click", () => {
    if (!validateCurrentStep()) return;
    setStep(current + 1);
  });

  form.addEventListener("submit", (e) => {
    if (!validateAllSteps()) {
      e.preventDefault();
    }
  });

  function goToIndicator(item) {
    const target = Number(item.getAttribute("data-admin-form-step-indicator"));
    if (Number.isNaN(target)) return;
    if (target > current && !validateCurrentStep()) return;
    setStep(target);
  }

  indicators.forEach((item) => {
    item.setAttribute("role", "button");
    item.setAttribute("tabindex", "0");
    item.addEventListener("click", () => goToIndicator(item));
    item.addEventListener("keydown", (e) => {
      if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        goToIndicator(item);
      }
    });
  });

  setStep(firstInvalidStep());
})();
