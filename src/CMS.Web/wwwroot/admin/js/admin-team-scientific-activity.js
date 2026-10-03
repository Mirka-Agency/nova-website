(function () {
  const root = document.querySelector("[data-team-scientific-activity]");
  if (!root) return;

  const list = root.querySelector("[data-scientific-activity-list]");
  const empty = root.querySelector("[data-scientific-activity-empty]");
  const addBtn = root.querySelector("[data-scientific-activity-add]");
  const template = document.getElementById("team-scientific-activity-row-template");
  if (!list || !addBtn || !template) return;

  const form = root.closest("form");

  function rows() {
    return Array.from(list.querySelectorAll("[data-scientific-activity-row]"));
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
          name.replace(/ScientificActivityItems\[[^\]]+\]/, `ScientificActivityItems[${index}]`)
        );
      });
    });
    setEmptyState();
  }

  function rowInput(row) {
    return row.querySelector("[name$='.Text']") || row.querySelector("input, textarea");
  }

  function addRow(focusText) {
    const node = template.content.firstElementChild.cloneNode(true);
    list.appendChild(node);
    reindex();
    if (focusText !== false) {
      rowInput(node)?.focus();
    }
    return node;
  }

  addBtn.addEventListener("click", () => {
    addRow(true);
  });

  list.addEventListener("click", (event) => {
    const removeBtn = event.target.closest("[data-scientific-activity-remove]");
    if (!removeBtn) return;
    const row = removeBtn.closest("[data-scientific-activity-row]");
    if (!row) return;
    row.remove();
    reindex();
  });

  if (form) {
    form.addEventListener("submit", reindex);
  }

  setEmptyState();
})();
