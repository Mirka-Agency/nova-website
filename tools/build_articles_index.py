# -*- coding: utf-8 -*-
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "src" / "CMS.Web" / "wwwroot" / "template" / "pages" / "articles.html"
OUT = ROOT / "src" / "CMS.Modules" / "News" / "Web" / "Views" / "News" / "Index.cshtml"

text = TEMPLATE.read_text(encoding="utf-8")
main = text[text.index("<main>") + len("<main>") : text.index("</main>")].strip()

grid_marker = '<div class="articles-archive__grid">'
grid_start = main.index(grid_marker) + len(grid_marker)
grid_end = main.index('<p class="articles-empty"', grid_start)
static_cards = main[grid_start:grid_end].strip()
if static_cards.endswith("</div>"):
    static_cards = static_cards[: -len("</div>")].rstrip()

static_cards = static_cards.replace("../assets/", "~/template/assets/")
static_cards = static_cards.replace('href="article-single.html"', 'asp-controller="News" asp-action="Index"')
static_cards = static_cards.replace("@", "@@")
# HTML entities already in template as &amp; — after @@ escape stay fine

cshtml = r'''@model IReadOnlyList<PublicArticleSummaryDto>
@using CMS.Modules.News.Domain.Enums
@{
    ViewData["Title"] = "مقالات تخصصی";
    ViewData["NavActive"] = "articles";
    var items = (Model ?? Array.Empty<PublicArticleSummaryDto>())
        .Where(x => x.Kind != ArticleKind.Event)
        .ToList();
    var hasCmsArticles = items.Count > 0;

    string MapCategory(string? name, string title, string excerpt)
    {
        var hay = $"{name} {title} {excerpt}";
        if (hay.Contains("آدرنال", StringComparison.Ordinal) || hay.Contains("adrenal", StringComparison.OrdinalIgnoreCase) || hay.Contains("فوق کلیه", StringComparison.Ordinal))
            return "adrenal";
        if (hay.Contains("تیروئید", StringComparison.Ordinal) || hay.Contains("thyroid", StringComparison.OrdinalIgnoreCase) || hay.Contains("hypocalcemia", StringComparison.OrdinalIgnoreCase) || hay.Contains("هیپوکلسمی", StringComparison.Ordinal))
            return "thyroid";
        return "other";
    }

    string TopicLabel(string category) => category switch
    {
        "thyroid" => "تیروئید",
        "adrenal" => "آدرنال",
        _ => "سایر"
    };
}

<section class="page-hero page-hero--articles" aria-labelledby="page-hero-title">
  <div class="container page-hero__inner">
    <nav class="page-breadcrumb reveal" aria-label="مسیر صفحه">
      <a asp-controller="Home" asp-action="Index">صفحه اصلی</a>
      <span class="page-breadcrumb__sep" aria-hidden="true">/</span>
      <span aria-current="page">مقالات تخصصی</span>
    </nav>
    <h1 class="page-hero__title reveal" id="page-hero-title">مقالات علمی و مورد تأیید نووا کلینیک</h1>
    <p class="page-hero__lead reveal">
      گزیده‌ای از مقالات، متاآنالیزها و منابع تخصصی در جراحی تیروئید، آدرنال و مراقبت‌های مرتبط با غدد درون‌ریز.
    </p>
  </div>
</section>

<section class="articles-archive" aria-label="آرشیو مقالات تخصصی" data-articles-archive>
  <div class="container">
    <div class="articles-toolbar reveal" role="group" aria-label="فیلتر موضوع مقاله">
      <button class="articles-filter is-active" type="button" data-article-filter="all" aria-pressed="true">همه</button>
      <button class="articles-filter" type="button" data-article-filter="thyroid" aria-pressed="false">تیروئید</button>
      <button class="articles-filter" type="button" data-article-filter="adrenal" aria-pressed="false">آدرنال</button>
      <button class="articles-filter" type="button" data-article-filter="other" aria-pressed="false">سایر</button>
    </div>

    <p class="articles-count reveal" data-articles-count aria-live="polite"></p>

    <div class="articles-archive__grid">
@if (hasCmsArticles)
{
    var index = 0;
    foreach (var item in items)
    {
        var category = MapCategory(item.CategoryName, item.Title, item.Excerpt);
        var topic = string.IsNullOrWhiteSpace(item.CategoryName) ? TopicLabel(category) : item.CategoryName;
        var typeLabel = string.IsNullOrWhiteSpace(item.AuthorDisplayName) ? "مقاله تخصصی" : item.AuthorDisplayName;
        var cover = string.IsNullOrWhiteSpace(item.CoverImageUrl)
            ? Url.Content("~/template/assets/images/articles/scientific-reports.jpg")
            : item.CoverImageUrl;
        var featuredClass = index == 0 ? " article-card--featured" : "";
        <a
          class="article-card@(featuredClass) reveal"
          asp-controller="News"
          asp-action="Details"
          asp-route-slug="@item.Slug"
          data-article-card
          data-article-category="@category"
        >
          <span class="article-card__media">
            <img
              src="@cover"
              width="800"
              height="500"
              alt="@item.Title"
              loading="@(index == 0 ? "eager" : "lazy")"
              decoding="async"
            >
          </span>
          <span class="article-card__body">
            <span class="article-card__meta">
              <span class="article-card__type">@typeLabel</span>
              <span class="article-card__topic">@topic</span>
            </span>
            <h3 class="article-card__title">@item.Title</h3>
            <p class="article-card__excerpt">
              @(string.IsNullOrWhiteSpace(item.Excerpt) ? "برای مطالعه جزئیات این مقاله تخصصی وارد شوید." : item.Excerpt)
            </p>
            <span class="article-card__more">مطالعه مقاله</span>
          </span>
        </a>
        index++;
    }
}
else
{
''' + static_cards + r'''
}
    </div>

    <p class="articles-empty" data-articles-empty hidden>مقاله‌ای در این دسته یافت نشد.</p>

    <nav class="articles-pagination" data-articles-pagination hidden aria-label="صفحه‌بندی مقالات">
      <button class="articles-pagination__btn" type="button" data-articles-prev aria-label="صفحه قبل">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="m9 18 6-6-6-6"/>
        </svg>
      </button>
      <div class="articles-pagination__pages" data-articles-pages></div>
      <button class="articles-pagination__btn" type="button" data-articles-next aria-label="صفحه بعد">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="m15 18-6-6 6-6"/>
        </svg>
      </button>
    </nav>
  </div>
</section>
'''

OUT.write_text(cshtml, encoding="utf-8", newline="\n")
print("wrote", OUT, "size", OUT.stat().st_size)
