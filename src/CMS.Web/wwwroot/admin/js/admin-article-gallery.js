(function () {
  const root = document.querySelector("[data-article-gallery]");
  if (!root) return;

  const list = root.querySelector("[data-article-gallery-list]");
  const empty = root.querySelector("[data-article-gallery-empty]");
  const staging = root.querySelector("[data-article-gallery-staging]");
  const template = document.getElementById("article-gallery-item-template");
  if (!list || !staging || !template) return;

  function items() {
    return Array.from(list.querySelectorAll("[data-article-gallery-item]"));
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
        el.setAttribute("name", name.replace(/GalleryImages\[[^\]]+\]/, `GalleryImages[${index}]`));
      });
    });
    setEmptyState();
  }

  function addImage(url) {
    if (!url) return;
    if (items().some((item) => item.querySelector("[data-gallery-url]")?.value === url)) {
      staging.value = "";
      return;
    }

    const node = template.content.firstElementChild.cloneNode(true);
    const img = node.querySelector("img");
    const urlInput = node.querySelector("[data-gallery-url]");
    if (img) img.src = url;
    if (urlInput) urlInput.value = url;
    list.appendChild(node);
    reindex();
    staging.value = "";
  }

  staging.addEventListener("change", () => addImage(staging.value.trim()));
  staging.addEventListener("input", () => {
    if (staging.value.trim()) addImage(staging.value.trim());
  });

  list.addEventListener("click", (event) => {
    const item = event.target.closest("[data-article-gallery-item]");
    if (!item) return;

    if (event.target.closest("[data-gallery-remove]")) {
      item.remove();
      reindex();
    }
  });

  const form = root.closest("form");
  form?.addEventListener("submit", reindex);

  setEmptyState();
})();
