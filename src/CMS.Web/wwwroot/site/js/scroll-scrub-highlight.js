/**
 * Scroll-scrub highlight runtime (Karboom-style wipe).
 *
 * Saved markup:
 *   <span class="scroll-scrub-highlight" data-scroll-scrub-color="#e7f2b0">…</span>
 *
 * Progress is tied to scroll (scrub). It only increases — scrolling back never
 * reverses the highlight. When it reaches 100% it stays locked.
 */
(function () {
  "use strict";

  var SELECTOR =
    ".scroll-scrub-highlight, .nova-scroll-highlight:not(.scroll-scrub-highlight)";
  var DEFAULT_COLOR = "#e7f2b0";
  var instances = [];
  var ticking = false;
  var scrollBound = false;

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
    return fallback || DEFAULT_COLOR;
  }

  function readColor(el) {
    return normalizeHex(
      el.getAttribute("data-scroll-scrub-color") ||
        el.getAttribute("data-nova-sh-to") ||
        el.style.getPropertyValue("--scroll-scrub-color"),
      DEFAULT_COLOR
    );
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

  function unwrapRuntimeWrappers(root) {
    root
      .querySelectorAll(".scroll-scrub-line, .scroll-scrub-word")
      .forEach(function (span) {
        var parent = span.parentNode;
        if (!parent) return;
        while (span.firstChild) parent.insertBefore(span.firstChild, span);
        parent.removeChild(span);
      });
    root.normalize();
  }

  function wrapTextNodeWords(textNode) {
    var text = textNode.nodeValue;
    if (!text || !/\S/.test(text)) return;

    var frag = document.createDocumentFragment();
    var re = /(\s+)|(\S+)/g;
    var match;
    while ((match = re.exec(text)) !== null) {
      if (match[1]) {
        frag.appendChild(document.createTextNode(match[1]));
      } else {
        var word = document.createElement("span");
        word.className = "scroll-scrub-word";
        word.textContent = match[2];
        frag.appendChild(word);
      }
    }
    textNode.parentNode.replaceChild(frag, textNode);
  }

  function wrapWords(root) {
    var walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
      acceptNode: function (node) {
        if (!node.nodeValue || !/\S/.test(node.nodeValue)) {
          return NodeFilter.FILTER_REJECT;
        }
        var parent = node.parentElement;
        if (!parent) return NodeFilter.FILTER_REJECT;
        if (parent.closest("script, style")) return NodeFilter.FILTER_REJECT;
        if (parent.classList.contains("scroll-scrub-word")) {
          return NodeFilter.FILTER_REJECT;
        }
        return NodeFilter.FILTER_ACCEPT;
      },
    });

    var nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);
    nodes.forEach(wrapTextNodeWords);
  }

  function nextSignificantSibling(node) {
    var cur = node.nextSibling;
    while (cur) {
      if (cur.nodeType === 1) return cur;
      if (cur.nodeType === 3 && /\S/.test(cur.nodeValue || "")) return cur;
      if (cur.nodeType === 3) {
        cur = cur.nextSibling;
        continue;
      }
      return cur;
    }
    return null;
  }

  function wrapSiblingRun(words, className, attrs) {
    if (!words.length) return null;
    var parent = words[0].parentNode;
    if (!parent) return null;

    var line = document.createElement("span");
    line.className = className;
    if (attrs) {
      Object.keys(attrs).forEach(function (key) {
        line.setAttribute(key, attrs[key]);
      });
    }

    var first = words[0];
    parent.insertBefore(line, first);

    var cursor = first;
    var last = words[words.length - 1];
    while (cursor) {
      var next = cursor.nextSibling;
      line.appendChild(cursor);
      if (cursor === last) break;
      cursor = next;
    }
    return line;
  }

  function groupWordsIntoLines(root) {
    var words = Array.from(root.querySelectorAll(".scroll-scrub-word"));
    if (!words.length) return [];

    var lineIndex = -1;
    var lastTop = null;
    words.forEach(function (word) {
      var top = word.offsetTop;
      if (lastTop === null || Math.abs(top - lastTop) > 3) {
        lineIndex += 1;
        lastTop = top;
      }
      word.setAttribute("data-scroll-scrub-line", String(lineIndex));
    });

    var lineMap = [];
    for (var i = 0; i <= lineIndex; i++) {
      var lineWords = words.filter(function (w) {
        return w.getAttribute("data-scroll-scrub-line") === String(i);
      });

      var groups = [];
      var current = [];
      lineWords.forEach(function (word) {
        if (!current.length) {
          current.push(word);
          return;
        }
        var prev = current[current.length - 1];
        if (
          word.parentNode === prev.parentNode &&
          nextSignificantSibling(prev) === word
        ) {
          current.push(word);
        } else {
          groups.push(current);
          current = [word];
        }
      });
      if (current.length) groups.push(current);

      var lineEls = [];
      groups.forEach(function (group) {
        var lineEl = wrapSiblingRun(group, "scroll-scrub-line", {
          "data-scroll-scrub-line": String(i),
        });
        if (lineEl) lineEls.push(lineEl);
      });
      lineMap.push(lineEls);
    }

    return lineMap;
  }

  function setLineProgress(lineEls, progress) {
    var p = progress <= 0 ? 0 : progress >= 1 ? 1 : progress;
    var pct = p * 100 + "%";
    lineEls.forEach(function (line) {
      // Never shrink a line that already reached full wipe.
      if (line.classList.contains("is-scroll-scrub-on")) {
        line.style.setProperty("--scroll-scrub-p", "100%");
        return;
      }
      line.style.setProperty("--scroll-scrub-p", pct);
      if (p >= 1) line.classList.add("is-scroll-scrub-on");
    });
  }

  function applyProgress(instance, progress) {
    var p = progress <= 0 ? 0 : progress >= 1 ? 1 : progress;
    if (p < instance.maxProgress) p = instance.maxProgress;
    if (p > instance.maxProgress) instance.maxProgress = p;

    var lines = instance.lineMap;
    var n = lines.length || 1;

    if (instance.maxProgress >= 1) {
      for (var i = 0; i < n; i++) setLineProgress(lines[i], 1);
      lockDone(instance);
      return;
    }

    // Spread progress evenly across visual lines for a continuous wipe.
    var scaled = instance.maxProgress * n;
    for (var li = 0; li < n; li++) {
      setLineProgress(lines[li], scaled - li);
    }

    instance.el.setAttribute(
      "data-scroll-scrub-progress",
      String(Math.round(instance.maxProgress * 100))
    );
  }

  function lockDone(instance) {
    if (instance.done) return;
    instance.done = true;
    instance.maxProgress = 1;
    instance.el.classList.add("is-scroll-scrub-done");
    instance.el.setAttribute("data-scroll-scrub-progress", "100");
    instance.el.setAttribute("data-scroll-scrub-animated", "1");
  }

  /**
   * Scrub while the block rises through the viewport.
   * Wider window → slower, more even fill. Progress is still clamped by
   * maxProgress so scrolling back never reverses.
   */
  function computeScrollProgress(el) {
    var rect = el.getBoundingClientRect();
    var vh = window.innerHeight || document.documentElement.clientHeight || 1;
    // Begin near bottom; longer travel = slower fill.
    var start = vh * 0.98;
    var travel = Math.max(vh * 1.15, rect.height + vh * 0.55);
    var end = start - travel;
    if (start === end) return rect.top < start ? 1 : 0;
    var raw = (start - rect.top) / (start - end);
    if (raw <= 0) return 0;
    if (raw >= 1) return 1;
    return raw;
  }

  function updateAll() {
    ticking = false;
    var active = false;
    for (var i = 0; i < instances.length; i++) {
      var inst = instances[i];
      if (inst.done) continue;
      active = true;
      applyProgress(inst, computeScrollProgress(inst.el));
    }
    if (!active) unbindScroll();
  }

  function requestUpdate() {
    if (ticking) return;
    ticking = true;
    window.requestAnimationFrame(updateAll);
  }

  function bindScroll() {
    if (scrollBound) return;
    scrollBound = true;
    window.addEventListener("scroll", requestUpdate, { passive: true });
    window.addEventListener("resize", requestUpdate, { passive: true });
  }

  function unbindScroll() {
    if (!scrollBound) return;
    var anyActive = instances.some(function (inst) {
      return !inst.done;
    });
    if (anyActive) return;
    scrollBound = false;
    window.removeEventListener("scroll", requestUpdate);
    window.removeEventListener("resize", requestUpdate);
  }

  function prepare(el) {
    if (!el || el.getAttribute("data-scroll-scrub-ready") === "1") return;
    el.setAttribute("data-scroll-scrub-ready", "1");

    unwrapRuntimeWrappers(el);

    var color = readColor(el);
    el.style.setProperty("--scroll-scrub-color", color);
    el.setAttribute("data-scroll-scrub-color", color);
    el.classList.add("scroll-scrub-highlight");
    el.classList.toggle("is-scroll-scrub-rtl", isRtl(el));
    el.classList.toggle("is-scroll-scrub-ltr", !isRtl(el));

    wrapWords(el);
    var lineMap = groupWordsIntoLines(el);
    if (!lineMap.length) {
      // No words — still paint solid final state.
      el.classList.add("is-scroll-scrub-done");
      return;
    }

    var instance = {
      el: el,
      lineMap: lineMap,
      maxProgress: 0,
      done: false,
    };
    instances.push(instance);

    // Start empty (no flash of full highlight).
    for (var i = 0; i < lineMap.length; i++) setLineProgress(lineMap[i], 0);

    if (prefersReducedMotion()) {
      applyProgress(instance, 1);
      return;
    }

    bindScroll();
    applyProgress(instance, computeScrollProgress(el));
  }

  function initAll(root) {
    var scope = root && root.querySelectorAll ? root : document;
    var nodes = scope.querySelectorAll(SELECTOR);
    nodes.forEach(prepare);
    requestUpdate();
  }

  function destroyAll() {
    unbindScroll();
    instances = [];
    ticking = false;
    scrollBound = false;
  }

  function boot() {
    initAll(document);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }

  window.ScrollScrubHighlightRuntime = {
    init: initAll,
    destroy: destroyAll,
    refresh: requestUpdate,
  };
  window.NovaScrollHighlightRuntime = window.ScrollScrubHighlightRuntime;
})();
