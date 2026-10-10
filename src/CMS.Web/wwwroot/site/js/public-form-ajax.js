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
        form.removeAttribute('data-submitting');
        if (submitBtn) submitBtn.disabled = false;

        if (result.ok && data.ok) {
          var msg = data.message || 'ارسال با موفقیت انجام شد.';
          try { form.reset(); } catch (e) { /* ignore */ }
          window.alert(msg);
          return;
        }

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
    // Popup forms are handled by popup.js
    if (form.closest('[data-cms-popup]')) return;
    form.setAttribute('data-public-ajax-bound', '1');
    form.addEventListener('submit', function (ev) {
      ev.preventDefault();
      if (form.getAttribute('data-submitting') === '1') return;
      submitAjax(form);
    });
  }

  function init() {
    document.querySelectorAll('form.public-form').forEach(bind);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
