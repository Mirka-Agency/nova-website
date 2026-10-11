(function () {
  const root = document.querySelector("[data-post-content-guide]");
  if (!root) return;

  const toggle = root.querySelector("[data-content-guide-toggle]");
  const fields = root.querySelector("[data-content-guide-fields]");
  const list = root.querySelector("[data-content-guide-list]");
  const empty = root.querySelector("[data-content-guide-empty]");
  const addBtn = root.querySelector("[data-content-guide-add]");
  const template = document.getElementById("post-content-guide-row-template");
  if (!list || !addBtn || !template || !fields) return;

  const form = root.closest("form");

  function rows() {
    return Array.from(list.querySelectorAll("[data-content-guide-row]"));
  }

  function setEmptyState() {
    if (empty) empty.hidden = rows().length > 0;
  }

  function syncFieldsVisibility() {
    const enabled = !!toggle?.checked;
    fields.hidden = !enabled;
  }

  function reindex() {
    rows().forEach((row, index) => {
      row.querySelectorAll("[name]").forEach((el) => {
        const name = el.getAttribute("name");
        if (!name) return;
        el.setAttribute(
          "name",
          name.replace(/ContentGuideItems\[[^\]]+\]/, `ContentGuideItems[${index}]`)
        );
      });
    });
    setEmptyState();
  }

  function rowInputs(row) {
    const title =
      row.querySelector("[name$='.Title']") ||
      row.querySelector("input");
    const description =
      row.querySelector("[name$='.Description']") ||
      row.querySelectorAll("input")[1];
    return { title, description };
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

  function moveRow(row, direction) {
    if (!row) return;
    if (direction < 0) {
      const prev = row.previousElementSibling;
      if (prev) list.insertBefore(row, prev);
    } else {
      const next = row.nextElementSibling;
      if (next) list.insertBefore(next, row);
    }
    reindex();
  }

  toggle?.addEventListener("change", () => {
    syncFieldsVisibility();
    if (toggle.checked && rows().length === 0) {
      addRow(true);
    }
  });

  addBtn.addEventListener("click", () => {
    addRow(true);
  });

  list.addEventListener("click", (event) => {
    const removeBtn = event.target.closest("[data-content-guide-remove]");
    if (removeBtn) {
      const row = removeBtn.closest("[data-content-guide-row]");
      if (!row) return;
      row.remove();
      reindex();
      return;
    }

    const upBtn = event.target.closest("[data-content-guide-move-up]");
    if (upBtn) {
      moveRow(upBtn.closest("[data-content-guide-row]"), -1);
      return;
    }

    const downBtn = event.target.closest("[data-content-guide-move-down]");
    if (downBtn) {
      moveRow(downBtn.closest("[data-content-guide-row]"), 1);
    }
  });

  if (form) {
    form.addEventListener("submit", reindex);
  }

  syncFieldsVisibility();
  setEmptyState();
})();
