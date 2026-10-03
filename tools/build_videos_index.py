# -*- coding: utf-8 -*-
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "src" / "CMS.Web" / "wwwroot" / "template" / "pages" / "videos.html"
OUT = ROOT / "src" / "CMS.Modules" / "Video" / "Web" / "Views" / "Videos" / "Index.cshtml"

text = TEMPLATE.read_text(encoding="utf-8")
main = text[text.index("<main>") + len("<main>") : text.index("</main>")].strip()

# Extract static video cards block
grid_start = main.index('<div class="videos-grid">')
grid_inner_start = grid_start + len('<div class="videos-grid">')
grid_end = main.index('<p class="videos-empty"', grid_inner_start)
static_cards = main[grid_inner_start:grid_end].strip()
# Template closes .videos-grid before .videos-empty; keep that out of the cards chunk.
if static_cards.endswith("</div>"):
    static_cards = static_cards[: -len("</div>")].rstrip()
static_cards = static_cards.replace("../assets/", "~/template/assets/")
static_cards = static_cards.replace('href="video-single.html"', 'asp-controller="Videos" asp-action="Index"')
static_cards = static_cards.replace("@", "@@")

cshtml = r'''@model IReadOnlyList<PublicVideoItemSummaryDto>
@using CMS.Application.Settings
@inject ISiteSettingsService SiteSettings
@{
    ViewData["Title"] = "ویدئوها";
    ViewData["NavActive"] = "videos";
    var settings = await SiteSettings.GetAsync();
    var phoneRaw = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "02191093492" : settings.ContactPhone;
    var phoneDigits = System.Text.RegularExpressions.Regex.Replace(phoneRaw, @"[^\d+]", "");
    if (string.IsNullOrWhiteSpace(phoneDigits)) { phoneDigits = "02191093492"; }
    var phoneDisplay = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "۰۲۱-۹۱۰۹۳۴۹۲" : settings.ContactPhone;
    var telHref = "tel:" + phoneDigits;
    var items = Model ?? Array.Empty<PublicVideoItemSummaryDto>();
    var hasCmsVideos = items.Count > 0;

    string MapCategory(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "clinic";
        var n = name.Trim();
        if (n.Contains("پاراتیروئید", StringComparison.Ordinal) || n.Contains("parathyroid", StringComparison.OrdinalIgnoreCase))
            return "parathyroid";
        if (n.Contains("تیروئید", StringComparison.Ordinal) || n.Contains("thyroid", StringComparison.OrdinalIgnoreCase) || n.Contains("TOETVA", StringComparison.OrdinalIgnoreCase))
            return "thyroid";
        if (n.Contains("فرآیند", StringComparison.Ordinal) || n.Contains("فرایند", StringComparison.Ordinal) || n.Contains("جراحی", StringComparison.Ordinal) || n.Contains("process", StringComparison.OrdinalIgnoreCase))
            return "process";
        if (n.Contains("کلینیک", StringComparison.Ordinal) || n.Contains("معرفی", StringComparison.Ordinal) || n.Contains("clinic", StringComparison.OrdinalIgnoreCase))
            return "clinic";
        return "clinic";
    }
}

<section class="page-hero page-hero--videos" aria-labelledby="page-hero-title">
  <div class="container page-hero__inner">
    <nav class="page-breadcrumb reveal" aria-label="مسیر صفحه">
      <a asp-controller="Home" asp-action="Index">صفحه اصلی</a>
      <span class="page-breadcrumb__sep" aria-hidden="true">/</span>
      <span aria-current="page">ویدئوها</span>
    </nav>
    <h1 class="page-hero__title reveal" id="page-hero-title">ویدئوهای نووا کلینیک</h1>
    <p class="page-hero__lead reveal">
      توضیحات تخصصی درباره جراحی غدد، روش‌های کم‌تهاجمی و مسیر درمان؛ با روایت دکتر کاوه همدانی.
    </p>
  </div>
</section>

<section class="videos-archive" aria-label="آرشیو ویدئوها" data-videos-archive>
  <div class="container">
    <div class="videos-toolbar reveal" role="group" aria-label="فیلتر موضوع ویدئو">
      <button class="videos-filter is-active" type="button" data-video-filter="all" aria-pressed="true">همه</button>
      <button class="videos-filter" type="button" data-video-filter="thyroid" aria-pressed="false">تیروئید</button>
      <button class="videos-filter" type="button" data-video-filter="parathyroid" aria-pressed="false">پاراتیروئید</button>
      <button class="videos-filter" type="button" data-video-filter="process" aria-pressed="false">فرآیند جراحی</button>
      <button class="videos-filter" type="button" data-video-filter="clinic" aria-pressed="false">معرفی کلینیک</button>
    </div>

    <p class="videos-count reveal" data-videos-count aria-live="polite"></p>

    <div class="videos-grid">
@if (hasCmsVideos)
{
    var isFirst = true;
    foreach (var item in items)
    {
        var category = MapCategory(item.CategoryName);
        var typeLabel = string.IsNullOrWhiteSpace(item.CategoryName) ? "ویدئو" : item.CategoryName;
        var cover = string.IsNullOrWhiteSpace(item.CoverImageUrl)
            ? Url.Content("~/template/assets/images/videos/endocrine.webp")
            : item.CoverImageUrl;
        <article class="video-card reveal" data-video-card data-video-category="@category">
          <a
            class="video-card__trigger"
            asp-controller="Videos"
            asp-action="Details"
            asp-route-slug="@item.Slug"
            aria-label="مشاهده ویدئو: @item.Title"
          >
            <span class="video-card__media">
              <img
                src="@cover"
                width="640"
                height="360"
                alt=""
                loading="@(isFirst ? "eager" : "lazy")"
                decoding="async"
              >
              <span class="video-card__play" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="currentColor">
                  <path d="M8 5.75c0-.9.95-1.45 1.7-.98l8.35 5.1c.7.43.7 1.53 0 1.96l-8.35 5.1c-.75.47-1.7-.08-1.7-.98V5.75z"/>
                </svg>
              </span>
            </span>
            <span class="video-card__body">
              <span class="video-card__type">@typeLabel</span>
              <h3 class="video-card__title">@item.Title</h3>
            </span>
          </a>
        </article>
        isFirst = false;
    }
}
else
{
''' + static_cards + r'''
}
    </div>

    <p class="videos-empty" data-videos-empty hidden>ویدئویی در این دسته یافت نشد.</p>

    <nav class="videos-pagination" data-videos-pagination hidden aria-label="صفحه‌بندی ویدئوها">
      <button class="videos-pagination__btn" type="button" data-videos-prev aria-label="صفحه قبل">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="m9 18 6-6-6-6"/>
        </svg>
      </button>
      <div class="videos-pagination__pages" data-videos-pages></div>
      <button class="videos-pagination__btn" type="button" data-videos-next aria-label="صفحه بعد">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="m15 18-6-6 6-6"/>
        </svg>
      </button>
    </nav>
  </div>
</section>

<section class="consult-band consult-band--compact" id="contact" aria-labelledby="consult-band-heading">
  <div class="container">
    <div class="consult-band__panel reveal">
      <div class="consult-band__copy">
        <p class="consult-band__kicker">نوبت مشاوره</p>
        <h2 class="consult-band__title" id="consult-band-heading">
          رزرو مشاوره تخصصی
        </h2>
        <p class="consult-band__meta">شنبه تا پنجشنبه · ۹ تا ۲۱</p>
      </div>

      <div class="consult-band__actions">
        <button class="btn-primary-custom" type="button" data-booking-open>
          رزرو نوبت
        </button>
        <a class="btn-secondary-custom" href="@telHref">
          <span class="phone-ltr">@phoneDisplay</span>
        </a>
      </div>
    </div>
  </div>
</section>
'''

OUT.write_text(cshtml, encoding="utf-8", newline="\n")
print("wrote", OUT, "size", OUT.stat().st_size)
