(function () {
  const root = document.querySelector("[data-post-video-schema]");
  if (!root) return;

  const list = root.querySelector("[data-video-schema-list]");
  const empty = root.querySelector("[data-video-schema-empty]");
  const addBtn = root.querySelector("[data-video-schema-add]");
  const template = document.getElementById("post-video-schema-row-template");
  if (!list || !addBtn || !template) return;

  const form = root.closest("form");

  function rows() {
    return Array.from(list.querySelectorAll("[data-video-schema-row]"));
  }

  function setEmptyState() {
    if (empty) empty.hidden = rows().length > 0;
  }

  function refreshMediaPicker() {
    try {
      window.AdminMediaPicker?.refreshClearButtons?.();
    } catch (_) {
      /* ignore media picker refresh errors so add/remove still works */
    }
  }

  function syncThumbState(row) {
    const shell = row.querySelector("[data-video-schema-thumb]");
    const input = row.querySelector("[data-video-schema-thumb-input]");
    if (!shell || !input) return;
    const hasValue = !!(input.value || "").trim();
    shell.classList.toggle("is-empty", !hasValue);
  }

  function wireThumbnailPicker(row, index) {
    const input = row.querySelector("[data-video-schema-thumb-input]");
    const picker = row.querySelector("[data-video-schema-thumb-picker]");
    const preview = row.querySelector("[data-video-schema-thumb-preview]");
    if (!input || !picker || !preview) return;

    const id = `VideoSchemaThumbnailUrl_${index}`;
    const selector = `#${id}`;
    input.id = id;
    input.name = `VideoSchemaItems[${index}].ThumbnailUrl`;
    picker.setAttribute("data-target", selector);
    preview.setAttribute("data-media-preview-for", selector);

    if (input.dataset.videoSchemaThumbBound !== "1") {
      input.dataset.videoSchemaThumbBound = "1";
      const sync = () => syncThumbState(row);
      input.addEventListener("change", sync);
      input.addEventListener("input", sync);
    }

    syncThumbState(row);
  }

  function reindex() {
    rows().forEach((row, index) => {
      const n = String(index + 1);
      row.querySelectorAll("[data-video-schema-index]").forEach((el) => {
        el.textContent = n;
      });
      row.querySelectorAll("[data-video-schema-index-label]").forEach((el) => {
        el.textContent = n;
      });

      row.querySelectorAll("[name]").forEach((el) => {
        const name = el.getAttribute("name");
        if (!name) return;
        el.setAttribute(
          "name",
          name.replace(/VideoSchemaItems\[[^\]]+\]/, `VideoSchemaItems[${index}]`)
        );
      });

      wireThumbnailPicker(row, index);
    });
    setEmptyState();
    refreshMediaPicker();
  }

  function rowInputs(row) {
    const title =
      row.querySelector("[name$='.Title']") ||
      row.querySelector("input:not([type='hidden']):not([type='number']):not([type='date'])");
    return { title };
  }

  function cloneTemplateRow() {
    let source =
      template.content?.querySelector?.("[data-video-schema-row]") ||
      template.content?.firstElementChild ||
      null;

    if (!source) {
      source = template.querySelector("[data-video-schema-row]") || template.firstElementChild;
    }

    return source ? source.cloneNode(true) : null;
  }

  function addRow(focusTitle) {
    const node = cloneTemplateRow();
    if (!node) return null;
    list.appendChild(node);
    reindex();
    if (focusTitle !== false) {
      rowInputs(node).title?.focus();
    }
    return node;
  }

  addBtn.addEventListener("click", (event) => {
    event.preventDefault();
    addRow(true);
  });

  list.addEventListener("click", (event) => {
    const removeBtn = event.target.closest("[data-video-schema-remove]");
    if (!removeBtn) return;
    event.preventDefault();
    const row = removeBtn.closest("[data-video-schema-row]");
    if (!row) return;
    row.remove();
    reindex();
  });

  if (form) {
    form.addEventListener("submit", reindex);
  }

  reindex();
})();
