(function () {
  const root = document.querySelector("[data-article-event-info]");
  if (!root) return;

  const list = root.querySelector("[data-event-info-list]");
  const empty = root.querySelector("[data-event-info-empty]");
  const addBtn = root.querySelector("[data-event-info-add]");
  const template = document.getElementById("article-event-info-item-template");
  if (!list || !addBtn || !template) return;

  function items() {
    return Array.from(list.querySelectorAll("[data-event-info-item]"));
  }

  function setEmptyState() {
    if (!empty) return;
    empty.hidden = items().length > 0;
  }

  function reindex() {
    items().forEach((item, index) => {
      item.querySelectorAll("[name]").forEach((el) => {
        const name = el.getAttribute("name");
        if (!name) return;
        el.setAttribute("name", name.replace(/EventInfoItems\[[^\]]+\]/, `EventInfoItems[${index}]`));
      });
    });
    setEmptyState();
  }

  function addItem() {
    const node = template.content.firstElementChild.cloneNode(true);
    list.appendChild(node);
    reindex();
    node.querySelector("[data-event-info-label]")?.focus();
  }

  addBtn.addEventListener("click", addItem);

  list.addEventListener("click", (event) => {
    const item = event.target.closest("[data-event-info-item]");
    if (!item) return;

    if (event.target.closest("[data-event-info-remove]")) {
      item.remove();
      reindex();
    }
  });

  const form = root.closest("form");
  form?.addEventListener("submit", reindex);

  setEmptyState();
})();
