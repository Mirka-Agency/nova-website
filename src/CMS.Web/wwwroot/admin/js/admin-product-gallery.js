(function () {
  const root = document.querySelector("[data-product-gallery]");
  if (!root) return;

  const list = root.querySelector("[data-product-gallery-list]");
  const empty = root.querySelector("[data-product-gallery-empty]");
  const staging = root.querySelector("[data-product-gallery-staging]");
  const coverInput = document.getElementById("CoverImageUrl");
  const template = document.getElementById("product-gallery-item-template");
  if (!list || !staging || !template) return;

  function items() {
    return Array.from(list.querySelectorAll("[data-product-gallery-item]"));
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
        // Match Images[0], Images[__INDEX__], etc. (same pattern as product specs).
        el.setAttribute("name", name.replace(/Images\[[^\]]+\]/, `Images[${index}]`));
      });

      const sort = item.querySelector("[data-gallery-sort]");
      if (sort) sort.value = String(index);
    });
    setEmptyState();
  }

  function setMain(item) {
    items().forEach((el) => {
      const checkbox = el.querySelector("[data-gallery-main]");
      const flag = el.querySelector("[data-gallery-main-flag]");
      const isMain = el === item;
      if (checkbox) checkbox.checked = isMain;
      if (flag) flag.value = isMain ? "true" : "false";
    });

    const url = item.querySelector("[data-gallery-url]")?.value;
    if (url && coverInput) {
      coverInput.value = url;
      coverInput.dispatchEvent(new Event("input", { bubbles: true }));
      coverInput.dispatchEvent(new Event("change", { bubbles: true }));
      const preview = document.querySelector('[data-media-preview-for="#CoverImageUrl"]');
      if (preview) preview.innerHTML = `<img src="${url}" alt="" />`;
    }
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

    if (items().length === 1) setMain(node);
    staging.value = "";
  }

  staging.addEventListener("change", () => addImage(staging.value.trim()));
  staging.addEventListener("input", () => {
    if (staging.value.trim()) addImage(staging.value.trim());
  });

  list.addEventListener("click", (event) => {
    const item = event.target.closest("[data-product-gallery-item]");
    if (!item) return;

    if (event.target.closest("[data-gallery-remove]")) {
      const wasMain = item.querySelector("[data-gallery-main]")?.checked;
      item.remove();
      reindex();
      const remaining = items();
      if (wasMain && remaining[0]) setMain(remaining[0]);
      return;
    }
  });

  list.addEventListener("change", (event) => {
    const checkbox = event.target.closest("[data-gallery-main]");
    if (!checkbox) return;
    const item = checkbox.closest("[data-product-gallery-item]");
    if (!item) return;
    if (checkbox.checked) {
      setMain(item);
      return;
    }
    checkbox.checked = true;
  });

  const form = root.closest("form");
  form?.addEventListener("submit", reindex);

  setEmptyState();
})();
