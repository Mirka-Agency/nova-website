(function () {
    function bind(node) {
        var form = node.closest("form");
        if (!form || form.dataset.recaptchaV3Bound === "1")
            return;

        var siteKey = node.getAttribute("data-recaptcha-sitekey");
        var action = node.getAttribute("data-recaptcha-action") || "submit";
        var tokenName = node.getAttribute("data-recaptcha-token") || "CaptchaToken";
        if (!siteKey)
            return;

        form.dataset.recaptchaV3Bound = "1";
        form.addEventListener("submit", function (event) {
            var tokenEl = form.querySelector('input[name="' + tokenName + '"]');
            if (!tokenEl || tokenEl.value)
                return;

            event.preventDefault();
            if (form.dataset.recaptchaV3Pending === "1")
                return;

            form.dataset.recaptchaV3Pending = "1";
            whenReady(function () {
                window.grecaptcha.execute(siteKey, { action: action }).then(function (token) {
                    form.dataset.recaptchaV3Pending = "0";
                    if (!token)
                        return;
                    tokenEl.value = token;
                    form.submit();
                }).catch(function () {
                    form.dataset.recaptchaV3Pending = "0";
                });
            }, function () {
                form.dataset.recaptchaV3Pending = "0";
            });
        });
    }

    function whenReady(onReady, onTimeout) {
        if (window.grecaptcha && window.grecaptcha.ready) {
            window.grecaptcha.ready(onReady);
            return;
        }

        var attempts = 0;
        var timer = window.setInterval(function () {
            attempts += 1;
            if (window.grecaptcha && window.grecaptcha.ready) {
                window.clearInterval(timer);
                window.grecaptcha.ready(onReady);
            } else if (attempts > 40) {
                window.clearInterval(timer);
                onTimeout();
            }
        }, 100);
    }

    function init() {
        document.querySelectorAll("[data-recaptcha-v3]").forEach(bind);
    }

    if (document.readyState === "loading")
        document.addEventListener("DOMContentLoaded", init);
    else
        init();
})();
