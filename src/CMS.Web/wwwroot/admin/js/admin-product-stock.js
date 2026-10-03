(function () {
  "use strict";

  const checkbox = document.querySelector("[data-unlimited-stock]");
  const stockField = document.querySelector("[data-stock-quantity-field]");
  if (!checkbox || !stockField) return;

  function sync() {
    stockField.hidden = checkbox.checked;
  }

  checkbox.addEventListener("change", sync);
  sync();
})();
