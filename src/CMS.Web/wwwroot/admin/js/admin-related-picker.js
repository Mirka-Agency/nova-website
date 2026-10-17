(function () {
  "use strict";

  function parseInitialSelected(root) {
    var raw = root.getAttribute("data-related-selected") || "[]";
    try {
      var parsed = JSON.parse(raw);
      if (!Array.isArray(parsed)) return [];
      return parsed
        .map(function (item) {
          return {
            id: String(item.id || item.Id || "").trim(),
            title: String(item.title || item.Title || "").trim()
          };
        })
        .filter(function (item) {
          return !!item.id;
        });
    } catch (e) {
      return [];
    }
  }

  function initPicker(root) {
    if (!root || root.getAttribute("data-related-bound") === "1") return;
    root.setAttribute("data-related-bound", "1");

    var searchUrl = root.getAttribute("data-related-search-url") || "";
    var excludeId = (root.getAttribute("data-related-exclude-id") || "").trim().toLowerCase();
    var maxItems = Number(root.getAttribute("data-related-max") || "6") || 6;
    var namePrefix = root.getAttribute("data-related-name-prefix") || "RelatedItems";
    var list = root.querySelector("[data-related-list]");
    var empty = root.querySelector("[data-related-empty]");
    var loading = root.querySelector("[data-related-loading]");
    var countEl = root.querySelector("[data-related-count]");
    var input = root.querySelector("[data-related-search-input]");
    var inputsHost = root.querySelector("[data-related-inputs]");
    var debounceTimer = null;
    var abortController = null;

    if (!list || !input || !inputsHost || !searchUrl) return;

    /** @type {Map<string, {id:string,title:string}>} */
    var selected = new Map();
    parseInitialSelected(root).forEach(function (item) {
      if (selected.size >= maxItems) return;
      selected.set(item.id.toLowerCase(), {
        id: item.id,
        title: item.title || item.id
      });
    });

    function syncHiddenInputs() {
      inputsHost.innerHTML = "";
      var index = 0;
      selected.forEach(function (item) {
        var wrap = document.createElement("div");
        wrap.setAttribute("data-related-row", "");
        wrap.setAttribute("data-related-id", item.id);

        var idInput = document.createElement("input");
        idInput.type = "hidden";
        idInput.name = namePrefix + "[" + index + "].Id";
        idInput.value = item.id;
        idInput.setAttribute("data-related-id-input", "");

        var titleInput = document.createElement("input");
        titleInput.type = "hidden";
        titleInput.name = namePrefix + "[" + index + "].Title";
        titleInput.value = item.title || "";
        titleInput.setAttribute("data-related-title-input", "");

        wrap.appendChild(idInput);
        wrap.appendChild(titleInput);
        inputsHost.appendChild(wrap);
        index += 1;
      });

      if (countEl) {
        countEl.textContent = selected.size + " از " + maxItems + " انتخاب شده";
      }
    }

    function setLoading(isLoading) {
      if (loading) loading.hidden = !isLoading;
    }

    function renderList(items) {
      list.innerHTML = "";
      var seen = new Set();
      var rows = [];

      // Keep currently selected items visible at the top even if search omits them.
      selected.forEach(function (item) {
        var key = item.id.toLowerCase();
        if (excludeId && key === excludeId) return;
        seen.add(key);
        rows.push(item);
      });

      (items || []).forEach(function (item) {
        var id = String(item.id || "").trim();
        var key = id.toLowerCase();
        if (!id || seen.has(key)) return;
        if (excludeId && key === excludeId) return;
        seen.add(key);
        rows.push({
          id: id,
          title: item.title || item.slug || id
        });
      });

      if (!rows.length) {
        if (empty) empty.hidden = false;
        return;
      }

      if (empty) empty.hidden = true;

      rows.forEach(function (item) {
        var key = item.id.toLowerCase();
        var isChecked = selected.has(key);

        var label = document.createElement("label");
        label.className = "admin-related-option";

        var checkbox = document.createElement("input");
        checkbox.type = "checkbox";
        checkbox.value = item.id;
        checkbox.checked = isChecked;
        checkbox.setAttribute("data-related-checkbox", "");
        checkbox.setAttribute("data-related-title", item.title || item.id);

        var text = document.createElement("span");
        text.className = "admin-related-option__text";
        text.textContent = item.title || item.id;

        label.appendChild(checkbox);
        label.appendChild(text);

        checkbox.addEventListener("change", function () {
          if (checkbox.checked) {
            if (selected.size >= maxItems) {
              checkbox.checked = false;
              window.alert("حداکثر " + maxItems + " مورد می‌توانید انتخاب کنید.");
              return;
            }
            selected.set(key, {
              id: item.id,
              title: item.title || item.id
            });
          } else {
            selected.delete(key);
          }
          syncHiddenInputs();
        });

        list.appendChild(label);
      });
    }

    function search(query) {
      if (abortController) abortController.abort();
      abortController = new AbortController();
      setLoading(true);

      var url = new URL(searchUrl, window.location.origin);
      if (query) url.searchParams.set("q", query);
      if (excludeId) url.searchParams.set("excludeId", excludeId);
      url.searchParams.set("take", "200");

      fetch(url.toString(), {
        method: "GET",
        headers: { Accept: "application/json" },
        credentials: "same-origin",
        signal: abortController.signal
      })
        .then(function (res) {
          if (!res.ok) throw new Error("search failed");
          return res.json();
        })
        .then(function (data) {
          renderList(data && data.items ? data.items : []);
        })
        .catch(function (err) {
          if (err && err.name === "AbortError") return;
          renderList([]);
        })
        .finally(function () {
          setLoading(false);
        });
    }

    input.addEventListener("input", function () {
      var q = (input.value || "").trim();
      window.clearTimeout(debounceTimer);
      debounceTimer = window.setTimeout(function () {
        search(q);
      }, 250);
    });

    syncHiddenInputs();
    search("");
  }

  document.querySelectorAll("[data-related-picker]").forEach(initPicker);
})();
