(function () {
  const body = document.body;
  const sidebar = document.querySelector("[data-admin-sidebar]");
  const backdrop = document.querySelector("[data-admin-sidebar-backdrop]");
  const openBtn = document.querySelector("[data-admin-sidebar-open]");
  const closeBtns = document.querySelectorAll("[data-admin-sidebar-close]");

  function setOpen(open) {
    body.classList.toggle("admin-nav-open", open);
    if (backdrop) backdrop.hidden = !open;
    if (openBtn) openBtn.setAttribute("aria-expanded", open ? "true" : "false");
    if (sidebar) sidebar.setAttribute("aria-hidden", open ? "false" : "true");
  }

  openBtn?.addEventListener("click", () => setOpen(true));
  closeBtns.forEach((btn) => btn.addEventListener("click", () => setOpen(false)));
  backdrop?.addEventListener("click", () => setOpen(false));

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") setOpen(false);
  });

  document.querySelectorAll("[data-admin-flash-dismiss]").forEach((btn) => {
    btn.addEventListener("click", () => btn.closest("[data-admin-flash]")?.remove());
  });

  document.querySelectorAll("[data-admin-flash-success]").forEach((el) => {
    window.setTimeout(() => el.remove(), 5000);
  });

  document.querySelectorAll(".admin-flash-success[data-admin-flash]").forEach((el) => {
    window.setTimeout(() => el.remove(), 5000);
  });

  const userMenus = document.querySelectorAll("[data-admin-user-menu]");

  function setUserMenuOpen(menu, open) {
    const trigger = menu.querySelector("[data-admin-user-menu-trigger]");
    menu.classList.toggle("is-open", open);
    if (trigger) trigger.setAttribute("aria-expanded", open ? "true" : "false");
  }

  userMenus.forEach((menu) => {
    const trigger = menu.querySelector("[data-admin-user-menu-trigger]");
    const panel = menu.querySelector("[data-admin-user-menu-panel]");
    trigger?.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      const willOpen = !menu.classList.contains("is-open");
      userMenus.forEach((other) => setUserMenuOpen(other, false));
      setUserMenuOpen(menu, willOpen);
    });
    panel?.addEventListener("click", (e) => e.stopPropagation());
  });

  document.addEventListener("click", () => {
    userMenus.forEach((menu) => setUserMenuOpen(menu, false));
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") {
      userMenus.forEach((menu) => setUserMenuOpen(menu, false));
    }
  });

  const STORAGE_KEY = "admin.nav.sections";

  function readOpenSections() {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      return raw ? JSON.parse(raw) : {};
    } catch {
      return {};
    }
  }

  function writeOpenSections(map) {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(map));
    } catch {
      /* ignore quota / private mode */
    }
  }

  function setNavSectionOpen(section, open, persist) {
    const toggle = section.querySelector("[data-admin-nav-section-toggle]");
    const subitems = section.querySelector("[data-admin-nav-subitems]");
    const id = section.getAttribute("data-admin-nav-section-id");

    section.classList.toggle("is-open", open);
    if (toggle) toggle.setAttribute("aria-expanded", open ? "true" : "false");
    if (subitems) subitems.hidden = !open;

    if (persist && id) {
      const map = readOpenSections();
      map[id] = open;
      writeOpenSections(map);
    }
  }

  const savedSections = readOpenSections();
  document.querySelectorAll("[data-admin-nav-section]").forEach((section) => {
    const id = section.getAttribute("data-admin-nav-section-id");
    const hasActive = !!section.querySelector(".admin-nav-sublink.is-active");
    const toggle = section.querySelector("[data-admin-nav-section-toggle]");

    if (hasActive) {
      setNavSectionOpen(section, true, true);
    } else if (id && Object.prototype.hasOwnProperty.call(savedSections, id)) {
      setNavSectionOpen(section, !!savedSections[id], false);
    }

    toggle?.addEventListener("click", () => {
      const willOpen = !section.classList.contains("is-open");
      setNavSectionOpen(section, willOpen, true);
    });
  });

  const filterPanels = document.querySelectorAll("[data-admin-filter]");

  function setFilterOpen(filter, open) {
    const trigger = filter.querySelector("[data-admin-filter-trigger]");
    const panel = filter.querySelector("[data-admin-filter-panel]");
    filter.classList.toggle("is-open", open);
    if (trigger) trigger.setAttribute("aria-expanded", open ? "true" : "false");
    if (panel) panel.hidden = !open;
    if (open) {
      window.setTimeout(() => {
        panel?.querySelector("input, select, textarea")?.focus();
      }, 0);
    }
  }

  function isJalaliDatepickerElement(node) {
    if (!(node instanceof Element)) return false;
    const tag = node.tagName;
    return tag === "JDP-CONTAINER" || tag === "JDP-OVERLAY";
  }

  function eventPathContainsJalaliDatepicker(e) {
    if (e.target instanceof Element && e.target.closest("jdp-container, jdp-overlay")) {
      return true;
    }
    if (typeof e.composedPath !== "function") return false;
    // composedPath stays valid even if the picker hides/removes itself mid-click.
    return e.composedPath().some(isJalaliDatepickerElement);
  }

  function isJalaliDatepickerVisible() {
    const container = document.querySelector("jdp-container");
    if (!container) return false;
    return window.getComputedStyle(container).display !== "none";
  }

  filterPanels.forEach((filter) => {
    const trigger = filter.querySelector("[data-admin-filter-trigger]");
    const panel = filter.querySelector("[data-admin-filter-panel]");
    const closeBtn = filter.querySelector("[data-admin-filter-close]");

    trigger?.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      const willOpen = !filter.classList.contains("is-open");
      filterPanels.forEach((other) => setFilterOpen(other, false));
      setFilterOpen(filter, willOpen);
    });

    closeBtn?.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      setFilterOpen(filter, false);
      trigger?.focus();
    });

    panel?.addEventListener("click", (e) => e.stopPropagation());
  });

  // Jalali datepicker renders jdp-container on document.body (outside the filter
  // panel). Ignore those clicks so selecting a date does not dismiss the popup.
  document.addEventListener("click", (e) => {
    if (eventPathContainsJalaliDatepicker(e)) return;
    filterPanels.forEach((filter) => setFilterOpen(filter, false));
  });

  document.addEventListener("keydown", (e) => {
    if (e.key !== "Escape") return;
    // Let the open datepicker consume Escape first; keep the filter panel open.
    if (isJalaliDatepickerVisible()) return;
    filterPanels.forEach((filter) => setFilterOpen(filter, false));
  });

  function resolveFocusableControl(control) {
    if (!control) return null;

    if (control.matches("textarea.admin-ckeditor-source")) {
      const editable = control
        .closest(".admin-field")
        ?.querySelector(".ck-editor__editable[contenteditable='true']");
      return editable || null;
    }

    if (control.type === "hidden") {
      return null;
    }

    const style = window.getComputedStyle(control);
    if (style.display === "none" || style.visibility === "hidden") {
      const editable = control
        .closest(".admin-field")
        ?.querySelector(".ck-editor__editable[contenteditable='true']");
      return editable || null;
    }

    return control;
  }

  function focusElement(el) {
    if (!el || typeof el.focus !== "function") return;
    try {
      el.focus({ preventScroll: true });
    } catch {
      el.focus();
    }
  }

  function focusFirstAdminValidationError(allowFallback) {
    const root = document.getElementById("admin-main") || document.body;

    const invalidControl = root.querySelector(
      "input.input-validation-error, select.input-validation-error, textarea.input-validation-error"
    );

    const errorMessage = Array.from(
      root.querySelectorAll(".field-validation-error, .admin-field-error, .admin-validation")
    ).find((el) => (el.textContent || "").trim().length > 0);

    if (!invalidControl && !errorMessage) {
      return true;
    }

    let focusTarget = null;
    let scrollTarget = null;

    if (invalidControl) {
      focusTarget = resolveFocusableControl(invalidControl);
      scrollTarget = invalidControl.closest(".admin-field") || invalidControl;

      if (!focusTarget) {
        if (!allowFallback) {
          return false;
        }

        const fieldMessage = scrollTarget.querySelector?.(
          ".field-validation-error, .admin-field-error"
        );
        focusTarget =
          fieldMessage && (fieldMessage.textContent || "").trim()
            ? fieldMessage
            : scrollTarget;

        if (!focusTarget.hasAttribute("tabindex")) {
          focusTarget.setAttribute("tabindex", "-1");
        }
      }
    } else if (errorMessage) {
      scrollTarget = errorMessage.closest(".admin-field") || errorMessage;
      const relatedControl = scrollTarget.querySelector?.(
        "input:not([type='hidden']), select, textarea"
      );
      focusTarget = relatedControl
        ? resolveFocusableControl(relatedControl)
        : errorMessage;

      if (!focusTarget) {
        if (!allowFallback) {
          return false;
        }
        focusTarget = errorMessage;
      }

      if (focusTarget === errorMessage && !errorMessage.hasAttribute("tabindex")) {
        errorMessage.setAttribute("tabindex", "-1");
      }
    }

    (scrollTarget || focusTarget)?.scrollIntoView({
      behavior: "smooth",
      block: "center",
    });
    window.setTimeout(() => focusElement(focusTarget), 80);
    return true;
  }

  function tryFocusFirstValidationError(attemptsLeft) {
    if (focusFirstAdminValidationError(attemptsLeft <= 0)) return;
    window.setTimeout(() => tryFocusFirstValidationError(attemptsLeft - 1), 200);
  }

  window.setTimeout(() => tryFocusFirstValidationError(8), 0);
})();
