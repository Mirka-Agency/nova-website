(function () {
  "use strict";

  var header = document.querySelector(".site-header");
  var navToggle = document.querySelector(".nav-toggle");
  var mobileNav = document.querySelector(".mobile-nav");
  var navBackdrop = document.querySelector(".nav-backdrop");
  var faqButtons = document.querySelectorAll(".faq-item__question");
  var revealItems = document.querySelectorAll(".reveal");
  var testimonialSliders = document.querySelectorAll("[data-testimonials-slider]");
  var voiceCards = document.querySelectorAll("[data-voice-card]");

  function toPersianDigits(value) {
    return String(value).replace(/\d/g, function (digit) {
      return "۰۱۲۳۴۵۶۷۸۹"[digit];
    });
  }

  function formatVoiceTime(totalSeconds) {
    var minutes = Math.floor(totalSeconds / 60);
    var seconds = Math.max(0, Math.floor(totalSeconds % 60));
    return toPersianDigits(minutes + ":" + String(seconds).padStart(2, "0"));
  }

  function createUiSelect(root, config) {
    if (!root) return null;

    config = config || {};
    var placeholder = config.placeholder || "";
    var defaultValue = config.defaultValue;

    var input = root.querySelector('input[type="hidden"]');
    var trigger =
      root.querySelector("[data-ui-select-trigger]") ||
      root.querySelector("[data-booking-select-trigger]");
    var valueLabel =
      root.querySelector("[data-ui-select-value]") ||
      root.querySelector("[data-booking-select-value]");
    var menu =
      root.querySelector("[data-ui-select-menu]") ||
      root.querySelector("[data-booking-select-menu]");
    var options = Array.prototype.slice.call(root.querySelectorAll('[role="option"]'));

    if (!trigger || !menu || !options.length) return null;

    function close() {
      root.classList.remove("is-open");
      menu.hidden = true;
      trigger.setAttribute("aria-expanded", "false");
      options.forEach(function (option) {
        option.classList.remove("is-active");
      });
    }

    function open() {
      root.classList.add("is-open");
      menu.hidden = false;
      trigger.setAttribute("aria-expanded", "true");
      var selected = options.find(function (option) {
        return option.classList.contains("is-selected");
      });
      var focusTarget = selected || options[0];
      if (focusTarget) {
        focusTarget.classList.add("is-active");
        focusTarget.focus();
      }
    }

    function setValue(value, label) {
      if (input) input.value = value == null ? "" : value;
      if (valueLabel) {
        valueLabel.textContent = label || placeholder || "";
        valueLabel.classList.toggle("is-placeholder", !value);
      }

      options.forEach(function (option) {
        var isSelected = option.getAttribute("data-value") === value;
        option.classList.toggle("is-selected", isSelected);
        option.setAttribute("aria-selected", isSelected ? "true" : "false");
      });
    }

    function reset() {
      if (defaultValue != null) {
        var match = options.find(function (option) {
          return option.getAttribute("data-value") === defaultValue;
        });
        if (match) {
          setValue(match.getAttribute("data-value"), match.textContent.trim());
          close();
          return;
        }
      }

      setValue("", placeholder);
      close();
    }

    function getValue() {
      return input ? input.value : "";
    }

    trigger.addEventListener("click", function () {
      if (root.classList.contains("is-open")) {
        close();
        trigger.focus();
      } else {
        open();
      }
    });

    options.forEach(function (option, index) {
      option.addEventListener("click", function () {
        setValue(option.getAttribute("data-value"), option.textContent.trim());
        close();
        trigger.focus();
      });

      option.addEventListener("keydown", function (event) {
        if (event.key === "ArrowDown") {
          event.preventDefault();
          var next = options[Math.min(index + 1, options.length - 1)];
          options.forEach(function (item) {
            item.classList.remove("is-active");
          });
          next.classList.add("is-active");
          next.focus();
        } else if (event.key === "ArrowUp") {
          event.preventDefault();
          var prev = options[Math.max(index - 1, 0)];
          options.forEach(function (item) {
            item.classList.remove("is-active");
          });
          prev.classList.add("is-active");
          prev.focus();
        } else if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          setValue(option.getAttribute("data-value"), option.textContent.trim());
          close();
          trigger.focus();
        } else if (event.key === "Escape") {
          event.preventDefault();
          event.stopPropagation();
          close();
          trigger.focus();
        }
      });
    });

    document.addEventListener("click", function (event) {
      if (!root.classList.contains("is-open")) return;
      if (!root.contains(event.target)) close();
    });

    return {
      root: root,
      trigger: trigger,
      close: close,
      open: open,
      setValue: setValue,
      reset: reset,
      getValue: getValue,
      isOpen: function () {
        return root.classList.contains("is-open");
      },
    };
  }

  function setMobileNavOpen(isOpen) {
    if (!navToggle || !mobileNav) return;

    mobileNav.classList.toggle("is-open", isOpen);
    navToggle.setAttribute("aria-expanded", isOpen ? "true" : "false");
    navToggle.setAttribute("aria-label", isOpen ? "بستن منو" : "باز کردن منو");
    mobileNav.setAttribute("aria-hidden", isOpen ? "false" : "true");
    document.body.classList.toggle("nav-open", isOpen);

    if (!isOpen) {
      mobileNav.querySelectorAll(".has-dropdown.is-open").forEach(function (item) {
        item.classList.remove("is-open");
        var openBtn = item.querySelector(".mobile-nav__submenu-toggle");
        if (openBtn) openBtn.setAttribute("aria-expanded", "false");
      });
    }

    if (navBackdrop) {
      navBackdrop.classList.toggle("is-visible", isOpen);
      navBackdrop.setAttribute("aria-hidden", isOpen ? "false" : "true");
    }
  }

  function onScroll() {
    if (header) {
      header.classList.toggle("is-scrolled", window.scrollY > 24);
    }

    var floatCall = document.querySelector("[data-float-call]");
    if (!floatCall) return;

    var show = window.scrollY > 280;
    var footer = document.querySelector(".site-footer");

    if (show && footer) {
      var footerTop = footer.getBoundingClientRect().top;
      if (footerTop < window.innerHeight - 24) {
        show = false;
      }
    }

    floatCall.classList.toggle("is-visible", show);
    floatCall.setAttribute("aria-hidden", show ? "false" : "true");

    floatCall.querySelectorAll("a, button").forEach(function (el) {
      el.tabIndex = show ? 0 : -1;
    });
  }

  if (navToggle && mobileNav) {
    navToggle.addEventListener("click", function () {
      setMobileNavOpen(!mobileNav.classList.contains("is-open"));
    });

    if (navBackdrop) {
      navBackdrop.addEventListener("click", function () {
        setMobileNavOpen(false);
      });
    }

    mobileNav.querySelectorAll("a").forEach(function (link) {
      link.addEventListener("click", function () {
        setMobileNavOpen(false);
      });
    });

    mobileNav.querySelectorAll(".mobile-nav__submenu-toggle").forEach(function (button) {
      button.addEventListener("click", function (event) {
        event.preventDefault();
        event.stopPropagation();
        var item = button.closest(".has-dropdown");
        if (!item) return;

        var willOpen = !item.classList.contains("is-open");
        mobileNav.querySelectorAll(".has-dropdown.is-open").forEach(function (openItem) {
          if (openItem === item) return;
          openItem.classList.remove("is-open");
          var openBtn = openItem.querySelector(".mobile-nav__submenu-toggle");
          if (openBtn) openBtn.setAttribute("aria-expanded", "false");
        });

        item.classList.toggle("is-open", willOpen);
        button.setAttribute("aria-expanded", willOpen ? "true" : "false");
      });
    });
  }

  // Desktop: submenu opens via CSS :hover; parent link always goes to its href.
  document.addEventListener("click", function (event) {
    if (event.target.closest(".desktop-nav .has-dropdown")) return;
    document.querySelectorAll(".desktop-nav .has-dropdown.is-open").forEach(function (item) {
      item.classList.remove("is-open");
      var link = item.querySelector(":scope > a");
      if (link) link.setAttribute("aria-expanded", "false");
    });
  });

  faqButtons.forEach(function (button) {
    button.addEventListener("click", function () {
      var item = button.closest(".faq-item");
      if (!item) return;

      var isOpen = item.classList.contains("is-open");
      var parent = item.parentElement;

      if (parent) {
        parent.querySelectorAll(".faq-item.is-open").forEach(function (openItem) {
          openItem.classList.remove("is-open");
          var openBtn = openItem.querySelector(".faq-item__question");
          if (openBtn) openBtn.setAttribute("aria-expanded", "false");
        });
      }

      if (!isOpen) {
        item.classList.add("is-open");
        button.setAttribute("aria-expanded", "true");
      }
    });
  });

  testimonialSliders.forEach(function (slider) {
    var viewport = slider.querySelector(".testimonials-slider__viewport");
    var slides = Array.from(slider.querySelectorAll(".testimonial-slide"));
    var prevButton = slider.querySelector("[data-testimonials-prev]");
    var nextButton = slider.querySelector("[data-testimonials-next]");
    var counter = slider.querySelector("[data-testimonials-counter]");
    var activeIndex = 0;
    var ticking = false;

    if (!viewport || !slides.length || !prevButton || !nextButton) return;

    function toPersianNumber(number) {
      return toPersianDigits(number);
    }

    function isFullyVisible(slide, viewportRect) {
      var rect = slide.getBoundingClientRect();
      return rect.left >= viewportRect.left - 1 && rect.right <= viewportRect.right + 1;
    }

    function updateSliderState() {
      var viewportRect = viewport.getBoundingClientRect();
      var nearestDistance = Infinity;

      slides.forEach(function (slide, index) {
        var distance = Math.abs(slide.getBoundingClientRect().right - viewportRect.right);
        if (distance < nearestDistance) {
          nearestDistance = distance;
          activeIndex = index;
        }
      });

      prevButton.disabled = isFullyVisible(slides[0], viewportRect);
      nextButton.disabled = isFullyVisible(slides[slides.length - 1], viewportRect);

      if (counter) {
        counter.textContent =
          toPersianNumber(activeIndex + 1) + " / " + toPersianNumber(slides.length);
      }
    }

    function goToSlide(index) {
      activeIndex = Math.max(0, Math.min(index, slides.length - 1));
      slides[activeIndex].scrollIntoView({
        behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth",
        block: "nearest",
        inline: "start"
      });
    }

    var scrollStep = 2;

    prevButton.addEventListener("click", function () {
      goToSlide(activeIndex - scrollStep);
    });

    nextButton.addEventListener("click", function () {
      goToSlide(activeIndex + scrollStep);
    });

    viewport.addEventListener(
      "scroll",
      function () {
        if (ticking) return;
        ticking = true;
        window.requestAnimationFrame(function () {
          updateSliderState();
          ticking = false;
        });
      },
      { passive: true }
    );

    viewport.addEventListener("keydown", function (event) {
      if (event.key === "ArrowLeft") {
        event.preventDefault();
        goToSlide(activeIndex + scrollStep);
      } else if (event.key === "ArrowRight") {
        event.preventDefault();
        goToSlide(activeIndex - scrollStep);
      }
    });

    window.addEventListener("resize", updateSliderState);
    updateSliderState();
  });

  (function initHonorsSlider() {
    var sliders = document.querySelectorAll("[data-honors-slider]");
    if (!sliders.length) return;

    sliders.forEach(function (slider) {
      var viewport = slider.querySelector(".about-honors__viewport");
      var slides = Array.from(slider.querySelectorAll(".about-honors__item"));
      var prevButton = slider.querySelector("[data-honors-prev]");
      var nextButton = slider.querySelector("[data-honors-next]");
      var counter = slider.querySelector("[data-honors-counter]");
      var activeIndex = 0;
      var ticking = false;

      if (!viewport || !slides.length || !prevButton || !nextButton) return;

      function isFullyVisible(slide, viewportRect) {
        var rect = slide.getBoundingClientRect();
        return rect.left >= viewportRect.left - 1 && rect.right <= viewportRect.right + 1;
      }

      function updateSliderState() {
        var viewportRect = viewport.getBoundingClientRect();
        var nearestDistance = Infinity;

        slides.forEach(function (slide, index) {
          var distance = Math.abs(slide.getBoundingClientRect().right - viewportRect.right);
          if (distance < nearestDistance) {
            nearestDistance = distance;
            activeIndex = index;
          }
        });

        prevButton.disabled = isFullyVisible(slides[0], viewportRect);
        nextButton.disabled = isFullyVisible(slides[slides.length - 1], viewportRect);

        if (counter) {
          counter.textContent =
            toPersianDigits(activeIndex + 1) + " از " + toPersianDigits(slides.length);
        }
      }

      function goToSlide(index) {
        activeIndex = Math.max(0, Math.min(index, slides.length - 1));
        slides[activeIndex].scrollIntoView({
          behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth",
          block: "nearest",
          inline: "start"
        });
      }

      prevButton.addEventListener("click", function () {
        goToSlide(activeIndex - 1);
      });

      nextButton.addEventListener("click", function () {
        goToSlide(activeIndex + 1);
      });

      viewport.addEventListener(
        "scroll",
        function () {
          if (ticking) return;
          ticking = true;
          window.requestAnimationFrame(function () {
            updateSliderState();
            ticking = false;
          });
        },
        { passive: true }
      );

      viewport.addEventListener("keydown", function (event) {
        if (event.key === "ArrowLeft") {
          event.preventDefault();
          goToSlide(activeIndex + 1);
        } else if (event.key === "ArrowRight") {
          event.preventDefault();
          goToSlide(activeIndex - 1);
        }
      });

      window.addEventListener("resize", updateSliderState);
      updateSliderState();
    });
  })();

  (function initVoicePlayers() {
    if (!voiceCards.length) return;

    var activeCard = null;
    var activeAudio = null;
    var rafId = null;
    var startedAt = 0;
    var elapsedBeforePause = 0;
    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function getCardParts(card) {
      return {
        playButton: card.querySelector("[data-voice-play]"),
        progress: card.querySelector("[data-voice-progress]"),
        current: card.querySelector("[data-voice-current]"),
        total: card.querySelector("[data-voice-total]"),
        duration: Number(card.getAttribute("data-duration")) || 45,
        audioSrc: (card.getAttribute("data-audio-src") || "").trim()
      };
    }

    function getOrCreateAudio(card) {
      var parts = getCardParts(card);
      if (!parts.audioSrc) return null;
      if (card._voiceAudio instanceof HTMLAudioElement) return card._voiceAudio;

      var audio = new Audio(parts.audioSrc);
      audio.preload = "metadata";
      audio.addEventListener("loadedmetadata", function () {
        if (!isFinite(audio.duration) || audio.duration <= 0) return;
        card.setAttribute("data-duration", String(Math.round(audio.duration)));
        var label = formatVoiceTime(audio.duration);
        if (parts.current && !card.classList.contains("is-playing")) {
          parts.current.textContent = label;
        }
        if (parts.total) parts.total.textContent = label;
      });
      audio.addEventListener("ended", function () {
        if (activeCard === card) stopActive();
      });
      card._voiceAudio = audio;
      return audio;
    }

    function resetCard(card) {
      var parts = getCardParts(card);
      var audio = card._voiceAudio;
      card.classList.remove("is-playing");
      if (parts.playButton) {
        parts.playButton.setAttribute("aria-pressed", "false");
      }
      if (parts.progress) {
        parts.progress.style.width = "0%";
      }
      if (audio) {
        audio.pause();
        audio.currentTime = 0;
      }
      if (parts.current) {
        var duration = audio && isFinite(audio.duration) && audio.duration > 0
          ? audio.duration
          : parts.duration;
        parts.current.textContent = formatVoiceTime(duration);
      }
    }

    function stopActive() {
      if (rafId) {
        window.cancelAnimationFrame(rafId);
        rafId = null;
      }
      if (activeAudio) {
        activeAudio.pause();
        activeAudio = null;
      }
      if (activeCard) {
        resetCard(activeCard);
        activeCard = null;
      }
      elapsedBeforePause = 0;
      startedAt = 0;
    }

    function tickSimulated() {
      if (!activeCard) return;

      var parts = getCardParts(activeCard);
      var elapsed = elapsedBeforePause + (performance.now() - startedAt) / 1000;
      var progress = Math.min(elapsed / parts.duration, 1);

      if (parts.progress) {
        parts.progress.style.width = progress * 100 + "%";
      }
      if (parts.current) {
        parts.current.textContent = formatVoiceTime(elapsed);
      }

      if (progress >= 1) {
        stopActive();
        return;
      }

      rafId = window.requestAnimationFrame(tickSimulated);
    }

    function tickAudio() {
      if (!activeCard || !activeAudio) return;

      var parts = getCardParts(activeCard);
      var duration = isFinite(activeAudio.duration) && activeAudio.duration > 0
        ? activeAudio.duration
        : parts.duration;
      var current = activeAudio.currentTime || 0;
      var progress = duration > 0 ? Math.min(current / duration, 1) : 0;

      if (parts.progress) {
        parts.progress.style.width = progress * 100 + "%";
      }
      if (parts.current) {
        parts.current.textContent = formatVoiceTime(current);
      }

      if (!activeAudio.paused && !activeAudio.ended) {
        rafId = window.requestAnimationFrame(tickAudio);
      }
    }

    function playCard(card) {
      var parts = getCardParts(card);
      var audio = getOrCreateAudio(card);

      if (activeCard && activeCard !== card) {
        stopActive();
      }

      if (activeCard === card) {
        if (audio) {
          audio.pause();
          activeAudio = null;
        } else {
          elapsedBeforePause += (performance.now() - startedAt) / 1000;
        }
        if (rafId) {
          window.cancelAnimationFrame(rafId);
          rafId = null;
        }
        card.classList.remove("is-playing");
        if (parts.playButton) {
          parts.playButton.setAttribute("aria-pressed", "false");
        }
        activeCard = null;
        return;
      }

      activeCard = card;
      card.classList.add("is-playing");
      if (parts.playButton) {
        parts.playButton.setAttribute("aria-pressed", "true");
      }

      if (audio) {
        activeAudio = audio;
        var playPromise = audio.play();
        if (playPromise && typeof playPromise.catch === "function") {
          playPromise.catch(function () {
            stopActive();
          });
        }
        rafId = window.requestAnimationFrame(tickAudio);
        return;
      }

      if (reduceMotion) {
        if (parts.progress) {
          parts.progress.style.width = "100%";
        }
        if (parts.current) {
          parts.current.textContent = formatVoiceTime(parts.duration);
        }
        window.setTimeout(stopActive, 450);
        return;
      }

      startedAt = performance.now();
      rafId = window.requestAnimationFrame(tickSimulated);
    }

    voiceCards.forEach(function (card) {
      var parts = getCardParts(card);
      getOrCreateAudio(card);
      resetCard(card);

      if (!parts.playButton) return;

      parts.playButton.addEventListener("click", function () {
        playCard(card);
      });
    });
  })();

  if ("IntersectionObserver" in window && revealItems.length) {
    var observer = new IntersectionObserver(
      function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.12, rootMargin: "0px 0px -40px 0px" }
    );

    revealItems.forEach(function (el) {
      observer.observe(el);
    });
  } else {
    revealItems.forEach(function (el) {
      el.classList.add("is-visible");
    });
  }

  var doctorPicker = document.querySelector(".doctor-picker");
  if (doctorPicker) {
    var doctorTabs = doctorPicker.querySelectorAll("[data-doctor-tab]");
    var doctorPanels = document.querySelectorAll("[data-doctor-panel]");

    function activateDoctor(id) {
      doctorTabs.forEach(function (tab) {
        var isActive = tab.getAttribute("data-doctor-tab") === id;
        tab.classList.toggle("is-active", isActive);
        tab.setAttribute("aria-selected", isActive ? "true" : "false");
        tab.tabIndex = isActive ? 0 : -1;
      });

      doctorPanels.forEach(function (panel) {
        var isActive = panel.getAttribute("data-doctor-panel") === id;
        panel.classList.toggle("is-active", isActive);
        panel.setAttribute("aria-hidden", isActive ? "false" : "true");
        if (isActive) {
          panel.removeAttribute("inert");
        } else {
          panel.setAttribute("inert", "");
        }
      });
    }

    doctorTabs.forEach(function (tab) {
      tab.addEventListener("click", function () {
        activateDoctor(tab.getAttribute("data-doctor-tab"));
      });

      tab.addEventListener("keydown", function (event) {
        var tabs = Array.prototype.slice.call(doctorTabs);
        var index = tabs.indexOf(tab);
        var nextIndex = index;

        if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
          event.preventDefault();
          if (event.key === "ArrowLeft") {
            nextIndex = (index + 1) % tabs.length;
          } else {
            nextIndex = (index - 1 + tabs.length) % tabs.length;
          }
          tabs[nextIndex].focus();
          activateDoctor(tabs[nextIndex].getAttribute("data-doctor-tab"));
        }
      });
    });
  }

  window.addEventListener("scroll", onScroll, { passive: true });
  onScroll();

  (function initHeroVideo() {
    var media = document.querySelector("[data-hero-video]");
    if (!media) return;

    var video = media.querySelector(".hero-media__video");
    var playButton = media.querySelector("[data-hero-video-play]");
    if (!video || !playButton) return;

    var hasStarted = false;

    function setPlaying(isPlaying) {
      media.classList.toggle("is-playing", isPlaying);
      playButton.setAttribute("aria-hidden", isPlaying ? "true" : "false");
      playButton.tabIndex = isPlaying ? -1 : 0;
    }

    function enableControls() {
      video.setAttribute("controls", "");
    }

    function resetToIdle() {
      hasStarted = false;
      setPlaying(false);
      video.removeAttribute("controls");
      video.currentTime = 0;
    }

    function playVideo() {
      var playPromise = video.play();
      if (playPromise && typeof playPromise.then === "function") {
        playPromise
          .then(function () {
            hasStarted = true;
            setPlaying(true);
            enableControls();
          })
          .catch(function () {
            setPlaying(false);
          });
      } else {
        hasStarted = true;
        setPlaying(true);
        enableControls();
      }
    }

    playButton.addEventListener("click", function () {
      playVideo();
    });

    video.addEventListener("play", function () {
      hasStarted = true;
      setPlaying(true);
      enableControls();
    });

    video.addEventListener("pause", function () {
      if (video.ended) return;
      // Keep overlay hidden and controls visible so seeking stays usable.
      if (hasStarted) {
        setPlaying(true);
        enableControls();
      }
    });

    video.addEventListener("ended", function () {
      resetToIdle();
    });
  })();

  (function initBookingModal() {
    var modal = document.getElementById("booking-modal");
    if (!modal) return;

    var dialog = modal.querySelector("[data-booking-dialog]");
    var openButtons = document.querySelectorAll("[data-booking-open]");
    var closeButtons = modal.querySelectorAll("[data-booking-close]");
    var form = modal.querySelector("[data-booking-form]");
    var formHost = modal.querySelector("[data-booking-form-host]");
    var success = modal.querySelector("[data-booking-success]");
    var selectRoot = modal.querySelector("[data-booking-select], [data-ui-select]");
    var bookingSelect = selectRoot
      ? createUiSelect(selectRoot, {
          defaultValue: "thyroid",
          placeholder: "انتخاب موضوع مراجعه",
        })
      : null;
    var lastFocus = null;

    function closeSelect() {
      if (bookingSelect) bookingSelect.close();
    }

    function resetSelect() {
      if (bookingSelect) bookingSelect.reset();
    }

    function getFocusable() {
      return Array.from(
        dialog.querySelectorAll(
          'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
        )
      ).filter(function (el) {
        return !el.hasAttribute("hidden") && el.offsetParent !== null;
      });
    }

    function openModal() {
      lastFocus = document.activeElement;
      setMobileNavOpen(false);
      closeSelect();
      modal.hidden = false;
      modal.setAttribute("aria-hidden", "false");
      document.body.classList.add("booking-open");

      if (form) form.hidden = false;
      if (formHost) formHost.hidden = false;
      if (success) success.hidden = true;

      window.setTimeout(function () {
        var firstInput =
          modal.querySelector("#booking-name") ||
          modal.querySelector(".booking-form--cms input:not([type='hidden'])");
        if (firstInput) firstInput.focus();
      }, 20);
    }

    function closeModal() {
      closeSelect();
      modal.hidden = true;
      modal.setAttribute("aria-hidden", "true");
      document.body.classList.remove("booking-open");

      if (form) {
        form.hidden = false;
        form.reset();
        resetSelect();
        form.querySelectorAll(".booking-form__field.is-invalid").forEach(function (field) {
          field.classList.remove("is-invalid");
        });
        form.querySelectorAll(".booking-form__error").forEach(function (error) {
          error.hidden = true;
        });
      }
      if (formHost) {
        formHost.hidden = false;
        var cmsForm = formHost.querySelector("form.public-form");
        if (cmsForm) cmsForm.reset();
      }
      if (success) success.hidden = true;

      if (lastFocus && typeof lastFocus.focus === "function") {
        lastFocus.focus();
      }
    }

    function isValidPhone(value) {
      var digits = String(value).replace(/[^\d]/g, "");
      return digits.length >= 10 && digits.length <= 12;
    }

    openButtons.forEach(function (button) {
      button.addEventListener("click", function () {
        openModal();
      });
    });

    closeButtons.forEach(function (button) {
      button.addEventListener("click", function () {
        closeModal();
      });
    });

    document.addEventListener("keydown", function (event) {
      if (modal.hidden) return;

      if (event.key === "Escape") {
        if (bookingSelect && bookingSelect.isOpen()) {
          event.preventDefault();
          bookingSelect.close();
          if (bookingSelect.trigger) bookingSelect.trigger.focus();
          return;
        }
        event.preventDefault();
        closeModal();
        return;
      }

      if (event.key !== "Tab") return;

      var focusable = getFocusable();
      if (!focusable.length) return;

      var first = focusable[0];
      var last = focusable[focusable.length - 1];

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    });

    if (form) {
      form.addEventListener("submit", function (event) {
        event.preventDefault();

        var nameInput = form.querySelector("#booking-name");
        var phoneInput = form.querySelector("#booking-phone");
        var nameField = nameInput ? nameInput.closest(".booking-form__field") : null;
        var phoneField = phoneInput ? phoneInput.closest(".booking-form__field") : null;
        var nameError = form.querySelector('[data-error-for="name"]');
        var phoneError = form.querySelector('[data-error-for="phone"]');
        var isValid = true;

        if (nameField) nameField.classList.remove("is-invalid");
        if (phoneField) phoneField.classList.remove("is-invalid");
        if (nameError) nameError.hidden = true;
        if (phoneError) phoneError.hidden = true;

        if (!nameInput || !String(nameInput.value).trim()) {
          isValid = false;
          if (nameField) nameField.classList.add("is-invalid");
          if (nameError) nameError.hidden = false;
        }

        if (!phoneInput || !isValidPhone(phoneInput.value)) {
          isValid = false;
          if (phoneField) phoneField.classList.add("is-invalid");
          if (phoneError) phoneError.hidden = false;
        }

        if (!isValid) {
          var firstInvalid = form.querySelector(".booking-form__field.is-invalid input");
          if (firstInvalid) firstInvalid.focus();
          return;
        }

        form.hidden = true;
        if (success) {
          success.hidden = false;
          var closeBtn = success.querySelector("[data-booking-close]");
          if (closeBtn) closeBtn.focus();
        }
      });
    }
  })();

  (function initEventGallery() {
    var gallery = document.querySelector("[data-event-gallery]");
    var lightbox = document.getElementById("gallery-lightbox");
    if (!gallery) return;

    var section = gallery.closest(".event-gallery");

    function hideGallery() {
      if (section) {
        section.hidden = true;
        section.setAttribute("aria-hidden", "true");
      }
      if (lightbox) {
        lightbox.hidden = true;
        lightbox.setAttribute("aria-hidden", "true");
      }
    }

    var items = Array.prototype.slice.call(gallery.querySelectorAll("[data-gallery-index]")).filter(function (item) {
      var img = item.querySelector("img");
      var src = img ? (img.getAttribute("src") || "").trim() : "";
      return Boolean(src);
    });

    if (!items.length || !lightbox) {
      hideGallery();
      return;
    }

    var imageEl = lightbox.querySelector("[data-gallery-image]");
    var captionEl = lightbox.querySelector("[data-gallery-caption]");
    var counterEl = lightbox.querySelector("[data-gallery-counter]");
    var dialog = lightbox.querySelector("[data-gallery-dialog]");
    var closeButtons = lightbox.querySelectorAll("[data-gallery-close]");
    var prevButton = lightbox.querySelector("[data-gallery-prev]");
    var nextButton = lightbox.querySelector("[data-gallery-next]");
    var activeIndex = 0;
    var lastFocus = null;
    var readyItems = [];
    var pendingChecks = items.length;

    function getItemData(index) {
      var item = readyItems[index];
      var img = item ? item.querySelector("img") : null;
      return {
        src: img ? img.currentSrc || img.src : "",
        alt: img ? img.alt || "" : ""
      };
    }

    function render(index) {
      if (!readyItems.length) return;
      activeIndex = (index + readyItems.length) % readyItems.length;
      var data = getItemData(activeIndex);
      if (imageEl) {
        imageEl.src = data.src;
        imageEl.alt = data.alt;
      }
      if (captionEl) captionEl.textContent = data.alt;
      if (counterEl) {
        counterEl.textContent = toPersianDigits(activeIndex + 1) + " / " + toPersianDigits(readyItems.length);
      }
    }

    function openLightbox(index) {
      lastFocus = document.activeElement;
      render(index);
      lightbox.hidden = false;
      lightbox.setAttribute("aria-hidden", "false");
      document.body.classList.add("gallery-open");
      if (dialog) dialog.focus();
      else if (closeButtons[0]) closeButtons[0].focus();
    }

    function closeLightbox() {
      lightbox.hidden = true;
      lightbox.setAttribute("aria-hidden", "true");
      document.body.classList.remove("gallery-open");
      if (imageEl) {
        imageEl.removeAttribute("src");
        imageEl.alt = "";
      }
      if (lastFocus && typeof lastFocus.focus === "function") lastFocus.focus();
    }

    function bindGallery() {
      readyItems.forEach(function (item, index) {
        item.hidden = false;
        item.addEventListener("click", function () {
          openLightbox(index);
        });
      });

      closeButtons.forEach(function (button) {
        button.addEventListener("click", closeLightbox);
      });

      if (prevButton) {
        prevButton.addEventListener("click", function () {
          render(activeIndex - 1);
        });
      }

      if (nextButton) {
        nextButton.addEventListener("click", function () {
          render(activeIndex + 1);
        });
      }

      if (dialog && !dialog.hasAttribute("tabindex")) {
        dialog.setAttribute("tabindex", "-1");
      }

      document.addEventListener("keydown", function (event) {
        if (lightbox.hidden) return;

        if (event.key === "Escape") {
          event.preventDefault();
          closeLightbox();
        } else if (event.key === "ArrowRight") {
          event.preventDefault();
          render(activeIndex - 1);
        } else if (event.key === "ArrowLeft") {
          event.preventDefault();
          render(activeIndex + 1);
        }
      });
    }

    function onItemChecked(item, isValid) {
      if (isValid) {
        readyItems.push(item);
      } else {
        item.hidden = true;
      }

      pendingChecks -= 1;
      if (pendingChecks > 0) return;

      if (!readyItems.length) {
        hideGallery();
        return;
      }

      if (section) {
        section.hidden = false;
        section.setAttribute("aria-hidden", "false");
      }

      bindGallery();
    }

    items.forEach(function (item) {
      var img = item.querySelector("img");
      if (!img) {
        onItemChecked(item, false);
        return;
      }

      if (img.complete) {
        onItemChecked(item, img.naturalWidth > 0);
        return;
      }

      img.addEventListener("load", function () {
        onItemChecked(item, true);
      });
      img.addEventListener("error", function () {
        onItemChecked(item, false);
      });
    });
  })();

  (function initVideosArchive() {
    var archive = document.querySelector("[data-videos-archive]");
    if (!archive) return;

    var PER_PAGE = 9;
    var filters = archive.querySelectorAll("[data-video-filter]");
    var cards = Array.prototype.slice.call(archive.querySelectorAll("[data-video-card]"));
    var countEl = archive.querySelector("[data-videos-count]");
    var emptyEl = archive.querySelector("[data-videos-empty]");
    var pagination = archive.querySelector("[data-videos-pagination]");
    var pagesEl = archive.querySelector("[data-videos-pages]");
    var prevBtn = archive.querySelector("[data-videos-prev]");
    var nextBtn = archive.querySelector("[data-videos-next]");
    var currentCategory = "all";
    var currentPage = 1;

    function getMatchedCards() {
      return cards.filter(function (card) {
        return currentCategory === "all" || card.getAttribute("data-video-category") === currentCategory;
      });
    }

    function updateCount(total) {
      if (countEl) {
        countEl.textContent = toPersianDigits(total) + " ویدئو";
      }
      if (emptyEl) {
        emptyEl.hidden = total > 0;
      }
    }

    function renderPagination(totalPages) {
      if (!pagination || !pagesEl) return;

      if (totalPages <= 1) {
        pagination.hidden = true;
        pagesEl.innerHTML = "";
        return;
      }

      pagination.hidden = false;
      pagesEl.innerHTML = "";

      for (var page = 1; page <= totalPages; page += 1) {
        var button = document.createElement("button");
        button.type = "button";
        button.className = "videos-pagination__page" + (page === currentPage ? " is-active" : "");
        button.textContent = toPersianDigits(page);
        button.setAttribute("data-videos-page", String(page));
        button.setAttribute("aria-label", "صفحه " + toPersianDigits(page));
        if (page === currentPage) {
          button.setAttribute("aria-current", "page");
        }
        pagesEl.appendChild(button);
      }

      if (prevBtn) prevBtn.disabled = currentPage <= 1;
      if (nextBtn) nextBtn.disabled = currentPage >= totalPages;
    }

    function render() {
      var matched = getMatchedCards();
      var totalPages = Math.max(1, Math.ceil(matched.length / PER_PAGE));

      if (currentPage > totalPages) currentPage = totalPages;

      var start = (currentPage - 1) * PER_PAGE;
      var end = start + PER_PAGE;

      cards.forEach(function (card) {
        card.classList.add("is-hidden");
      });

      matched.forEach(function (card, index) {
        if (index >= start && index < end) {
          card.classList.remove("is-hidden");
        }
      });

      updateCount(matched.length);
      renderPagination(matched.length ? totalPages : 0);
    }

    filters.forEach(function (button) {
      button.addEventListener("click", function () {
        currentCategory = button.getAttribute("data-video-filter") || "all";
        currentPage = 1;
        filters.forEach(function (item) {
          var isActive = item === button;
          item.classList.toggle("is-active", isActive);
          item.setAttribute("aria-pressed", isActive ? "true" : "false");
        });
        render();
      });
    });

    if (prevBtn) {
      prevBtn.addEventListener("click", function () {
        if (currentPage <= 1) return;
        currentPage -= 1;
        render();
        archive.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }

    if (nextBtn) {
      nextBtn.addEventListener("click", function () {
        var totalPages = Math.max(1, Math.ceil(getMatchedCards().length / PER_PAGE));
        if (currentPage >= totalPages) return;
        currentPage += 1;
        render();
        archive.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }

    if (pagesEl) {
      pagesEl.addEventListener("click", function (event) {
        var target = event.target.closest("[data-videos-page]");
        if (!target) return;
        var page = Number(target.getAttribute("data-videos-page"));
        if (!page || page === currentPage) return;
        currentPage = page;
        render();
        archive.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }

    render();
  })();

  (function initArticlesArchive() {
    var archive = document.querySelector("[data-articles-archive]");
    if (!archive) return;

    var PAGE_SIZE = 6;
    var filters = archive.querySelectorAll("[data-article-filter]");
    var cards = Array.prototype.slice.call(archive.querySelectorAll("[data-article-card]"));
    var countEl = archive.querySelector("[data-articles-count]");
    var emptyEl = archive.querySelector("[data-articles-empty]");
    var pagination = archive.querySelector("[data-articles-pagination]");
    var pagesEl = archive.querySelector("[data-articles-pages]");
    var prevBtn = archive.querySelector("[data-articles-prev]");
    var nextBtn = archive.querySelector("[data-articles-next]");
    var currentCategory = "all";
    var currentPage = 1;

    function getMatchedCards() {
      return cards.filter(function (card) {
        return currentCategory === "all" || card.getAttribute("data-article-category") === currentCategory;
      });
    }

    function getTotalPages(total) {
      if (total <= 0) return 0;
      return Math.ceil(total / PAGE_SIZE);
    }

    function getPageRange(page) {
      var start = (Math.max(1, page) - 1) * PAGE_SIZE;
      return { start: start, end: start + PAGE_SIZE };
    }

    function updateCount(total) {
      if (countEl) {
        countEl.textContent = toPersianDigits(total) + " مقاله";
      }
      if (emptyEl) {
        emptyEl.hidden = total > 0;
      }
    }

    function renderPagination(totalPages) {
      if (!pagination || !pagesEl) return;

      if (totalPages <= 1) {
        pagination.hidden = true;
        pagesEl.innerHTML = "";
        return;
      }

      pagination.hidden = false;
      pagesEl.innerHTML = "";

      for (var page = 1; page <= totalPages; page += 1) {
        var button = document.createElement("button");
        button.type = "button";
        button.className = "articles-pagination__page" + (page === currentPage ? " is-active" : "");
        button.textContent = toPersianDigits(page);
        button.setAttribute("data-articles-page", String(page));
        button.setAttribute("aria-label", "صفحه " + toPersianDigits(page));
        if (page === currentPage) {
          button.setAttribute("aria-current", "page");
        }
        pagesEl.appendChild(button);
      }

      if (prevBtn) prevBtn.disabled = currentPage <= 1;
      if (nextBtn) nextBtn.disabled = currentPage >= totalPages;
    }

    function render() {
      var matched = getMatchedCards();
      var totalPages = Math.max(1, getTotalPages(matched.length));

      if (currentPage > totalPages) currentPage = totalPages;

      var range = getPageRange(currentPage);

      cards.forEach(function (card) {
        card.classList.add("is-hidden");
      });

      matched.forEach(function (card, index) {
        if (index >= range.start && index < range.end) {
          card.classList.remove("is-hidden");
        }
      });

      updateCount(matched.length);
      renderPagination(matched.length ? totalPages : 0);
    }

    filters.forEach(function (button) {
      button.addEventListener("click", function () {
        currentCategory = button.getAttribute("data-article-filter") || "all";
        currentPage = 1;
        filters.forEach(function (item) {
          var isActive = item === button;
          item.classList.toggle("is-active", isActive);
          item.setAttribute("aria-pressed", isActive ? "true" : "false");
        });
        render();
      });
    });

    if (prevBtn) {
      prevBtn.addEventListener("click", function () {
        if (currentPage <= 1) return;
        currentPage -= 1;
        render();
        archive.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }

    if (nextBtn) {
      nextBtn.addEventListener("click", function () {
        var totalPages = Math.max(1, getTotalPages(getMatchedCards().length));
        if (currentPage >= totalPages) return;
        currentPage += 1;
        render();
        archive.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }

    if (pagesEl) {
      pagesEl.addEventListener("click", function (event) {
        var target = event.target.closest("[data-articles-page]");
        if (!target) return;
        var page = Number(target.getAttribute("data-articles-page"));
        if (!page || page === currentPage) return;
        currentPage = page;
        render();
        archive.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }

    render();
  })();

  (function initBlogArchive() {
    var archive = document.querySelector("[data-blog-archive]");
    if (!archive) return;

    var FIRST_PAGE_SIZE = 7;
    var OTHER_PAGE_SIZE = 12;
    var cards = Array.prototype.slice.call(archive.querySelectorAll("[data-blog-card]"));
    var countEl = archive.querySelector("[data-blog-count]");
    var emptyEl = archive.querySelector("[data-blog-empty]");
    var pagination = archive.querySelector("[data-blog-pagination]");
    var pagesEl = archive.querySelector("[data-blog-pages]");
    var prevBtn = archive.querySelector("[data-blog-prev]");
    var nextBtn = archive.querySelector("[data-blog-next]");
    var currentPage = 1;

    function getTotalPages(total) {
      if (total <= FIRST_PAGE_SIZE) return Math.max(1, total > 0 ? 1 : 0);
      return 1 + Math.ceil((total - FIRST_PAGE_SIZE) / OTHER_PAGE_SIZE);
    }

    function getPageRange(page) {
      if (page <= 1) {
        return { start: 0, end: FIRST_PAGE_SIZE };
      }

      var start = FIRST_PAGE_SIZE + (page - 2) * OTHER_PAGE_SIZE;
      return { start: start, end: start + OTHER_PAGE_SIZE };
    }

    function updateCount(total, visibleStart, visibleEnd) {
      if (!countEl) return;

      if (!total) {
        countEl.textContent = "۰ مطلب";
        return;
      }

      countEl.textContent =
        "نمایش " +
        toPersianDigits(visibleStart) +
        " تا " +
        toPersianDigits(visibleEnd) +
        " از " +
        toPersianDigits(total) +
        " مطلب";
    }

    function renderPagination(totalPages) {
      if (!pagination || !pagesEl) return;

      if (totalPages <= 1) {
        pagination.hidden = true;
        pagesEl.innerHTML = "";
        return;
      }

      pagination.hidden = false;
      pagesEl.innerHTML = "";

      for (var page = 1; page <= totalPages; page += 1) {
        var button = document.createElement("button");
        button.type = "button";
        button.className = "blog-pagination__page" + (page === currentPage ? " is-active" : "");
        button.textContent = toPersianDigits(page);
        button.setAttribute("data-blog-page", String(page));
        button.setAttribute("aria-label", "صفحه " + toPersianDigits(page));
        if (page === currentPage) {
          button.setAttribute("aria-current", "page");
        }
        pagesEl.appendChild(button);
      }

      if (prevBtn) prevBtn.disabled = currentPage <= 1;
      if (nextBtn) nextBtn.disabled = currentPage >= totalPages;
    }

    function render() {
      var total = cards.length;
      var totalPages = Math.max(1, getTotalPages(total));

      if (currentPage > totalPages) currentPage = totalPages;

      var range = getPageRange(currentPage);

      cards.forEach(function (card, index) {
        if (index >= range.start && index < range.end) {
          card.classList.remove("is-hidden");
        } else {
          card.classList.add("is-hidden");
        }
      });

      if (emptyEl) {
        emptyEl.hidden = total > 0;
      }

      var visibleEnd = Math.min(range.end, total);
      var visibleStart = total ? range.start + 1 : 0;
      updateCount(total, visibleStart, visibleEnd);
      renderPagination(total ? totalPages : 0);
    }

    if (prevBtn) {
      prevBtn.addEventListener("click", function () {
        if (currentPage <= 1) return;
        currentPage -= 1;
        render();
        archive.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }

    if (nextBtn) {
      nextBtn.addEventListener("click", function () {
        var totalPages = Math.max(1, getTotalPages(cards.length));
        if (currentPage >= totalPages) return;
        currentPage += 1;
        render();
        archive.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }

    if (pagesEl) {
      pagesEl.addEventListener("click", function (event) {
        var target = event.target.closest("[data-blog-page]");
        if (!target) return;
        var page = Number(target.getAttribute("data-blog-page"));
        if (!page || page === currentPage) return;
        currentPage = page;
        render();
        archive.scrollIntoView({ behavior: "smooth", block: "start" });
      });
    }

    render();
  })();

  (function initServiceToc() {
    var toc = document.querySelector("[data-service-toc]");
    if (!toc) return;

    var links = Array.prototype.slice.call(toc.querySelectorAll(".service-toc__list a"));
    if (!links.length) return;

    function resolveHeading(el) {
      if (!el) return null;
      if (el.matches("h2")) return el;
      return el.querySelector("h2") || el;
    }

    var sections = links
      .map(function (link) {
        var id = link.getAttribute("href");
        if (!id || id.charAt(0) !== "#") return null;
        var el = document.querySelector(id);
        var heading = resolveHeading(el);
        return heading ? { link: link, heading: heading } : null;
      })
      .filter(Boolean);

    if (!sections.length) return;

    function setActive(activeLink) {
      links.forEach(function (link) {
        var isActive = link === activeLink;
        link.classList.toggle("is-active", isActive);
        if (isActive) link.setAttribute("aria-current", "true");
        else link.removeAttribute("aria-current");
      });
    }

    function getOffset() {
      var headerEl = document.querySelector(".site-header");
      var headerH = headerEl ? headerEl.offsetHeight : 80;
      return headerH + 20;
    }

    function updateActive() {
      var offset = getOffset();
      var current = sections[0];

      sections.forEach(function (item) {
        if (item.heading.getBoundingClientRect().top - offset <= 0) {
          current = item;
        }
      });

      setActive(current.link);
    }

    var ticking = false;
    function onScroll() {
      if (ticking) return;
      ticking = true;
      window.requestAnimationFrame(function () {
        updateActive();
        ticking = false;
      });
    }

    setActive(sections[0].link);
    updateActive();
    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll);
  })();

  (function initContactForm() {
    var form = document.querySelector("[data-contact-form]");
    if (!form) return;

    var success = form.querySelector("[data-contact-success]");
    var nameInput = form.querySelector("#contact-name");
    var phoneInput = form.querySelector("#contact-phone");
    var emailInput = form.querySelector("#contact-email");
    var subjectInput = form.querySelector("#contact-subject");
    var messageInput = form.querySelector("#contact-message");
    createUiSelect(form.querySelector("[data-ui-select]"), {
      placeholder: "انتخاب موضوع",
    });

    function isValidPhone(value) {
      var digits = String(value).replace(/[^\d]/g, "");
      return digits.length >= 10 && digits.length <= 12;
    }

    function isValidEmail(value) {
      if (!value) return true;
      return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(value).trim());
    }

    function clearErrors() {
      form.querySelectorAll(".contact-form__field.is-invalid").forEach(function (field) {
        field.classList.remove("is-invalid");
      });
      form.querySelectorAll(".contact-form__error").forEach(function (error) {
        error.hidden = true;
      });
    }

    function setInvalid(input, errorKey) {
      if (!input) return;
      var field = input.closest(".contact-form__field");
      if (field) field.classList.add("is-invalid");
      var error = form.querySelector('[data-error-for="' + errorKey + '"]');
      if (error) error.hidden = false;
    }

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      clearErrors();

      var valid = true;

      if (!nameInput || !String(nameInput.value).trim()) {
        setInvalid(nameInput, "name");
        valid = false;
      }

      if (!phoneInput || !isValidPhone(phoneInput.value)) {
        setInvalid(phoneInput, "phone");
        valid = false;
      }

      if (emailInput && !isValidEmail(emailInput.value)) {
        setInvalid(emailInput, "email");
        valid = false;
      }

      if (!subjectInput || !subjectInput.value) {
        setInvalid(subjectInput, "subject");
        valid = false;
      }

      if (!messageInput || !String(messageInput.value).trim()) {
        setInvalid(messageInput, "message");
        valid = false;
      }

      if (!valid) {
        var firstInvalid = form.querySelector(
          ".contact-form__field.is-invalid input:not([type='hidden']), .contact-form__field.is-invalid textarea, .contact-form__field.is-invalid .booking-select__trigger"
        );
        if (firstInvalid) firstInvalid.focus();
        return;
      }

      form.classList.add("is-sent");
      if (success) success.hidden = false;
    });
  })();
})();
