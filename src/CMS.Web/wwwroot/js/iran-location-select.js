(function () {
  "use strict";

  const provincesUrl = "/api/v1/locations/provinces";

  function citiesUrl(provinceId) {
    return `/api/v1/locations/provinces/${provinceId}/cities`;
  }

  async function fetchJson(url) {
    const response = await fetch(url, {
      headers: { Accept: "application/json" },
      credentials: "same-origin",
    });
    if (!response.ok) throw new Error("location fetch failed");
    return response.json();
  }

  function fillSelect(select, items, selectedValue, placeholder) {
    const current = selectedValue || select.value || "";
    select.innerHTML = "";
    const empty = document.createElement("option");
    empty.value = "";
    empty.textContent = placeholder;
    select.appendChild(empty);

    items.forEach((item) => {
      const opt = document.createElement("option");
      opt.value = item.name;
      opt.textContent = item.name;
      if (item.id) opt.dataset.id = item.id;
      if (current && current === item.name) opt.selected = true;
      select.appendChild(opt);
    });
  }

  async function loadCities(provinceSelect, citySelect, selectedCity) {
    const selected = provinceSelect.selectedOptions[0];
    const provinceId = selected?.dataset?.id;
    citySelect.disabled = true;
    fillSelect(citySelect, [], "", citySelect.dataset.placeholder || "شهر");

    if (!provinceId) {
      citySelect.disabled = true;
      return;
    }

    const cities = await fetchJson(citiesUrl(provinceId));
    fillSelect(citySelect, cities, selectedCity || "", citySelect.dataset.placeholder || "شهر");
    citySelect.disabled = false;
  }

  async function bindPair(root) {
    const provinceSelect = root.querySelector("[data-iran-province]");
    const citySelect = root.querySelector("[data-iran-city]");
    if (!provinceSelect || !citySelect) return;

    const selectedProvince = provinceSelect.dataset.selected || provinceSelect.value || "";
    const selectedCity = citySelect.dataset.selected || citySelect.value || "";

    provinceSelect.disabled = true;
    citySelect.disabled = true;

    try {
      const provinces = await fetchJson(provincesUrl);
      fillSelect(
        provinceSelect,
        provinces,
        selectedProvince,
        provinceSelect.dataset.placeholder || "استان"
      );

      // Attach province ids onto options for subsequent city loads.
      Array.from(provinceSelect.options).forEach((opt) => {
        if (!opt.value) return;
        const match = provinces.find((p) => p.name === opt.value);
        if (match) opt.dataset.id = match.id;
      });

      provinceSelect.disabled = false;
      await loadCities(provinceSelect, citySelect, selectedCity);
    } catch (err) {
      console.error(err);
      provinceSelect.disabled = false;
    }

    provinceSelect.addEventListener("change", () => {
      loadCities(provinceSelect, citySelect, "");
    });
  }

  function bindAll(scope) {
    (scope || document).querySelectorAll("[data-iran-location]").forEach((root) => {
      if (root.dataset.iranBound === "1") return;
      root.dataset.iranBound = "1";
      bindPair(root);
    });
  }

  window.IranLocationSelect = {
    bindAll,
    bindRoot: bindPair,
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", () => bindAll(document));
  } else {
    bindAll(document);
  }
})();
