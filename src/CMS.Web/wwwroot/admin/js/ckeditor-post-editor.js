(function () {
  "use strict";

  if (typeof CKEDITOR === "undefined") {
    return;
  }

  const textareas = document.querySelectorAll("textarea.admin-ckeditor-source");
  if (!textareas.length) {
    return;
  }

  const {
    ClassicEditor,
    Essentials,
    Paragraph,
    Heading,
    Bold,
    Italic,
    Underline,
    Strikethrough,
    Subscript,
    Superscript,
    Link,
    List,
    ListProperties,
    TodoList,
    TextPartLanguage,
    BlockQuote,
    Image,
    ImageCaption,
    ImageStyle,
    ImageToolbar,
    ImageUtils,
    ImageResize,
    ImageCustomResizeUI,
    Plugin,
    ButtonView,
    Highlight,
    Font,
    Alignment,
    Undo,
    Table,
    TableToolbar,
    TableCaption,
    TableProperties,
    TableCellProperties,
    MediaEmbed,
    MediaEmbedToolbar,
    HtmlEmbed,
    SourceEditing,
    GeneralHtmlSupport,
    HorizontalLine,
    CodeBlock,
    RemoveFormat,
    Indent,
    IndentBlock,
    FindAndReplace,
    PageBreak,
    PasteFromOffice,
    UpcastWriter,
    ClickObserver,
    WidgetToolbarRepository,
    Widget,
    toWidget,
    toWidgetEditable,
  } = CKEDITOR;

  const ScrollHighlightPlugin =
    window.NovaScrollHighlight && window.NovaScrollHighlight.plugin
      ? window.NovaScrollHighlight.plugin
      : null;

  const mediaLibraryImageIcon =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">' +
    '<path d="M6.91 10.54c.26-.23.64-.21.88.03l3.36 3.14 2.23-2.06a.64.64 0 0 1 .87 0l2.52 2.97V4.5H3.2v10.12l3.71-4.08zm10.27-7.51c.6 0 1.09.47 1.09 1.05v11.84c0 .59-.49 1.06-1.09 1.06H2.83c-.6 0-1.09-.47-1.09-1.06V4.08c0-.58.49-1.05 1.1-1.05h14.34zm-5.22 5.56a1.96 1.96 0 1 1 3.4-1.96 1.96 1.96 0 0 1-3.4 1.96z"/>' +
    "</svg>";

  const replaceImageIcon =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">' +
    '<path d="M4.2 3.5h7.1c.6 0 1.1.5 1.1 1.1v2.2h-1.5V5H4.7v7.4H7v1.5H4.2c-.6 0-1.1-.5-1.1-1.1V4.6c0-.6.5-1.1 1.1-1.1zm5.8 5.2h6.8c.6 0 1.1.5 1.1 1.1v5.6c0 .6-.5 1.1-1.1 1.1H10c-.6 0-1.1-.5-1.1-1.1v-5.6c0-.6.5-1.1 1.1-1.1zm1.3 2.1v3.4h4.2v-3.4h-4.2zM8.1 2.2l2.1 2.1-2.1 2.1V4.9H3.8V3.7h4.3V2.2z"/>' +
    "</svg>";

  const editImageIcon =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">' +
    '<path d="M3.5 13.9V16.5h2.6l7.7-7.7-2.6-2.6L3.5 13.9zm12.6-7.4c.3-.3.3-.7 0-1l-1.6-1.6a.7.7 0 0 0-1 0l-1.2 1.2 2.6 2.6 1.2-1.2z"/>' +
    "</svg>";

  const deleteImageIcon =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">' +
    '<path d="M6.2 3.8h7.6v1.4H6.2V3.8zm1.2 3h1.4v7.2H7.4V6.8zm3.3 0h1.4v7.2h-1.4V6.8zm-5.1-4.4h7.2l.7 1.2H4.8l.7-1.2zM5.4 17.2c0 .6.5 1.1 1.1 1.1h7c.6 0 1.1-.5 1.1-1.1V6.2H5.4v11z"/>' +
    "</svg>";

  const ctaHelperIcon =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">' +
    '<path d="M3.2 4.2h13.6c.66 0 1.2.54 1.2 1.2v4.2c0 .66-.54 1.2-1.2 1.2H11l-2.2 2.6c-.3.36-.9.14-.9-.3V10.8H3.2c-.66 0-1.2-.54-1.2-1.2V5.4c0-.66.54-1.2 1.2-1.2zm1.1 2.1v1.8h5.4V6.3H4.3zm7.2 0v1.8h4.1V6.3h-4.1zM3.2 12.6h7.4c.66 0 1.2.54 1.2 1.2v2c0 .66-.54 1.2-1.2 1.2H3.2c-.66 0-1.2-.54-1.2-1.2v-2c0-.66.54-1.2 1.2-1.2z"/>' +
    "</svg>";

  const videoPosterIcon =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">' +
    '<path d="M3.2 4.5h8.4c.55 0 1 .45 1 1v2.2l2.7-1.7c.45-.28 1.05.05 1.05.58v7.84c0 .53-.6.86-1.05.58l-2.7-1.7v2.2c0 .55-.45 1-1 1H3.2c-.55 0-1-.45-1-1V5.5c0-.55.45-1 1-1zm1.3 2.2v6.6h5.8V6.7H4.5zm7.4 1.55v3.5l1.9 1.2V7.05l-1.9 1.2z"/>' +
    '<path d="M5.2 8.1h2.4v1.3H5.2V8.1zm0 2.2h3.6v1.3H5.2v-1.3z"/>' +
    "</svg>";

  const videoWarningIcon =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">' +
    '<path d="M10 2.2c.4 0 .75.22.94.57l7.2 13.2c.38.7-.12 1.53-.94 1.53H2.8c-.82 0-1.32-.83-.94-1.53l7.2-13.2c.19-.35.54-.57.94-.57zm0 4.1c-.5 0-.9.4-.9.9v4.2c0 .5.4.9.9.9s.9-.4.9-.9V7.2c0-.5-.4-.9-.9-.9zm0 7.4a1.05 1.05 0 1 0 0 2.1 1.05 1.05 0 0 0 0-2.1z"/>' +
    "</svg>";

  const VIDEO_WARNING_DEFAULT_TEXT =
    "⚠️ توجه: ویدئو شامل تصاویر واقعی جراحی است.";

  const CTA_HELPER_MESSAGE = "nova-cta-helper-insert";
  const CTA_HELPER_READY = "nova-cta-helper-ready";
  const CTA_HELPER_EDIT_CONTEXT = "nova-cta-helper-edit-context";

  function isNovaCtaHtml(html) {
    return typeof html === "string" && html.indexOf("data-nova-cta=") !== -1;
  }

  function findSelectedCtaEmbed(editor) {
    const selected = editor.model.document.selection.getSelectedElement();
    if (selected && selected.is("element", "rawHtml")) {
      const value = selected.getAttribute("value") || "";
      if (isNovaCtaHtml(value)) return selected;
    }
    return null;
  }

  function elementStillInDocument(element) {
    return !!(element && element.root && element.root.document);
  }

  function insertCtaHtml(editor, html, options) {
    if (!html) return;
    const opts = options || {};
    const replaceTarget =
      opts.replace && opts.target && elementStillInDocument(opts.target)
        ? opts.target
        : null;

    try {
      editor.model.change(function (writer) {
        if (replaceTarget) {
          writer.setAttribute("value", html, replaceTarget);
          writer.setSelection(replaceTarget, "on");
          return;
        }

        const embed = writer.createElement("rawHtml", { value: html });
        editor.model.insertContent(embed);
      });
      editor.editing.view.focus();
      return;
    } catch (err) {
      console.warn("rawHtml insert failed, falling back to htmlEmbed command", err);
    }

    try {
      if (replaceTarget) {
        editor.model.change(function (writer) {
          writer.setAttribute("value", html, replaceTarget);
        });
        editor.editing.view.focus();
        return;
      }

      editor.execute("htmlEmbed");
      const selected = editor.model.document.selection.getSelectedElement();
      if (selected && selected.is("element", "rawHtml")) {
        editor.model.change(function (writer) {
          writer.setAttribute("value", html, selected);
        });
        editor.editing.view.focus();
      }
    } catch (err2) {
      console.error("Failed to insert CTA HTML", err2);
    }
  }

  class CtaHelperInsert extends Plugin {
    static get pluginName() {
      return "CtaHelperInsert";
    }

    init() {
      const editor = this.editor;
      let helperWindow = null;
      let editingTarget = null;
      let toolbarButtonView = null;

      const openHelper = function () {
        const selected = findSelectedCtaEmbed(editor);
        editingTarget = selected;

        const url = "/admin/ctahelper?embed=1";
        if (helperWindow && !helperWindow.closed) {
          helperWindow.focus();
          helperWindow.postMessage(
            {
              type: CTA_HELPER_EDIT_CONTEXT,
              html: selected ? selected.getAttribute("value") : null,
              editing: !!selected,
            },
            window.location.origin
          );
          return;
        }

        helperWindow = window.open(
          url,
          "novaCtaHelper",
          "popup=yes,width=1100,height=860,scrollbars=yes,resizable=yes"
        );
      };

      const syncButtonLabel = function () {
        if (!toolbarButtonView) return;
        const selected = findSelectedCtaEmbed(editor);
        toolbarButtonView.set({
          label: selected ? "ویرایش CTA" : "درج CTA",
        });
      };

      const onMessage = function (event) {
        if (event.origin !== window.location.origin) return;
        const data = event.data;
        if (!data || typeof data !== "object") return;

        if (data.type === CTA_HELPER_READY) {
          const selected = findSelectedCtaEmbed(editor);
          if (selected) editingTarget = selected;
          if (event.source && !event.source.closed) {
            event.source.postMessage(
              {
                type: CTA_HELPER_EDIT_CONTEXT,
                html: editingTarget
                  ? editingTarget.getAttribute("value")
                  : selected
                    ? selected.getAttribute("value")
                    : null,
                editing: !!(editingTarget || selected),
              },
              window.location.origin
            );
          }
          return;
        }

        if (data.type === CTA_HELPER_MESSAGE && typeof data.html === "string") {
          const replace = data.replace === true;
          const target =
            replace && elementStillInDocument(editingTarget)
              ? editingTarget
              : replace
                ? findSelectedCtaEmbed(editor)
                : null;

          insertCtaHtml(editor, data.html, {
            replace: !!target,
            target: target,
          });

          if (target) {
            editingTarget = target;
          } else {
            editingTarget = findSelectedCtaEmbed(editor);
          }

          syncButtonLabel();

          if (helperWindow && !helperWindow.closed) {
            helperWindow.focus();
          }
        }
      };

      window.addEventListener("message", onMessage);

      editor.model.document.selection.on("change:range", syncButtonLabel);
      editor.model.document.selection.on("change:attribute", syncButtonLabel);

      editor.on("destroy", function () {
        window.removeEventListener("message", onMessage);
      });

      editor.ui.componentFactory.add("ctaHelper", function (locale) {
        const view = new ButtonView(locale);
        toolbarButtonView = view;

        view.set({
          label: "درج CTA",
          icon: ctaHelperIcon,
          tooltip: true,
        });

        view.on("execute", openHelper);

        return view;
      });

      const findCtaModelFromViewTarget = function (viewTarget) {
        let viewElement = viewTarget;
        while (viewElement) {
          const modelElement = editor.editing.mapper.toModelElement(viewElement);
          if (
            modelElement &&
            modelElement.is("element", "rawHtml") &&
            isNovaCtaHtml(modelElement.getAttribute("value") || "")
          ) {
            return modelElement;
          }
          viewElement = viewElement.parent;
        }
        return null;
      };

      const openCtaFromViewEvent = function (evt, data) {
        const domTarget =
          data.domTarget || (data.domEvent && data.domEvent.target);
        if (
          domTarget &&
          typeof domTarget.closest === "function" &&
          domTarget.closest(
            "textarea, input, select, option, button, .ck-button"
          )
        ) {
          return;
        }

        const modelElement = findCtaModelFromViewTarget(data.target);
        if (!modelElement) return;

        editor.model.change(function (writer) {
          writer.setSelection(modelElement, "on");
        });
        data.preventDefault();
        evt.stop();
        openHelper();
      };

      // Click / double-click a CTA HTML embed to reopen the helper for select + edit.
      if (typeof ClickObserver === "function") {
        editor.editing.view.addObserver(ClickObserver);
        editor.editing.view.document.on("click", openCtaFromViewEvent);
      }
      editor.editing.view.document.on("dblclick", openCtaFromViewEvent);
    }
  }

  class VideoWarningInsert extends Plugin {
    static get pluginName() {
      return "VideoWarningInsert";
    }

    static get requires() {
      return typeof Widget === "function" ? [Widget] : [];
    }

    init() {
      const editor = this.editor;

      if (
        typeof Widget !== "function" ||
        typeof toWidget !== "function" ||
        typeof toWidgetEditable !== "function"
      ) {
        console.warn("VideoWarningInsert skipped: Widget APIs unavailable");
        return;
      }

      this._defineSchema();
      this._defineConverters();
      this._definePostFixer();

      editor.ui.componentFactory.add("videoWarning", function (locale) {
        const view = new ButtonView(locale);

        view.set({
          label: "هشدار محتوای ویدیو",
          icon: videoWarningIcon,
          tooltip: true,
        });

        view.on("execute", function () {
          editor.model.change(function (writer) {
            const box = writer.createElement("novaVideoWarning");
            const body = writer.createElement("novaVideoWarningText");
            writer.insertText(VIDEO_WARNING_DEFAULT_TEXT, body);
            writer.append(body, box);

            const selection = editor.model.document.selection;
            const selected = selection.getSelectedElement();
            const root = editor.model.document.getRoot();

            // writer.insert only (no insertObject/insertContent): those can
            // replace the selection or detach nodes inside an open change,
            // and autosave then stores an empty body for the next edit.
            if (
              selected &&
              (selected.is("element", "media") ||
                selected.is("element", "rawHtml"))
            ) {
              writer.insert(box, writer.createPositionBefore(selected));
            } else {
              const anchor = selection.getFirstPosition();
              let insertPos = writer.createPositionAt(root, "end");

              if (anchor) {
                const allowedParent = editor.model.schema.findAllowedParent(
                  anchor,
                  "novaVideoWarning"
                );

                if (allowedParent === anchor.parent) {
                  insertPos = anchor;
                } else if (allowedParent) {
                  let node = anchor.parent;
                  while (node && node.parent && node.parent !== allowedParent) {
                    node = node.parent;
                  }
                  if (node && node.parent === allowedParent) {
                    insertPos = anchor.isAtStart
                      ? writer.createPositionBefore(node)
                      : writer.createPositionAfter(node);
                  } else {
                    insertPos = writer.createPositionAt(allowedParent, "end");
                  }
                }
              }

              writer.insert(box, insertPos);
            }

            if (box.root && box.root.document) {
              writer.setSelection(body, "in");
            }
          });

          editor.editing.view.focus();
        });

        return view;
      });
    }

    _defineSchema() {
      const schema = this.editor.model.schema;

      schema.register("novaVideoWarning", {
        inheritAllFrom: "$blockObject",
        allowChildren: ["novaVideoWarningText"],
      });

      // Compact editable text (inline only) — avoids nested <p> height/gaps.
      schema.register("novaVideoWarningText", {
        isLimit: true,
        allowIn: "novaVideoWarning",
      });

      schema.extend("$text", { allowIn: "novaVideoWarningText" });
      if (schema.isRegistered("softBreak")) {
        schema.extend("softBreak", { allowIn: "novaVideoWarningText" });
      }

      schema.addChildCheck(function (context, childDefinition) {
        if (
          childDefinition.name === "novaVideoWarning" &&
          typeof context.endsWith === "function" &&
          context.endsWith("novaVideoWarningText")
        ) {
          return false;
        }
      });
    }

    _defineConverters() {
      const conversion = this.editor.conversion;
      const warningViewAttrs = {
        class: "nova-video-warning",
        "data-nova-video-warning": "1",
        dir: "rtl",
        role: "note",
      };

      function isVideoWarningView(viewElement) {
        return (
          viewElement &&
          viewElement.is &&
          viewElement.is("element", "div") &&
          (viewElement.hasClass("nova-video-warning") ||
            viewElement.getAttribute("data-nova-video-warning") === "1" ||
            viewElement.getAttribute("data-nova-video-warning") === "true")
        );
      }

      function isVideoWarningTextView(viewElement) {
        return (
          viewElement &&
          viewElement.is &&
          viewElement.is("element", "div") &&
          typeof viewElement.hasClass === "function" &&
          viewElement.hasClass("nova-video-warning__text")
        );
      }

      function extractPlainText(viewNode) {
        if (!viewNode) return "";
        if (viewNode.is && viewNode.is("$text")) {
          return viewNode.data || "";
        }
        if (typeof viewNode.getChildren !== "function") return "";
        let out = "";
        for (const child of viewNode.getChildren()) {
          if (child.is && child.is("element", "br")) {
            out += "\n";
            continue;
          }
          out += extractPlainText(child);
        }
        return out;
      }

      // Highest priority: build the full model subtree so GHS/HtmlEmbed cannot
      // steal the markup on reload (that left the editor blank after save).
      conversion.for("upcast").add(function (dispatcher) {
        dispatcher.on(
          "element:div",
          function (evt, data, conversionApi) {
            const viewItem = data.viewItem;
            if (!isVideoWarningView(viewItem)) return;
            if (!conversionApi.consumable.consume(viewItem, { name: true })) {
              return;
            }

            const box = conversionApi.writer.createElement("novaVideoWarning");
            if (!conversionApi.safeInsert(box, data.modelCursor)) {
              return;
            }

            const body = conversionApi.writer.createElement(
              "novaVideoWarningText"
            );
            conversionApi.writer.append(body, box);

            let textContainer = null;
            for (const child of viewItem.getChildren()) {
              if (isVideoWarningTextView(child)) {
                textContainer = child;
                conversionApi.consumable.consume(child, { name: true });
                break;
              }
            }

            const rawText = (
              extractPlainText(textContainer || viewItem) || ""
            ).trim();
            conversionApi.writer.insertText(
              rawText || VIDEO_WARNING_DEFAULT_TEXT,
              body
            );

            conversionApi.updateConversionResult(box, data);
            evt.stop();
          },
          { priority: "highest" }
        );

        dispatcher.on(
          "element:span",
          function (evt, data, conversionApi) {
            const viewItem = data.viewItem;
            if (
              !viewItem ||
              typeof viewItem.hasClass !== "function" ||
              !viewItem.hasClass("nova-video-warning__icon")
            ) {
              return;
            }

            if (!conversionApi.consumable.consume(viewItem, { name: true })) {
              return;
            }

            data.modelRange = conversionApi.writer.createRange(data.modelCursor);
            evt.stop();
          },
          { priority: "highest" }
        );
      });

      conversion.for("dataDowncast").elementToElement({
        model: "novaVideoWarning",
        view: {
          name: "div",
          classes: "nova-video-warning",
          attributes: {
            "data-nova-video-warning": "1",
            dir: "rtl",
            role: "note",
          },
        },
      });

      conversion.for("editingDowncast").elementToElement({
        model: "novaVideoWarning",
        view: function (modelElement, { writer }) {
          const container = writer.createContainerElement(
            "div",
            warningViewAttrs
          );
          return toWidget(container, writer, {
            label: "هشدار محتوای ویدیو",
            hasSelectionHandle: true,
          });
        },
      });

      conversion.for("dataDowncast").elementToElement({
        model: "novaVideoWarningText",
        view: {
          name: "div",
          classes: "nova-video-warning__text",
        },
      });

      conversion.for("editingDowncast").elementToElement({
        model: "novaVideoWarningText",
        view: function (modelElement, { writer }) {
          const editable = writer.createEditableElement("div", {
            class: "nova-video-warning__text",
          });
          return toWidgetEditable(editable, writer);
        },
      });
    }

    _definePostFixer() {
      const editor = this.editor;
      const model = editor.model;

      model.document.registerPostFixer(function (writer) {
        let changed = false;
        const warnings = [];

        for (const value of model.createRangeIn(model.document.getRoot())) {
          if (value.item.is && value.item.is("element", "novaVideoWarning")) {
            warnings.push(value.item);
          }
        }

        warnings.forEach(function (item) {
          const children = Array.from(item.getChildren());
          const textChild = children.find(function (child) {
            return child.is("element", "novaVideoWarningText");
          });

          if (!textChild) {
            const body = writer.createElement("novaVideoWarningText");
            const orphans = children.slice();
            writer.append(body, item);

            orphans.forEach(function (orphan) {
              if (orphan.is("element", "paragraph")) {
                Array.from(orphan.getChildren()).forEach(function (node) {
                  writer.move(
                    writer.createRangeOn(node),
                    writer.createPositionAt(body, "end")
                  );
                });
                writer.remove(orphan);
              } else {
                writer.move(
                  writer.createRangeOn(orphan),
                  writer.createPositionAt(body, "end")
                );
              }
            });

            if (!body.childCount) {
              writer.insertText(VIDEO_WARNING_DEFAULT_TEXT, body);
            }

            changed = true;
            return;
          }

          children.forEach(function (child) {
            if (child === textChild) return;
            if (child.is("element", "paragraph")) {
              Array.from(child.getChildren()).forEach(function (node) {
                writer.move(
                  writer.createRangeOn(node),
                  writer.createPositionAt(textChild, "end")
                );
              });
              writer.remove(child);
            } else {
              writer.move(
                writer.createRangeOn(child),
                writer.createPositionAt(textChild, "end")
              );
            }
            changed = true;
          });

          // Flatten a single wrapping paragraph inside the text container.
          const textKids = Array.from(textChild.getChildren());
          if (
            textKids.length === 1 &&
            textKids[0].is &&
            textKids[0].is("element", "paragraph")
          ) {
            const onlyP = textKids[0];
            Array.from(onlyP.getChildren()).forEach(function (node) {
              writer.move(
                writer.createRangeOn(node),
                writer.createPositionAt(textChild, "end")
              );
            });
            writer.remove(onlyP);
            changed = true;
          }

          // Keep empty text container valid — do not auto-restore default text
          // while the user is clearing/editing (that felt like "delete on select").
          if (!textChild.childCount) {
            writer.insertText("\u00A0", textChild);
            changed = true;
          }
        });

        return changed;
      });
    }
  }

  function getSelectedImageElement(editor) {
    const imageUtils = editor.plugins.get("ImageUtils");
    if (!imageUtils || typeof imageUtils.getClosestSelectedImageElement !== "function") {
      return null;
    }
    return imageUtils.getClosestSelectedImageElement(editor.model.document.selection);
  }

  function bindImageButtonEnabled(editor, view) {
    function refresh() {
      view.isEnabled = !!getSelectedImageElement(editor);
    }

    refresh();
    editor.model.document.selection.on("change", refresh);
    return refresh;
  }

  function applyImageSource(editor, imageElement, item) {
    if (!elementStillInDocument(imageElement) || !item || !item.publicUrl) return;

    editor.model.change((writer) => {
      writer.setAttribute("src", item.publicUrl, imageElement);
      if (Object.prototype.hasOwnProperty.call(item, "alt")) {
        writer.setAttribute("alt", item.alt || "", imageElement);
      }
    });
    editor.editing.view.focus();
  }

  function escapeAttr(value) {
    return String(value)
      .replace(/&/g, "&amp;")
      .replace(/"/g, "&quot;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;");
  }

  function isVideoLikeHtml(html) {
    return typeof html === "string" && /<(video|iframe)\b/i.test(html);
  }

  function videoTargetFromElement(element) {
    if (!element || typeof element.is !== "function") return null;

    if (element.is("element", "media")) {
      const url = String(element.getAttribute("url") || "").trim();
      if (!url) return null;
      return { kind: "media", element: element, url: url };
    }

    if (element.is("element", "rawHtml")) {
      const value = element.getAttribute("value") || "";
      if (!isVideoLikeHtml(value)) return null;
      return { kind: "rawHtml", element: element, html: value };
    }

    return null;
  }

  function findSelectedVideoTarget(editor) {
    const selection = editor.model.document.selection;
    const direct = videoTargetFromElement(selection.getSelectedElement());
    if (direct) return direct;

    const position = selection.getFirstPosition();
    if (!position) return null;

    let node = position.parent;
    while (node) {
      const fromParent = videoTargetFromElement(node);
      if (fromParent) return fromParent;
      node = node.parent;
    }

    const before = videoTargetFromElement(position.nodeBefore);
    if (before) return before;

    const after = videoTargetFromElement(position.nodeAfter);
    if (after) return after;

    return null;
  }

  function extractAttrValue(html, attrName) {
    const re = new RegExp(
      "\\b" + attrName + "\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))",
      "i"
    );
    const match = String(html || "").match(re);
    if (!match) return "";
    return String(match[1] || match[2] || match[3] || "").trim();
  }

  function extractPosterFromHtml(html) {
    return (
      extractAttrValue(html, "data-nova-video-poster") ||
      extractAttrValue(html, "poster") ||
      ""
    );
  }

  function extractPosterFromTarget(target) {
    if (!target) return "";
    if (target.kind === "rawHtml") {
      return extractPosterFromHtml(
        target.element.getAttribute("value") || target.html || ""
      );
    }
    return "";
  }

  function removePosterFromHtml(html) {
    let next = String(html || "");
    next = next.replace(
      /\s*poster\s*=\s*("[^"]*"|'[^']*'|[^\s>]+)/gi,
      ""
    );
    next = next.replace(
      /\s*data-nova-video-poster\s*=\s*("[^"]*"|'[^']*'|[^\s>]+)/gi,
      ""
    );
    return next;
  }

  function applyPosterToTarget(editor, target, posterUrl) {
    if (!target || !elementStillInDocument(target.element)) {
      window.alert("ویدیو دیگر در محتوا نیست. دوباره انتخاب کنید.");
      return false;
    }

    let html = null;
    const safePoster = posterUrl ? String(posterUrl).trim() : "";

    if (target.kind === "media") {
      html = mediaUrlToCoveredHtml(target.url, safePoster || null);
    } else {
      const value = target.element.getAttribute("value") || target.html || "";
      html = safePoster
        ? applyPosterToVideoHtml(value, safePoster)
        : removePosterFromHtml(value);
    }

    if (!html) {
      window.alert("اعمال کاور برای این ویدیو ممکن نشد.");
      return false;
    }

    insertOrReplaceRawHtml(editor, html, target.element);
    return true;
  }

  function closeVideoPosterModal(modal) {
    if (!modal) return;
    modal.hidden = true;
    modal._novaPosterCtx = null;
    modal._novaPosterOriginal = "";
    document.body.classList.remove("admin-modal-open");
    if (modal._novaPosterEsc) {
      document.removeEventListener("keydown", modal._novaPosterEsc);
      modal._novaPosterEsc = null;
    }
  }

  function openVideoPosterMediaPicker(modal) {
    const picker = window.AdminMediaPicker;
    if (!picker || typeof picker.open !== "function") {
      window.alert("کتابخانه رسانه در این صفحه در دسترس نیست.");
      return;
    }

    picker.open({
      onSelect: function (item) {
        if (!item || !item.publicUrl) return;
        const url = String(item.publicUrl).trim();
        if (!url) return;
        modal._novaPosterDraft = url;
        syncVideoPosterModalPreview(modal, url);
      },
    });
  }

  function ensureVideoPosterModal() {
    let modal = document.getElementById("video-poster-modal");
    if (modal) return modal;

    modal = document.createElement("div");
    modal.id = "video-poster-modal";
    modal.className = "admin-modal";
    modal.hidden = true;
    modal.innerHTML =
      '<div class="admin-modal-backdrop" data-video-poster-close></div>' +
      '<div class="admin-modal-dialog admin-video-poster-dialog" role="dialog" aria-modal="true" aria-labelledby="video-poster-modal-title">' +
      '<div class="admin-modal-header">' +
      '<div>' +
      '<h2 id="video-poster-modal-title">کاور ویدیو</h2>' +
      '<p class="admin-video-poster-subtitle">این تصویر قبل از پخش روی ویدیوی انتخاب‌شده نمایش داده می‌شود.</p>' +
      "</div>" +
      '<button type="button" class="btn btn-ghost" data-video-poster-close aria-label="بستن">×</button>' +
      "</div>" +
      '<div class="admin-video-poster-body">' +
      '<div class="admin-video-poster-preview" data-video-poster-preview>' +
      '<img data-video-poster-image alt="پیش‌نمایش کاور" hidden />' +
      '<div class="admin-video-poster-empty" data-video-poster-empty>' +
      '<span class="admin-video-poster-empty-icon" aria-hidden="true">' +
      '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">' +
      '<rect x="3" y="5" width="18" height="14" rx="2"/>' +
      '<path d="M10 9.5l5 2.5-5 2.5v-5z"/>' +
      "</svg></span>" +
      '<p class="admin-video-poster-empty-title">کاور تنظیم نشده</p>' +
      '<p class="admin-video-poster-empty-text">یک تصویر ۱۶:۹ از کتابخانه رسانه انتخاب کنید یا آپلود کنید.</p>' +
      '<button type="button" class="btn btn-primary" data-video-poster-pick>انتخاب تصویر</button>' +
      "</div>" +
      '<button type="button" class="admin-video-poster-change" data-video-poster-pick data-video-poster-change hidden>تغییر تصویر</button>' +
      "</div>" +
      '<div class="admin-video-poster-actions">' +
      '<button type="button" class="btn btn-danger-ghost" data-video-poster-clear>حذف کاور</button>' +
      '<div class="admin-video-poster-actions-end">' +
      '<button type="button" class="btn btn-ghost" data-video-poster-close>انصراف</button>' +
      '<button type="button" class="btn btn-primary" data-video-poster-apply>اعمال کاور</button>' +
      "</div>" +
      "</div>" +
      "</div>" +
      "</div>";

    document.body.appendChild(modal);

    modal.querySelectorAll("[data-video-poster-close]").forEach(function (el) {
      el.addEventListener("click", function () {
        closeVideoPosterModal(modal);
      });
    });

    modal.querySelectorAll("[data-video-poster-pick]").forEach(function (el) {
      el.addEventListener("click", function () {
        openVideoPosterMediaPicker(modal);
      });
    });

    modal
      .querySelector("[data-video-poster-clear]")
      .addEventListener("click", function () {
        modal._novaPosterDraft = "";
        syncVideoPosterModalPreview(modal, "");
      });

    modal
      .querySelector("[data-video-poster-apply]")
      .addEventListener("click", function () {
        const ctx = modal._novaPosterCtx;
        if (!ctx || !ctx.editor || !ctx.target) {
          closeVideoPosterModal(modal);
          return;
        }

        const draft =
          typeof modal._novaPosterDraft === "string"
            ? modal._novaPosterDraft
            : "";
        applyPosterToTarget(ctx.editor, ctx.target, draft);
        closeVideoPosterModal(modal);
      });

    return modal;
  }

  function syncVideoPosterModalPreview(modal, posterUrl) {
    const preview = modal.querySelector("[data-video-poster-preview]");
    const img = modal.querySelector("[data-video-poster-image]");
    const empty = modal.querySelector("[data-video-poster-empty]");
    const changeBtn = modal.querySelector("[data-video-poster-change]");
    const clearBtn = modal.querySelector("[data-video-poster-clear]");
    const applyBtn = modal.querySelector("[data-video-poster-apply]");
    const url = String(posterUrl || "").trim();
    const original =
      typeof modal._novaPosterOriginal === "string"
        ? modal._novaPosterOriginal
        : "";

    if (preview) preview.classList.toggle("has-image", !!url);

    if (img) {
      if (url) {
        img.hidden = false;
        img.src = url;
      } else {
        img.hidden = true;
        img.removeAttribute("src");
      }
    }

    if (empty) empty.hidden = !!url;
    if (changeBtn) changeBtn.hidden = !url;
    if (clearBtn) clearBtn.disabled = !url;
    if (applyBtn) applyBtn.disabled = url === original;
  }

  function openVideoPosterManager(editor, preferredTarget) {
    const target = preferredTarget || findSelectedVideoTarget(editor);
    if (!target) {
      window.alert(
        "ابتدا روی ویدیوی داخل محتوا کلیک کنید، بعد «تنظیم کاور ویدیو» را بزنید."
      );
      return;
    }

    editor.model.change(function (writer) {
      writer.setSelection(target.element, "on");
    });

    const modal = ensureVideoPosterModal();
    const currentPoster = extractPosterFromTarget(target);
    modal._novaPosterCtx = { editor: editor, target: target };
    modal._novaPosterOriginal = currentPoster;
    modal._novaPosterDraft = currentPoster;
    syncVideoPosterModalPreview(modal, currentPoster);

    if (modal.parentElement === document.body) {
      document.body.appendChild(modal);
    }

    modal.hidden = false;
    document.body.classList.add("admin-modal-open");

    if (modal._novaPosterEsc) {
      document.removeEventListener("keydown", modal._novaPosterEsc);
    }
    modal._novaPosterEsc = function (event) {
      if (event.key !== "Escape") return;
      const mediaPicker = document.getElementById("media-library-picker");
      if (mediaPicker && !mediaPicker.hidden) return;
      closeVideoPosterModal(modal);
    };
    document.addEventListener("keydown", modal._novaPosterEsc);

    const focusTarget =
      modal.querySelector("[data-video-poster-pick]:not([hidden])") ||
      modal.querySelector("[data-video-poster-close]");
    if (focusTarget && typeof focusTarget.focus === "function") {
      focusTarget.focus();
    }
  }

  function openVideoPosterPicker(editor, preferredTarget) {
    openVideoPosterManager(editor, preferredTarget);
  }

  function buildRawVideoHtml(src, posterUrl) {
    const safeSrc = escapeAttr(src);
    const posterAttr = posterUrl
      ? ' poster="' + escapeAttr(posterUrl) + '"'
      : "";
    const posterData = posterUrl
      ? ' data-nova-video-poster="' + escapeAttr(posterUrl) + '"'
      : "";
    return (
      '<figure class="nova-video"' +
      posterData +
      ">" +
      '<div style="position:relative;padding-bottom:56.25%;height:0;background:#0f1f22;">' +
      '<video controls playsinline preload="metadata" src="' +
      safeSrc +
      '"' +
      posterAttr +
      ' style="position:absolute;inset:0;width:100%;height:100%;object-fit:contain;"></video>' +
      "</div></figure>"
    );
  }

  function buildAparatHtml(hash, posterUrl) {
    const src =
      "https://www.aparat.com/video/video/embed/videohash/" +
      hash +
      "/vt/frame";
    const posterData = posterUrl
      ? ' data-nova-video-poster="' + escapeAttr(posterUrl) + '"'
      : "";
    return (
      '<figure class="nova-video"' +
      posterData +
      ">" +
      '<div style="position:relative;padding-bottom:56.25%;height:0;">' +
      '<iframe src="' +
      escapeAttr(src) +
      '" style="position:absolute;inset:0;width:100%;height:100%;border:0;" ' +
      'allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture; fullscreen" ' +
      'allowfullscreen loading="lazy"></iframe>' +
      "</div></figure>"
    );
  }

  function buildYoutubeHtml(id, posterUrl) {
    const src = "https://www.youtube.com/embed/" + id;
    const posterData = posterUrl
      ? ' data-nova-video-poster="' + escapeAttr(posterUrl) + '"'
      : "";
    return (
      '<figure class="nova-video"' +
      posterData +
      ">" +
      '<div style="position:relative;padding-bottom:56.25%;height:0;">' +
      '<iframe src="' +
      escapeAttr(src) +
      '" style="position:absolute;inset:0;width:100%;height:100%;border:0;" ' +
      'allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture; fullscreen" ' +
      'allowfullscreen loading="lazy"></iframe>' +
      "</div></figure>"
    );
  }

  function mediaUrlToCoveredHtml(url, posterUrl) {
    const raw = String(url || "").trim();
    if (!raw) return null;
    const normalized = raw.replace(/^https?:\/\//i, "").replace(/^www\./i, "");

    const aparat =
      normalized.match(/^aparat\.com\/v\/([A-Za-z0-9_-]+)/i) ||
      normalized.match(
        /^aparat\.com\/video\/video\/embed(?:_box)?\/videohash\/([A-Za-z0-9_-]+)/i
      );
    if (aparat) return buildAparatHtml(aparat[1], posterUrl);

    const yt =
      normalized.match(/^youtube\.com\/watch\?(?:.*&)?v=([A-Za-z0-9_-]+)/i) ||
      normalized.match(/^youtu\.be\/([A-Za-z0-9_-]+)/i) ||
      normalized.match(/^youtube\.com\/embed\/([A-Za-z0-9_-]+)/i);
    if (yt) return buildYoutubeHtml(yt[1], posterUrl);

    if (/\.(mp4|webm|ogg|ogv|m4v|mov)(?:[?#][^\s]*)?$/i.test(normalized)) {
      const src = /^https?:\/\//i.test(raw) ? raw : "https://" + raw;
      return buildRawVideoHtml(src, posterUrl);
    }

    // Fallback: keep generic responsive iframe when possible.
    const iframeSrc = /^https?:\/\//i.test(raw) ? raw : "https://" + raw;
    const posterData = posterUrl
      ? ' data-nova-video-poster="' + escapeAttr(posterUrl) + '"'
      : "";
    return (
      '<figure class="nova-video"' +
      posterData +
      ">" +
      '<div style="position:relative;padding-bottom:56.25%;height:0;">' +
      '<iframe src="' +
      escapeAttr(iframeSrc) +
      '" style="position:absolute;inset:0;width:100%;height:100%;border:0;" ' +
      'allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture; fullscreen" ' +
      'allowfullscreen loading="lazy"></iframe>' +
      "</div></figure>"
    );
  }

  function applyPosterToVideoHtml(html, posterUrl) {
    const safePoster = escapeAttr(posterUrl);
    let next = String(html || "");

    if (/<video\b/i.test(next)) {
      next = next.replace(/<video\b([^>]*)>/i, function (_m, attrs) {
        const cleaned = String(attrs || "").replace(
          /\s*poster\s*=\s*("[^"]*"|'[^']*'|[^\s>]+)/i,
          ""
        );
        return '<video poster="' + safePoster + '"' + cleaned + ">";
      });
    }

    if (/<figure\b/i.test(next)) {
      next = next.replace(/<figure\b([^>]*)>/i, function (_m, attrs) {
        let a = String(attrs || "");
        a = a.replace(/\s*data-nova-video-poster\s*=\s*("[^"]*"|'[^']*')/i, "");
        if (/\bclass\s*=\s*"/i.test(a)) {
          a = a.replace(/\bclass\s*=\s*"/i, 'class="nova-video ');
          a = a.replace(/class="nova-video\s+nova-video\s+/i, 'class="nova-video ');
        } else {
          a += ' class="nova-video"';
        }
        return (
          "<figure" + a + ' data-nova-video-poster="' + safePoster + '">'
        );
      });
      return next;
    }

    return (
      '<figure class="nova-video" data-nova-video-poster="' +
      safePoster +
      '">' +
      next +
      "</figure>"
    );
  }

  function insertOrReplaceRawHtml(editor, html, replaceElement) {
    editor.model.change(function (writer) {
      if (replaceElement && elementStillInDocument(replaceElement)) {
        if (replaceElement.is("element", "rawHtml")) {
          writer.setAttribute("value", html, replaceElement);
          writer.setSelection(replaceElement, "on");
          return;
        }
        const embed = writer.createElement("rawHtml", { value: html });
        writer.insert(embed, writer.createPositionBefore(replaceElement));
        writer.remove(replaceElement);
        writer.setSelection(embed, "on");
        return;
      }
      const embed = writer.createElement("rawHtml", { value: html });
      editor.model.insertContent(embed);
    });
    editor.editing.view.focus();
  }

  class VideoPoster extends Plugin {
    static get pluginName() {
      return "VideoPoster";
    }

    static get requires() {
      const deps = [];
      if (typeof WidgetToolbarRepository === "function") {
        deps.push(WidgetToolbarRepository);
      }
      if (typeof MediaEmbedToolbar === "function") {
        deps.push(MediaEmbedToolbar);
      }
      return deps;
    }

    init() {
      const editor = this.editor;
      let toolbarButtonView = null;
      let removeButtonView = null;

      const findVideoTargetFromView = function (viewTarget) {
        let viewElement = viewTarget;
        while (viewElement) {
          const modelElement =
            editor.editing.mapper.toModelElement(viewElement);
          const target = videoTargetFromElement(modelElement);
          if (target) return target;
          viewElement = viewElement.parent;
        }
        return null;
      };

      const syncToolbarState = function () {
        const selected = findSelectedVideoTarget(editor);
        const enabled = !!selected;
        if (toolbarButtonView) {
          toolbarButtonView.isEnabled = enabled;
          toolbarButtonView.isOn = enabled;
          toolbarButtonView.label = selected
            ? "تنظیم کاور ویدیو"
            : "کاور ویدیو";
        }
        if (removeButtonView) {
          const hasPoster = !!extractPosterFromTarget(selected);
          removeButtonView.isEnabled = enabled && hasPoster;
        }
      };

      editor.ui.componentFactory.add("videoPoster", (locale) => {
        const view = new ButtonView(locale);
        toolbarButtonView = view;

        view.set({
          label: "تنظیم کاور ویدیو",
          icon: videoPosterIcon,
          tooltip: "کاور (پوستر) برای ویدیوی انتخاب‌شده",
          isEnabled: false,
        });

        view.on("execute", () => {
          openVideoPosterManager(editor);
        });

        return view;
      });

      editor.ui.componentFactory.add("videoPosterRemove", (locale) => {
        const view = new ButtonView(locale);
        removeButtonView = view;

        view.set({
          label: "حذف کاور ویدیو",
          icon: deleteImageIcon,
          tooltip: true,
          isEnabled: false,
        });

        view.on("execute", () => {
          const target = findSelectedVideoTarget(editor);
          if (!target) return;
          applyPosterToTarget(editor, target, "");
          syncToolbarState();
        });

        return view;
      });

      editor.model.document.selection.on("change:range", syncToolbarState);
      editor.model.document.on("change:data", syncToolbarState);
      syncToolbarState();

      editor.on("ready", function () {
        try {
          const repo = editor.plugins.get("WidgetToolbarRepository");
          if (!repo || typeof repo.register !== "function") return;

          // MediaEmbed widgets use mediaEmbed.toolbar; this covers HtmlEmbed/rawHtml videos.
          repo.register("novaVideoPosterToolbar", {
            ariaLabel: "نوار ابزار کاور ویدیو",
            items: ["videoPoster", "videoPosterRemove"],
            getRelatedElement: function (selection) {
              const viewElement = selection.getSelectedElement();
              if (!viewElement) return null;

              const modelElement =
                editor.editing.mapper.toModelElement(viewElement);
              if (!modelElement || !modelElement.is("element", "rawHtml")) {
                return null;
              }

              const target = videoTargetFromElement(modelElement);
              if (!target) return null;

              if (
                isNovaCtaHtml(
                  target.html || target.element.getAttribute("value") || ""
                )
              ) {
                return null;
              }

              return viewElement;
            },
          });
        } catch (err) {
          console.warn("Video poster contextual toolbar unavailable", err);
        }
      });

      const onVideoViewEvent = function (evt, data, openPicker) {
        const domTarget =
          data.domTarget || (data.domEvent && data.domEvent.target);
        if (
          domTarget &&
          typeof domTarget.closest === "function" &&
          domTarget.closest(
            "textarea, input, select, option, button, .ck-button"
          )
        ) {
          return;
        }

        const target = findVideoTargetFromView(data.target);
        if (!target) return;

        // Don't steal CTA clicks — those embeds are handled separately.
        if (
          target.kind === "rawHtml" &&
          isNovaCtaHtml(target.html || target.element.getAttribute("value") || "")
        ) {
          return;
        }

        editor.model.change(function (writer) {
          writer.setSelection(target.element, "on");
        });
        syncToolbarState();

        if (openPicker) {
          data.preventDefault();
          evt.stop();
          openVideoPosterManager(editor, target);
        }
      };

      if (typeof ClickObserver === "function") {
        editor.editing.view.addObserver(ClickObserver);
        editor.editing.view.document.on("click", function (evt, data) {
          onVideoViewEvent(evt, data, false);
        });
      }

      editor.editing.view.document.on("dblclick", function (evt, data) {
        onVideoViewEvent(evt, data, true);
      });
    }
  }

  class MediaLibraryImage extends Plugin {
    static get pluginName() {
      return "MediaLibraryImage";
    }

    static get requires() {
      return [ImageUtils];
    }

    init() {
      const editor = this.editor;

      editor.ui.componentFactory.add("mediaLibraryImage", (locale) => {
        const view = new ButtonView(locale);

        view.set({
          label: "درج تصویر از کتابخانه رسانه",
          icon: mediaLibraryImageIcon,
          tooltip: true,
        });

        view.on("execute", () => {
          const picker = window.AdminMediaPicker;
          if (!picker || typeof picker.open !== "function") {
            console.error("AdminMediaPicker is not available on this page.");
            return;
          }

          picker.open({
            onSelect(item) {
              if (!item || !item.publicUrl) return;

              try {
                const imageUtils = editor.plugins.get("ImageUtils");
                imageUtils.insertImage({
                  src: item.publicUrl,
                  alt: item.altText || item.title || "",
                });
                editor.editing.view.focus();
              } catch (err) {
                console.error("Failed to insert media library image", err);
              }
            },
          });
        });

        return view;
      });

      editor.ui.componentFactory.add("replaceImage", (locale) => {
        const view = new ButtonView(locale);

        view.set({
          label: "تعویض تصویر",
          icon: replaceImageIcon,
          tooltip: true,
        });

        bindImageButtonEnabled(editor, view);

        view.on("execute", () => {
          const imageElement = getSelectedImageElement(editor);
          if (!imageElement) return;

          const picker = window.AdminMediaPicker;
          if (!picker || typeof picker.open !== "function") {
            console.error("AdminMediaPicker is not available on this page.");
            return;
          }

          picker.open({
            onSelect(item) {
              if (!item || !item.publicUrl) return;
              applyImageSource(editor, imageElement, {
                publicUrl: item.publicUrl,
                alt: item.altText || item.title || imageElement.getAttribute("alt") || "",
              });
            },
          });
        });

        return view;
      });

      editor.ui.componentFactory.add("editImage", (locale) => {
        const view = new ButtonView(locale);

        view.set({
          label: "ویرایش تصویر",
          icon: editImageIcon,
          tooltip: true,
        });

        bindImageButtonEnabled(editor, view);

        view.on("execute", async () => {
          const imageElement = getSelectedImageElement(editor);
          if (!imageElement) return;

          const src = String(imageElement.getAttribute("src") || "").trim();
          if (!src) return;

          const mediaEditor = window.AdminMediaImageEditor;
          if (!mediaEditor || typeof mediaEditor.open !== "function") {
            window.alert("ویرایشگر تصویر در این صفحه در دسترس نیست.");
            return;
          }

          try {
            let asset = null;
            let id = null;
            if (typeof mediaEditor.resolveByUrl === "function") {
              asset = await mediaEditor.resolveByUrl(src);
              id = asset?.id || null;
            }

            if (!id) {
              window.alert("این تصویر در کتابخانه رسانه پیدا نشد. ابتدا آن را از کتابخانه درج کنید.");
              return;
            }

            await mediaEditor.open({
              id,
              asset: asset || undefined,
              onSaved(data) {
                if (!data?.publicUrl) return;
                applyImageSource(editor, imageElement, {
                  publicUrl: data.publicUrl,
                  alt: imageElement.getAttribute("alt") || "",
                });
              },
            });
          } catch (err) {
            console.error("Failed to edit media library image", err);
            window.alert("ویرایش تصویر ممکن نشد.");
          }
        });

        return view;
      });

      editor.ui.componentFactory.add("deleteImage", (locale) => {
        const view = new ButtonView(locale);

        view.set({
          label: "حذف تصویر",
          icon: deleteImageIcon,
          tooltip: true,
        });

        bindImageButtonEnabled(editor, view);

        view.on("execute", () => {
          const imageElement = getSelectedImageElement(editor);
          if (!elementStillInDocument(imageElement)) return;

          editor.model.change((writer) => {
            writer.remove(imageElement);
          });
          editor.editing.view.focus();
        });

        return view;
      });
    }
  }

  const editorConfig = function () {
    return {
      licenseKey: "GPL",
      language: {
        ui: "fa",
        content: "fa",
        textPartLanguage: [
          { title: "فارسی", languageCode: "fa", textDirection: "rtl" },
          { title: "English", languageCode: "en", textDirection: "ltr" },
        ],
      },
      plugins: [
        Essentials,
        Paragraph,
        Heading,
        Bold,
        Italic,
        Underline,
        Strikethrough,
        Subscript,
        Superscript,
        Link,
        List,
        ListProperties,
        TodoList,
        TextPartLanguage,
        BlockQuote,
        Image,
        ImageCaption,
        ImageStyle,
        ImageToolbar,
        ImageUtils,
        ImageResize,
        ImageCustomResizeUI,
        MediaLibraryImage,
        VideoPoster,
        CtaHelperInsert,
        VideoWarningInsert,
        ...(ScrollHighlightPlugin ? [ScrollHighlightPlugin] : []),
        Highlight,
        Font,
        Alignment,
        Undo,
        Table,
        TableToolbar,
        TableCaption,
        TableProperties,
        TableCellProperties,
        MediaEmbed,
        ...(typeof MediaEmbedToolbar === "function" ? [MediaEmbedToolbar] : []),
        HtmlEmbed,
        SourceEditing,
        GeneralHtmlSupport,
        HorizontalLine,
        CodeBlock,
        RemoveFormat,
        Indent,
        IndentBlock,
        FindAndReplace,
        PageBreak,
        PasteFromOffice,
      ],
      toolbar: {
        items: [
          "undo",
          "redo",
          "|",
          "heading",
          "|",
          "bold",
          "italic",
          "underline",
          "strikethrough",
          "subscript",
          "superscript",
          "removeFormat",
          "|",
          "fontColor",
          "fontBackgroundColor",
          "highlight",
          ...(ScrollHighlightPlugin ? ["scrollHighlight"] : []),
          "|",
          "link",
          "mediaLibraryImage",
          "ctaHelper",
          "videoWarning",
          "mediaEmbed",
          "videoPoster",
          "videoPosterRemove",
          "insertTable",
          "htmlEmbed",
          "codeBlock",
          "blockQuote",
          "horizontalLine",
          "pageBreak",
          "|",
          "bulletedList",
          "numberedList",
          "todoList",
          "outdent",
          "indent",
          "alignment",
          "textPartLanguage",
          "|",
          "findAndReplace",
          "sourceEditing",
        ],
        shouldNotGroupWhenFull: false,
      },
      list: {
        properties: {
          styles: true,
          startIndex: true,
          reversed: true,
        },
      },
      heading: {
        options: [
          { model: "paragraph", title: "پاراگراف", class: "ck-heading_paragraph" },
          { model: "heading2", view: "h2", title: "عنوان ۲", class: "ck-heading_heading2" },
          { model: "heading3", view: "h3", title: "عنوان ۳", class: "ck-heading_heading3" },
          { model: "heading4", view: "h4", title: "عنوان ۴", class: "ck-heading_heading4" },
        ],
      },
      highlight: {
        options: [
          { model: "yellowMarker", class: "marker-yellow", title: "هایلایت زرد", color: "var(--ck-highlight-marker-yellow)", type: "marker" },
          { model: "greenMarker", class: "marker-green", title: "هایلایت سبز", color: "var(--ck-highlight-marker-green)", type: "marker" },
          { model: "pinkMarker", class: "marker-pink", title: "هایلایت صورتی", color: "var(--ck-highlight-marker-pink)", type: "marker" },
          { model: "blueMarker", class: "marker-blue", title: "هایلایت آبی", color: "var(--ck-highlight-marker-blue)", type: "marker" },
        ],
      },
      image: {
        toolbar: [
          "imageTextAlternative",
          "toggleImageCaption",
          "|",
          "imageStyle:inline",
          "imageStyle:block",
          "imageStyle:side",
          "|",
          "resizeImage",
          "|",
          "editImage",
          "replaceImage",
          "deleteImage",
        ],
        resizeUnit: "%",
        resizeOptions: [
          { name: "resizeImage:original", value: null, label: "اندازه اصلی" },
          { name: "resizeImage:25", value: "25", label: "۲۵٪" },
          { name: "resizeImage:50", value: "50", label: "۵۰٪" },
          { name: "resizeImage:75", value: "75", label: "۷۵٪" },
          { name: "resizeImage:custom", value: "custom", label: "سفارشی" },
        ],
      },
      table: {
        contentToolbar: [
          "tableColumn",
          "tableRow",
          "mergeTableCells",
          "tableProperties",
          "tableCellProperties",
          "toggleTableCaption",
        ],
      },
      mediaEmbed: {
        previewsInData: true,
        toolbar: ["videoPoster", "videoPosterRemove"],
        extraProviders: [
          {
            // Direct video files (S3 / CDN / Arvan / etc.) — protocol+www stripped before match.
            name: "rawVideo",
            url: [
              /^.+\.mp4(?:[?#][^\s]*)?$/i,
              /^.+\.webm(?:[?#][^\s]*)?$/i,
              /^.+\.ogg(?:[?#][^\s]*)?$/i,
              /^.+\.ogv(?:[?#][^\s]*)?$/i,
              /^.+\.m4v(?:[?#][^\s]*)?$/i,
              /^.+\.mov(?:[?#][^\s]*)?$/i,
            ],
            html: function (match) {
              const path = match[0] || "";
              const src = /^https?:\/\//i.test(path) ? path : "https://" + path;
              const safe = escapeAttr(src);
              return (
                '<div style="position:relative;padding-bottom:56.25%;height:0;background:#0f1f22;">' +
                '<video controls playsinline preload="metadata" src="' +
                safe +
                '" ' +
                'style="position:absolute;inset:0;width:100%;height:100%;object-fit:contain;">' +
                "</video></div>"
              );
            },
          },
          {
            name: "aparat",
            url: [
              /^aparat\.com\/v\/([A-Za-z0-9_-]+)/i,
              /^www\.aparat\.com\/v\/([A-Za-z0-9_-]+)/i,
              /^aparat\.com\/video\/video\/embed(?:_box)?\/videohash\/([A-Za-z0-9_-]+)/i,
              /^www\.aparat\.com\/video\/video\/embed(?:_box)?\/videohash\/([A-Za-z0-9_-]+)/i,
            ],
            html: function (match) {
              const hash = match[1];
              const src =
                "https://www.aparat.com/video/video/embed/videohash/" +
                hash +
                "/vt/frame";
              return (
                '<div style="position:relative;padding-bottom:56.25%;height:0;">' +
                '<iframe src="' +
                escapeAttr(src) +
                '" ' +
                'style="position:absolute;inset:0;width:100%;height:100%;border:0;" ' +
                'allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture; fullscreen" ' +
                'allowfullscreen loading="lazy"></iframe></div>'
              );
            },
          },
        ],
      },
      htmlSupport: {
        allow: [
          {
            name: /^(div|section|article|figure|figcaption|iframe|video|audio|source|ul|ol|li|p|span|strong|a|img|h2|h3|h4)$/,
            attributes: {
              dir: true,
              lang: true,
              style: true,
              class: true,
              href: true,
              src: true,
              alt: true,
              title: true,
              loading: true,
              target: true,
              rel: true,
              controls: true,
              playsinline: true,
              preload: true,
              poster: true,
              allow: true,
              allowfullscreen: true,
              "data-oembed-url": true,
              "data-nova-cta": true,
              "data-nova-cta-config": true,
              "data-nova-scroll-highlight": true,
              "data-nova-sh-from": true,
              "data-nova-sh-to": true,
              "data-nova-video-poster": true,
              "data-nova-video-warning": true,
              "data-popup-open": true,
              "data-booking-open": true,
              role: true,
            },
            classes: true,
            styles: true,
          },
        ],
        // Let VideoWarningInsert own these nodes on reload (avoid GHS clash).
        disallow: [
          {
            name: "div",
            classes: ["nova-video-warning", "nova-video-warning__text"],
          },
        ],
      },
      htmlEmbed: {
        showPreviews: true,
      },
    };
  };

  function escapeHtml(text) {
    return String(text)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  /** Plain text: single newlines → <br>, blank lines → new <p>. */
  function plainTextToEditorHtml(text) {
    const normalized = String(text).replace(/\r\n/g, "\n").replace(/\r/g, "\n");
    const blocks = normalized.split(/\n{2,}/);
    return blocks
      .map(function (block) {
        const lines = block.split("\n").map(escapeHtml);
        return "<p>" + lines.join("<br>") + "</p>";
      })
      .join("");
  }

  function collectViewElements(node, out) {
    if (!node) return;
    if (node.is && node.is("element")) {
      out.push(node);
      for (const child of node.getChildren()) {
        collectViewElements(child, out);
      }
      return;
    }
    if (typeof node.getChildren === "function") {
      for (const child of node.getChildren()) {
        collectViewElements(child, out);
      }
    }
  }

  /**
   * Strip inline styles / Word junk from pasted HTML so each line
   * does not keep its own pasted style.
   */
  function sanitizePastedViewFragment(viewFragment, viewDocument) {
    if (!viewFragment || !UpcastWriter) {
      return viewFragment;
    }

    const writer = new UpcastWriter(viewDocument);
    const elements = [];
    collectViewElements(viewFragment, elements);

    elements.forEach(function (el) {
      if (el.hasAttribute("style")) {
        writer.removeAttribute("style", el);
      }

      if (el.hasAttribute("class")) {
        const cls = String(el.getAttribute("class") || "");
        if (/Mso|mso-|Apple-|moz-|Normal|BodyText/i.test(cls)) {
          writer.removeAttribute("class", el);
        }
      }

      ["face", "size", "color", "align"].forEach(function (attr) {
        if (el.hasAttribute(attr)) {
          writer.removeAttribute(attr, el);
        }
      });
    });

    return viewFragment;
  }

  function bindPasteSanitizer(editor) {
    const viewDocument = editor.editing.view.document;
    const clipboardPipeline = editor.plugins.get("ClipboardPipeline");

    // Plain-text paste: single newlines stay as <br>, not a new block per line.
    viewDocument.on(
      "clipboardInput",
      function (evt, data) {
        if (evt.defaultPrevented || !data.dataTransfer) return;

        const html = data.dataTransfer.getData("text/html");
        const text = data.dataTransfer.getData("text/plain");
        if (html || !text) return;

        data.content = editor.data.htmlProcessor.toView(plainTextToEditorHtml(text));
      },
      { priority: "high" }
    );

    if (!clipboardPipeline) return;

    clipboardPipeline.on(
      "inputTransformation",
      function (evt, data) {
        if (!data.content) return;
        data.content = sanitizePastedViewFragment(data.content, viewDocument);
      },
      { priority: "low" }
    );
  }

  textareas.forEach(function (textarea) {
    ClassicEditor.create(textarea, editorConfig())
      .then(function (editor) {
        textarea.ckEditorInstance = editor;
        bindPasteSanitizer(editor);
        textarea.dispatchEvent(new CustomEvent("ckeditor:ready", { bubbles: true }));
        const form = textarea.closest("form");
        if (form) {
          form.addEventListener("submit", function () {
            textarea.value = editor.getData();
          });
        }
      })
      .catch(function (error) {
        console.error("CKEditor init failed", error);
      });
  });
})();
