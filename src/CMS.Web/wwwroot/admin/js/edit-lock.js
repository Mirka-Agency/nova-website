(function () {
  "use strict";

  const root = document.querySelector("[data-edit-lock]");
  if (!root) return;

  const entityType = root.getAttribute("data-entity-type");
  const entityId = root.getAttribute("data-entity-id");
  const lockToken = root.getAttribute("data-lock-token");
  const heartbeatUrl = root.getAttribute("data-heartbeat-url");
  const releaseUrl = root.getAttribute("data-release-url");
  const intervalMs = Number(root.getAttribute("data-interval-ms") || "20000");

  if (!entityType || !entityId || !lockToken || !heartbeatUrl || !releaseUrl) return;

  let released = false;
  let timerId = 0;

  function antiforgeryToken() {
    return (
      document.querySelector('meta[name="request-verification-token"]')?.getAttribute("content")
      || document.querySelector('input[name="__RequestVerificationToken"]')?.value
      || ""
    );
  }

  function payload() {
    return JSON.stringify({
      entityType: entityType,
      entityId: entityId,
      lockToken: lockToken
    });
  }

  function jsonHeaders() {
    return {
      "Content-Type": "application/json",
      Accept: "application/json",
      RequestVerificationToken: antiforgeryToken()
    };
  }

  async function heartbeat() {
    if (released || document.hidden) return;
    try {
      const res = await fetch(heartbeatUrl, {
        method: "POST",
        credentials: "same-origin",
        headers: jsonHeaders(),
        body: payload()
      });
      if (res.status === 409) {
        released = true;
        window.clearInterval(timerId);
        window.location.reload();
      }
    } catch (_) {
      /* network blip — lock TTL expires if heartbeats stay down */
    }
  }

  function release() {
    if (released) return;
    released = true;
    window.clearInterval(timerId);

    try {
      fetch(releaseUrl, {
        method: "POST",
        credentials: "same-origin",
        headers: jsonHeaders(),
        body: payload(),
        keepalive: true
      });
    } catch (_) {
      /* ignore unload errors */
    }
  }

  timerId = window.setInterval(heartbeat, Math.max(10000, intervalMs));
  heartbeat();

  window.addEventListener("pagehide", release);
  document.addEventListener("visibilitychange", function () {
    if (!document.hidden) heartbeat();
  });
})();
