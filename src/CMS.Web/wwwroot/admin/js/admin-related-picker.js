(function () {
  "use strict";

  function initPicker(root) {
    if (!root || root.getAttribute("data-related-bound") === "1") return;
    root.setAttribute("data-related-bound", "1");

    var searchUrl = root.getAttribute("data-related-search-url") || "";
    var excludeId = (root.getAttribute("data-related-exclude-id") || "").trim();
    var maxItems = Number(root.getAttribute("data-related-max") || "6") || 6;
    var namePrefix = root.getAttribute("data-related-name-prefix") || "RelatedItems";
    var list = root.querySelector("[data-related-list]");
    var empty = root.querySelector("[data-related-empty]");
    var input = root.querySelector("[data-related-search-input]");
    var results = root.querySelector("[data-related-search-results]");
    var debounceTimer = null;
    var abortController = null;

    if (!list || !input || !results || !searchUrl) return;

    function selectedIds() {
      return Array.from(list.querySelectorAll("[data-related-row]"))
        .map(function (row) {
          return (row.getAttribute("data-related-id") || "").toLowerCase();
        })
        .filter(Boolean);
    }

    function reindex() {
      var rows = Array.from(list.querySelectorAll("[data-related-row]"));
      rows.forEach(function (row, index) {
        var idInput = row.querySelector("[data-related-id-input]");
        var titleInput = row.querySelector("[data-related-title-input]");
        if (idInput) idInput.name = namePrefix + "[" + index + "].Id";
        if (titleInput) titleInput.name = namePrefix + "[" + index + "].Title";
      });
      if (empty) empty.hidden = rows.length > 0;
    }

    function addItem(id, title) {
      var normalized = String(id || "").toLowerCase();
      if (!normalized || selectedIds().indexOf(normalized) >= 0) return;
      if (selectedIds().length >= maxItems) {
        window.alert("حداکثر " + maxItems + " مورد می‌توانید انتخاب کنید.");
        return;
      }

      var row = document.createElement("div");
      row.className = "admin-faq-row admin-related-row";
      row.setAttribute("data-related-row", "");
      row.setAttribute("data-related-id", id);
      row.innerHTML =
        '<input type="hidden" data-related-id-input />' +
        '<input type="hidden" data-related-title-input />' +
        '<div class="admin-field admin-faq-field" style="flex:1;">' +
        '<span class="admin-related-row__title"></span>' +
        "</div>" +
        '<div class="admin-faq-actions">' +
        '<button type="button" class="btn btn-ghost" data-related-move-up title="بالا"><i class="fas fa-arrow-up" aria-hidden="true"></i></button>' +
        '<button type="button" class="btn btn-ghost" data-related-move-down title="پایین"><i class="fas fa-arrow-down" aria-hidden="true"></i></button>' +
        '<button type="button" class="btn btn-text-danger" data-related-remove><i class="fas fa-trash-alt" aria-hidden="true"></i><span>حذف</span></button>' +
        "</div>";

      var idInput = row.querySelector("[data-related-id-input]");
      var titleInput = row.querySelector("[data-related-title-input]");
      var titleEl = row.querySelector(".admin-related-row__title");
      if (idInput) idInput.value = id;
      if (titleInput) titleInput.value = title || "";
      if (titleEl) titleEl.textContent = title || id;

      list.appendChild(row);
      reindex();
      hideResults();
      input.value = "";
      input.focus();
    }

    function hideResults() {
      results.innerHTML = "";
      results.hidden = true;
    }

    function renderResults(items) {
      results.innerHTML = "";
      var selected = selectedIds();
      var filtered = (items || []).filter(function (item) {
        var id = String(item.id || "").toLowerCase();
        if (!id) return false;
        if (excludeId && id === excludeId.toLowerCase()) return false;
        if (selected.indexOf(id) >= 0) return false;
        return true;
      });

      if (!filtered.length) {
        var emptyLi = document.createElement("li");
        emptyLi.className = "admin-related-search__empty";
        emptyLi.textContent = "موردی پیدا نشد.";
        results.appendChild(emptyLi);
        results.hidden = false;
        return;
      }

      filtered.forEach(function (item) {
        var li = document.createElement("li");
        var btn = document.createElement("button");
        btn.type = "button";
        btn.className = "admin-related-search__item";
        btn.textContent = item.title || item.slug || String(item.id);
        btn.addEventListener("click", function () {
          addItem(item.id, item.title || item.slug || String(item.id));
        });
        li.appendChild(btn);
        results.appendChild(li);
      });
      results.hidden = false;
    }

    function search(query) {
      if (abortController) abortController.abort();
      abortController = new AbortController();
      var url = new URL(searchUrl, window.location.origin);
      if (query) url.searchParams.set("q", query);
      if (excludeId) url.searchParams.set("excludeId", excludeId);
      url.searchParams.set("take", "20");

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
          renderResults(data && data.items ? data.items : []);
        })
        .catch(function (err) {
          if (err && err.name === "AbortError") return;
          hideResults();
        });
    }

    input.addEventListener("input", function () {
      var q = (input.value || "").trim();
      window.clearTimeout(debounceTimer);
      if (!q) {
        hideResults();
        return;
      }
      debounceTimer = window.setTimeout(function () {
        search(q);
      }, 250);
    });

    input.addEventListener("focus", function () {
      var q = (input.value || "").trim();
      if (q) search(q);
    });

    document.addEventListener("click", function (event) {
      if (!root.contains(event.target)) hideResults();
    });

    list.addEventListener("click", function (event) {
      var removeBtn = event.target.closest("[data-related-remove]");
      if (removeBtn) {
        var row = removeBtn.closest("[data-related-row]");
        if (row) row.remove();
        reindex();
        return;
      }

      var upBtn = event.target.closest("[data-related-move-up]");
      if (upBtn) {
        var upRow = upBtn.closest("[data-related-row]");
        if (upRow && upRow.previousElementSibling) {
          list.insertBefore(upRow, upRow.previousElementSibling);
          reindex();
        }
        return;
      }

      var downBtn = event.target.closest("[data-related-move-down]");
      if (downBtn) {
        var downRow = downBtn.closest("[data-related-row]");
        if (downRow && downRow.nextElementSibling) {
          list.insertBefore(downRow.nextElementSibling, downRow);
          reindex();
        }
      }
    });

    reindex();
  }

  document.querySelectorAll("[data-related-picker]").forEach(initPicker);
})();
