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
  } = CKEDITOR;

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

      // Double-click a CTA HTML embed to edit it.
      editor.editing.view.document.on("dblclick", function (evt, data) {
        let viewElement = data.target;
        while (viewElement) {
          const modelElement = editor.editing.mapper.toModelElement(viewElement);
          if (
            modelElement &&
            modelElement.is("element", "rawHtml") &&
            isNovaCtaHtml(modelElement.getAttribute("value") || "")
          ) {
            editor.model.change(function (writer) {
              writer.setSelection(modelElement, "on");
            });
            data.preventDefault();
            evt.stop();
            openHelper();
            return;
          }
          viewElement = viewElement.parent;
        }
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
        CtaHelperInsert,
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
          "|",
          "link",
          "mediaLibraryImage",
          "ctaHelper",
          "mediaEmbed",
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
              loading: true,
              target: true,
              rel: true,
              "data-nova-cta": true,
              "data-nova-cta-config": true,
            },
            classes: true,
            styles: true,
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
