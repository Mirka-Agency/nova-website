# -*- coding: utf-8 -*-
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "src" / "CMS.Web" / "wwwroot" / "template" / "pages" / "blog.html"
OUT = ROOT / "src" / "CMS.Modules" / "Blog" / "Web" / "Views" / "Blog" / "Index.cshtml"

text = TEMPLATE.read_text(encoding="utf-8")
main = text[text.index("<main>") + len("<main>") : text.index("</main>")].strip()

grid_marker = '<div class="blog-grid">'
grid_start = main.index(grid_marker) + len(grid_marker)
grid_end = main.index('<p class="blog-empty"', grid_start)
static_cards = main[grid_start:grid_end].strip()
if static_cards.endswith("</div>"):
    static_cards = static_cards[: -len("</div>")].rstrip()

static_cards = static_cards.replace("../assets/", "~/template/assets/")
# blog-single maps to Details when CMS slug exists; static fallback stays on listing
static_cards = static_cards.replace('href="blog-single.html"', 'asp-controller="Blog" asp-action="Index"')
static_cards = static_cards.replace("@", "@@")

cshtml = r'''@model IReadOnlyList<PublicPostSummaryDto>
@using CMS.Application.Settings
@using System.Globalization
@inject ISiteSettingsService SiteSettings
@{
    ViewData["Title"] = "وبلاگ";
    ViewData["NavActive"] = "blog";
    var settings = await SiteSettings.GetAsync();
    var phoneRaw = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "02191093492" : settings.ContactPhone;
    var phoneDigits = System.Text.RegularExpressions.Regex.Replace(phoneRaw, @"[^\d+]", "");
    if (string.IsNullOrWhiteSpace(phoneDigits)) { phoneDigits = "02191093492"; }
    var phoneDisplay = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "۰۲۱-۹۱۰۹۳۴۹۲" : settings.ContactPhone;
    var telHref = "tel:" + phoneDigits;
    var items = Model ?? Array.Empty<PublicPostSummaryDto>();
    var hasCmsPosts = items.Count > 0;
    var fa = CultureInfo.GetCultureInfo("fa-IR");
}

<section class="page-hero page-hero--blog" aria-labelledby="page-hero-title">
  <div class="container page-hero__inner">
    <nav class="page-breadcrumb reveal" aria-label="مسیر صفحه">
      <a asp-controller="Home" asp-action="Index">صفحه اصلی</a>
      <span class="page-breadcrumb__sep" aria-hidden="true">/</span>
      <span aria-current="page">وبلاگ</span>
    </nav>
    <h1 class="page-hero__title reveal" id="page-hero-title">وبلاگ نووا کلینیک</h1>
    <p class="page-hero__lead reveal">
      مطالب آموزشی درباره بیماری‌ها، علائم و مراقبت‌های مرتبط با جراحی غدد درون‌ریز؛ به زبان ساده و قابل‌اعتماد.
    </p>
  </div>
</section>

<section class="blog-archive" aria-label="آرشیو وبلاگ" data-blog-archive>
  <div class="container">
    <p class="blog-count reveal" data-blog-count aria-live="polite"></p>

    <div class="blog-grid">
@if (hasCmsPosts)
{
    var index = 0;
    foreach (var item in items)
    {
        var local = item.PublishedAtUtc.ToLocalTime();
        var dateAttr = local.ToString("yyyy-MM-dd");
        var dateLabel = local.ToString("d MMMM yyyy", fa);
        var cover = string.IsNullOrWhiteSpace(item.CoverImageUrl)
            ? Url.Content("~/template/assets/images/blog/thyroid-risks.webp")
            : item.CoverImageUrl;
        var featuredClass = index == 0 ? " blog-card--featured" : null;
        <article class="blog-card@(featuredClass) reveal" data-blog-card>
          <a class="blog-card__link" asp-controller="Blog" asp-action="Details" asp-route-slug="@item.Slug">
            <span class="blog-card__media">
              <img
                src="@cover"
                width="800"
                height="500"
                alt=""
                loading="@(index == 0 ? "eager" : "lazy")"
                decoding="async"
              >
            </span>
            <span class="blog-card__body">
              <span class="blog-card__meta">
                <time datetime="@dateAttr">@dateLabel</time>
                @if (!string.IsNullOrWhiteSpace(item.CategoryName))
                {
                  <span>@item.CategoryName</span>
                }
              </span>
              <h3 class="blog-card__title">@item.Title</h3>
              <p class="blog-card__excerpt">@(string.IsNullOrWhiteSpace(item.Excerpt) ? "برای ادامه مطلب وارد شوید." : item.Excerpt)</p>
              <span class="blog-card__more">ادامه مطلب</span>
            </span>
          </a>
        </article>
        index++;
    }
}
else
{
''' + static_cards + r'''
}
    </div>

    <p class="blog-empty" data-blog-empty hidden>مطلبی در این دسته یافت نشد.</p>

    <nav class="blog-pagination" data-blog-pagination hidden aria-label="صفحه‌بندی وبلاگ">
      <button class="blog-pagination__btn" type="button" data-blog-prev aria-label="صفحه قبل">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="m9 18 6-6-6-6"/>
        </svg>
      </button>
      <div class="blog-pagination__pages" data-blog-pages></div>
      <button class="blog-pagination__btn" type="button" data-blog-next aria-label="صفحه بعد">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="m15 18-6-6 6-6"/>
        </svg>
      </button>
    </nav>
  </div>
</section>

<section class="consult-band consult-band--compact" aria-labelledby="consult-band-heading">
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
