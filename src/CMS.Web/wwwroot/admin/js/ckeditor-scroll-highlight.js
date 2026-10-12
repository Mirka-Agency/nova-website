/**
 * Nova Scroll-Scrubbed Text Highlight — reusable CKEditor 5 plugin.
 * Depends on global CKEDITOR (UMD). Exposes window.NovaScrollHighlight.
 */
(function (global) {
  "use strict";

  if (typeof CKEDITOR === "undefined") {
    return;
  }

  const {
    Plugin,
    Command,
    ButtonView,
    View,
    createDropdown,
    findAttributeRange,
  } = CKEDITOR;

  const INLINE_ATTR = "scrollHighlight";
  const BLOCK_NAME = "scrollHighlightBlock";
  const CLASS_NAME = "nova-scroll-highlight";
  const DATA_MARKER = "data-nova-scroll-highlight";
  const DATA_FROM = "data-nova-sh-from";
  const DATA_TO = "data-nova-sh-to";
  const DEFAULT_FROM = "#6b7280";
  const DEFAULT_TO = "#0f172a";
  const COMPONENT = "scrollHighlight";

  const TOOLBAR_ICON =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">' +
    '<path d="M3.2 14.8c2.4-1.2 4.4-3.8 5.6-6.6.4-.9 1.2-2.4 2.2-2.4.7 0 1.1.6 1.4 1.3.6 1.5 1.3 3 2.4 4.2.4.4 1 .6 1.6.6v1.5c-1.1 0-2.1-.4-2.9-1.2-.8-.9-1.4-2-1.9-3.2-.2.5-.5 1.1-.8 1.6C9.4 12.8 7 15.6 4.2 16.8l-1-2z"/>' +
    '<path d="M2.5 5.2h6.2v1.4H2.5V5.2zm0 3.2h4.6v1.4H2.5V8.4z" opacity=".55"/>' +
    "</svg>";

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

  function findBlockAncestor(element) {
    let current = element;
    while (current) {
      if (current.is && current.is("element", BLOCK_NAME)) return current;
      current = current.parent;
    }
    return null;
  }

  function selectionCoversWholeBlock(selection, block) {
    if (!selection || !block) return false;
    if (selection.getSelectedElement() === block) return true;
    const range = selection.getFirstRange();
    if (!range || typeof range.containsItem !== "function") return false;
    try {
      if (!range.containsItem(block)) return false;
      return Array.from(block.getChildren()).every(function (child) {
        return range.containsItem(child);
      });
    } catch (_) {
      return false;
    }
  }

  function getActiveState(editor) {
    const selection = editor.model.document.selection;
    const selected = selection.getSelectedElement();

    if (selected && selected.is("element", BLOCK_NAME)) {
      return {
        mode: "block",
        element: selected,
        from: normalizeHex(selected.getAttribute("scrollHighlightFrom"), DEFAULT_FROM),
        to: normalizeHex(selected.getAttribute("scrollHighlightTo"), DEFAULT_TO),
      };
    }

    const pos = selection.getFirstPosition();
    if (pos) {
      const block = findBlockAncestor(pos.parent);
      if (block) {
        return {
          mode: "block",
          element: block,
          from: normalizeHex(block.getAttribute("scrollHighlightFrom"), DEFAULT_FROM),
          to: normalizeHex(block.getAttribute("scrollHighlightTo"), DEFAULT_TO),
        };
      }
    }

    const inlineValue = selection.getAttribute(INLINE_ATTR);
    if (inlineValue) {
      const colors = decodeColors(inlineValue);
      return { mode: "inline", element: null, from: colors.from, to: colors.to };
    }

    if (pos && pos.textNode && typeof pos.textNode.hasAttribute === "function") {
      if (pos.textNode.hasAttribute(INLINE_ATTR)) {
        const colors = decodeColors(pos.textNode.getAttribute(INLINE_ATTR));
        return { mode: "inline", element: null, from: colors.from, to: colors.to };
      }
    }

    return null;
  }

  function getInlineAttributeRange(editor, position) {
    if (!position) return null;
    const model = editor.model;

    let attrValue = null;
    if (position.textNode && position.textNode.hasAttribute(INLINE_ATTR)) {
      attrValue = position.textNode.getAttribute(INLINE_ATTR);
    } else {
      attrValue = editor.model.document.selection.getAttribute(INLINE_ATTR);
    }
    if (!attrValue) return null;

    if (typeof findAttributeRange === "function") {
      try {
        return findAttributeRange(position, INLINE_ATTR, attrValue, model);
      } catch (_) {
        /* fall through to manual expand */
      }
    }

    // Expand left/right across adjacent text with the same attribute.
    const root = position.root;
    const range = model.createRange(
      model.createPositionAt(root, 0),
      model.createPositionAt(root, "end")
    );
    // Safer: walk siblings from text node.
    const textNode = position.textNode;
    if (!textNode || !textNode.parent) {
      return model.document.selection.getFirstRange();
    }

    let startOffset = textNode.startOffset;
    let endOffset = textNode.endOffset;
    const parent = textNode.parent;

    for (let i = textNode.index - 1; i >= 0; i--) {
      const prev = parent.getChild(i);
      if (
        prev &&
        prev.is &&
        prev.is("$text") &&
        prev.getAttribute(INLINE_ATTR) === attrValue
      ) {
        startOffset = prev.startOffset;
      } else break;
    }
    for (let i = textNode.index + 1; i < parent.childCount; i++) {
      const next = parent.getChild(i);
      if (
        next &&
        next.is &&
        next.is("$text") &&
        next.getAttribute(INLINE_ATTR) === attrValue
      ) {
        endOffset = next.endOffset;
      } else break;
    }

    return model.createRange(
      model.createPositionAt(parent, startOffset),
      model.createPositionAt(parent, endOffset)
    );
  }

  function blockViewAttributes(modelElement, forEditing) {
    const from = normalizeHex(
      modelElement.getAttribute("scrollHighlightFrom"),
      DEFAULT_FROM
    );
    const to = normalizeHex(
      modelElement.getAttribute("scrollHighlightTo"),
      DEFAULT_TO
    );
    const attrs = {
      class: CLASS_NAME,
      [DATA_MARKER]: "1",
      [DATA_FROM]: from,
      [DATA_TO]: to,
      style: forEditing
        ? `--nova-sh-from:${from};--nova-sh-to:${to};color:${from}`
        : `color:${from}`,
    };
    return attrs;
  }

  function applyInlineToSelection(writer, model, selection, encoded) {
    for (const range of model.schema.getValidRanges(
      selection.getRanges(),
      INLINE_ATTR
    )) {
      writer.setAttribute(INLINE_ATTR, encoded, range);
    }
  }

  function removeInlineFromSelection(writer, model, selection) {
    for (const range of model.schema.getValidRanges(
      selection.getRanges(),
      INLINE_ATTR
    )) {
      writer.removeAttribute(INLINE_ATTR, range);
    }
  }

  class ScrollHighlightCommand extends Command {
    refresh() {
      const selection = this.editor.model.document.selection;
      const active = getActiveState(this.editor);
      this.value = active
        ? { from: active.from, to: active.to, mode: active.mode }
        : null;

      const selected = selection.getSelectedElement();
      const blocked =
        selected &&
        (selected.is("element", "imageBlock") ||
          selected.is("element", "imageInline") ||
          selected.is("element", "rawHtml") ||
          selected.is("element", "media") ||
          selected.is("element", "table") ||
          selected.is("element", "novaVideoWarning"));

      this.isEnabled = !!active || (!selection.isCollapsed && !blocked);
    }

    execute(options) {
      const opts = options || {};
      if (opts.action === "remove") {
        this._remove();
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
      const active = getActiveState(editor);

      model.change((writer) => {
        // Updating an existing block only when the whole block is targeted.
        if (
          active &&
          active.mode === "block" &&
          active.element &&
          (selection.isCollapsed ||
            selection.getSelectedElement() === active.element ||
            selectionCoversWholeBlock(selection, active.element))
        ) {
          writer.setAttribute("scrollHighlightFrom", from, active.element);
          writer.setAttribute("scrollHighlightTo", to, active.element);
          return;
        }

        if (selection.isCollapsed) {
          if (active && active.mode === "inline") {
            const range = getInlineAttributeRange(
              editor,
              selection.getFirstPosition()
            );
            if (range) writer.setAttribute(INLINE_ATTR, encoded, range);
          }
          return;
        }

        // Always apply as inline formatting across valid ranges.
        // Avoid wrapping multi-block selections in a div (breaks lists/tables).
        applyInlineToSelection(writer, model, selection, encoded);
      });
    }

    _remove() {
      const editor = this.editor;
      const model = editor.model;
      const selection = model.document.selection;
      const active = getActiveState(editor);

      model.change((writer) => {
        if (active && active.mode === "block" && active.element) {
          const whole =
            selection.isCollapsed ||
            selection.getSelectedElement() === active.element ||
            selectionCoversWholeBlock(selection, active.element);

          if (whole) {
            if (typeof writer.unwrap === "function") {
              writer.unwrap(active.element);
            } else {
              const parent = active.element.parent;
              const index = parent.getChildIndex(active.element);
              const children = Array.from(active.element.getChildren());
              writer.remove(active.element);
              let at = index;
              for (const child of children) {
                writer.insert(child, parent, at);
                at += 1;
              }
            }
            return;
          }

          // Partial selection inside a legacy block: strip inline attrs only.
          removeInlineFromSelection(writer, model, selection);
          return;
        }

        if (!selection.isCollapsed) {
          removeInlineFromSelection(writer, model, selection);
          return;
        }

        if (active && active.mode === "inline") {
          const range = getInlineAttributeRange(
            editor,
            selection.getFirstPosition()
          );
          if (range) writer.removeAttribute(INLINE_ATTR, range);
        }
      });
    }
  }

  class ScrollHighlightFormView extends View {
    constructor(locale) {
      super(locale);

      this.set({
        fromColor: DEFAULT_FROM,
        toColor: DEFAULT_TO,
        canRemove: false,
      });

      this.applyButton = this._button("اعمال", "ck-button-action");
      this.removeButton = this._button("حذف افکت", "ck-button-flat");
      this.cancelButton = this._button("بستن", "ck-button-flat");

      this.removeButton.bind("isVisible").to(this, "canRemove");

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
              "متن را انتخاب کنید، رنگ اولیه و نهایی را تنظیم کنید، سپس «اعمال» را بزنید.",
            ],
          },
          {
            tag: "label",
            attributes: { class: ["ck-scroll-highlight-form__label"] },
            children: [
              "رنگ اولیه",
              {
                tag: "input",
                attributes: {
                  type: "color",
                  class: ["ck-scroll-highlight-form__color"],
                  value: DEFAULT_FROM,
                  "data-role": "from",
                },
              },
            ],
          },
          {
            tag: "label",
            attributes: { class: ["ck-scroll-highlight-form__label"] },
            children: [
              "رنگ نهایی",
              {
                tag: "input",
                attributes: {
                  type: "color",
                  class: ["ck-scroll-highlight-form__color"],
                  value: DEFAULT_TO,
                  "data-role": "to",
                },
              },
            ],
          },
          {
            tag: "div",
            attributes: { class: ["ck-scroll-highlight-form__hex-row"] },
            children: [
              {
                tag: "input",
                attributes: {
                  type: "text",
                  class: ["ck-scroll-highlight-form__hex"],
                  maxlength: "7",
                  spellcheck: "false",
                  "data-role": "from-hex",
                  "aria-label": "کد رنگ اولیه",
                  value: DEFAULT_FROM,
                },
              },
              {
                tag: "input",
                attributes: {
                  type: "text",
                  class: ["ck-scroll-highlight-form__hex"],
                  maxlength: "7",
                  spellcheck: "false",
                  "data-role": "to-hex",
                  "aria-label": "کد رنگ نهایی",
                  value: DEFAULT_TO,
                },
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

      this.element.addEventListener("submit", (event) => {
        event.preventDefault();
      });

      // Keep CKEditor dropdown open while interacting with native color UI.
      this.element.addEventListener(
        "mousedown",
        (event) => {
          event.stopPropagation();
        },
        true
      );
      this.element.addEventListener(
        "pointerdown",
        (event) => {
          event.stopPropagation();
        },
        true
      );

      this.element.addEventListener("input", (event) => {
        const target = event.target;
        if (!target) return;
        const role = target.getAttribute("data-role");
        if (role === "from" || role === "from-hex") {
          this.fromColor = normalizeHex(target.value, this.fromColor);
          this._syncInputs();
        }
        if (role === "to" || role === "to-hex") {
          this.toColor = normalizeHex(target.value, this.toColor);
          this._syncInputs();
        }
      });
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
      this._readInputs();
      return {
        from: normalizeHex(this.fromColor, DEFAULT_FROM),
        to: normalizeHex(this.toColor, DEFAULT_TO),
      };
    }

    setRemoveVisible(visible) {
      this.canRemove = !!visible;
    }

    _syncInputs() {
      if (!this.element) return;
      const from = this.element.querySelector('input[data-role="from"]');
      const to = this.element.querySelector('input[data-role="to"]');
      const fromHex = this.element.querySelector('input[data-role="from-hex"]');
      const toHex = this.element.querySelector('input[data-role="to-hex"]');
      if (from) from.value = this.fromColor;
      if (to) to.value = this.toColor;
      if (fromHex) fromHex.value = this.fromColor;
      if (toHex) toHex.value = this.toColor;
    }

    _readInputs() {
      if (!this.element) return;
      const fromHex = this.element.querySelector('input[data-role="from-hex"]');
      const toHex = this.element.querySelector('input[data-role="to-hex"]');
      const from = this.element.querySelector('input[data-role="from"]');
      const to = this.element.querySelector('input[data-role="to"]');
      if (fromHex && fromHex.value) this.fromColor = fromHex.value;
      else if (from) this.fromColor = from.value;
      if (toHex && toHex.value) this.toColor = toHex.value;
      else if (to) this.toColor = to.value;
    }
  }

  class ScrollHighlight extends Plugin {
    static get pluginName() {
      return "ScrollHighlight";
    }

    init() {
      this._defineSchema();
      this._defineConverters();
      this.editor.commands.add(
        "scrollHighlight",
        new ScrollHighlightCommand(this.editor)
      );
      this._createUi();
      this._hookRemoveFormat();
    }

    _defineSchema() {
      const schema = this.editor.model.schema;

      schema.extend("$text", { allowAttributes: INLINE_ATTR });
      schema.setAttributeProperties(INLINE_ATTR, {
        isFormatting: true,
        copyOnEnter: false,
      });

      if (!schema.isRegistered(BLOCK_NAME)) {
        // Legacy block wrappers from older content — keep for upcast/remove.
        if (schema.isRegistered("$container")) {
          schema.register(BLOCK_NAME, {
            inheritAllFrom: "$container",
            allowAttributes: ["scrollHighlightFrom", "scrollHighlightTo"],
          });
        } else {
          schema.register(BLOCK_NAME, {
            allowWhere: "$block",
            allowContentOf: "$root",
            allowAttributes: ["scrollHighlightFrom", "scrollHighlightTo"],
          });
        }
      }
    }

    _defineConverters() {
      const conversion = this.editor.conversion;

      function inlineViewFactory(value, writer, forEditing) {
        if (!value) return;
        const colors = decodeColors(value);
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

      conversion.for("editingDowncast").attributeToElement({
        model: INLINE_ATTR,
        view: (value, { writer }) => inlineViewFactory(value, writer, true),
      });

      conversion.for("dataDowncast").attributeToElement({
        model: INLINE_ATTR,
        view: (value, { writer }) => inlineViewFactory(value, writer, false),
      });

      conversion.for("upcast").elementToAttribute({
        view: {
          name: "span",
          classes: CLASS_NAME,
        },
        model: {
          key: INLINE_ATTR,
          value: (viewElement) => {
            if (viewElement.hasClass("nova-sh-char")) return null;
            const from =
              viewElement.getAttribute(DATA_FROM) ||
              viewElement.getStyle("color") ||
              DEFAULT_FROM;
            const to = viewElement.getAttribute(DATA_TO) || DEFAULT_TO;
            return encodeColors(from, to);
          },
        },
        converterPriority: "high",
      });

      conversion.for("editingDowncast").elementToElement({
        model: BLOCK_NAME,
        view: (modelElement, { writer }) => {
          const el = writer.createContainerElement(
            "div",
            blockViewAttributes(modelElement, true)
          );
          writer.setCustomProperty("scrollHighlight", true, el);
          return el;
        },
      });

      conversion.for("dataDowncast").elementToElement({
        model: BLOCK_NAME,
        view: (modelElement, { writer }) =>
          writer.createContainerElement(
            "div",
            blockViewAttributes(modelElement, false)
          ),
      });

      conversion.for("upcast").elementToElement({
        view: {
          name: "div",
          classes: CLASS_NAME,
        },
        model: (viewElement, { writer }) =>
          writer.createElement(BLOCK_NAME, {
            scrollHighlightFrom: normalizeHex(
              viewElement.getAttribute(DATA_FROM) ||
                viewElement.getStyle("color"),
              DEFAULT_FROM
            ),
            scrollHighlightTo: normalizeHex(
              viewElement.getAttribute(DATA_TO),
              DEFAULT_TO
            ),
          }),
        converterPriority: "high",
      });
    }

    _hookRemoveFormat() {
      const editor = this.editor;
      const removeFormat = editor.commands.get("removeFormat");
      if (!removeFormat) return;

      // After removeFormat runs, also clear our formatting attribute.
      this.listenTo(removeFormat, "execute", () => {
        const model = editor.model;
        const selection = model.document.selection;
        model.change((writer) => {
          if (!selection.isCollapsed) {
            removeInlineFromSelection(writer, model, selection);
          } else {
            const active = getActiveState(editor);
            if (active && active.mode === "inline") {
              const range = getInlineAttributeRange(
                editor,
                selection.getFirstPosition()
              );
              if (range) writer.removeAttribute(INLINE_ATTR, range);
            }
          }
        });
      });
    }

    _createUi() {
      const editor = this.editor;
      const command = editor.commands.get("scrollHighlight");

      editor.ui.componentFactory.add(COMPONENT, (locale) => {
        const dropdown = createDropdown(locale);
        const form = new ScrollHighlightFormView(locale);
        let forceKeepOpen = false;

        dropdown.buttonView.set({
          label: "هایلایت اسکرولی متن",
          icon: TOOLBAR_ICON,
          tooltip: true,
        });

        dropdown.bind("isEnabled").to(command, "isEnabled");
        dropdown.panelView.children.add(form);

        dropdown.panelView.once("render", () => {
          const panelEl = dropdown.panelView.element;
          if (!panelEl) return;
          panelEl.addEventListener(
            "mousedown",
            (event) => {
              event.stopPropagation();
            },
            true
          );
          panelEl.addEventListener(
            "pointerdown",
            (event) => {
              event.stopPropagation();
            },
            true
          );
        });

        dropdown.on("change:isOpen", (_evt, _name, isOpen) => {
          if (!isOpen) {
            const activeEl = document.activeElement;
            const stillInForm =
              forceKeepOpen ||
              (activeEl &&
                form.element &&
                (form.element.contains(activeEl) ||
                  activeEl.getAttribute("type") === "color"));
            if (stillInForm) {
              dropdown.isOpen = true;
              return;
            }
            forceKeepOpen = false;
            return;
          }

          forceKeepOpen = false;
          const active = getActiveState(editor);
          if (active) {
            form.setColors(active.from, active.to);
            form.setRemoveVisible(true);
          } else {
            form.setColors(DEFAULT_FROM, DEFAULT_TO);
            form.setRemoveVisible(false);
          }

          // Defer sync so DOM inputs exist; wire color-picker keep-open hooks once.
          setTimeout(function () {
            form._syncInputs();
            if (form.element && !form.element.getAttribute("data-sh-hooks")) {
              form.element.setAttribute("data-sh-hooks", "1");
              form.element
                .querySelectorAll('input[type="color"]')
                .forEach(function (input) {
                  input.addEventListener("focus", function () {
                    forceKeepOpen = true;
                  });
                  input.addEventListener("blur", function () {
                    window.setTimeout(function () {
                      forceKeepOpen = false;
                    }, 250);
                  });
                });
            }
          }, 0);
        });

        form.applyButton.on("execute", () => {
          const colors = form.getColors();
          forceKeepOpen = false;
          editor.execute("scrollHighlight", {
            action: "apply",
            from: colors.from,
            to: colors.to,
          });
          dropdown.isOpen = false;
          editor.editing.view.focus();
        });

        form.removeButton.on("execute", () => {
          forceKeepOpen = false;
          editor.execute("scrollHighlight", { action: "remove" });
          dropdown.isOpen = false;
          editor.editing.view.focus();
        });

        form.cancelButton.on("execute", () => {
          forceKeepOpen = false;
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
    defaults: { from: DEFAULT_FROM, to: DEFAULT_TO },
    className: CLASS_NAME,
  };
})(typeof window !== "undefined" ? window : globalThis);
