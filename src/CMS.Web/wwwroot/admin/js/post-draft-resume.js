(function () {
  "use strict";

  const modal = document.getElementById("draft-resume-modal");
  if (!modal) return;

  const form = document.querySelector("form[data-post-autosave]");

  function continueDraft() {
    modal.hidden = true;
    if (form) {
      form.removeAttribute("data-autosave-hold");
      form.dispatchEvent(new CustomEvent("post-autosave:resume", { bubbles: true }));
    }

    if (window.history?.replaceState) {
      const url = new URL(window.location.href);
      url.searchParams.delete("resumeDraft");
      window.history.replaceState({}, "", url.pathname + url.search + url.hash);
    }
  }

  modal.querySelectorAll("[data-draft-resume-continue]").forEach((el) => {
    el.addEventListener("click", continueDraft);
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && !modal.hidden) {
      continueDraft();
    }
  });
})();
