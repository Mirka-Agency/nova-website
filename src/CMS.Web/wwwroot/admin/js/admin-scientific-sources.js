(function () {
  const root = document.querySelector("[data-scientific-sources]");
  if (!root) return;

  const list = root.querySelector("[data-scientific-sources-list]");
  const empty = root.querySelector("[data-scientific-sources-empty]");
  const addBtn = root.querySelector("[data-scientific-sources-add]");
  const template = root.querySelector("template[data-scientific-sources-template]")
    || document.getElementById("scientific-sources-row-template");
  if (!list || !addBtn || !template) return;

  const form = root.closest("form");

  function rows() {
    return Array.from(list.querySelectorAll("[data-scientific-sources-row]"));
  }

  function setEmptyState() {
    if (empty) empty.hidden = rows().length > 0;
  }

  function reindex() {
    rows().forEach((row, index) => {
      row.querySelectorAll("[name]").forEach((el) => {
        const name = el.getAttribute("name");
        if (!name) return;
        el.setAttribute(
          "name",
          name.replace(/SourcesItems\[[^\]]+\]/, `SourcesItems[${index}]`)
        );
      });
    });
    setEmptyState();
  }

  function rowInputs(row) {
    const title =
      row.querySelector("[name$='.Title']") ||
      row.querySelector("input");
    const url =
      row.querySelector("[name$='.Url']") ||
      row.querySelectorAll("input")[1];
    return { title, url };
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

  addBtn.addEventListener("click", () => {
    addRow(true);
  });

  list.addEventListener("click", (event) => {
    const removeBtn = event.target.closest("[data-scientific-sources-remove]");
    if (removeBtn) {
      const row = removeBtn.closest("[data-scientific-sources-row]");
      if (!row) return;
      row.remove();
      reindex();
      return;
    }

    const upBtn = event.target.closest("[data-scientific-sources-move-up]");
    if (upBtn) {
      moveRow(upBtn.closest("[data-scientific-sources-row]"), -1);
      return;
    }

    const downBtn = event.target.closest("[data-scientific-sources-move-down]");
    if (downBtn) {
      moveRow(downBtn.closest("[data-scientific-sources-row]"), 1);
    }
  });

  if (form) {
    form.addEventListener("submit", reindex);
  }

  setEmptyState();
})();
