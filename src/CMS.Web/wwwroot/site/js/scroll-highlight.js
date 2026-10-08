/**
 * Scroll-scrubbed text highlight (GSAP + ScrollTrigger).
 * Progress only increases on scroll-down; never reverses on scroll-up.
 */
(function () {
  "use strict";

  var SELECTOR = ".nova-scroll-highlight";
  var CHAR_CLASS = "nova-sh-char";
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

  function parseRgb(hex) {
    var h = normalizeHex(hex, "#000000").slice(1);
    return {
      r: parseInt(h.slice(0, 2), 16),
      g: parseInt(h.slice(2, 4), 16),
      b: parseInt(h.slice(4, 6), 16),
    };
  }

  function lerpColor(from, to, t) {
    var a = parseRgb(from);
    var b = parseRgb(to);
    var k = t < 0 ? 0 : t > 1 ? 1 : t;
    var r = Math.round(a.r + (b.r - a.r) * k);
    var g = Math.round(a.g + (b.g - a.g) * k);
    var bl = Math.round(a.b + (b.b - a.b) * k);
    return "rgb(" + r + "," + g + "," + bl + ")";
  }

  function readColors(el) {
    return {
      from: normalizeHex(el.getAttribute("data-nova-sh-from"), DEFAULT_FROM),
      to: normalizeHex(el.getAttribute("data-nova-sh-to"), DEFAULT_TO),
    };
  }

  function splitTextNodes(root) {
    var chars = [];
    var walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
      acceptNode: function (node) {
        if (!node.nodeValue || !node.nodeValue.length) return NodeFilter.FILTER_REJECT;
        if (!node.parentElement) return NodeFilter.FILTER_REJECT;
        if (node.parentElement.closest("script,style,noscript")) {
          return NodeFilter.FILTER_REJECT;
        }
        return NodeFilter.FILTER_ACCEPT;
      },
    });

    var textNodes = [];
    while (walker.nextNode()) textNodes.push(walker.currentNode);

    textNodes.forEach(function (textNode) {
      var text = textNode.nodeValue;
      if (!text) return;
      var frag = document.createDocumentFragment();
      for (var i = 0; i < text.length; i++) {
        var ch = text.charAt(i);
        if (ch === "\n" || ch === "\r") {
          frag.appendChild(document.createTextNode(ch));
          continue;
        }
        var span = document.createElement("span");
        span.className = CHAR_CLASS;
        span.textContent = ch;
        if (ch === " ") span.style.whiteSpace = "pre";
        frag.appendChild(span);
        chars.push(span);
      }
      textNode.parentNode.replaceChild(frag, textNode);
    });

    return chars;
  }

  function applyProgress(chars, progress, from, to) {
    var n = chars.length;
    if (!n) return;
    var soft = Math.max(3 / n, 0.015);

    for (var i = 0; i < n; i++) {
      var edge = (i + 0.5) / n;
      var t;
      if (progress >= edge + soft / 2) t = 1;
      else if (progress <= edge - soft / 2) t = 0;
      else t = (progress - (edge - soft / 2)) / soft;
      chars[i].style.color = lerpColor(from, to, t);
    }
  }

  function setSolidColor(el, color) {
    el.style.color = color;
    el.querySelectorAll("." + CHAR_CLASS).forEach(function (ch) {
      ch.style.color = color;
    });
  }

  function initOne(el) {
    if (!el || el.getAttribute("data-nova-sh-ready") === "1") return;
    el.setAttribute("data-nova-sh-ready", "1");

    var colors = readColors(el);
    el.style.setProperty("--nova-sh-from", colors.from);
    el.style.setProperty("--nova-sh-to", colors.to);

    if (prefersReducedMotion()) {
      setSolidColor(el, colors.to);
      return;
    }

    if (typeof gsap === "undefined" || typeof ScrollTrigger === "undefined") {
      setSolidColor(el, colors.to);
      return;
    }

    var chars = splitTextNodes(el);
    if (!chars.length) {
      setSolidColor(el, colors.to);
      return;
    }

    applyProgress(chars, 0, colors.from, colors.to);

    var maxProgress = 0;
    var trigger = ScrollTrigger.create({
      trigger: el,
      // Long scrub distance so highlight completes slowly.
      start: "top 80%",
      end: "bottom+=140% top",
      scrub: 0.6,
      invalidateOnRefresh: true,
      onUpdate: function (self) {
        if (self.progress > maxProgress) {
          maxProgress = self.progress;
          applyProgress(chars, maxProgress, colors.from, colors.to);
        }
      },
      onRefresh: function (self) {
        // Keep sticky progress after layout changes.
        if (maxProgress > 0) {
          applyProgress(chars, maxProgress, colors.from, colors.to);
        } else if (self.progress > 0) {
          maxProgress = self.progress;
          applyProgress(chars, maxProgress, colors.from, colors.to);
        }
      },
    });

    // If already past the end on load, complete.
    if (trigger.progress >= 1) {
      maxProgress = 1;
      applyProgress(chars, 1, colors.from, colors.to);
    } else if (trigger.progress > 0) {
      maxProgress = trigger.progress;
      applyProgress(chars, maxProgress, colors.from, colors.to);
    }

    instances.push({ el: el, trigger: trigger, chars: chars });
  }

  function initAll(root) {
    var scope = root && root.querySelectorAll ? root : document;
    var nodes = scope.querySelectorAll(SELECTOR);
    if (!nodes.length) return;

    if (typeof gsap !== "undefined" && typeof ScrollTrigger !== "undefined") {
      gsap.registerPlugin(ScrollTrigger);
    }

    nodes.forEach(initOne);

    if (typeof ScrollTrigger !== "undefined") {
      ScrollTrigger.refresh();
    }
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
