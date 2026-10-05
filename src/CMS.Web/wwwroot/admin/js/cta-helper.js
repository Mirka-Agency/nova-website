(function () {
  "use strict";

  var root = document.querySelector("[data-cta-helper]");
  if (!root) return;

  var MSG_TYPE = "nova-cta-helper-insert";

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

  function getData(section) {
    var data = {};
    section.querySelectorAll("[data-key]").forEach(function (el) {
      data[el.getAttribute("data-key")] = el.value;
    });
    return data;
  }

  function btnPrimary(href, text) {
    return (
      '<a href="' +
      esc(href) +
      '" style="display:inline-flex;align-items:center;justify-content:center;gap:.5rem;min-height:48px;padding:.75rem 1.4rem;border-radius:999px;background:linear-gradient(135deg,#0a8491 0%,#09707d 55%,#065a64 100%);color:#fff;font-weight:700;font-size:.94rem;text-decoration:none;border:1.5px solid transparent;line-height:1.15;box-shadow:0 8px 18px rgba(9,112,125,.22);">' +
      esc(text) +
      "</a>"
    );
  }

  function btnSecondary(href, text) {
    return (
      '<a href="' +
      esc(href) +
      '" style="display:inline-flex;align-items:center;justify-content:center;gap:.5rem;min-height:48px;padding:.75rem 1.4rem;border-radius:999px;background:#e6f3f5;color:#065a64;font-weight:700;font-size:.94rem;text-decoration:none;border:1.5px solid rgba(9,112,125,.16);line-height:1.15;">' +
      esc(text) +
      "</a>"
    );
  }

  function btnLight(href, text) {
    return (
      '<a href="' +
      esc(href) +
      '" style="display:inline-flex;align-items:center;justify-content:center;gap:.5rem;min-height:48px;padding:.75rem 1.4rem;border-radius:999px;background:#fff;color:#065a64;font-weight:700;font-size:.94rem;text-decoration:none;border:1.5px solid rgba(255,255,255,.95);line-height:1.15;box-shadow:0 8px 18px rgba(0,0,0,.12);">' +
      esc(text) +
      "</a>"
    );
  }

  function btnOutlineOnDark(href, text) {
    return (
      '<a href="' +
      esc(href) +
      '" style="display:inline-flex;align-items:center;justify-content:center;gap:.5rem;min-height:48px;padding:.75rem 1.4rem;border-radius:999px;background:rgba(255,255,255,.08);color:#fff;font-weight:700;font-size:.94rem;text-decoration:none;border:1.5px solid rgba(255,255,255,.5);line-height:1.15;backdrop-filter:blur(4px);">' +
      esc(text) +
      "</a>"
    );
  }

  function btnCompact(href, text) {
    return (
      '<a href="' +
      esc(href) +
      '" style="display:inline-flex;align-items:center;justify-content:center;min-height:42px;padding:.55rem 1.15rem;border-radius:999px;background:linear-gradient(135deg,#0a8491,#09707d);color:#fff;font-weight:700;font-size:.88rem;text-decoration:none;white-space:nowrap;box-shadow:0 6px 14px rgba(9,112,125,.2);">' +
      esc(text) +
      "</a>"
    );
  }

  var builders = {
    appointment: function (d) {
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
        '<div style="display:flex;flex-wrap:wrap;gap:.7rem;">' +
        btnLight(d.primaryUrl, d.primaryText) +
        (d.secondaryText && String(d.secondaryText).trim()
          ? btnOutlineOnDark(d.secondaryUrl, d.secondaryText)
          : "") +
        "</div></div></div>"
      );
    },

    consultation: function (d) {
      return (
        '<div class="nova-cta nova-cta--consultation" style="margin:1.75rem 0;padding:1.45rem 1.5rem;border-radius:22px;background:linear-gradient(180deg,#fff 0%,#f7fafa 100%);border:1px solid rgba(9,112,125,.14);box-shadow:0 10px 28px rgba(9,112,125,.08);font-family:Peyda,Tahoma,sans-serif;">' +
        '<h3 style="margin:0 0 .5rem;font-size:1.18rem;font-weight:800;color:#1a3a3f;line-height:1.4;letter-spacing:-.015em;">' +
        esc(d.title) +
        "</h3>" +
        '<p style="margin:0 0 1.1rem;color:#2c3e40;line-height:1.8;">' +
        esc(d.description) +
        "</p>" +
        '<div style="display:flex;flex-wrap:wrap;align-items:center;gap:.85rem 1.1rem;">' +
        btnPrimary(d.buttonUrl, d.buttonText) +
        '<a href="tel:' +
        esc(telDigits(d.phoneNumber)) +
        '" dir="ltr" style="display:inline-flex;align-items:center;gap:.55rem;padding:.45rem .75rem .45rem .45rem;border-radius:999px;background:#e6f3f5;color:#065a64;font-weight:700;text-decoration:none;direction:ltr;unicode-bidi:isolate;">' +
        '<span style="display:inline-grid;place-items:center;width:2.15rem;height:2.15rem;border-radius:999px;background:#09707d;color:#fff;font-size:.9rem;">☎</span>' +
        '<span><span style="display:block;font-size:.72rem;color:#6b7c7e;font-weight:700;">' +
        esc(d.phoneLabel) +
        '</span><span style="font-size:1rem;">' +
        esc(d.phoneNumber) +
        "</span></span></a></div></div>"
      );
    },

    doctor: function (d) {
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
        '<div style="display:flex;flex-wrap:wrap;gap:.6rem;">' +
        btnPrimary(d.profileUrl, d.profileText) +
        btnSecondary(d.appointmentUrl, d.appointmentText) +
        "</div></div></div>"
      );
    },

    surgery: function (d) {
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
        '<div><strong style="display:block;color:#09707d;font-size:.8rem;margin-bottom:.2rem;letter-spacing:.02em;">خدمت مرتبط</strong>' +
        '<span style="color:#1a3a3f;font-weight:800;">' +
        esc(d.serviceName) +
        "</span></div>" +
        '<div style="display:flex;flex-wrap:wrap;gap:.6rem;">' +
        btnPrimary(d.buttonUrl, d.buttonText) +
        (d.secondaryText && String(d.secondaryText).trim()
          ? btnSecondary(d.secondaryUrl, d.secondaryText)
          : "") +
        "</div></div></div>"
      );
    },

    articleEnd: function (d) {
      return (
        '<div class="nova-cta nova-cta--article-end" style="margin:2rem 0 1.5rem;padding:1.7rem 1.5rem;border-radius:24px;background:linear-gradient(180deg,#f7fafa 0%,#eef6f7 100%);border:1px solid rgba(9,112,125,.14);text-align:center;box-shadow:0 12px 30px rgba(9,112,125,.08);font-family:Peyda,Tahoma,sans-serif;">' +
        '<h3 style="margin:0 0 .55rem;font-size:1.28rem;font-weight:800;color:#1a3a3f;letter-spacing:-.02em;">' +
        esc(d.title) +
        "</h3>" +
        '<p style="margin:0 auto 1.25rem;max-width:34rem;color:#2c3e40;line-height:1.8;">' +
        esc(d.description) +
        "</p>" +
        '<div style="display:flex;flex-wrap:wrap;justify-content:center;gap:.7rem;">' +
        btnPrimary(d.appointmentUrl, d.appointmentText) +
        btnSecondary(d.consultationUrl, d.consultationText) +
        "</div></div>"
      );
    },

    contact: function (d) {
      return (
        '<div class="nova-cta nova-cta--contact" style="margin:1.75rem 0;display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:1rem;padding:1.2rem 1.35rem;border-radius:20px;background:linear-gradient(180deg,#fff 0%,#f7fafa 100%);border:1px solid rgba(9,112,125,.14);box-shadow:0 10px 26px rgba(9,112,125,.08);font-family:Peyda,Tahoma,sans-serif;">' +
        '<div style="min-width:0;flex:1 1 16rem;">' +
        '<strong style="display:block;color:#1a3a3f;font-size:1.08rem;margin-bottom:.3rem;letter-spacing:-.01em;">' +
        esc(d.title) +
        "</strong>" +
        '<span style="color:#2c3e40;line-height:1.7;">' +
        esc(d.description) +
        "</span></div>" +
        '<div style="display:flex;flex-wrap:wrap;gap:.55rem;align-items:center;">' +
        '<a href="tel:' +
        esc(telDigits(d.phone)) +
        '" dir="ltr" style="display:inline-flex;align-items:center;min-height:42px;padding:.5rem 1.05rem;border-radius:999px;background:#e6f3f5;color:#065a64;font-weight:700;text-decoration:none;direction:ltr;unicode-bidi:isolate;">☎ ' +
        esc(d.phone) +
        "</a>" +
        '<a href="' +
        esc(d.whatsappUrl) +
        '" style="display:inline-flex;align-items:center;min-height:42px;padding:.5rem 1.05rem;border-radius:999px;border:1px solid rgba(9,112,125,.18);color:#065a64;font-weight:700;text-decoration:none;background:#fff;">' +
        esc(d.whatsappText || "واتساپ / تماس") +
        "</a>" +
        btnCompact(d.appointmentUrl, d.appointmentText) +
        "</div></div>"
      );
    },

    inline: function (d) {
      return (
        '<div class="nova-cta nova-cta--inline" style="margin:1.35rem 0;display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:.9rem;padding:1rem 1.1rem;border-radius:16px;background:linear-gradient(120deg,#e6f3f5 0%,#f3fafb 100%);border:1px solid rgba(9,112,125,.16);box-shadow:0 6px 16px rgba(9,112,125,.06);font-family:Peyda,Tahoma,sans-serif;">' +
        '<div style="min-width:0;flex:1 1 14rem;">' +
        '<strong style="display:block;color:#065a64;font-size:.98rem;margin-bottom:.2rem;">' +
        esc(d.title) +
        "</strong>" +
        '<span style="color:#2c3e40;font-size:.9rem;line-height:1.65;">' +
        esc(d.description) +
        "</span></div>" +
        btnCompact(d.buttonUrl, d.buttonText) +
        "</div>"
      );
    },

    highlight: function (d) {
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
        '<div style="display:flex;flex-wrap:wrap;gap:.6rem;">' +
        btnPrimary(d.primaryUrl, d.primaryText) +
        (d.secondaryText && String(d.secondaryText).trim()
          ? btnSecondary(d.secondaryUrl, d.secondaryText)
          : "") +
        "</div></div>"
      );
    }
  };

  function render(section) {
    var type = section.getAttribute("data-module");
    var builder = builders[type];
    if (!builder) return;
    var html = builder(getData(section));
    section.querySelector("[data-preview]").innerHTML = html;
    section.querySelector("[data-code]").value = html;
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

  function tryInsertIntoEditor(html) {
    if (window.opener && !window.opener.closed) {
      window.opener.postMessage(
        { type: MSG_TYPE, html: html },
        window.location.origin
      );
      return true;
    }
    return false;
  }

  function showModule(moduleId) {
    var sections = root.querySelectorAll("[data-module]");
    var active = null;

    sections.forEach(function (section) {
      var isMatch = section.id === moduleId;
      section.hidden = !isMatch;
      section.classList.toggle("is-active", isMatch);
      if (isMatch) active = section;
    });

    if (active) {
      render(active);
    }

    var picker = root.querySelector("[data-cta-picker]");
    if (picker) {
      picker.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  }

  var select = root.querySelector("[data-cta-select]");
  if (select) {
    select.addEventListener("change", function () {
      showModule(select.value);
    });
  }

  root.querySelectorAll("[data-module]").forEach(function (section) {
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
        if (tryInsertIntoEditor(html)) {
          flash(feedback, "در ادیتور درج شد");
          return;
        }
        copyText(html).then(function () {
          flash(feedback, "کپی شد — در بلوک HTML ادیتور جای‌گذاری کنید");
        });
      });
    }
  });

  if (select) {
    showModule(select.value);
  }
})();
