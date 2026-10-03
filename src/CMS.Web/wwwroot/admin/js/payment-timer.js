(function () {
  function pad(n) {
    return String(n).padStart(2, "0");
  }

  function formatRemaining(ms) {
    var totalSec = Math.max(0, Math.floor(ms / 1000));
    var h = Math.floor(totalSec / 3600);
    var m = Math.floor((totalSec % 3600) / 60);
    var s = totalSec % 60;
    if (h > 0) return pad(h) + ":" + pad(m) + ":" + pad(s);
    return pad(m) + ":" + pad(s);
  }

  function tick(el) {
    var raw = el.getAttribute("data-expires-utc");
    if (!raw) return;
    var expires = Date.parse(raw);
    if (Number.isNaN(expires)) return;

    var remaining = expires - Date.now();
    if (remaining <= 0) {
      el.textContent = el.getAttribute("data-expired-label") || "منقضی";
      el.classList.add("is-expired");
      return;
    }

    el.textContent = formatRemaining(remaining);
    el.classList.toggle("is-urgent", remaining <= 5 * 60 * 1000);
  }

  function refreshAll() {
    document.querySelectorAll(".admin-payment-timer, .payment-timer").forEach(tick);
  }

  refreshAll();
  setInterval(refreshAll, 1000);
})();
