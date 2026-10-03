(function () {
  "use strict";

  // ASCII "." plus the Persian/Arabic decimal separators .NET renders under
  // fa-IR culture ("٫" U+066B arabic decimal separator, "،" U+060C arabic comma).
  const DECIMAL_SEPARATORS = /[.٫،]/;

  function digitsOnly(value) {
    const str = String(value || "").trim();
    if (!str) return "";
    // Server-rendered decimals (e.g. "150000٫00" from a NUMERIC(18,2) column
    // formatted under fa-IR culture) must be truncated at the decimal
    // separator, not stripped of it — otherwise the fractional digits merge
    // into the integer part (150000٫00 -> 15000000).
    const sepIndex = str.search(DECIMAL_SEPARATORS);
    const integerPart = sepIndex === -1 ? str : str.slice(0, sepIndex);
    return integerPart.replace(/[^\d]/g, "");
  }

  function formatPrice(value) {
    const raw = digitsOnly(value);
    if (!raw) return "";
    return Number(raw).toLocaleString("en-US");
  }

  function bindInput(input) {
    if (!input || input.dataset.priceBound === "1") return;
    input.dataset.priceBound = "1";
    input.setAttribute("inputmode", "numeric");
    input.setAttribute("autocomplete", "off");

    if (input.value) {
      input.value = formatPrice(input.value);
    }

    input.addEventListener("input", () => {
      const start = input.selectionStart;
      const before = input.value.length;
      input.value = formatPrice(input.value);
      const after = input.value.length;
      const next = Math.max(0, (start || 0) + (after - before));
      try {
        input.setSelectionRange(next, next);
      } catch {
        /* ignore */
      }
    });

    input.addEventListener("blur", () => {
      input.value = formatPrice(input.value);
    });
  }

  function bindAll(root) {
    (root || document).querySelectorAll("[data-price-input]").forEach(bindInput);
  }

  function prepareForm(form) {
    form.querySelectorAll("[data-price-input]").forEach((input) => {
      input.value = digitsOnly(input.value);
    });
  }

  bindAll(document);

  document.querySelectorAll("form").forEach((form) => {
    if (!form.querySelector("[data-price-input]")) return;
    form.addEventListener("submit", () => prepareForm(form));
  });

  window.AdminPriceInput = {
    bind: bindInput,
    bindAll: bindAll,
  };
})();
