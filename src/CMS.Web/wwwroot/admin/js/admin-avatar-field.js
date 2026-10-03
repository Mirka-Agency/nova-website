(function () {
  "use strict";

  function setPreview(preview, url) {
    if (!preview || !url) return;
    let img = preview.querySelector("img");
    if (!img) {
      img = document.createElement("img");
      img.alt = "";
      preview.appendChild(img);
    }
    img.src = url;
  }

  document.querySelectorAll("[data-avatar-clear]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const selector = btn.getAttribute("data-target");
      const gravatar = btn.getAttribute("data-gravatar") || "";
      if (!selector) return;

      const input = document.querySelector(selector);
      if (input) {
        input.value = "";
        input.dispatchEvent(new Event("input", { bubbles: true }));
        input.dispatchEvent(new Event("change", { bubbles: true }));
      }

      const preview = document.querySelector(
        `[data-media-preview-for="${CSS.escape(selector)}"]`
      );
      const fallback =
        preview?.getAttribute("data-avatar-fallback") || gravatar;
      setPreview(preview, fallback);
    });
  });
})();
