/**
 * Scroll-scrubbed text highlight (GSAP + ScrollTrigger).
 *
 * Uses background-clip gradient wipe — no per-character spans —
 * so Persian/Arabic shaping stays intact.
 * Progress only increases on scroll-down; never reverses on scroll-up.
 */
(function () {
  "use strict";

  var SELECTOR = ".nova-scroll-highlight";
  var DEFAULT_FROM = "#6b7280";
  var DEFAULT_TO = "#0f172a";
  var instances = [];

  function prefersReducedMotion() {
    return (
      typeof window.matchMedia === "function" &&
      window.matchMedia("(prefers-reduced-motion: reduce)").matches
    );
  }

  function normalizeHex(value, fallback) {
    var raw = String(value || "")
      .trim()
      .toLowerCase();
    if (/^#[0-9a-f]{6}$/.test(raw)) return raw;
    if (/^#[0-9a-f]{3}$/.test(raw)) {
      return "#" + raw[1] + raw[1] + raw[2] + raw[2] + raw[3] + raw[3];
    }
    if (/^[0-9a-f]{6}$/.test(raw)) return "#" + raw;
    return fallback;
  }

  function readColors(el) {
    return {
      from: normalizeHex(el.getAttribute("data-nova-sh-from"), DEFAULT_FROM),
      to: normalizeHex(el.getAttribute("data-nova-sh-to"), DEFAULT_TO),
    };
  }

  function isRtl(el) {
    var dir = (el.getAttribute("dir") || "").toLowerCase();
    if (dir === "rtl") return true;
    if (dir === "ltr") return false;
    var root = el.closest("[dir]");
    if (root) {
      var d = (root.getAttribute("dir") || "").toLowerCase();
      if (d === "rtl") return true;
      if (d === "ltr") return false;
    }
    return (
      (document.documentElement.getAttribute("dir") || "").toLowerCase() ===
      "rtl"
    );
  }

  function applyProgress(el, progress) {
    var p = progress <= 0 ? 0 : progress >= 1 ? 1 : progress;
    el.style.setProperty("--nova-sh-p", p * 100 + "%");
    el.setAttribute("data-nova-sh-progress", String(Math.round(p * 100)));
  }

  function paintSolid(el, color) {
    el.style.backgroundImage = "none";
    el.style.webkitBackgroundClip = "border-box";
    el.style.backgroundClip = "border-box";
    el.style.color = color;
    el.style.setProperty("--nova-sh-p", "100%");
  }

  function paintGradientSetup(el, from, to, rtl) {
    el.style.setProperty("--nova-sh-from", from);
    el.style.setProperty("--nova-sh-to", to);
    el.style.color = "transparent";
    el.style.webkitBackgroundClip = "text";
    el.style.backgroundClip = "text";
    // Wipe along reading direction: RTL → to left, LTR → to right.
    var angle = rtl ? "to left" : "to right";
    el.style.backgroundImage =
      "linear-gradient(" +
      angle +
      "," +
      "var(--nova-sh-to) 0%," +
      "var(--nova-sh-to) calc(var(--nova-sh-p, 0%) - 6%)," +
      "var(--nova-sh-from) calc(var(--nova-sh-p, 0%) + 6%)," +
      "var(--nova-sh-from) 100%)";
    applyProgress(el, 0);
  }

  function unwrapLegacyChars(el) {
    // Older runtime wrapped each glyph in .nova-sh-char — restore text nodes.
    var chars = el.querySelectorAll(".nova-sh-char");
    if (!chars.length) return;
    chars.forEach(function (span) {
      var text = document.createTextNode(span.textContent || "");
      span.parentNode.replaceChild(text, span);
    });
    el.normalize();
  }

  function initOne(el) {
    if (!el || el.getAttribute("data-nova-sh-ready") === "1") return;
    el.setAttribute("data-nova-sh-ready", "1");

    unwrapLegacyChars(el);

    var colors = readColors(el);
    var rtl = isRtl(el);

    if (prefersReducedMotion()) {
      paintSolid(el, colors.to);
      return;
    }

    if (typeof gsap === "undefined" || typeof ScrollTrigger === "undefined") {
      paintSolid(el, colors.to);
      return;
    }

    paintGradientSetup(el, colors.from, colors.to, rtl);

    var maxProgress = 0;
    var trigger = ScrollTrigger.create({
      trigger: el,
      start: "top 85%",
      end: "bottom+=80% top",
      scrub: 0.6,
      invalidateOnRefresh: true,
      onUpdate: function (self) {
        if (self.progress > maxProgress) {
          maxProgress = self.progress;
          applyProgress(el, maxProgress);
        }
      },
      onRefresh: function (self) {
        if (maxProgress > 0) {
          applyProgress(el, maxProgress);
        } else if (self.progress > 0) {
          maxProgress = self.progress;
          applyProgress(el, maxProgress);
        }
      },
    });

    if (trigger.progress >= 1) {
      maxProgress = 1;
      applyProgress(el, 1);
    } else if (trigger.progress > 0) {
      maxProgress = trigger.progress;
      applyProgress(el, maxProgress);
    }

    instances.push({ el: el, trigger: trigger });
  }

  function initAll(root) {
    var scope = root && root.querySelectorAll ? root : document;
    var nodes = scope.querySelectorAll(SELECTOR);
    if (!nodes.length) return;

    if (prefersReducedMotion()) {
      nodes.forEach(function (el) {
        unwrapLegacyChars(el);
        var colors = readColors(el);
        paintSolid(el, colors.to);
        el.setAttribute("data-nova-sh-ready", "1");
      });
      return;
    }

    if (typeof gsap === "undefined" || typeof ScrollTrigger === "undefined") {
      nodes.forEach(function (el) {
        unwrapLegacyChars(el);
        var colors = readColors(el);
        paintSolid(el, colors.to);
        el.setAttribute("data-nova-sh-ready", "1");
      });
      return;
    }

    gsap.registerPlugin(ScrollTrigger);
    nodes.forEach(initOne);
    ScrollTrigger.refresh();
  }

  function destroyAll() {
    instances.forEach(function (item) {
      if (item.trigger) item.trigger.kill();
    });
    instances = [];
  }

  function boot() {
    initAll(document);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }

  window.NovaScrollHighlightRuntime = {
    init: initAll,
    destroy: destroyAll,
    refresh: function () {
      if (typeof ScrollTrigger !== "undefined") ScrollTrigger.refresh();
    },
  };
})();
