(function () {
  'use strict';

  // Layout always loads this script; guard against a second include.
  if (window.__cmsPopupInit) return;
  window.__cmsPopupInit = true;

  var STORAGE_PREFIX = 'cms.popup.';
  var host = document.querySelector('[data-cms-popup-host]');
  if (!host) return;

  var popups = Array.prototype.slice.call(host.querySelectorAll('[data-cms-popup]'));
  if (!popups.length) return;

  host.hidden = false;

  var openCount = 0;
  var visitShown = Object.create(null);

  function storageKey(slug) {
    return STORAGE_PREFIX + slug;
  }

  function wasShown(popup) {
    var freq = popup.getAttribute('data-frequency') || 'always';
    var slug = popup.getAttribute('data-popup-slug') || '';
    if (freq === 'always') return false;
    if (freq === 'every_visit') return !!visitShown[slug];
    try {
      if (freq === 'once_session') return sessionStorage.getItem(storageKey(slug)) === '1';
      if (freq === 'once_browser') return localStorage.getItem(storageKey(slug)) === '1';
    } catch (e) { /* private mode */ }
    return false;
  }

  function markShown(popup) {
    var freq = popup.getAttribute('data-frequency') || 'always';
    var slug = popup.getAttribute('data-popup-slug') || '';
    if (freq === 'always') return;
    if (freq === 'every_visit') {
      visitShown[slug] = true;
      return;
    }
    try {
      if (freq === 'once_session') sessionStorage.setItem(storageKey(slug), '1');
      if (freq === 'once_browser') localStorage.setItem(storageKey(slug), '1');
    } catch (e) { /* ignore */ }
  }

  function findByKey(key) {
    if (!key) return null;
    var lower = String(key).toLowerCase();
    for (var i = 0; i < popups.length; i++) {
      var p = popups[i];
      var slug = (p.getAttribute('data-popup-slug') || '').toLowerCase();
      var id = (p.getAttribute('data-popup-id') || '').toLowerCase();
      if (slug === lower || id === lower) return p;
    }
    return null;
  }

  function syncBodyLock() {
    if (openCount > 0) document.body.classList.add('cms-popup-open');
    else document.body.classList.remove('cms-popup-open');
  }

  function resetPopupFormState(popup) {
    if (!popup) return;
    var content = popup.querySelector('[data-popup-content]');
    var success = popup.querySelector('[data-popup-success]');
    if (content) content.hidden = false;
    if (success) success.hidden = true;
    var form = popup.querySelector('form.public-form');
    if (form) {
      form.removeAttribute('data-submitting');
      try { form.reset(); } catch (e) { /* ignore */ }
      var submitBtn = form.querySelector('[type="submit"]');
      if (submitBtn) submitBtn.disabled = false;
    }
  }

  function showPopupFormSuccess(popup, form, message) {
    var success = popup.querySelector('[data-popup-success]');
    if (success) {
      var content = popup.querySelector('[data-popup-content]');
      if (content) content.hidden = true;
      var text = success.querySelector('[data-popup-success-text]');
      if (text) text.textContent = message || 'ارسال با موفقیت انجام شد.';
      success.hidden = false;
      var closeBtn = success.querySelector('[data-popup-close]');
      if (closeBtn) closeBtn.focus();
      return true;
    }
    if (!form) return false;
    var fallback = document.createElement('p');
    fallback.className = 'public-form-success';
    fallback.textContent = message || 'ارسال با موفقیت انجام شد.';
    form.replaceWith(fallback);
    return true;
  }

  function openPopup(popup, options) {
    if (!popup) return false;
    var force = options && options.force;
    if (!force && wasShown(popup)) return false;
    if (!popup.hidden) return true;

    resetPopupFormState(popup);
    popup.hidden = false;
    openCount++;
    if (popup.getAttribute('data-lock-scroll') === 'true') syncBodyLock();

    var closeBtn = popup.querySelector('[data-popup-close]');
    // Skip honeypot (.public-form-hp) — focusing it scrolls the page off-screen (white page).
    var firstInput = popup.querySelector(
      'form.public-form .public-form-field input:not([type="hidden"]), form.public-form .public-form-field textarea, form.public-form .public-form-field select'
    );
    if (firstInput) firstInput.focus();
    else if (closeBtn) closeBtn.focus();
    markShown(popup);
    popup.dispatchEvent(new CustomEvent('cms-popup:open', { bubbles: true }));
    return true;
  }

  function closePopup(popup) {
    if (!popup || popup.hidden) return;
    popup.hidden = true;
    openCount = Math.max(0, openCount - 1);
    syncBodyLock();
    resetPopupFormState(popup);
    popup.dispatchEvent(new CustomEvent('cms-popup:close', { bubbles: true }));
  }

  function closeAll() {
    popups.forEach(closePopup);
  }

  popups.forEach(function (popup) {
    var backdrop = popup.querySelector('[data-popup-backdrop]');

    popup.querySelectorAll('[data-popup-close]').forEach(function (closeBtn) {
      closeBtn.addEventListener('click', function () { closePopup(popup); });
    });
    if (backdrop && popup.getAttribute('data-close-overlay') === 'true') {
      backdrop.addEventListener('click', function () { closePopup(popup); });
    }

    var trigger = (popup.getAttribute('data-trigger') || 'manual').toLowerCase();

    if (trigger === 'timer') {
      var delay = parseInt(popup.getAttribute('data-trigger-delay') || '0', 10);
      if (isNaN(delay) || delay < 0) delay = 0;
      window.setTimeout(function () { openPopup(popup); }, delay * 1000);
    }

    if (trigger === 'scroll') {
      var percent = parseInt(popup.getAttribute('data-trigger-scroll') || '50', 10);
      if (isNaN(percent) || percent < 1) percent = 50;
      var fired = false;
      function onScroll() {
        if (fired) return;
        var doc = document.documentElement;
        var scrollTop = window.scrollY || doc.scrollTop;
        var height = doc.scrollHeight - doc.clientHeight;
        if (height <= 0) return;
        var progress = (scrollTop / height) * 100;
        if (progress >= percent) {
          fired = true;
          openPopup(popup);
          window.removeEventListener('scroll', onScroll, { passive: true });
        }
      }
      window.addEventListener('scroll', onScroll, { passive: true });
      onScroll();
    }

    var selector = popup.getAttribute('data-trigger-selector');
    if (selector) {
      try {
        document.querySelectorAll(selector).forEach(function (el) {
          el.addEventListener('click', function (ev) {
            ev.preventDefault();
            openPopup(popup, { force: true });
          });
        });
      } catch (e) { /* invalid selector */ }
    }

    // AJAX form submit inside popup — keep dialog open on validation errors
    popup.querySelectorAll('form.public-form').forEach(function (form) {
      if (form.getAttribute('data-popup-ajax-bound') === '1') return;
      form.setAttribute('data-popup-ajax-bound', '1');
      form.addEventListener('submit', function (ev) {
        if (form.getAttribute('data-popup-ajax') === 'off') return;
        ev.preventDefault();
        if (form.getAttribute('data-submitting') === '1') return;
        submitFormAjax(form, popup);
      });
    });
  });

  document.addEventListener('click', function (ev) {
    var opener = ev.target.closest('[data-popup-open]');
    if (!opener) return;
    var key = opener.getAttribute('data-popup-open');
    var popup = findByKey(key);
    if (!popup) return;
    ev.preventDefault();
    if (opener.getAttribute('data-popup-close-current') === 'true') {
      var current = opener.closest('[data-cms-popup]');
      if (current) closePopup(current);
    }
    openPopup(popup, { force: true });
  });

  document.addEventListener('keydown', function (ev) {
    if (ev.key !== 'Escape') return;
    for (var i = popups.length - 1; i >= 0; i--) {
      var p = popups[i];
      if (!p.hidden && p.getAttribute('data-close-esc') === 'true') {
        closePopup(p);
        break;
      }
    }
  });

  function getAntiForgery(form) {
    var input = form.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
  }

  function submitFormAjax(form, popup) {
    if (form.getAttribute('data-submitting') === '1') return;
    form.setAttribute('data-submitting', '1');

    var action = form.getAttribute('action') || window.location.href;
    var fd = new FormData(form);
    var errorBox = form.querySelector('.public-form-errors') || form.querySelector('[data-valmsg-summary]');
    if (errorBox) {
      errorBox.innerHTML = '';
      errorBox.classList.remove('validation-summary-errors');
      errorBox.classList.add('validation-summary-valid');
    }
    form.querySelectorAll('.public-form-field-error, .field-validation-error').forEach(function (el) {
      el.textContent = '';
    });

    var submitBtn = form.querySelector('[type="submit"]');
    if (submitBtn) submitBtn.disabled = true;

    fetch(action, {
      method: 'POST',
      body: fd,
      headers: {
        'Accept': 'application/json',
        'X-Requested-With': 'XMLHttpRequest',
        'RequestVerificationToken': getAntiForgery(form)
      },
      credentials: 'same-origin'
    }).then(function (res) {
      return res.json().then(function (data) {
        return { ok: res.ok, status: res.status, data: data };
      }).catch(function () {
        return { ok: res.ok, status: res.status, data: null };
      });
    }).then(function (result) {
      var data = result.data || {};
      if (result.ok && data.ok) {
        markShown(popup);
        if (data.redirectUrl) {
          window.location.href = data.redirectUrl;
          return;
        }
        var msg = data.message || 'ارسال با موفقیت انجام شد.';
        showPopupFormSuccess(popup, form, msg);
        return;
      }

      form.removeAttribute('data-submitting');
      if (submitBtn) submitBtn.disabled = false;

      var errors = (data && data.errors) || {};
      var messages = [];
      Object.keys(errors).forEach(function (key) {
        var list = errors[key] || [];
        list.forEach(function (m) { messages.push(m); });
        var field = form.querySelector('[name="' + key + '"], [name="' + key.replace(/^Values\[/, 'Values[').replace(/\]$/, '') + '"]');
        if (!field && key.indexOf('Values[') === 0) {
          field = form.querySelector('[name="' + key + '"]');
        }
        var container = field ? field.closest('.public-form-field') : null;
        var errEl = container ? container.querySelector('.public-form-field-error, .field-validation-error') : null;
        if (errEl && list[0]) errEl.textContent = list[0];
      });
      if (errorBox) {
        errorBox.classList.remove('validation-summary-valid');
        errorBox.classList.add('validation-summary-errors');
        if (messages.length) {
          var ul = document.createElement('ul');
          messages.forEach(function (m) {
            var li = document.createElement('li');
            li.textContent = m;
            ul.appendChild(li);
          });
          errorBox.appendChild(ul);
        } else {
          errorBox.textContent = (data && data.message) || 'لطفاً خطاهای فرم را بررسی کنید.';
        }
      }
      // keep popup open
    }).catch(function () {
      form.removeAttribute('data-submitting');
      if (submitBtn) submitBtn.disabled = false;
      if (errorBox) {
        errorBox.textContent = 'ارسال فرم با خطا مواجه شد. دوباره تلاش کنید.';
        errorBox.classList.add('validation-summary-errors');
      }
    });
  }

  window.MirkaPopup = {
    open: function (key) {
      return openPopup(findByKey(key), { force: true });
    },
    close: function (key) {
      if (!key) { closeAll(); return; }
      closePopup(findByKey(key));
    },
    toggle: function (key) {
      var p = findByKey(key);
      if (!p) return false;
      if (p.hidden) return openPopup(p, { force: true });
      closePopup(p);
      return true;
    },
    isOpen: function (key) {
      var p = findByKey(key);
      return !!(p && !p.hidden);
    }
  };
})();
