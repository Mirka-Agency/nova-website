(function () {
  "use strict";

  function enhanceVideoCovers(root) {
    const scope = root || document;
    const nodes = scope.querySelectorAll("[data-nova-video-poster]");
    nodes.forEach(function (wrap) {
      if (wrap.getAttribute("data-nova-video-cover-ready") === "1") return;

      const poster = String(wrap.getAttribute("data-nova-video-poster") || "").trim();
      if (!poster) return;

      const video = wrap.querySelector("video");
      if (video) {
        if (!video.getAttribute("poster")) {
          video.setAttribute("poster", poster);
        }
        wrap.setAttribute("data-nova-video-cover-ready", "1");
        return;
      }

      const iframe = wrap.querySelector("iframe");
      if (!iframe) return;

      wrap.classList.add("nova-video", "nova-video--covered");
      wrap.setAttribute("data-nova-video-cover-ready", "1");

      const cover = document.createElement("button");
      cover.type = "button";
      cover.className = "nova-video__cover";
      cover.setAttribute("aria-label", "پخش ویدیو");
      cover.style.backgroundImage = "url(" + JSON.stringify(poster) + ")";

      const play = document.createElement("span");
      play.className = "nova-video__play";
      play.setAttribute("aria-hidden", "true");
      play.innerHTML =
        '<svg viewBox="0 0 24 24" fill="currentColor" width="28" height="28"><path d="M8 5.14v13.72L19.03 12 8 5.14z"/></svg>';
      cover.appendChild(play);

      cover.addEventListener("click", function () {
        wrap.classList.add("is-playing");
        cover.remove();
        try {
          const src = iframe.getAttribute("src") || "";
          if (src && !/[?&]autoplay=1/i.test(src)) {
            iframe.setAttribute(
              "src",
              src + (src.indexOf("?") >= 0 ? "&" : "?") + "autoplay=1"
            );
          }
        } catch (_) {
          /* ignore */
        }
      });

      wrap.appendChild(cover);
    });
  }

  function boot() {
    enhanceVideoCovers(document);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }

  window.NovaBlogVideoCover = { enhance: enhanceVideoCovers };
})();
