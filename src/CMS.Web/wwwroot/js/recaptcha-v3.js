/**
 * Shared Google reCAPTCHA v3 loader for Forms, Comments, and Admin login.
 * - Injects api.js once (no per-form duplicates)
 * - Defers Google download until popup/modal open, form interaction, or submit
 * - Executes tokens only at submit time (avoids expiry)
 * - Capture-phase submit + requestSubmit so AJAX/popup handlers still run
 */
(function () {
    'use strict';

    if (window.NovaRecaptcha)
        return;

    var API_SRC = 'https://www.google.com/recaptcha/api.js';
    var LOAD_TIMEOUT_MS = 12000;
    var READY_POLL_MS = 50;
    var READY_MAX_ATTEMPTS = 240;
    var scriptPromise = null;
    var loadedSiteKey = null;

    function nodes() {
        return document.querySelectorAll('[data-recaptcha-v3]');
    }

    function siteKeyOf(node) {
        return (node.getAttribute('data-recaptcha-sitekey') || '').trim();
    }

    function actionOf(node) {
        return (node.getAttribute('data-recaptcha-action') || 'submit').trim() || 'submit';
    }

    function tokenNameOf(node) {
        return (node.getAttribute('data-recaptcha-token') || 'CaptchaToken').trim() || 'CaptchaToken';
    }

    function waitUntilReady(resolve, reject) {
        var attempts = 0;
        var timer = window.setInterval(function () {
            attempts += 1;
            if (window.grecaptcha && typeof window.grecaptcha.ready === 'function') {
                window.clearInterval(timer);
                window.grecaptcha.ready(function () {
                    resolve();
                });
                return;
            }
            if (attempts >= READY_MAX_ATTEMPTS) {
                window.clearInterval(timer);
                scriptPromise = null;
                reject(new Error('reCAPTCHA ready timeout'));
            }
        }, READY_POLL_MS);
    }

    function ensureApi(siteKey) {
        if (!siteKey)
            return Promise.reject(new Error('reCAPTCHA site key missing'));

        if (window.grecaptcha && typeof window.grecaptcha.execute === 'function')
            return Promise.resolve();

        if (scriptPromise)
            return scriptPromise;

        loadedSiteKey = siteKey;
        scriptPromise = new Promise(function (resolve, reject) {
            var existing = document.querySelector('script[data-nova-recaptcha-api="1"]');
            if (existing) {
                waitUntilReady(resolve, reject);
                return;
            }

            var script = document.createElement('script');
            script.src = API_SRC + '?render=' + encodeURIComponent(siteKey);
            script.async = true;
            script.defer = true;
            script.setAttribute('data-nova-recaptcha-api', '1');

            var timedOut = false;
            var timer = window.setTimeout(function () {
                timedOut = true;
                scriptPromise = null;
                reject(new Error('reCAPTCHA network timeout'));
            }, LOAD_TIMEOUT_MS);

            script.onload = function () {
                if (timedOut)
                    return;
                window.clearTimeout(timer);
                waitUntilReady(resolve, reject);
            };
            script.onerror = function () {
                if (timedOut)
                    return;
                window.clearTimeout(timer);
                scriptPromise = null;
                reject(new Error('reCAPTCHA failed to load'));
            };

            (document.head || document.documentElement).appendChild(script);
        });

        return scriptPromise;
    }

    function execute(siteKey, action) {
        return ensureApi(siteKey).then(function () {
            return window.grecaptcha.execute(siteKey, { action: action || 'submit' });
        });
    }

    function findErrorTarget(form, node) {
        var field = node.closest('.public-form-field, .admin-field, .cms-comments__field') || node.parentElement;
        if (!field)
            return null;
        return field.querySelector('.public-form-field-error, .admin-field-error, .field-validation-error');
    }

    function reportError(form, node, message) {
        var el = findErrorTarget(form, node);
        if (el)
            el.textContent = message;
    }

    function clearError(form, node) {
        var el = findErrorTarget(form, node);
        if (el)
            el.textContent = '';
    }

    function redispatchSubmit(form, submitter) {
        if (typeof form.requestSubmit === 'function') {
            try {
                if (submitter)
                    form.requestSubmit(submitter);
                else
                    form.requestSubmit();
                return;
            } catch (e) {
                /* fall through */
            }
        }

        var event = new Event('submit', { bubbles: true, cancelable: true });
        if (form.dispatchEvent(event))
            HTMLFormElement.prototype.submit.call(form);
    }

    function bind(node) {
        var form = node.closest('form');
        if (!form || form.dataset.recaptchaV3Bound === '1')
            return;

        var siteKey = siteKeyOf(node);
        var action = actionOf(node);
        var tokenName = tokenNameOf(node);
        if (!siteKey)
            return;

        form.dataset.recaptchaV3Bound = '1';

        var warm = function () {
            ensureApi(siteKey).catch(function () { /* warmed opportunistically */ });
        };

        form.addEventListener('pointerdown', warm, { once: true, passive: true });
        form.addEventListener('focusin', warm, { once: true });

        form.addEventListener('submit', function (event) {
            if (form.dataset.recaptchaV3Ok === '1') {
                form.dataset.recaptchaV3Ok = '0';
                return;
            }

            var tokenEl = form.querySelector('input[name="' + tokenName + '"]');
            if (!tokenEl)
                return;

            event.preventDefault();
            event.stopImmediatePropagation();

            if (form.dataset.recaptchaV3Pending === '1')
                return;

            form.dataset.recaptchaV3Pending = '1';
            clearError(form, node);

            var submitBtn = form.querySelector('[type="submit"]');
            var submitter = event.submitter || null;
            if (submitBtn)
                submitBtn.disabled = true;

            execute(siteKey, action).then(function (token) {
                form.dataset.recaptchaV3Pending = '0';
                if (submitBtn)
                    submitBtn.disabled = false;

                if (!token) {
                    reportError(form, node, 'تأیید امنیتی ناموفق بود. دوباره تلاش کنید.');
                    return;
                }

                tokenEl.value = token;
                form.dataset.recaptchaV3Ok = '1';
                redispatchSubmit(form, submitter);
            }).catch(function () {
                form.dataset.recaptchaV3Pending = '0';
                if (submitBtn)
                    submitBtn.disabled = false;
                tokenEl.value = '';
                reportError(
                    form,
                    node,
                    'سرویس تأیید امنیتی در دسترس نیست. اتصال اینترنت را بررسی کنید و دوباره تلاش کنید.');
            });
        }, true);
    }

    function warmFromRoot(root) {
        if (!root || !root.querySelector)
            return;
        var node = root.querySelector('[data-recaptcha-v3]');
        if (!node)
            return;
        var key = siteKeyOf(node);
        if (key)
            ensureApi(key).catch(function () { /* ignore */ });
    }

    function init() {
        nodes().forEach(bind);

        // Warm Google API only when a protected surface becomes relevant —
        // not on initial paint (keeps Lighthouse / LCP free of recaptcha__en.js).
        document.addEventListener('cms-popup:open', function (event) {
            warmFromRoot(event.target);
        });
        document.addEventListener('cms-booking:open', function (event) {
            warmFromRoot(event.target);
        });
    }

    window.NovaRecaptcha = {
        ensure: ensureApi,
        execute: execute,
        bindAll: function () { nodes().forEach(bind); },
        get loadedSiteKey() { return loadedSiteKey; }
    };

    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', init);
    else
        init();
})();
