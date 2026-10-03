(function () {
  function isSlugInput(el) {
    return (
      el instanceof HTMLInputElement &&
      !el.readOnly &&
      !el.disabled &&
      el.type !== "hidden" &&
      (el.name === "Slug" || el.name.endsWith(".Slug"))
    );
  }

  function normalizeSlug(value, trimEdges) {
    if (!value) return "";

    let out = "";
    for (const ch of value.toLowerCase().normalize("NFD")) {
      if (/\p{Mn}/u.test(ch)) continue;
      if (/[\p{L}\p{N}]/u.test(ch)) out += ch;
      else if (/[\s_\u200c-]/.test(ch)) out += "-";
    }

    out = out.replace(/-{2,}/g, "-").replace(/^-+/, "");
    if (trimEdges) out = out.replace(/-+$/, "");
    return out;
  }

  function limitLength(input, value) {
    const max = input.maxLength;
    if (max > 0 && value.length > max) return value.slice(0, max).replace(/-+$/, "");
    return value;
  }

  function correct(input, trimEdges) {
    if (!isSlugInput(input) || input.composing) return;

    const next = limitLength(input, normalizeSlug(input.value, trimEdges));
    if (next === input.value) return;

    const atEnd =
      input.selectionStart === input.value.length && input.selectionEnd === input.value.length;
    const start = input.selectionStart;
    input.value = next;

    if (document.activeElement === input && start != null) {
      const pos = atEnd ? next.length : Math.min(start, next.length);
      input.setSelectionRange(pos, pos);
    }
  }

  function prepare(input) {
    if (!isSlugInput(input) || input.dataset.slugBound === "1") return;
    input.dataset.slugBound = "1";
    input.dir = "ltr";
    input.spellcheck = false;
    input.autocapitalize = "off";
    correct(input, true);
  }

  document.querySelectorAll("input").forEach(prepare);

  document.addEventListener("focusin", (event) => {
    if (isSlugInput(event.target)) prepare(event.target);
  });

  document.addEventListener("compositionstart", (event) => {
    if (isSlugInput(event.target)) event.target.composing = true;
  });

  document.addEventListener("compositionend", (event) => {
    if (!isSlugInput(event.target)) return;
    event.target.composing = false;
    correct(event.target, false);
  });

  document.addEventListener("input", (event) => {
    if (isSlugInput(event.target)) correct(event.target, false);
  });

  document.addEventListener("focusout", (event) => {
    if (isSlugInput(event.target)) correct(event.target, true);
  });

  document.addEventListener(
    "submit",
    (event) => {
      const form = event.target;
      if (!(form instanceof HTMLFormElement)) return;
      form.querySelectorAll("input").forEach((input) => correct(input, true));
    },
    true
  );
})();
