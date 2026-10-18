/**
 * ScrollScrubHighlight — CKEditor 5 plugin (Classic).
 *
 * Model: text attribute `scrollScrubHighlight` = "#RRGGBB"
 * Data:  <span class="scroll-scrub-highlight" data-scroll-scrub-color="#e7f2b0">…</span>
 * Editing view also sets inline background-color for visibility (not persisted).
 *
 * Legacy upcast: .nova-scroll-highlight → same attribute (default color).
 * Depends on global CKEDITOR (UMD). Exposes window.ScrollScrubHighlight (+ NovaScrollHighlight alias).
 */
(function (global) {
  "use strict";

  if (typeof CKEDITOR === "undefined") {
    console.error("[ScrollScrubHighlight] CKEDITOR global is missing.");
    return;
  }

  const {
    Plugin,
    Command,
    ButtonView,
    createDropdown,
    findAttributeRange,
  } = CKEDITOR;

  if (!Plugin || !Command) {
    console.error("[ScrollScrubHighlight] Required CKEDITOR exports are missing.");
    return;
  }

  const ATTR = "scrollScrubHighlight";
  const CLASS_NAME = "scroll-scrub-highlight";
  const DATA_COLOR = "data-scroll-scrub-color";
  const DEFAULT_COLOR = "#e7f2b0";
  const COMPONENT = "scrollScrubHighlight";
  const COMMAND = "scrollScrubHighlight";

  const LEGACY_CLASS = "nova-scroll-highlight";
  const LEGACY_CHAR = "nova-sh-char";

  const PRESETS = [
    { color: "#e7f2b0", label: "سبز روشن" },
    { color: "#fde68a", label: "زرد" },
    { color: "#fbcfe8", label: "صورتی" },
    { color: "#bae6fd", label: "آبی" },
    { color: "#ddd6fe", label: "بنفش روشن" },
  ];

  const TOOLBAR_ICON =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">' +
    '<path d="M3.2 14.8c2.4-1.2 4.4-3.8 5.6-6.6.4-.9 1.2-2.4 2.2-2.4.7 0 1.1.6 1.4 1.3.6 1.5 1.3 3 2.4 4.2.4.4 1 .6 1.6.6v1.5c-1.1 0-2.1-.4-2.9-1.2-.8-.9-1.4-2-1.9-3.2-.2.5-.5 1.1-.8 1.6C9.4 12.8 7 15.6 4.2 16.8l-1-2z"/>' +
    '<rect x="2.5" y="16.2" width="15" height="1.6" rx=".4" opacity=".45"/>' +
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
    if (/^[0-9a-f]{3}$/.test(raw)) {
      return "#" + raw[0] + raw[0] + raw[1] + raw[1] + raw[2] + raw[2];
    }
    return fallback || DEFAULT_COLOR;
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

  function applyAttributeToRanges(writer, model, ranges, color) {
    for (const range of model.schema.getValidRanges(ranges, ATTR)) {
      writer.setAttribute(ATTR, color, range);
    }
  }

  function removeAttributeFromRanges(writer, model, ranges) {
    for (const range of model.schema.getValidRanges(ranges, ATTR)) {
      writer.removeAttribute(ATTR, range);
    }
  }

  function createHighlightSpan(writer, color, forEditing) {
    const hex = normalizeHex(color, DEFAULT_COLOR);
    const attrs = {
      class: CLASS_NAME,
      [DATA_COLOR]: hex,
    };
    if (forEditing) {
      attrs.style = "background-color:" + hex;
    }
    return writer.createAttributeElement("span", attrs, { priority: 5 });
  }

  function applyHighlightToRange(writer, editor, modelRange, color) {
    for (const item of modelRange.getItems()) {
      if (
        !(item.is("$text") || item.is("$textProxy")) ||
        !editor.model.schema.checkAttribute(item, ATTR)
      ) {
        continue;
      }
      writer.setAttribute(ATTR, color, item);
    }
  }

  function readColorFromViewElement(viewElement) {
    const fromData = viewElement.getAttribute(DATA_COLOR);
    if (fromData) return normalizeHex(fromData, DEFAULT_COLOR);

    const bg =
      viewElement.getStyle("background-color") ||
      viewElement.getStyle("background");
    if (bg && /^#/.test(bg.trim())) {
      return normalizeHex(bg.trim(), DEFAULT_COLOR);
    }

    // Legacy nova-scroll-highlight: use default marker color (not text-from/to).
    return DEFAULT_COLOR;
  }

  function isScrubView(viewElement) {
    return (
      viewElement.is("element") &&
      (viewElement.hasClass(CLASS_NAME) || viewElement.hasClass(LEGACY_CLASS)) &&
      !viewElement.hasClass(LEGACY_CHAR) &&
      !viewElement.hasClass("scroll-scrub-word") &&
      !viewElement.hasClass("scroll-scrub-line")
    );
  }

  class ScrollScrubHighlightCommand extends Command {
    refresh() {
      const model = this.editor.model;
      const selection = model.document.selection;
      const value = getAttributeAtSelection(selection);

      this.value = value ? normalizeHex(value, DEFAULT_COLOR) : null;
      this.isEnabled = model.schema.checkAttributeInSelection(selection, ATTR);
    }

    execute(options) {
      const opts = options || {};
      if (opts.action === "remove" || opts.forceValue === false) {
        this._remove();
        return;
      }
      if (opts.action === "toggle") {
        if (this.value) {
          this._remove();
        } else {
          this._apply(normalizeHex(opts.color, DEFAULT_COLOR));
        }
        return;
      }
      this._apply(normalizeHex(opts.color != null ? opts.color : opts.value, DEFAULT_COLOR));
    }

    _apply(color) {
      const editor = this.editor;
      const model = editor.model;
      const selection = model.document.selection;
      const hex = normalizeHex(color, DEFAULT_COLOR);
      const current = getAttributeAtSelection(selection);

      model.change((writer) => {
        if (selection.isCollapsed) {
          if (!current) return;
          const range = findFullAttributeRange(
            editor,
            selection.getFirstPosition(),
            current
          );
          if (range) writer.setAttribute(ATTR, hex, range);
          return;
        }

        applyAttributeToRanges(writer, model, selection.getRanges(), hex);
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

  class ScrollScrubHighlight extends Plugin {
    static get pluginName() {
      return "ScrollScrubHighlight";
    }

    init() {
      this._lastColor = DEFAULT_COLOR;
      this._defineSchema();
      this._defineConverters();
      this._registerCaretBehavior();
      this.editor.commands.add(COMMAND, new ScrollScrubHighlightCommand(this.editor));
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
      const plugins = this.editor.plugins;
      if (!plugins || typeof plugins.has !== "function" || !plugins.has("TwoStepCaretMovement")) {
        return;
      }
      const twoStep = plugins.get("TwoStepCaretMovement");
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

            // Unwrap runtime-only wrappers if pasted back into the editor.
            if (
              viewItem.hasClass("scroll-scrub-word") ||
              viewItem.hasClass("scroll-scrub-line") ||
              viewItem.hasClass(LEGACY_CHAR)
            ) {
              if (
                !conversionApi.consumable.consume(viewItem, {
                  name: true,
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

            if (!isScrubView(viewItem)) return;
            if (!conversionApi.consumable.test(viewItem, { name: true })) return;

            conversionApi.consumable.consume(viewItem, { name: true });
            if (viewItem.hasClass(CLASS_NAME)) {
              conversionApi.consumable.consume(viewItem, {
                classes: CLASS_NAME,
              });
            }
            if (viewItem.hasClass(LEGACY_CLASS)) {
              conversionApi.consumable.consume(viewItem, {
                classes: LEGACY_CLASS,
              });
            }
            conversionApi.consumable.consume(viewItem, {
              attributes: [
                DATA_COLOR,
                "data-nova-scroll-highlight",
                "data-nova-sh-from",
                "data-nova-sh-to",
              ],
            });
            conversionApi.consumable.consume(viewItem, {
              styles: "background-color",
            });
            conversionApi.consumable.consume(viewItem, { styles: "color" });

            const color = readColorFromViewElement(viewItem);
            const { modelRange } = conversionApi.convertChildren(
              viewItem,
              data.modelCursor
            );

            if (modelRange) {
              applyHighlightToRange(
                conversionApi.writer,
                editor,
                modelRange,
                color
              );
            }

            data.modelRange = modelRange;
            data.modelCursor = modelRange ? modelRange.end : data.modelCursor;
            evt.stop();
          },
          { priority: "high" }
        );
      });

      // Legacy block wrapper → unwrap children with attribute.
      conversion.for("upcast").add((dispatcher) => {
        dispatcher.on(
          "element:div",
          (evt, data, conversionApi) => {
            const viewItem = data.viewItem;
            if (!viewItem.hasClass(LEGACY_CLASS)) return;

            if (
              !conversionApi.consumable.consume(viewItem, {
                name: true,
                classes: LEGACY_CLASS,
              })
            ) {
              return;
            }

            const color = readColorFromViewElement(viewItem);
            const { modelRange } = conversionApi.convertChildren(
              viewItem,
              data.modelCursor
            );

            if (modelRange) {
              applyHighlightToRange(
                conversionApi.writer,
                editor,
                modelRange,
                color
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
        if (typeof createDropdown === "function") {
          return plugin._createDropdownUi(locale, command);
        }
        return plugin._createToggleButton(locale, command);
      });
    }

    _createToggleButton(locale, command) {
      const plugin = this;
      const button = new ButtonView(locale);
      button.set({
        label: "هایلایت اسکرولی",
        icon: TOOLBAR_ICON,
        tooltip: true,
      });
      button.bind("isEnabled").to(command, "isEnabled");
      button.bind("isOn").to(command, "value", (value) => !!value);
      button.on("execute", () => {
        const selection = plugin.editor.model.document.selection;
        if (selection.isCollapsed && !command.value) return;
        plugin.editor.execute(COMMAND, {
          action: "toggle",
          color: plugin._lastColor,
        });
        plugin.editor.editing.view.focus();
      });
      return button;
    }

    _createDropdownUi(locale, command) {
      const editor = this.editor;
      const plugin = this;
      const dropdown = createDropdown(locale);

      dropdown.buttonView.set({
        label: "هایلایت اسکرولی",
        icon: TOOLBAR_ICON,
        tooltip: true,
        isOn: !!command.value,
      });

      // Avoid observable-bind-rebind: sync pressed state manually.
      command.on("change:value", (_evt, _name, value) => {
        dropdown.buttonView.isOn = !!value;
      });
      command.on("change:isEnabled", (_evt, _name, value) => {
        dropdown.isEnabled = !!value;
        dropdown.buttonView.isEnabled = !!value;
      });
      dropdown.isEnabled = command.isEnabled;
      dropdown.buttonView.isEnabled = command.isEnabled;

      const panel = dropdown.panelView;
      const wrap = document.createElement("div");
      wrap.className = "ck ck-scroll-scrub-panel";
      wrap.setAttribute("dir", "rtl");
      wrap.setAttribute("lang", "fa");

      PRESETS.forEach((preset) => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "ck-scroll-scrub-swatch-btn";
        btn.title = preset.label;
        btn.setAttribute("aria-label", preset.label);
        btn.style.background = preset.color;
        btn.addEventListener("mousedown", (event) => {
          // Keep focus inside dropdown panel until execute finishes.
          event.preventDefault();
        });
        btn.addEventListener("click", () => {
          const selection = editor.model.document.selection;
          plugin._lastColor = preset.color;
          if (selection.isCollapsed && !command.value) {
            dropdown.isOpen = false;
            editor.editing.view.focus();
            return;
          }
          editor.execute(COMMAND, { color: preset.color });
          dropdown.isOpen = false;
          editor.editing.view.focus();
        });
        wrap.appendChild(btn);
      });

      const removeBtn = document.createElement("button");
      removeBtn.type = "button";
      removeBtn.className = "ck-scroll-scrub-remove-btn";
      removeBtn.textContent = "حذف هایلایت اسکرولی";
      removeBtn.addEventListener("mousedown", (event) => {
        event.preventDefault();
      });
      removeBtn.addEventListener("click", () => {
        editor.execute(COMMAND, { action: "remove" });
        dropdown.isOpen = false;
        editor.editing.view.focus();
      });
      wrap.appendChild(removeBtn);

      dropdown.on("change:isOpen", (_evt, _name, isOpen) => {
        if (!isOpen || !panel.element) return;
        if (!panel.element.contains(wrap)) {
          panel.element.appendChild(wrap);
        }
      });

      return dropdown;
    }
  }

  const api = {
    plugin: ScrollScrubHighlight,
    pluginName: "ScrollScrubHighlight",
    componentName: COMPONENT,
    commandName: COMMAND,
    defaults: { color: DEFAULT_COLOR },
    className: CLASS_NAME,
  };

  global.ScrollScrubHighlight = api;
  // Backward-compatible alias for older script wiring.
  global.NovaScrollHighlight = api;
})(typeof window !== "undefined" ? window : globalThis);
