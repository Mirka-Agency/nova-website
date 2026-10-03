(function () {
  const form = document.querySelector("[data-product-specs]");
  if (!form) return;

  const list = form.querySelector("[data-spec-list]");
  const empty = form.querySelector("[data-spec-empty]");
  const addBtn = form.querySelector("[data-spec-add]");
  const template = document.getElementById("product-spec-row-template");
  if (!list || !addBtn || !template) return;

  function rows() {
    return Array.from(list.querySelectorAll("[data-spec-row]"));
  }

  function setEmptyState() {
    if (empty) empty.hidden = rows().length > 0;
  }

  function reindex() {
    rows().forEach((row, index) => {
      row.querySelectorAll("[name]").forEach((el) => {
        const name = el.getAttribute("name");
        if (!name) return;
        // Match Items[0], Items[__INDEX__], etc.
        el.setAttribute("name", name.replace(/Items\[[^\]]+\]/, `Items[${index}]`));
      });
    });
    setEmptyState();
  }

  function rowInputs(row) {
    const inputs = Array.from(row.querySelectorAll("input[type='text'], input:not([type])"));
    const key = inputs.find((el) => /\.Key$/i.test(el.name || "")) || inputs[0] || null;
    const value = inputs.find((el) => /\.Value$/i.test(el.name || "")) || inputs[1] || null;
    return { key, value };
  }

  function addRow(focusKey) {
    const node = template.content.firstElementChild.cloneNode(true);
    list.appendChild(node);
    reindex();
    if (focusKey !== false) {
      rowInputs(node).key?.focus();
    }
    return node;
  }

  addBtn.addEventListener("click", () => {
    addRow(true);
    if (empty) empty.hidden = true;
  });

  list.addEventListener("click", (event) => {
    const removeBtn = event.target.closest("[data-spec-remove]");
    if (!removeBtn) return;
    const row = removeBtn.closest("[data-spec-row]");
    if (!row) return;
    row.remove();
    reindex();
  });

  list.addEventListener("keydown", (event) => {
    if (event.key !== "Enter") return;
    const input = event.target.closest("input");
    if (!input || !list.contains(input)) return;

    event.preventDefault();

    const row = input.closest("[data-spec-row]");
    if (!row) return;

    const { key, value } = rowInputs(row);
    if (input === key && value) {
      value.focus();
      value.select?.();
      return;
    }

    if (input === value) {
      addRow(true);
    }
  });

  form.addEventListener("submit", reindex);

  setEmptyState();
  if (rows().length === 0) addRow(true);
})();
