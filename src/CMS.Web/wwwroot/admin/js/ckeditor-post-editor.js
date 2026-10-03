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
    TodoList,
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
  } = CKEDITOR;

  const mediaLibraryImageIcon =
    '<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">' +
    '<path d="M6.91 10.54c.26-.23.64-.21.88.03l3.36 3.14 2.23-2.06a.64.64 0 0 1 .87 0l2.52 2.97V4.5H3.2v10.12l3.71-4.08zm10.27-7.51c.6 0 1.09.47 1.09 1.05v11.84c0 .59-.49 1.06-1.09 1.06H2.83c-.6 0-1.09-.47-1.09-1.06V4.08c0-.58.49-1.05 1.1-1.05h14.34zm-5.22 5.56a1.96 1.96 0 1 1 3.4-1.96 1.96 1.96 0 0 1-3.4 1.96z"/>' +
    "</svg>";

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
      language: "fa",
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
        TodoList,
        BlockQuote,
        Image,
        ImageCaption,
        ImageStyle,
        ImageToolbar,
        ImageUtils,
        MediaLibraryImage,
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
          "|",
          "findAndReplace",
          "sourceEditing",
        ],
        shouldNotGroupWhenFull: false,
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
            name: /.*/,
            attributes: true,
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

  textareas.forEach(function (textarea) {
    ClassicEditor.create(textarea, editorConfig())
      .then(function (editor) {
        textarea.ckEditorInstance = editor;
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
