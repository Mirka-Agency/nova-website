(function () {
  const root = document.querySelector("[data-team-specialty-path]");
  if (!root) return;

  const list = root.querySelector("[data-specialty-path-list]");
  const empty = root.querySelector("[data-specialty-path-empty]");
  const addBtn = root.querySelector("[data-specialty-path-add]");
  const template = document.getElementById("team-specialty-path-row-template");
  if (!list || !addBtn || !template) return;

  const form = root.closest("form");

  function rows() {
    return Array.from(list.querySelectorAll("[data-specialty-path-row]"));
  }

  function setEmptyState() {
    if (empty) empty.hidden = rows().length > 0;
  }

  function wireIconPicker(row, index) {
    const input = row.querySelector("[data-specialty-path-icon-input]");
    const picker = row.querySelector("[data-specialty-path-icon-picker]");
    const preview = row.querySelector("[data-specialty-path-icon-preview]");
    if (!input || !picker || !preview) return;

    const id = `SpecialtyPathIconUrl_${index}`;
    const selector = `#${id}`;
    input.id = id;
    input.name = `SpecialtyPathItems[${index}].IconUrl`;
    picker.setAttribute("data-target", selector);
    preview.setAttribute("data-media-preview-for", selector);

    if (input.value) {
      let img = preview.querySelector("img");
      if (!img) {
        img = document.createElement("img");
        img.alt = "";
        preview.appendChild(img);
      }
      img.src = input.value;
    } else {
      preview.innerHTML = "";
    }
  }

  function reindex() {
    rows().forEach((row, index) => {
      row.querySelectorAll("[name]").forEach((el) => {
        const name = el.getAttribute("name");
        if (!name) return;
        el.setAttribute(
          "name",
          name.replace(/SpecialtyPathItems\[[^\]]+\]/, `SpecialtyPathItems[${index}]`)
        );
      });
      wireIconPicker(row, index);
    });
    setEmptyState();
    window.AdminMediaPicker?.refreshClearButtons?.();
  }

  function rowInputs(row) {
    const title =
      row.querySelector("[name$='.Title']") || row.querySelector("input:not([type='hidden'])");
    const text =
      row.querySelector("[name$='.Text']") || row.querySelector("textarea");
    return { title, text };
  }

  function addRow(focusTitle) {
    const node = template.content.firstElementChild.cloneNode(true);
    list.appendChild(node);
    reindex();
    if (focusTitle !== false) {
      rowInputs(node).title?.focus();
    }
    return node;
  }

  addBtn.addEventListener("click", () => {
    addRow(true);
  });

  list.addEventListener("click", (event) => {
    const removeBtn = event.target.closest("[data-specialty-path-remove]");
    if (!removeBtn) return;
    const row = removeBtn.closest("[data-specialty-path-row]");
    if (!row) return;
    row.remove();
    reindex();
  });

  if (form) {
    form.addEventListener("submit", reindex);
  }

  reindex();
})();
