/* Editor content/SEO tabs + panel sub-tabs; analyzer auto-inits via MirkaSeoAnalysis. */
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

  function initEditorTabs(root) {
    var tabs = root.querySelectorAll("[data-post-editor-tab]");
    var panels = root.querySelectorAll("[data-post-editor-panel]");
    if (!tabs.length || !panels.length) return;

    function activate(name) {
      var next = name || "content";
      tabs.forEach(function (tab) {
        tab.classList.toggle("is-active", tab.getAttribute("data-post-editor-tab") === next);
      });
      panels.forEach(function (panel) {
        var match = panel.getAttribute("data-post-editor-panel") === next;
        panel.classList.toggle("is-hidden", !match);
        if (match) panel.removeAttribute("hidden");
        else panel.setAttribute("hidden", "hidden");
      });
      if (next === "content") {
        window.dispatchEvent(new Event("resize"));
      }
    }

    tabs.forEach(function (tab) {
      tab.addEventListener("click", function () {
        activate(tab.getAttribute("data-post-editor-tab"));
      });
    });
  }

  function boot() {
    document.querySelectorAll("[data-seo-panel]").forEach(initPanel);
    document.querySelectorAll("[data-post-editor-tabs]").forEach(function (nav) {
      var root = nav.closest(".admin-post-editor-main") || nav.parentElement;
      if (root) initEditorTabs(root);
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
