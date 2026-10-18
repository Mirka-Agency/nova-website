/**
 * Nova Scroll-Scrubbed Text Highlight — CKEditor 5 plugin.
 *
 * Model: text attribute `scrollHighlight` = "#from|#to"
 * View:  <span class="nova-scroll-highlight" data-nova-scroll-highlight="1"
 *             data-nova-sh-from data-nova-sh-to style="color:from">…</span>
 *
 * Legacy <div class="nova-scroll-highlight"> is unwrapped on upcast.
 *
 * UI: SplitButton — click toggles default/last colors; arrow opens color form.
 * Depends on global CKEDITOR (UMD). Exposes window.NovaScrollHighlight.
 */
(function (global) {
  "use strict";

  if (typeof CKEDITOR === "undefined") {
    console.error("[NovaScrollHighlight] CKEDITOR global is missing.");
    return;
  }

  const {
    Plugin,
    Command,
    ButtonView,
    View,
    createDropdown,
    findAttributeRange,
    SplitButtonView,
    submitHandler,
  } = CKEDITOR;

  if (!Plugin || !Command || !createDropdown || !SplitButtonView) {
    console.error("[NovaScrollHighlight] Required CKEDITOR exports are missing.");
    return;
  }

  const ATTR = "scrollHighlight";
  const CLASS_NAME = "nova-scroll-highlight";
  const CHAR_CLASS = "nova-sh-char";
  const DATA_MARKER = "data-nova-scroll-highlight";
  const DATA_FROM = "data-nova-sh-from";
  const DATA_TO = "data-nova-sh-to";
  const DEFAULT_FROM = "#6b7280";
  const DEFAULT_TO = "#0f172a";
  const COMPONENT = "scrollHighlight";
  const COMMAND = "scrollHighlight";

  const TOOLBAR_ICON =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">' +
    '<path d="M3.2 14.8c2.4-1.2 4.4-3.8 5.6-6.6.4-.9 1.2-2.4 2.2-2.4.7 0 1.1.6 1.4 1.3.6 1.5 1.3 3 2.4 4.2.4.4 1 .6 1.6.6v1.5c-1.1 0-2.1-.4-2.9-1.2-.8-.9-1.4-2-1.9-3.2-.2.5-.5 1.1-.8 1.6C9.4 12.8 7 15.6 4.2 16.8l-1-2z"/>' +
    '<path d="M2.5 5.2h6.2v1.4H2.5V5.2zm0 3.2h4.6v1.4H2.5V8.4z" opacity=".55"/>' +
    "</svg>";

  const PRESETS = [
    { from: "#6b7280", to: "#0f172a", label: "خاکستری → مشکی" },
    { from: "#9ca3af", to: "#09707d", label: "خاکستری → برند" },
    { from: "#94a3b8", to: "#1e3a3f", label: "آبی‌خاکستری → تیره" },
  ];

  function normalizeHex(value, fallback) {
    const raw = String(value || "")
      .trim()
      .toLowerCase();
    if (/^#[0-9a-f]{6}$/.test(raw)) return raw;
    if (/^#[0-9a-f]{3}$/.test(raw)) {
      return "#" + raw[1] + raw[1] + raw[2] + raw[2] + raw[3] + raw[3];
    }
    if (/^[0-9a-f]{6}$/.test(raw)) return "#" + raw;
    return fallback || DEFAULT_FROM;
  }

  function encodeColors(from, to) {
    return normalizeHex(from, DEFAULT_FROM) + "|" + normalizeHex(to, DEFAULT_TO);
  }

  function decodeColors(value) {
    const parts = String(value || "").split("|");
    return {
      from: normalizeHex(parts[0], DEFAULT_FROM),
      to: normalizeHex(parts[1], DEFAULT_TO),
    };
  }

  function readColorsFromViewElement(viewElement) {
    const from =
      viewElement.getAttribute(DATA_FROM) ||
      viewElement.getStyle("color") ||
      DEFAULT_FROM;
    const to = viewElement.getAttribute(DATA_TO) || DEFAULT_TO;
    return encodeColors(from, to);
  }

  function isHighlightView(viewElement) {
    return (
      viewElement.is("element") &&
      viewElement.hasClass(CLASS_NAME) &&
      !viewElement.hasClass(CHAR_CLASS)
    );
  }

  function getAttributeAtSelection(selection) {
    const direct = selection.getAttribute(ATTR);
    if (direct) return direct;

    const pos = selection.getFirstPosition();
    if (pos && pos.textNode && pos.textNode.hasAttribute(ATTR)) {
      return pos.textNode.getAttribute(ATTR);
    }
    return null;
  }

  function findFullAttributeRange(editor, position, attrValue) {
    if (!position || !attrValue) return null;
    const model = editor.model;

    if (typeof findAttributeRange === "function") {
      try {
        return findAttributeRange(position, ATTR, attrValue, model);
      } catch (_) {
        /* fall through */
      }
    }

    const textNode = position.textNode;
    if (!textNode || !textNode.parent) return null;

    let startOffset = textNode.startOffset;
    let endOffset = textNode.endOffset;
    const parent = textNode.parent;

    for (let i = textNode.index - 1; i >= 0; i--) {
      const prev = parent.getChild(i);
      if (prev && prev.is("$text") && prev.getAttribute(ATTR) === attrValue) {
        startOffset = prev.startOffset;
      } else break;
    }
    for (let i = textNode.index + 1; i < parent.childCount; i++) {
      const next = parent.getChild(i);
      if (next && next.is("$text") && next.getAttribute(ATTR) === attrValue) {
        endOffset = next.endOffset;
      } else break;
    }

    return model.createRange(
      model.createPositionAt(parent, startOffset),
      model.createPositionAt(parent, endOffset)
    );
  }

  function applyAttributeToRanges(writer, model, ranges, encoded) {
    for (const range of model.schema.getValidRanges(ranges, ATTR)) {
      writer.setAttribute(ATTR, encoded, range);
    }
  }

  function removeAttributeFromRanges(writer, model, ranges) {
    for (const range of model.schema.getValidRanges(ranges, ATTR)) {
      writer.removeAttribute(ATTR, range);
    }
  }

  function createHighlightSpan(writer, encoded, forEditing) {
    const colors = decodeColors(encoded);
    return writer.createAttributeElement(
      "span",
      {
        class: CLASS_NAME,
        [DATA_MARKER]: "1",
        [DATA_FROM]: colors.from,
        [DATA_TO]: colors.to,
        style: forEditing
          ? `--nova-sh-from:${colors.from};--nova-sh-to:${colors.to};color:${colors.from}`
          : `color:${colors.from}`,
      },
      { priority: 5 }
    );
  }

  function applyHighlightToRange(writer, editor, modelRange, encoded) {
    for (const item of modelRange.getItems()) {
      if (
        !(item.is("$text") || item.is("$textProxy")) ||
        !editor.model.schema.checkAttribute(item, ATTR)
      ) {
        continue;
      }
      writer.setAttribute(ATTR, encoded, item);
      if (item.hasAttribute("fontColor")) {
        writer.removeAttribute("fontColor", item);
      }
      if (typeof item.getAttributes === "function") {
        for (const pair of Array.from(item.getAttributes())) {
          const key = pair[0];
          if (String(key).startsWith("html")) {
            writer.removeAttribute(key, item);
          }
        }
      }
    }
  }

  class ScrollHighlightCommand extends Command {
    refresh() {
      const model = this.editor.model;
      const selection = model.document.selection;
      const encoded = getAttributeAtSelection(selection);

      this.value = encoded ? decodeColors(encoded) : null;
      // Keep toolbar usable whenever formatting is allowed in selection.
      this.isEnabled = model.schema.checkAttributeInSelection(selection, ATTR);
    }

    execute(options) {
      const opts = options || {};
      if (opts.action === "remove") {
        this._remove();
        return;
      }
      if (opts.action === "toggle") {
        if (this.value) {
          this._remove();
        } else {
          this._apply(
            normalizeHex(opts.from, DEFAULT_FROM),
            normalizeHex(opts.to, DEFAULT_TO)
          );
        }
        return;
      }
      this._apply(
        normalizeHex(opts.from, DEFAULT_FROM),
        normalizeHex(opts.to, DEFAULT_TO)
      );
    }

    _apply(from, to) {
      const editor = this.editor;
      const model = editor.model;
      const selection = model.document.selection;
      const encoded = encodeColors(from, to);
      const current = getAttributeAtSelection(selection);

      model.change((writer) => {
        if (selection.isCollapsed) {
          if (!current) return;
          const range = findFullAttributeRange(
            editor,
            selection.getFirstPosition(),
            current
          );
          if (range) writer.setAttribute(ATTR, encoded, range);
          return;
        }

        applyAttributeToRanges(writer, model, selection.getRanges(), encoded);
      });
    }

    _remove() {
      const editor = this.editor;
      const model = editor.model;
      const selection = model.document.selection;
      const current = getAttributeAtSelection(selection);

      model.change((writer) => {
        if (!selection.isCollapsed) {
          removeAttributeFromRanges(writer, model, selection.getRanges());
          return;
        }

        if (!current) return;
        const range = findFullAttributeRange(
          editor,
          selection.getFirstPosition(),
          current
        );
        if (range) writer.removeAttribute(ATTR, range);
      });
    }
  }

  /**
   * Color form shown inside the split-button dropdown panel.
   * Implements focus()/focusLast() required by DropdownPanelView.
   */
  class ScrollHighlightFormView extends View {
    constructor(locale) {
      super(locale);

      this.fromInput = this._textInput("from-hex", "کد رنگ اولیه", DEFAULT_FROM);
      this.toInput = this._textInput("to-hex", "کد رنگ نهایی", DEFAULT_TO);

      this.applyButton = this._button("اعمال", "ck-button-action");
      this.removeButton = this._button("حذف افکت", "ck-button-flat");
      this.cancelButton = this._button("بستن", "ck-button-flat");

      this.set({
        fromColor: DEFAULT_FROM,
        toColor: DEFAULT_TO,
        canRemove: false,
      });

      this.removeButton.bind("isVisible").to(this, "canRemove");

      const presetChildren = PRESETS.map((preset, index) => ({
        tag: "button",
        attributes: {
          type: "button",
          class: ["ck-scroll-highlight-form__preset"],
          "data-preset": String(index),
          title: preset.label,
          style: `background:linear-gradient(90deg,${preset.from},${preset.to})`,
          tabindex: "-1",
        },
      }));

      this.setTemplate({
        tag: "form",
        attributes: {
          class: ["ck", "ck-scroll-highlight-form"],
          tabindex: "-1",
          dir: "rtl",
          lang: "fa",
        },
        children: [
          {
            tag: "p",
            attributes: { class: ["ck-scroll-highlight-form__hint"] },
            children: [
              "متن را انتخاب کنید. کلیک روی دکمهٔ ابزار، هایلایت را اعمال/حذف می‌کند. از اینجا رنگ را تغییر دهید.",
            ],
          },
          {
            tag: "div",
            attributes: {
              class: ["ck-scroll-highlight-form__presets"],
              role: "group",
              "aria-label": "پیش‌فرض رنگ",
            },
            children: presetChildren,
          },
          {
            tag: "label",
            attributes: { class: ["ck-scroll-highlight-form__label"] },
            children: [
              "رنگ اولیه",
              {
                tag: "span",
                attributes: { class: ["ck-scroll-highlight-form__swatch-wrap"] },
                children: [
                  {
                    tag: "span",
                    attributes: {
                      class: ["ck-scroll-highlight-form__swatch"],
                      "data-role": "from-swatch",
                      style: `background:${DEFAULT_FROM}`,
                    },
                  },
                  this.fromInput,
                ],
              },
            ],
          },
          {
            tag: "label",
            attributes: { class: ["ck-scroll-highlight-form__label"] },
            children: [
              "رنگ نهایی",
              {
                tag: "span",
                attributes: { class: ["ck-scroll-highlight-form__swatch-wrap"] },
                children: [
                  {
                    tag: "span",
                    attributes: {
                      class: ["ck-scroll-highlight-form__swatch"],
                      "data-role": "to-swatch",
                      style: `background:${DEFAULT_TO}`,
                    },
                  },
                  this.toInput,
                ],
              },
            ],
          },
          {
            tag: "div",
            attributes: { class: ["ck-scroll-highlight-form__actions"] },
            children: [this.applyButton, this.removeButton, this.cancelButton],
          },
        ],
      });
    }

    render() {
      super.render();

      if (typeof submitHandler === "function") {
        submitHandler({ view: this });
      }

      this.element.addEventListener(
        "mousedown",
        (event) => {
          event.stopPropagation();
        },
        true
      );

      this.element.addEventListener("click", (event) => {
        const presetBtn = event.target.closest("[data-preset]");
        if (!presetBtn || !this.element.contains(presetBtn)) return;
        const preset = PRESETS[Number(presetBtn.getAttribute("data-preset"))];
        if (preset) this.setColors(preset.from, preset.to);
      });

      const onHexInput = (input, key) => {
        input.element.addEventListener("input", () => {
          this[key] = normalizeHex(input.element.value, this[key]);
          this._syncSwatches();
        });
        input.element.addEventListener("change", () => {
          this[key] = normalizeHex(
            input.element.value,
            key === "fromColor" ? DEFAULT_FROM : DEFAULT_TO
          );
          this._syncInputs();
        });
      };
      onHexInput(this.fromInput, "fromColor");
      onHexInput(this.toInput, "toColor");
    }

    // Required by DropdownPanelView — without this, opening the panel breaks.
    focus() {
      if (this.fromInput && this.fromInput.element) {
        this.fromInput.element.focus();
        return;
      }
      if (this.element) this.element.focus();
    }

    focusLast() {
      if (this.cancelButton && typeof this.cancelButton.focus === "function") {
        this.cancelButton.focus();
        return;
      }
      this.focus();
    }

    _textInput(role, ariaLabel, value) {
      const input = new View(this.locale);
      input.setTemplate({
        tag: "input",
        attributes: {
          type: "text",
          class: ["ck-scroll-highlight-form__hex", "ck-input", "ck-input-text"],
          maxlength: "7",
          spellcheck: "false",
          "data-role": role,
          "aria-label": ariaLabel,
          value: value,
          dir: "ltr",
        },
      });
      input.focus = function focusInput() {
        if (input.element) input.element.focus();
      };
      return input;
    }

    _button(label, extraClass) {
      const button = new ButtonView(this.locale);
      button.set({
        label: label,
        withText: true,
        class: extraClass,
      });
      return button;
    }

    setColors(from, to) {
      this.fromColor = normalizeHex(from, DEFAULT_FROM);
      this.toColor = normalizeHex(to, DEFAULT_TO);
      this._syncInputs();
    }

    getColors() {
      if (this.fromInput.element && this.fromInput.element.value) {
        this.fromColor = this.fromInput.element.value;
      }
      if (this.toInput.element && this.toInput.element.value) {
        this.toColor = this.toInput.element.value;
      }
      return {
        from: normalizeHex(this.fromColor, DEFAULT_FROM),
        to: normalizeHex(this.toColor, DEFAULT_TO),
      };
    }

    setRemoveVisible(visible) {
      this.canRemove = !!visible;
    }

    _syncInputs() {
      if (this.fromInput.element) this.fromInput.element.value = this.fromColor;
      if (this.toInput.element) this.toInput.element.value = this.toColor;
      this._syncSwatches();
    }

    _syncSwatches() {
      if (!this.element) return;
      const fromSwatch = this.element.querySelector('[data-role="from-swatch"]');
      const toSwatch = this.element.querySelector('[data-role="to-swatch"]');
      if (fromSwatch) fromSwatch.style.background = this.fromColor;
      if (toSwatch) toSwatch.style.background = this.toColor;
    }
  }

  class ScrollHighlight extends Plugin {
    static get pluginName() {
      return "ScrollHighlight";
    }

    init() {
      this._lastFrom = DEFAULT_FROM;
      this._lastTo = DEFAULT_TO;

      this._defineSchema();
      this._defineConverters();
      this._registerCaretBehavior();
      this.editor.commands.add(COMMAND, new ScrollHighlightCommand(this.editor));
      this._createUi();
    }

    _defineSchema() {
      const schema = this.editor.model.schema;
      schema.extend("$text", { allowAttributes: ATTR });
      schema.setAttributeProperties(ATTR, {
        isFormatting: true,
        copyOnEnter: false,
      });
    }

    _registerCaretBehavior() {
      const twoStep = this.editor.plugins.get("TwoStepCaretMovement");
      if (twoStep && typeof twoStep.registerAttribute === "function") {
        twoStep.registerAttribute(ATTR);
      }
    }

    _defineConverters() {
      const conversion = this.editor.conversion;
      const editor = this.editor;

      conversion.for("editingDowncast").attributeToElement({
        model: ATTR,
        view: (value, { writer }) =>
          value ? createHighlightSpan(writer, value, true) : null,
      });

      conversion.for("dataDowncast").attributeToElement({
        model: ATTR,
        view: (value, { writer }) =>
          value ? createHighlightSpan(writer, value, false) : null,
      });

      conversion.for("upcast").add((dispatcher) => {
        dispatcher.on(
          "element:span",
          (evt, data, conversionApi) => {
            const viewItem = data.viewItem;

            if (viewItem.hasClass(CHAR_CLASS)) {
              if (
                !conversionApi.consumable.consume(viewItem, {
                  name: true,
                  classes: CHAR_CLASS,
                })
              ) {
                return;
              }
              const { modelRange } = conversionApi.convertChildren(
                viewItem,
                data.modelCursor
              );
              data.modelRange = modelRange;
              data.modelCursor = modelRange.end;
              evt.stop();
              return;
            }

            const isMarked =
              isHighlightView(viewItem) ||
              viewItem.getAttribute(DATA_MARKER) === "1" ||
              viewItem.hasAttribute(DATA_FROM) ||
              viewItem.hasAttribute(DATA_TO);

            if (!isMarked) return;
            if (!conversionApi.consumable.test(viewItem, { name: true })) return;

            conversionApi.consumable.consume(viewItem, { name: true });
            if (viewItem.hasClass(CLASS_NAME)) {
              conversionApi.consumable.consume(viewItem, {
                classes: CLASS_NAME,
              });
            }
            conversionApi.consumable.consume(viewItem, {
              attributes: [DATA_MARKER, DATA_FROM, DATA_TO],
            });
            conversionApi.consumable.consume(viewItem, { styles: "color" });

            const encoded = readColorsFromViewElement(viewItem);
            const { modelRange } = conversionApi.convertChildren(
              viewItem,
              data.modelCursor
            );

            if (modelRange) {
              applyHighlightToRange(
                conversionApi.writer,
                editor,
                modelRange,
                encoded
              );
            }

            data.modelRange = modelRange;
            data.modelCursor = modelRange ? modelRange.end : data.modelCursor;
            evt.stop();
          },
          { priority: "high" }
        );
      });

      conversion.for("upcast").add((dispatcher) => {
        dispatcher.on(
          "element:div",
          (evt, data, conversionApi) => {
            const viewItem = data.viewItem;
            if (!isHighlightView(viewItem)) return;

            if (
              !conversionApi.consumable.consume(viewItem, {
                name: true,
                classes: CLASS_NAME,
              })
            ) {
              return;
            }

            conversionApi.consumable.consume(viewItem, {
              attributes: [DATA_MARKER, DATA_FROM, DATA_TO],
            });
            conversionApi.consumable.consume(viewItem, { styles: "color" });

            const encoded = readColorsFromViewElement(viewItem);
            const { modelRange } = conversionApi.convertChildren(
              viewItem,
              data.modelCursor
            );

            if (modelRange) {
              applyHighlightToRange(
                conversionApi.writer,
                editor,
                modelRange,
                encoded
              );
            }

            data.modelRange = modelRange;
            data.modelCursor = modelRange ? modelRange.end : data.modelCursor;
            evt.stop();
          },
          { priority: "high" }
        );
      });
    }

    _createUi() {
      const editor = this.editor;
      const command = editor.commands.get(COMMAND);
      const plugin = this;

      editor.ui.componentFactory.add(COMPONENT, (locale) => {
        const dropdown = createDropdown(locale, SplitButtonView);
        const form = new ScrollHighlightFormView(locale);

        dropdown.buttonView.set({
          label: "هایلایت اسکرولی متن",
          icon: TOOLBAR_ICON,
          tooltip: true,
        });

        dropdown.bind("isEnabled").to(command, "isEnabled");
        dropdown.buttonView.bind("isOn").to(command, "value", (value) => !!value);

        dropdown.panelView.children.add(form);

        // Main split action: toggle highlight with last/default colors.
        dropdown.buttonView.on("execute", () => {
          const selection = editor.model.document.selection;
          const hasValue = !!command.value;
          const canApply = !selection.isCollapsed || hasValue;

          if (!canApply) {
            dropdown.isOpen = true;
            return;
          }

          editor.execute(COMMAND, {
            action: "toggle",
            from: plugin._lastFrom,
            to: plugin._lastTo,
          });
          editor.editing.view.focus();
        });

        dropdown.on("change:isOpen", (_evt, _name, isOpen) => {
          if (!isOpen) return;

          const value = command.value;
          if (value) {
            form.setColors(value.from, value.to);
            form.setRemoveVisible(true);
          } else {
            form.setColors(plugin._lastFrom, plugin._lastTo);
            form.setRemoveVisible(false);
          }

          setTimeout(function () {
            form._syncInputs();
          }, 0);
        });

        form.on("submit", () => {
          const colors = form.getColors();
          plugin._lastFrom = colors.from;
          plugin._lastTo = colors.to;
          editor.execute(COMMAND, {
            action: "apply",
            from: colors.from,
            to: colors.to,
          });
          dropdown.isOpen = false;
          editor.editing.view.focus();
        });

        form.applyButton.on("execute", () => {
          form.fire("submit");
        });

        form.removeButton.on("execute", () => {
          editor.execute(COMMAND, { action: "remove" });
          dropdown.isOpen = false;
          editor.editing.view.focus();
        });

        form.cancelButton.on("execute", () => {
          dropdown.isOpen = false;
          editor.editing.view.focus();
        });

        return dropdown;
      });
    }
  }

  global.NovaScrollHighlight = {
    plugin: ScrollHighlight,
    pluginName: "ScrollHighlight",
    componentName: COMPONENT,
    commandName: COMMAND,
    defaults: { from: DEFAULT_FROM, to: DEFAULT_TO },
    className: CLASS_NAME,
  };
})(typeof window !== "undefined" ? window : globalThis);
