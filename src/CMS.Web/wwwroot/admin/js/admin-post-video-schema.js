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

    const url = (input.value || "").trim();
    if (url) {
      let img = preview.querySelector("img");
      if (!img) {
        img = document.createElement("img");
        img.alt = "";
        preview.appendChild(img);
      }
      img.src = url;
    } else {
      preview.innerHTML = "";
    }
  }

  function reindex() {
    rows().forEach((row, index) => {
      const indexLabel = row.querySelector("[data-video-schema-index]");
      if (indexLabel) indexLabel.textContent = String(index + 1);

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
