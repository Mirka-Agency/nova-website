(function () {
  const root = document.querySelector("[data-team-education-path]");
  if (!root) return;

  const list = root.querySelector("[data-education-path-list]");
  const empty = root.querySelector("[data-education-path-empty]");
  const addBtn = root.querySelector("[data-education-path-add]");
  const template = document.getElementById("team-education-path-row-template");
  if (!list || !addBtn || !template) return;

  const form = root.closest("form");

  function rows() {
    return Array.from(list.querySelectorAll("[data-education-path-row]"));
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
          name.replace(/EducationPathItems\[[^\]]+\]/, `EducationPathItems[${index}]`)
        );
      });
    });
    setEmptyState();
  }

  function rowInputs(row) {
    const year = row.querySelector("[name$='.Year']");
    const title = row.querySelector("[name$='.Title']");
    const place = row.querySelector("[name$='.Place']");
    return { year, title, place };
  }

  function addRow(focusYear) {
    const node = template.content.firstElementChild.cloneNode(true);
    list.appendChild(node);
    reindex();
    if (focusYear !== false) {
      rowInputs(node).year?.focus();
    }
    return node;
  }

  addBtn.addEventListener("click", () => {
    addRow(true);
  });

  list.addEventListener("click", (event) => {
    const removeBtn = event.target.closest("[data-education-path-remove]");
    if (!removeBtn) return;
    const row = removeBtn.closest("[data-education-path-row]");
    if (!row) return;
    row.remove();
    reindex();
  });

  if (form) {
    form.addEventListener("submit", reindex);
  }

  setEmptyState();
})();
