(function () {
  const root = document.querySelector("[data-post-faq]");
  if (!root) return;

  const list = root.querySelector("[data-faq-list]");
  const empty = root.querySelector("[data-faq-empty]");
  const addBtn = root.querySelector("[data-faq-add]");
  const template = document.getElementById("post-faq-row-template");
  if (!list || !addBtn || !template) return;

  const form = root.closest("form");

  function rows() {
    return Array.from(list.querySelectorAll("[data-faq-row]"));
  }

  function setEmptyState() {
    if (empty) empty.hidden = rows().length > 0;
  }

  function reindex() {
    rows().forEach((row, index) => {
      row.querySelectorAll("[name]").forEach((el) => {
        const name = el.getAttribute("name");
        if (!name) return;
        el.setAttribute("name", name.replace(/FaqItems\[[^\]]+\]/, `FaqItems[${index}]`));
      });
    });
    setEmptyState();
  }

  function rowInputs(row) {
    const question =
      row.querySelector("[name$='.Question']") ||
      row.querySelector("input");
    const answer =
      row.querySelector("[name$='.Answer']") ||
      row.querySelector("textarea");
    return { question, answer };
  }

  function addRow(focusQuestion) {
    const node = template.content.firstElementChild.cloneNode(true);
    list.appendChild(node);
    reindex();
    if (focusQuestion !== false) {
      rowInputs(node).question?.focus();
    }
    return node;
  }

  addBtn.addEventListener("click", () => {
    addRow(true);
  });

  list.addEventListener("click", (event) => {
    const removeBtn = event.target.closest("[data-faq-remove]");
    if (!removeBtn) return;
    const row = removeBtn.closest("[data-faq-row]");
    if (!row) return;
    row.remove();
    reindex();
  });

  if (form) {
    form.addEventListener("submit", reindex);
  }

  setEmptyState();
})();
