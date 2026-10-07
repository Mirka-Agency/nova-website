/**
 * AJAX submit for Core FormEmbed on Nova surfaces (contact page + booking modal).
 * Uses Accept: application/json so PublicFormsController returns JSON.
 */
(function () {
  'use strict';

  function token(form) {
    var input = form.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
  }

  function clearErrors(form) {
    var errorBox = form.querySelector('.public-form-errors') || form.querySelector('[data-valmsg-summary]');
    if (errorBox) {
      errorBox.innerHTML = '';
      errorBox.classList.remove('validation-summary-errors');
      errorBox.classList.add('validation-summary-valid');
    }
    form.querySelectorAll('.public-form-field-error, .field-validation-error').forEach(function (el) {
      el.textContent = '';
    });
  }

  function showFieldErrors(form, errors) {
    Object.keys(errors || {}).forEach(function (key) {
      var list = errors[key] || [];
      if (!list.length) return;
      var field = form.querySelector('[name="' + key + '"]');
      var container = field ? field.closest('.public-form-field') : null;
      var errEl = container
        ? container.querySelector('.public-form-field-error, .field-validation-error')
        : null;
      if (errEl) errEl.textContent = list[0];
    });
  }

  function showSummary(form, data) {
    var errorBox = form.querySelector('.public-form-errors') || form.querySelector('[data-valmsg-summary]');
    if (!errorBox) return;
    var errors = (data && data.errors) || {};
    var messages = [];
    Object.keys(errors).forEach(function (key) {
      (errors[key] || []).forEach(function (m) { messages.push(m); });
    });
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

  function onBookingSuccess(form, message) {
    var modal = form.closest('.booking-modal');
    if (modal) {
      var host = modal.querySelector('[data-booking-form-host]');
      var success = modal.querySelector('[data-booking-success]');
      if (host) host.hidden = true;
      if (success) {
        var text = success.querySelector('[data-booking-success-text]');
        if (text && message) text.textContent = message;
        success.hidden = false;
        var closeBtn = success.querySelector('[data-booking-close]');
        if (closeBtn) closeBtn.focus();
      }
      return true;
    }

    var cmsPopup = form.closest('.cms-popup--booking, [data-cms-popup]');
    if (!cmsPopup) return false;
    var popupSuccess = cmsPopup.querySelector('[data-popup-success]');
    if (!popupSuccess) return false;
    var content = cmsPopup.querySelector('[data-popup-content]');
    if (content) content.hidden = true;
    var popupText = popupSuccess.querySelector('[data-popup-success-text]');
    if (popupText && message) popupText.textContent = message;
    popupSuccess.hidden = false;
    var popupClose = popupSuccess.querySelector('[data-popup-close]');
    if (popupClose) popupClose.focus();
    return true;
  }

  function onContactSuccess(form, message) {
    var panel = form.closest('.contact-form');
    if (!panel) return false;
    form.hidden = true;
    var existing = panel.querySelector('[data-contact-cms-success]');
    if (existing) existing.remove();
    var box = document.createElement('div');
    box.className = 'contact-form__success';
    box.setAttribute('data-contact-cms-success', '');
    box.innerHTML =
      '<p class="contact-form__success-title">پیام شما دریافت شد</p>' +
      '<p class="contact-form__success-text"></p>';
    var text = box.querySelector('.contact-form__success-text');
    if (text) text.textContent = message || 'به‌زودی با شما تماس می‌گیریم.';
    panel.appendChild(box);
    return true;
  }

  function submitAjax(form) {
    if (form.getAttribute('data-submitting') === '1') return;
    form.setAttribute('data-submitting', '1');

    var action = form.getAttribute('action') || window.location.href;
    clearErrors(form);
    var submitBtn = form.querySelector('[type="submit"]');
    if (submitBtn) submitBtn.disabled = true;

    fetch(action, {
      method: 'POST',
      body: new FormData(form),
      headers: {
        Accept: 'application/json',
        'X-Requested-With': 'XMLHttpRequest',
        RequestVerificationToken: token(form)
      },
      credentials: 'same-origin'
    })
      .then(function (res) {
        return res
          .json()
          .then(function (data) {
            return { ok: res.ok, data: data };
          })
          .catch(function () {
            return { ok: res.ok, data: null };
          });
      })
      .then(function (result) {
        var data = result.data || {};
        if (result.ok && data.ok) {
          if (data.redirectUrl) {
            window.location.href = data.redirectUrl;
            return;
          }
          var msg = data.message || 'ارسال با موفقیت انجام شد.';
          if (onBookingSuccess(form, msg)) return;
          if (onContactSuccess(form, msg)) return;
          var success = document.createElement('p');
          success.className = 'public-form-success';
          success.textContent = msg;
          form.replaceWith(success);
          return;
        }
        form.removeAttribute('data-submitting');
        if (submitBtn) submitBtn.disabled = false;
        showFieldErrors(form, data.errors);
        showSummary(form, data);
      })
      .catch(function () {
        form.removeAttribute('data-submitting');
        if (submitBtn) submitBtn.disabled = false;
        showSummary(form, { message: 'ارسال فرم با خطا مواجه شد. دوباره تلاش کنید.' });
      });
  }

  function bind(form) {
    if (!form || form.getAttribute('data-public-ajax') === 'off') return;
    if (form.getAttribute('data-public-ajax-bound') === '1') return;
    form.setAttribute('data-public-ajax-bound', '1');
    form.addEventListener('submit', function (ev) {
      ev.preventDefault();
      if (form.getAttribute('data-submitting') === '1') return;
      submitAjax(form);
    });
  }

  function init() {
    document
      .querySelectorAll(
        '.booking-modal form.public-form, .contact-form form.public-form, .contact-form-panel form.public-form'
      )
      .forEach(bind);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
