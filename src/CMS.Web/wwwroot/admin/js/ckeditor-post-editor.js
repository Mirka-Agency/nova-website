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

  const ctaHelperIcon =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">' +
    '<path d="M3.2 4.2h13.6c.66 0 1.2.54 1.2 1.2v4.2c0 .66-.54 1.2-1.2 1.2H11l-2.2 2.6c-.3.36-.9.14-.9-.3V10.8H3.2c-.66 0-1.2-.54-1.2-1.2V5.4c0-.66.54-1.2 1.2-1.2zm1.1 2.1v1.8h5.4V6.3H4.3zm7.2 0v1.8h4.1V6.3h-4.1zM3.2 12.6h7.4c.66 0 1.2.54 1.2 1.2v2c0 .66-.54 1.2-1.2 1.2H3.2c-.66 0-1.2-.54-1.2-1.2v-2c0-.66.54-1.2 1.2-1.2z"/>' +
    "</svg>";

  const CTA_HELPER_MESSAGE = "nova-cta-helper-insert";

  function insertCtaHtml(editor, html) {
    if (!html) return;

    try {
      editor.model.change(function (writer) {
        const embed = writer.createElement("rawHtml", { value: html });
        editor.model.insertContent(embed);
      });
      editor.editing.view.focus();
      return;
    } catch (err) {
      console.warn("rawHtml insert failed, falling back to htmlEmbed command", err);
    }

    try {
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

      const onMessage = function (event) {
        if (event.origin !== window.location.origin) return;
        const data = event.data;
        if (!data || data.type !== CTA_HELPER_MESSAGE || typeof data.html !== "string") return;
        insertCtaHtml(editor, data.html);
        if (helperWindow && !helperWindow.closed) {
          helperWindow.focus();
        }
      };

      window.addEventListener("message", onMessage);

      editor.on("destroy", function () {
        window.removeEventListener("message", onMessage);
      });

      editor.ui.componentFactory.add("ctaHelper", function (locale) {
        const view = new ButtonView(locale);

        view.set({
          label: "درج CTA",
          icon: ctaHelperIcon,
          tooltip: true,
        });

        view.on("execute", function () {
          const url = "/Admin/CtaHelper?embed=1";
          if (helperWindow && !helperWindow.closed) {
            helperWindow.focus();
            return;
          }
          helperWindow = window.open(
            url,
            "novaCtaHelper",
            "popup=yes,width=1100,height=860,scrollbars=yes,resizable=yes"
          );
        });

        return view;
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
          "|",
          "imageStyle:inline",
          "imageStyle:block",
          "imageStyle:side",
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
