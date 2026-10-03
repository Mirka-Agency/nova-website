/* Bootstrap tabs; analyzer auto-inits via MirkaSeoAnalysis.autoInit in the bundle. */
(function () {
  "use strict";

  function initPanel(panel) {
    var tabs = panel.querySelectorAll("[data-seo-tab]");
    tabs.forEach(function (tab) {
      tab.addEventListener("click", function () {
        var name = tab.getAttribute("data-seo-tab");
        tabs.forEach(function (t) {
          t.classList.toggle("is-active", t === tab);
        });
        panel.querySelectorAll("[data-seo-tab-panel]").forEach(function (p) {
          var match = p.getAttribute("data-seo-tab-panel") === name;
          p.classList.toggle("is-active", match);
          if (match) p.removeAttribute("hidden");
          else p.setAttribute("hidden", "hidden");
        });
      });
    });

    if (window.MirkaSeoAnalysis && typeof window.MirkaSeoAnalysis.init === "function") {
      try {
        window.MirkaSeoAnalysis.init(panel);
      } catch (err) {
        console.warn("[seo-panel] init failed", err);
      }
    }
  }

  function boot() {
    document.querySelectorAll("[data-seo-panel]").forEach(initPanel);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
