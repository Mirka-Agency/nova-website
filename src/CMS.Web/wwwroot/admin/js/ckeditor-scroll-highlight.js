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

    const attr = selection.getAttribute(INLINE_ATTR);
    if (attr) {
      const colors = decodeColors(attr);
      return { mode: "inline", element: null, from: colors.from, to: colors.to };
    }

    return null;
  }

  function getInlineAttributeRange(editor, position) {
    if (!position || !position.textNode) {
      // Walk to nearest text with attribute.
      const selection = editor.model.document.selection;
      if (!selection.hasAttribute(INLINE_ATTR)) return null;
    }

    try {
      const range = editor.model.document.selection.getFirstRange();
      if (range && !range.isCollapsed && selectionHasAttrInRange(editor, range)) {
        return range;
      }
    } catch (_) {
      /* ignore */
    }

    const model = editor.model;
    const root = position.root;
    const attr = INLINE_ATTR;

    // Expand from caret across contiguous attributed text.
    let start = position;
    let end = position;

    const walkerBack = model.createRange(model.createPositionAt(root, 0), position).getWalker({
      direction: "backward",
      ignoreElementEnd: true,
    });
    for (const value of walkerBack) {
      if (value.type !== "text" || !value.item.hasAttribute(attr)) break;
      start = model.createPositionAt(value.item.parent, value.item.startOffset);
    }

    const walkerFwd = model.createRange(position, model.createPositionAt(root, "end")).getWalker({
      ignoreElementEnd: true,
    });
    for (const value of walkerFwd) {
      if (value.type !== "text" || !value.item.hasAttribute(attr)) break;
      end = model.createPositionAt(
        value.item.parent,
        value.item.startOffset + value.item.data.length
      );
    }

    if (start.isEqual(end)) return null;
    return model.createRange(start, end);
  }

  function selectionHasAttrInRange(editor, range) {
    for (const item of range.getItems()) {
      if (item.is?.("$textProxy") && item.hasAttribute(INLINE_ATTR)) return true;
      if (item.is?.("$text") && item.hasAttribute(INLINE_ATTR)) return true;
    }
    return false;
  }

  function blockViewAttributes(modelElement) {
    return {
      class: CLASS_NAME,
      [DATA_MARKER]: "1",
      [DATA_FROM]: normalizeHex(
        modelElement.getAttribute("scrollHighlightFrom"),
        DEFAULT_FROM
      ),
      [DATA_TO]: normalizeHex(
        modelElement.getAttribute("scrollHighlightTo"),
        DEFAULT_TO
      ),
    };
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
          selected.is("element", "rawHtml") ||
          selected.is("element", "media"));

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
        if (active && active.mode === "block" && active.element) {
          writer.setAttribute("scrollHighlightFrom", from, active.element);
          writer.setAttribute("scrollHighlightTo", to, active.element);
          return;
        }

        if (selection.isCollapsed) {
          if (active && active.mode === "inline") {
            const range = getInlineAttributeRange(editor, selection.getFirstPosition());
            if (range) writer.setAttribute(INLINE_ATTR, encoded, range);
          }
          return;
        }

        const blocks = Array.from(selection.getSelectedBlocks());
        if (blocks.length > 1) {
          for (const range of model.schema.getValidRanges(selection.getRanges(), INLINE_ATTR)) {
            writer.removeAttribute(INLINE_ATTR, range);
          }

          const content = model.getSelectedContent(selection);
          if (!content.childCount) return;

          const wrapper = writer.createElement(BLOCK_NAME, {
            scrollHighlightFrom: from,
            scrollHighlightTo: to,
          });
          writer.append(content, wrapper);
          model.insertContent(wrapper);
          return;
        }

        for (const range of model.schema.getValidRanges(selection.getRanges(), INLINE_ATTR)) {
          writer.setAttribute(INLINE_ATTR, encoded, range);
        }
      });
    }

    _remove() {
      const editor = this.editor;
      const model = editor.model;
      const selection = model.document.selection;
      const active = getActiveState(editor);

      model.change((writer) => {
        if (active && active.mode === "block" && active.element) {
          const parent = active.element.parent;
          const index = parent.getChildIndex(active.element);
          const children = Array.from(active.element.getChildren());
          writer.remove(active.element);
          let at = index;
          for (const child of children) {
            writer.insert(child, parent, at);
            at += 1;
          }
          return;
        }

        if (!selection.isCollapsed) {
          for (const range of model.schema.getValidRanges(selection.getRanges(), INLINE_ATTR)) {
            writer.removeAttribute(INLINE_ATTR, range);
          }
          return;
        }

        if (active && active.mode === "inline") {
          const range = getInlineAttributeRange(editor, selection.getFirstPosition());
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

      const bind = this.bindTemplate;

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
            children: ["رنگ اولیه و نهایی هایلایت اسکرولی را انتخاب کنید."],
          },
          {
            tag: "label",
            attributes: { class: ["ck-scroll-highlight-form__label"] },
            children: [
              { text: "رنگ اولیه متن" },
              {
                tag: "input",
                attributes: {
                  type: "color",
                  class: ["ck-scroll-highlight-form__color"],
                  value: bind.to("fromColor"),
                  "data-role": "from",
                },
              },
            ],
          },
          {
            tag: "label",
            attributes: { class: ["ck-scroll-highlight-form__label"] },
            children: [
              { text: "رنگ نهایی هایلایت" },
              {
                tag: "input",
                attributes: {
                  type: "color",
                  class: ["ck-scroll-highlight-form__color"],
                  value: bind.to("toColor"),
                  "data-role": "to",
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

      this.element.addEventListener("input", (event) => {
        const target = event.target;
        if (!target || target.getAttribute("type") !== "color") return;
        const role = target.getAttribute("data-role");
        if (role === "from") this.fromColor = target.value;
        if (role === "to") this.toColor = target.value;
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
      if (from) from.value = this.fromColor;
      if (to) to.value = this.toColor;
    }

    _readInputs() {
      if (!this.element) return;
      const from = this.element.querySelector('input[data-role="from"]');
      const to = this.element.querySelector('input[data-role="to"]');
      if (from) this.fromColor = from.value;
      if (to) this.toColor = to.value;
    }
  }

  class ScrollHighlight extends Plugin {
    static get pluginName() {
      return "ScrollHighlight";
    }

    init() {
      this._defineSchema();
      this._defineConverters();
      this.editor.commands.add("scrollHighlight", new ScrollHighlightCommand(this.editor));
      this._createUi();
    }

    _defineSchema() {
      const schema = this.editor.model.schema;

      schema.extend("$text", { allowAttributes: INLINE_ATTR });
      schema.setAttributeProperties(INLINE_ATTR, {
        isFormatting: true,
        copyOnEnter: false,
      });

      schema.register(BLOCK_NAME, {
        inheritAllFrom: "$container",
        allowAttributes: ["scrollHighlightFrom", "scrollHighlightTo"],
      });
    }

    _defineConverters() {
      const conversion = this.editor.conversion;

      conversion.for("downcast").attributeToElement({
        model: INLINE_ATTR,
        view: (value, { writer }) => {
          if (!value) return;
          const colors = decodeColors(value);
          return writer.createAttributeElement(
            "span",
            {
              class: CLASS_NAME,
              [DATA_MARKER]: "1",
              [DATA_FROM]: colors.from,
              [DATA_TO]: colors.to,
            },
            { priority: 5 }
          );
        },
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
            if (!viewElement.getAttribute(DATA_FROM) && !viewElement.getAttribute(DATA_TO)) {
              return null;
            }
            return encodeColors(
              viewElement.getAttribute(DATA_FROM),
              viewElement.getAttribute(DATA_TO)
            );
          },
        },
      });

      conversion.for("editingDowncast").elementToElement({
        model: BLOCK_NAME,
        view: (modelElement, { writer }) => {
          const el = writer.createContainerElement("div", blockViewAttributes(modelElement));
          writer.setCustomProperty("scrollHighlight", true, el);
          return el;
        },
      });

      conversion.for("dataDowncast").elementToElement({
        model: BLOCK_NAME,
        view: (modelElement, { writer }) =>
          writer.createContainerElement("div", blockViewAttributes(modelElement)),
      });

      conversion.for("upcast").elementToElement({
        view: {
          name: "div",
          classes: CLASS_NAME,
        },
        model: (viewElement, { writer }) =>
          writer.createElement(BLOCK_NAME, {
            scrollHighlightFrom: normalizeHex(
              viewElement.getAttribute(DATA_FROM),
              DEFAULT_FROM
            ),
            scrollHighlightTo: normalizeHex(
              viewElement.getAttribute(DATA_TO),
              DEFAULT_TO
            ),
          }),
      });
    }

    _createUi() {
      const editor = this.editor;
      const command = editor.commands.get("scrollHighlight");

      editor.ui.componentFactory.add(COMPONENT, (locale) => {
        const dropdown = createDropdown(locale);
        const form = new ScrollHighlightFormView(locale);

        dropdown.buttonView.set({
          label: "هایلایت اسکرولی متن",
          icon: TOOLBAR_ICON,
          tooltip: true,
        });

        dropdown.bind("isEnabled").to(command, "isEnabled");
        dropdown.panelView.children.add(form);

        dropdown.on("change:isOpen", () => {
          if (!dropdown.isOpen) return;
          const active = getActiveState(editor);
          if (active) {
            form.setColors(active.from, active.to);
            form.setRemoveVisible(true);
          } else {
            form.setColors(DEFAULT_FROM, DEFAULT_TO);
            form.setRemoveVisible(false);
          }
        });

        form.applyButton.on("execute", () => {
          const colors = form.getColors();
          editor.execute("scrollHighlight", {
            action: "apply",
            from: colors.from,
            to: colors.to,
          });
          dropdown.isOpen = false;
          editor.editing.view.focus();
        });

        form.removeButton.on("execute", () => {
          editor.execute("scrollHighlight", { action: "remove" });
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
    defaults: { from: DEFAULT_FROM, to: DEFAULT_TO },
    className: CLASS_NAME,
  };
})(typeof window !== "undefined" ? window : globalThis);
