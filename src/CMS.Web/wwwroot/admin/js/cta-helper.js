(function () {
  "use strict";

  var root = document.querySelector("[data-cta-helper]");
  if (!root) return;

  var MSG_TYPE = "nova-cta-helper-insert";
  var MSG_READY = "nova-cta-helper-ready";
  var MSG_EDIT_CONTEXT = "nova-cta-helper-edit-context";
  var MAX_BUTTONS = 4;
  var editing = false;

  var TYPE_TO_SECTION = {
    appointment: "cta-appointment",
    consultation: "cta-consultation",
    doctor: "cta-doctor",
    surgery: "cta-surgery",
    articleEnd: "cta-article-end",
    contact: "cta-contact",
    inline: "cta-inline",
    highlight: "cta-highlight"
  };

  var STYLE_LABELS = {
    primary: "اصلی",
    secondary: "ثانویه",
    light: "روشن (پس‌زمینه تیره)",
    outline: "حاشیه‌دار (پس‌زمینه تیره)",
    compact: "فشرده",
    phone: "تلفن"
  };

  function esc(value) {
    return String(value == null ? "" : value)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function telDigits(phone) {
    var persianMap = {
      "۰": "0",
      "۱": "1",
      "۲": "2",
      "۳": "3",
      "۴": "4",
      "۵": "5",
      "۶": "6",
      "۷": "7",
      "۸": "8",
      "۹": "9"
    };
    var normalized = String(phone == null ? "" : phone).replace(/[۰-۹]/g, function (ch) {
      return persianMap[ch] || ch;
    });
    var digits = normalized.replace(/[^\d+]/g, "");
    return digits || normalized;
  }

  function parsePopupHref(href) {
    var raw = String(href == null ? "" : href).trim();
    if (!raw) return null;

    var lower = raw.toLowerCase();
    if (lower.indexOf("popup:") === 0) {
      var slug = raw.slice(6).trim();
      return slug || "booking";
    }
    if (lower === "#booking" || lower === "booking") return "booking";
    return null;
  }

  function btnOpenAttrs(href) {
    var popupSlug = parsePopupHref(href);
    if (popupSlug) {
      var attrs =
        ' href="#"' +
        ' data-popup-open="' +
        esc(popupSlug) +
        '" role="button"';
      if (popupSlug.toLowerCase() === "booking") {
        attrs += " data-booking-open";
      }
      return attrs;
    }
    return ' href="' + esc(href) + '"';
  }

  function actionsRow(innerHtml, count, extraStyle) {
    if (!innerHtml) return "";
    var n = Math.max(0, count || 0);
    var wrap = n >= 3 ? "wrap" : "nowrap";
    var style =
      "display:flex;flex-wrap:" +
      wrap +
      ";align-items:stretch;gap:.55rem;width:100%;" +
      (extraStyle || "");
    return (
      '<div class="nova-cta__actions" data-count="' +
      esc(String(n)) +
      '" style="' +
      style +
      '">' +
      innerHtml +
      "</div>"
    );
  }

  function btnPrimary(href, text) {
    return (
      "<a" +
      btnOpenAttrs(href) +
      ' class="nova-cta__btn" style="display:inline-flex;flex:1 1 8.5rem;align-items:center;justify-content:center;gap:.4rem;min-width:min(100%,8.5rem);min-height:44px;padding:.65rem 1rem;border-radius:999px;background:linear-gradient(135deg,#0a8491 0%,#09707d 55%,#065a64 100%);color:#fff;font-weight:700;font-size:.88rem;text-decoration:none;border:1.5px solid transparent;line-height:1.15;text-align:center;box-shadow:0 8px 18px rgba(9,112,125,.22);box-sizing:border-box;">' +
      esc(text) +
      "</a>"
    );
  }

  function btnSecondary(href, text) {
    return (
      "<a" +
      btnOpenAttrs(href) +
      ' class="nova-cta__btn" style="display:inline-flex;flex:1 1 8.5rem;align-items:center;justify-content:center;gap:.4rem;min-width:min(100%,8.5rem);min-height:44px;padding:.65rem 1rem;border-radius:999px;background:#e6f3f5;color:#065a64;font-weight:700;font-size:.88rem;text-decoration:none;border:1.5px solid rgba(9,112,125,.16);line-height:1.15;text-align:center;box-sizing:border-box;">' +
      esc(text) +
      "</a>"
    );
  }

  function btnLight(href, text) {
    return (
      "<a" +
      btnOpenAttrs(href) +
      ' class="nova-cta__btn" style="display:inline-flex;flex:1 1 8.5rem;align-items:center;justify-content:center;gap:.4rem;min-width:min(100%,8.5rem);min-height:44px;padding:.65rem 1rem;border-radius:999px;background:#fff;color:#065a64;font-weight:700;font-size:.88rem;text-decoration:none;border:1.5px solid rgba(255,255,255,.95);line-height:1.15;text-align:center;box-shadow:0 8px 18px rgba(0,0,0,.12);box-sizing:border-box;">' +
      esc(text) +
      "</a>"
    );
  }

  function btnOutlineOnDark(href, text) {
    return (
      "<a" +
      btnOpenAttrs(href) +
      ' class="nova-cta__btn" style="display:inline-flex;flex:1 1 8.5rem;align-items:center;justify-content:center;gap:.4rem;min-width:min(100%,8.5rem);min-height:44px;padding:.65rem 1rem;border-radius:999px;background:rgba(255,255,255,.08);color:#fff;font-weight:700;font-size:.88rem;text-decoration:none;border:1.5px solid rgba(255,255,255,.5);line-height:1.15;text-align:center;backdrop-filter:blur(4px);box-sizing:border-box;">' +
      esc(text) +
      "</a>"
    );
  }

  function btnCompact(href, text) {
    return (
      "<a" +
      btnOpenAttrs(href) +
      ' class="nova-cta__btn" style="display:inline-flex;flex:1 1 8rem;align-items:center;justify-content:center;min-width:min(100%,8rem);min-height:42px;padding:.5rem .9rem;border-radius:999px;background:linear-gradient(135deg,#0a8491,#09707d);color:#fff;font-weight:700;font-size:.85rem;text-decoration:none;text-align:center;box-shadow:0 6px 14px rgba(9,112,125,.2);box-sizing:border-box;">' +
      esc(text) +
      "</a>"
    );
  }

  function btnPhone(hrefOrNumber, label) {
    var raw = String(hrefOrNumber == null ? "" : hrefOrNumber).trim();
    var display = raw.replace(/^tel:/i, "");
    var digits = telDigits(display);
    return (
      '<a href="tel:' +
      esc(digits) +
      '" dir="ltr" class="nova-cta__btn nova-cta__phone" style="display:inline-flex;flex:1.15 1 9.5rem;align-items:center;justify-content:center;gap:.35rem;min-width:min(100%,9.5rem);min-height:44px;padding:.45rem .7rem;border-radius:999px;background:#e6f3f5;color:#065a64;font-weight:700;text-decoration:none;direction:ltr;unicode-bidi:isolate;box-sizing:border-box;">' +
      '<span style="display:inline-grid;place-items:center;flex:0 0 auto;width:1.7rem;height:1.7rem;border-radius:999px;background:#09707d;color:#fff;font-size:.75rem;" aria-hidden="true">☎</span>' +
      '<span style="min-width:0;text-align:start;">' +
      '<span style="display:block;font-size:.62rem;color:#6b7c7e;font-weight:700;line-height:1.15;">' +
      esc(label || "تماس") +
      '</span><span class="phone-ltr" style="display:block;font-size:.84rem;line-height:1.2;white-space:nowrap;letter-spacing:0;">' +
      esc(display) +
      "</span></span></a>"
    );
  }

  function hasText(value) {
    return !!(value && String(value).trim());
  }

  function toFaDigits(value) {
    return String(value).replace(/\d/g, function (d) {
      return "۰۱۲۳۴۵۶۷۸۹"[Number(d)];
    });
  }

  function normalizeButtons(type, data) {
    if (Array.isArray(data.buttons) && data.buttons.length) {
      return data.buttons
        .map(function (btn) {
          return {
            text: btn && btn.text != null ? String(btn.text) : "",
            url: btn && btn.url != null ? String(btn.url) : "",
            style: btn && btn.style ? String(btn.style) : "primary"
          };
        })
        .filter(function (btn) {
          return hasText(btn.text) || hasText(btn.url);
        })
        .slice(0, MAX_BUTTONS);
    }

    return legacyButtons(type, data);
  }

  function legacyButtons(type, data) {
    switch (type) {
      case "appointment":
        return [
          { text: data.primaryText || "رزرو نوبت", url: data.primaryUrl || "", style: "light" },
          hasText(data.secondaryText)
            ? { text: data.secondaryText, url: data.secondaryUrl || "", style: "outline" }
            : null
        ].filter(Boolean);
      case "consultation":
        return [
          { text: data.buttonText || "درخواست مشاوره", url: data.buttonUrl || "", style: "primary" },
          {
            text: data.phoneLabel || "مشاوره تلفنی",
            url: data.phoneNumber ? "tel:" + telDigits(data.phoneNumber) : "",
            style: "phone"
          }
        ];
      case "doctor":
        return [
          { text: data.profileText || "مشاهده پروفایل", url: data.profileUrl || "", style: "primary" },
          {
            text: data.appointmentText || "رزرو نوبت",
            url: data.appointmentUrl || "",
            style: "secondary"
          }
        ];
      case "surgery":
        return [
          { text: data.buttonText || "مشاهده خدمت", url: data.buttonUrl || "", style: "primary" },
          hasText(data.secondaryText)
            ? { text: data.secondaryText, url: data.secondaryUrl || "", style: "secondary" }
            : null
        ].filter(Boolean);
      case "articleEnd":
        return [
          {
            text: data.appointmentText || "رزرو نوبت",
            url: data.appointmentUrl || "",
            style: "primary"
          },
          {
            text: data.consultationText || "درخواست مشاوره",
            url: data.consultationUrl || "",
            style: "secondary"
          }
        ];
      case "contact":
        return [
          {
            text: "تماس",
            url: data.phone ? "tel:" + telDigits(data.phone) : "",
            style: "phone"
          },
          {
            text: data.whatsappText || "واتساپ / تماس",
            url: data.whatsappUrl || "",
            style: "secondary"
          },
          {
            text: data.appointmentText || "رزرو نوبت",
            url: data.appointmentUrl || "",
            style: "compact"
          }
        ];
      case "inline":
        return [
          { text: data.buttonText || "رزرو نوبت", url: data.buttonUrl || "", style: "compact" }
        ];
      case "highlight":
        return [
          { text: data.primaryText || "رزرو نوبت", url: data.primaryUrl || "", style: "primary" },
          hasText(data.secondaryText)
            ? { text: data.secondaryText, url: data.secondaryUrl || "", style: "secondary" }
            : null
        ].filter(Boolean);
      default:
        return [];
    }
  }

  function renderButton(btn, index) {
    var style = btn.style || (index === 0 ? "primary" : "secondary");
    var text = btn.text || "";
    var url = btn.url || "#";

    if (style === "phone") {
      return btnPhone(url, text);
    }
    if (style === "light") return btnLight(url, text);
    if (style === "outline") return btnOutlineOnDark(url, text);
    if (style === "compact") return btnCompact(url, text);
    if (style === "secondary") return btnSecondary(url, text);
    return btnPrimary(url, text);
  }

  function renderActions(buttons, extraStyle) {
    var list = buttons || [];
    var html = list
      .map(function (btn, index) {
        return renderButton(btn, index);
      })
      .join("");
    return actionsRow(html, list.length, extraStyle);
  }

  var builders = {
    appointment: function (d) {
      var buttons = normalizeButtons("appointment", d);
      return (
        '<div class="nova-cta nova-cta--appointment" style="position:relative;margin:1.75rem 0;padding:clamp(1.5rem,3vw,2.15rem);border-radius:24px;overflow:hidden;background:linear-gradient(135deg,#0a555e 0%,#09707d 55%,#0a8491 100%);color:#fff;box-shadow:0 14px 34px rgba(9,112,125,.18);font-family:Peyda,Tahoma,sans-serif;">' +
        '<div style="position:absolute;inset-block-start:-3rem;inset-inline-end:-2.5rem;width:10rem;height:10rem;border-radius:50%;border:1px solid rgba(255,255,255,.12);box-shadow:0 0 0 2.5rem rgba(255,255,255,.04);pointer-events:none;"></div>' +
        '<div style="position:relative;z-index:1;">' +
        '<div style="display:inline-flex;align-items:center;font-size:.82rem;font-weight:700;background:rgba(255,255,255,.16);padding:.3rem .9rem;border-radius:999px;margin-bottom:.9rem;border:1px solid rgba(255,255,255,.18);">' +
        esc(d.eyebrow) +
        "</div>" +
        '<h3 style="margin:0 0 .55rem;color:#fff;font-weight:800;font-size:clamp(1.2rem,2.4vw,1.5rem);line-height:1.35;letter-spacing:-.02em;">' +
        esc(d.title) +
        "</h3>" +
        '<p style="margin:0 0 1.25rem;max-width:36rem;color:rgba(255,255,255,.9);line-height:1.8;font-size:1.02rem;">' +
        esc(d.description) +
        "</p>" +
        renderActions(buttons) +
        "</div></div>"
      );
    },

    consultation: function (d) {
      var buttons = normalizeButtons("consultation", d);
      return (
        '<div class="nova-cta nova-cta--consultation" style="margin:1.75rem 0;padding:1.45rem 1.5rem;border-radius:22px;background:linear-gradient(180deg,#fff 0%,#f7fafa 100%);border:1px solid rgba(9,112,125,.14);box-shadow:0 10px 28px rgba(9,112,125,.08);font-family:Peyda,Tahoma,sans-serif;">' +
        '<h3 style="margin:0 0 .5rem;font-size:1.18rem;font-weight:800;color:#1a3a3f;line-height:1.4;letter-spacing:-.015em;">' +
        esc(d.title) +
        "</h3>" +
        '<p style="margin:0 0 1.1rem;color:#2c3e40;line-height:1.8;">' +
        esc(d.description) +
        "</p>" +
        renderActions(buttons) +
        "</div>"
      );
    },

    doctor: function (d) {
      var buttons = normalizeButtons("doctor", d);
      return (
        '<div class="nova-cta nova-cta--doctor" style="margin:1.75rem 0;display:flex;flex-wrap:wrap;align-items:center;gap:1.2rem;padding:1.2rem;border-radius:22px;background:linear-gradient(180deg,#fff 0%,#f7fafa 100%);border:1px solid rgba(9,112,125,.14);box-shadow:0 12px 30px rgba(9,112,125,.1);font-family:Peyda,Tahoma,sans-serif;">' +
        '<div style="flex:0 0 128px;width:128px;border-radius:18px;overflow:hidden;background:#e6f3f5;aspect-ratio:3/4;box-shadow:0 8px 20px rgba(9,112,125,.16);">' +
        '<img src="' +
        esc(d.image) +
        '" alt="' +
        esc(d.name) +
        '" loading="lazy" style="width:100%;height:100%;object-fit:cover;display:block;" />' +
        "</div>" +
        '<div style="min-width:0;flex:1 1 16rem;">' +
        '<div style="display:inline-flex;margin-bottom:.5rem;font-size:.78rem;font-weight:700;color:#065a64;background:#e6f3f5;padding:.2rem .75rem;border-radius:999px;border:1px solid rgba(9,112,125,.12);">' +
        esc(d.specialty) +
        "</div>" +
        '<h3 style="margin:0 0 .4rem;font-size:1.2rem;font-weight:800;color:#1a3a3f;letter-spacing:-.015em;">' +
        esc(d.name) +
        "</h3>" +
        '<p style="margin:0 0 1rem;color:#2c3e40;line-height:1.75;font-size:.95rem;">' +
        esc(d.description) +
        "</p>" +
        renderActions(buttons) +
        "</div></div>"
      );
    },

    surgery: function (d) {
      var buttons = normalizeButtons("surgery", d);
      return (
        '<div class="nova-cta nova-cta--surgery" style="margin:1.75rem 0;display:grid;grid-template-columns:minmax(0,1fr);gap:0;border-radius:22px;overflow:hidden;border:1px solid rgba(9,112,125,.14);background:#fff;box-shadow:0 12px 30px rgba(9,112,125,.08);font-family:Peyda,Tahoma,sans-serif;">' +
        '<div style="padding:1.4rem 1.5rem;background:linear-gradient(120deg,#e6f3f5 0%,#f7fafa 55%,#fff 100%);border-bottom:1px solid rgba(9,112,125,.1);">' +
        '<span style="display:inline-flex;margin-bottom:.6rem;font-size:.75rem;font-weight:700;color:#065a64;background:rgba(255,255,255,.9);border:1px solid rgba(9,112,125,.14);padding:.2rem .75rem;border-radius:999px;">' +
        esc(d.badge) +
        "</span>" +
        '<h3 style="margin:0 0 .45rem;font-size:1.22rem;font-weight:800;color:#1a3a3f;letter-spacing:-.015em;">' +
        esc(d.title) +
        "</h3>" +
        '<p style="margin:0;color:#2c3e40;line-height:1.8;">' +
        esc(d.description) +
        "</p></div>" +
        '<div style="display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:.9rem;padding:1.15rem 1.5rem;background:#fff;">' +
        '<div style="min-width:0;"><strong style="display:block;color:#09707d;font-size:.8rem;margin-bottom:.2rem;letter-spacing:.02em;">خدمت مرتبط</strong>' +
        '<span style="color:#1a3a3f;font-weight:800;">' +
        esc(d.serviceName) +
        "</span></div>" +
        renderActions(buttons, "flex:1 1 14rem;") +
        "</div></div>"
      );
    },

    articleEnd: function (d) {
      var buttons = normalizeButtons("articleEnd", d);
      return (
        '<div class="nova-cta nova-cta--article-end" style="margin:2rem 0 1.5rem;padding:1.7rem 1.5rem;border-radius:24px;background:linear-gradient(180deg,#f7fafa 0%,#eef6f7 100%);border:1px solid rgba(9,112,125,.14);text-align:center;box-shadow:0 12px 30px rgba(9,112,125,.08);font-family:Peyda,Tahoma,sans-serif;">' +
        '<h3 style="margin:0 0 .55rem;font-size:1.28rem;font-weight:800;color:#1a3a3f;letter-spacing:-.02em;">' +
        esc(d.title) +
        "</h3>" +
        '<p style="margin:0 auto 1.25rem;max-width:34rem;color:#2c3e40;line-height:1.8;">' +
        esc(d.description) +
        "</p>" +
        renderActions(buttons, "justify-content:center;max-width:28rem;margin-inline:auto;") +
        "</div>"
      );
    },

    contact: function (d) {
      var buttons = normalizeButtons("contact", d);
      return (
        '<div class="nova-cta nova-cta--contact" style="margin:1.75rem 0;display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:1rem;padding:1.2rem 1.35rem;border-radius:20px;background:linear-gradient(180deg,#fff 0%,#f7fafa 100%);border:1px solid rgba(9,112,125,.14);box-shadow:0 10px 26px rgba(9,112,125,.08);font-family:Peyda,Tahoma,sans-serif;">' +
        '<div style="min-width:0;flex:1 1 16rem;">' +
        '<strong style="display:block;color:#1a3a3f;font-size:1.08rem;margin-bottom:.3rem;letter-spacing:-.01em;">' +
        esc(d.title) +
        "</strong>" +
        '<span style="color:#2c3e40;line-height:1.7;">' +
        esc(d.description) +
        "</span></div>" +
        renderActions(buttons, "flex:1 1 16rem;") +
        "</div>"
      );
    },

    inline: function (d) {
      var buttons = normalizeButtons("inline", d);
      var wrap = buttons.length > 1 ? "wrap" : "nowrap";
      var actionsHtml;
      if (buttons.length <= 1) {
        var only = buttons[0] || { text: "", url: "#", style: "compact" };
        actionsHtml = renderButton(only, 0)
          .replace("flex:1 1 8.5rem;", "flex:0 0 auto;")
          .replace("flex:1 1 8rem;", "flex:0 0 auto;")
          .replace("min-width:min(100%,8.5rem);", "min-width:0;")
          .replace("min-width:min(100%,8rem);", "min-width:0;");
      } else {
        actionsHtml = renderActions(buttons, "flex:1 1 12rem;");
      }
      return (
        '<div class="nova-cta nova-cta--inline" data-btn-count="' +
        esc(String(buttons.length)) +
        '" style="margin:1.35rem 0;display:flex;flex-wrap:' +
        wrap +
        ";align-items:center;justify-content:space-between;gap:.75rem;padding:1rem 1.1rem;border-radius:16px;background:linear-gradient(120deg,#e6f3f5 0%,#f3fafb 100%);border:1px solid rgba(9,112,125,.16);box-shadow:0 6px 16px rgba(9,112,125,.06);font-family:Peyda,Tahoma,sans-serif;\">" +
        '<div style="min-width:0;flex:1 1 12rem;">' +
        '<strong style="display:block;color:#065a64;font-size:.98rem;margin-bottom:.2rem;">' +
        esc(d.title) +
        "</strong>" +
        '<span style="color:#2c3e40;font-size:.9rem;line-height:1.65;">' +
        esc(d.description) +
        "</span></div>" +
        actionsHtml +
        "</div>"
      );
    },

    highlight: function (d) {
      var buttons = normalizeButtons("highlight", d);
      return (
        '<div class="nova-cta nova-cta--highlight" style="margin:1.75rem 0;padding:1.45rem 1.5rem;border-radius:22px;background:linear-gradient(180deg,#fff 0%,#fcfaf5 100%);border:1px solid rgba(196,163,90,.28);border-inline-start:4px solid #c4a35a;box-shadow:0 12px 30px rgba(9,112,125,.08);font-family:Peyda,Tahoma,sans-serif;">' +
        '<span style="display:inline-flex;margin-bottom:.6rem;font-size:.75rem;font-weight:700;color:#8a6d2f;background:rgba(196,163,90,.18);padding:.2rem .75rem;border-radius:999px;border:1px solid rgba(196,163,90,.22);">' +
        esc(d.badge) +
        "</span>" +
        '<h3 style="margin:0 0 .5rem;font-size:1.22rem;font-weight:800;color:#1a3a3f;letter-spacing:-.015em;">' +
        esc(d.title) +
        "</h3>" +
        '<p style="margin:0 0 1.1rem;color:#2c3e40;line-height:1.8;">' +
        esc(d.description) +
        "</p>" +
        renderActions(buttons) +
        "</div>"
      );
    }
  };

  function encodeConfig(data) {
    try {
      return btoa(unescape(encodeURIComponent(JSON.stringify(data))));
    } catch (err) {
      return "";
    }
  }

  function decodeConfig(value) {
    if (!value) return null;
    try {
      return JSON.parse(decodeURIComponent(escape(atob(value))));
    } catch (err) {
      try {
        return JSON.parse(atob(value));
      } catch (err2) {
        return null;
      }
    }
  }

  function wrapCtaHtml(type, data, html) {
    var config = encodeConfig(data);
    var attrs =
      ' data-nova-cta="' +
      esc(type) +
      '" data-nova-cta-config="' +
      esc(config) +
      '"';

    if (/^<div\b/i.test(html)) {
      return html.replace(/^<div\b/i, "<div" + attrs);
    }

    return (
      '<div class="nova-cta-wrap"' +
      attrs +
      ' style="margin:1.75rem 0;">' +
      html +
      "</div>"
    );
  }

  function styleOptionsHtml(selected) {
    return Object.keys(STYLE_LABELS)
      .map(function (key) {
        return (
          '<option value="' +
          esc(key) +
          '"' +
          (key === selected ? " selected" : "") +
          ">" +
          esc(STYLE_LABELS[key]) +
          "</option>"
        );
      })
      .join("");
  }

  function createButtonRow(btn, index) {
    var item = btn || { text: "", url: "", style: "primary" };
    var row = document.createElement("div");
    row.className = "cta-mod__btn-row";
    row.setAttribute("data-cta-btn-row", "");
    row.innerHTML =
      '<div class="cta-mod__btn-row-head">' +
      '<span class="cta-mod__btn-row-title">دکمه ' +
      esc(toFaDigits(index + 1)) +
      "</span>" +
      '<button type="button" class="btn btn-ghost cta-mod__btn-remove" data-cta-remove-btn>حذف</button>' +
      "</div>" +
      '<div class="cta-mod__btn-fields">' +
      "<div>" +
      "<label>متن دکمه</label>" +
      '<input type="text" data-btn-text value="' +
      esc(item.text || "") +
      '" />' +
      "</div>" +
      "<div>" +
      '<label>لینک <span class="admin-hint">(popup:booking = پاپ‌آپ رزرو)</span></label>' +
      '<input type="text" data-btn-url dir="ltr" value="' +
      esc(item.url || "") +
      '" />' +
      "</div>" +
      "<div>" +
      "<label>سبک</label>" +
      '<select data-btn-style>' +
      styleOptionsHtml(item.style || "primary") +
      "</select>" +
      "</div>" +
      "</div>";
    return row;
  }

  function getButtonsList(section) {
    return section.querySelector("[data-cta-buttons]");
  }

  function syncButtonUi(section) {
    ensureButtonMeta(section);
    var list = getButtonsList(section);
    if (!list) return;

    var rows = list.querySelectorAll("[data-cta-btn-row]");
    var count = rows.length;
    list.setAttribute("data-count", String(count));
    list.classList.toggle("is-empty", count === 0);
    list.hidden = count === 0;

    rows.forEach(function (row, index) {
      var title = row.querySelector(".cta-mod__btn-row-title");
      if (title) title.textContent = "دکمه " + toFaDigits(index + 1);
      var removeBtn = row.querySelector("[data-cta-remove-btn]");
      if (removeBtn) removeBtn.disabled = false;
      row.style.setProperty("--btn-index", String(index));
    });

    var addBtn = section.querySelector("[data-cta-add-btn]");
    if (addBtn) {
      addBtn.disabled = count >= MAX_BUTTONS;
      addBtn.setAttribute(
        "aria-label",
        count >= MAX_BUTTONS
          ? "حداکثر تعداد دکمه رسیده است"
          : "افزودن دکمه"
      );
    }

    var meta = section.querySelector("[data-cta-btn-meta]");
    if (meta) {
      meta.textContent = toFaDigits(count + " از " + MAX_BUTTONS + " دکمه");
    }

    var empty = section.querySelector("[data-cta-btn-empty]");
    if (empty) {
      empty.hidden = count > 0;
    }
  }

  function ensureButtonMeta(section) {
    var headText = section.querySelector(".cta-mod__buttons-head > div");
    if (!headText) return;
    if (!headText.querySelector("[data-cta-btn-meta]")) {
      var meta = document.createElement("p");
      meta.className = "cta-mod__btn-meta";
      meta.setAttribute("data-cta-btn-meta", "");
      headText.appendChild(meta);
    }
    var panel = section.querySelector(".cta-mod__buttons-panel");
    if (panel && !panel.querySelector("[data-cta-btn-empty]")) {
      var empty = document.createElement("div");
      empty.className = "cta-mod__btn-empty";
      empty.setAttribute("data-cta-btn-empty", "");
      empty.hidden = true;
      empty.innerHTML =
        "<strong>هنوز دکمه‌ای نیست</strong><span>با «افزودن دکمه» اولین دکمه را بسازید.</span>";
      var list = getButtonsList(section);
      if (list) panel.insertBefore(empty, list);
    }
  }

  function setButtons(section, buttons) {
    var list = getButtonsList(section);
    if (!list) return;

    list.innerHTML = "";
    var items = Array.isArray(buttons) ? buttons.slice(0, MAX_BUTTONS) : [];
    items.forEach(function (btn, index) {
      list.appendChild(createButtonRow(btn, index));
    });
    syncButtonUi(section);
    bindButtonListEvents(section);
  }

  function readButtonsFromDom(section) {
    var list = getButtonsList(section);
    if (!list) return [];
    var buttons = [];
    list.querySelectorAll("[data-cta-btn-row]").forEach(function (row) {
      var textEl = row.querySelector("[data-btn-text]");
      var urlEl = row.querySelector("[data-btn-url]");
      var styleEl = row.querySelector("[data-btn-style]");
      buttons.push({
        text: textEl ? textEl.value : "",
        url: urlEl ? urlEl.value : "",
        style: styleEl ? styleEl.value : "primary"
      });
    });
    return buttons;
  }

  function getData(section) {
    var data = {};
    section.querySelectorAll("[data-key]").forEach(function (el) {
      data[el.getAttribute("data-key")] = el.value;
    });
    data.buttons = readButtonsFromDom(section);
    return data;
  }

  function bindButtonListEvents(section) {
    var list = getButtonsList(section);
    if (!list || list.getAttribute("data-bound") === "1") return;
    list.setAttribute("data-bound", "1");

    list.addEventListener("input", function () {
      render(section);
    });
    list.addEventListener("change", function () {
      render(section);
    });
    list.addEventListener("click", function (event) {
      var removeBtn = event.target.closest("[data-cta-remove-btn]");
      if (!removeBtn || !list.contains(removeBtn)) return;
      var row = removeBtn.closest("[data-cta-btn-row]");
      if (!row) return;
      var rows = list.querySelectorAll("[data-cta-btn-row]");
      if (rows.length <= 0) return;
      row.remove();
      syncButtonUi(section);
      render(section);
    });
  }

  function bindAddButton(section) {
    var addBtn = section.querySelector("[data-cta-add-btn]");
    if (!addBtn || addBtn.getAttribute("data-bound") === "1") return;
    addBtn.setAttribute("data-bound", "1");
    addBtn.addEventListener("click", function () {
      var list = getButtonsList(section);
      if (!list) return;
      var count = list.querySelectorAll("[data-cta-btn-row]").length;
      if (count >= MAX_BUTTONS) return;
      var defaultStyle = list.getAttribute("data-default-style") || "secondary";
      list.appendChild(
        createButtonRow(
          {
            text: "",
            url: "",
            style: defaultStyle
          },
          count
        )
      );
      syncButtonUi(section);
      render(section);
    });
  }

  function render(section) {
    var type = section.getAttribute("data-module");
    var builder = builders[type];
    if (!builder) return;
    var data = getData(section);
    var html = wrapCtaHtml(type, data, builder(data));
    section.querySelector("[data-preview]").innerHTML = html;
    section.querySelector("[data-code]").value = html;
  }

  function setEditMode(on) {
    editing = !!on;
    root.classList.toggle("is-editing", editing);

    var banner = root.querySelector("[data-cta-edit-banner]");
    if (banner) banner.hidden = !editing;

    root.querySelectorAll("[data-insert]").forEach(function (btn) {
      btn.textContent = editing ? "به‌روزرسانی در ادیتور" : "درج در ادیتور";
    });

    root.querySelectorAll("[data-insert-new]").forEach(function (btn) {
      btn.hidden = !editing;
    });
  }

  function applyEditHtml(html) {
    if (!html) {
      setEditMode(false);
      return false;
    }

    var doc = new DOMParser().parseFromString(html, "text/html");
    var el = doc.querySelector("[data-nova-cta]");
    if (!el) {
      setEditMode(false);
      return false;
    }

    var type = el.getAttribute("data-nova-cta");
    var sectionId = TYPE_TO_SECTION[type];
    if (!sectionId) {
      setEditMode(false);
      return false;
    }

    var data = decodeConfig(el.getAttribute("data-nova-cta-config")) || {};
    showModule(sectionId, { skipScroll: true });

    var section = document.getElementById(sectionId);
    if (!section) {
      setEditMode(false);
      return false;
    }

    Object.keys(data).forEach(function (key) {
      if (key === "buttons") return;
      var input = section.querySelector('[data-key="' + key + '"]');
      if (input) input.value = data[key] == null ? "" : String(data[key]);
    });

    setButtons(section, normalizeButtons(type, data));

    section.querySelectorAll("[data-media-preview-for]").forEach(function (preview) {
      var selector = preview.getAttribute("data-media-preview-for");
      var input = selector ? section.querySelector(selector) : null;
      var url = input ? String(input.value || "").trim() : "";
      preview.innerHTML = url ? '<img src="' + esc(url) + '" alt="" />' : "";
    });

    render(section);
    setEditMode(true);

    var picker = root.querySelector("[data-cta-picker]");
    if (picker) {
      picker.scrollIntoView({ behavior: "smooth", block: "start" });
    }
    return true;
  }

  function copyText(text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      return navigator.clipboard.writeText(text);
    }
    return new Promise(function (resolve, reject) {
      var ta = document.createElement("textarea");
      ta.value = text;
      ta.setAttribute("readonly", "");
      ta.style.position = "fixed";
      ta.style.top = "-9999px";
      document.body.appendChild(ta);
      ta.select();
      try {
        document.execCommand("copy");
        resolve();
      } catch (err) {
        reject(err);
      }
      document.body.removeChild(ta);
    });
  }

  function flash(el, text) {
    if (!el) return;
    if (text) el.textContent = text;
    el.classList.add("is-on");
    setTimeout(function () {
      el.classList.remove("is-on");
    }, 1800);
  }

  function tryInsertIntoEditor(html, replace) {
    if (window.opener && !window.opener.closed) {
      window.opener.postMessage(
        {
          type: MSG_TYPE,
          html: html,
          replace: !!replace
        },
        window.location.origin
      );
      return true;
    }
    return false;
  }

  function syncPickerUi(moduleId) {
    var selectEl = root.querySelector("[data-cta-select]");
    if (selectEl && selectEl.value !== moduleId) {
      selectEl.value = moduleId;
    }

    root.querySelectorAll("[data-cta-pick]").forEach(function (chip) {
      var active = chip.getAttribute("data-cta-pick") === moduleId;
      chip.classList.toggle("is-active", active);
      chip.setAttribute("aria-selected", active ? "true" : "false");
    });
  }

  function showModule(moduleId, options) {
    var opts = options || {};
    var sections = root.querySelectorAll("[data-module]");
    var active = null;

    sections.forEach(function (section) {
      var isMatch = section.id === moduleId;
      section.hidden = !isMatch;
      section.classList.toggle("is-active", isMatch);
      if (isMatch) active = section;
    });

    syncPickerUi(moduleId);

    if (active) {
      render(active);
    }

    if (!opts.skipScroll) {
      var picker = root.querySelector("[data-cta-picker]");
      if (picker) {
        picker.scrollIntoView({ behavior: "smooth", block: "start" });
      }
    }
  }

  var select = root.querySelector("[data-cta-select]");
  if (select) {
    select.addEventListener("change", function () {
      showModule(select.value);
    });
  }

  root.querySelectorAll("[data-cta-pick]").forEach(function (chip) {
    chip.addEventListener("click", function () {
      var moduleId = chip.getAttribute("data-cta-pick");
      if (!moduleId) return;
      showModule(moduleId);
    });
  });

  root.querySelectorAll("[data-module]").forEach(function (section) {
    bindAddButton(section);
    bindButtonListEvents(section);
    syncButtonUi(section);
    render(section);

    section.querySelectorAll("[data-key]").forEach(function (input) {
      input.addEventListener("input", function () {
        render(section);
      });
      input.addEventListener("change", function () {
        render(section);
      });
    });

    var codeEl = section.querySelector("[data-code]");
    var feedback = section.querySelector("[data-feedback]");

    var copyBtn = section.querySelector("[data-copy]");
    if (copyBtn) {
      copyBtn.addEventListener("click", function () {
        copyText(codeEl.value).then(function () {
          flash(feedback, "کپی شد");
        });
      });
    }

    var insertBtn = section.querySelector("[data-insert]");
    if (insertBtn) {
      insertBtn.addEventListener("click", function () {
        var html = codeEl.value;
        var replace = editing;
        if (tryInsertIntoEditor(html, replace)) {
          flash(feedback, replace ? "CTA به‌روزرسانی شد" : "در ادیتور درج شد");
          return;
        }
        copyText(html).then(function () {
          flash(feedback, "کپی شد — در بلوک HTML ادیتور جای‌گذاری کنید");
        });
      });
    }

    var insertNewBtn = section.querySelector("[data-insert-new]");
    if (insertNewBtn) {
      insertNewBtn.addEventListener("click", function () {
        var html = codeEl.value;
        if (tryInsertIntoEditor(html, false)) {
          flash(feedback, "CTA جدید درج شد");
          return;
        }
        copyText(html).then(function () {
          flash(feedback, "کپی شد — در بلوک HTML ادیتور جای‌گذاری کنید");
        });
      });
    }
  });

  function onParentMessage(event) {
    if (event.origin !== window.location.origin) return;
    var data = event.data;
    if (!data || data.type !== MSG_EDIT_CONTEXT) return;
    if (data.html) {
      applyEditHtml(data.html);
    } else {
      setEditMode(false);
    }
  }

  function requestEditContext() {
    window.addEventListener("message", onParentMessage);

    if (!window.opener || window.opener.closed) {
      setEditMode(false);
      return;
    }

    window.opener.postMessage({ type: MSG_READY }, window.location.origin);
  }

  if (select) {
    showModule(select.value, { skipScroll: true });
  }

  requestEditContext();
})();
