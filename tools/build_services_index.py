# -*- coding: utf-8 -*-
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "src" / "CMS.Web" / "wwwroot" / "template" / "pages" / "services.html"
OUT = ROOT / "src" / "CMS.Modules" / "Services" / "Web" / "Views" / "Services" / "Index.cshtml"


def escape_at(html: str) -> str:
    return html.replace("@", "@@")


def slice_main(text: str) -> str:
    s = text.index("<main>")
    e = text.index("</main>")
    return text[s + len("<main>") : e].strip()


def convert(html: str) -> str:
    html = html.replace('href="../index.html"', 'asp-controller="Home" asp-action="Index"')
    html = html.replace('href="mailto:info@nova-clinic.org"', 'href="mailto:@(contactEmail)"')
    html = html.replace("info@nova-clinic.org", "@contactEmail")
    html = html.replace('href="tel:02191093492"', 'href="@telHref"')
    html = html.replace("۰۲۱-۹۱۰۹۳۴۹۲", "@phoneDisplay")
    html = html.replace(
        "شریعتی، بالاتر از خواجه عبدالله، نبش کوچه ذکایی (جنب آرش موتورز)، ساختمان کسری، طبقه ۴، واحد ۱۵",
        "@address",
    )
    html = html.replace(
        '<a class="btn-secondary-custom" href="service-single.html">',
        '<a class="btn-secondary-custom" href="#thyroid">',
    )
    html = html.replace(
        '<a class="service-group reveal" href="service-single.html">',
        '<a class="service-group reveal" href="#thyroid">',
    )
    return html


text = TEMPLATE.read_text(encoding="utf-8")
main = convert(slice_main(text))

# Split into: hero | overview header+groups | detail sections | cta
hero_end = main.index('<section class="services-overview"')
overview_start = hero_end
details_start = main.index('<section class="service-detail" id="thyroid"')
cta_start = main.index('<section class="cta-section"')

hero = main[:overview_start].strip()
overview = main[overview_start:details_start].strip()
details = main[details_start:cta_start].strip()
cta = main[cta_start:].strip()

# Escape @ in static HTML chunks, then restore Razor tokens
def prep(chunk: str) -> str:
    chunk = escape_at(chunk)
    chunk = chunk.replace("mailto:@@(contactEmail)", "mailto:@(contactEmail)")
    chunk = chunk.replace("@@contactEmail", "@contactEmail")
    chunk = chunk.replace('href="tel:@@telHref"', 'href="@telHref"')
    # phone/address already replaced before escape, so they became @@phoneDisplay
    chunk = chunk.replace("@@phoneDisplay", "@phoneDisplay")
    chunk = chunk.replace("@@address", "@address")
    chunk = chunk.replace("@@telHref", "@telHref")
    return chunk


hero = prep(hero)
overview = prep(overview)
details = prep(details)
cta = prep(cta)

# Extract just the service-groups inner cards from overview for static fallback
groups_open = overview.index('<div class="service-groups">')
groups_inner_start = groups_open + len('<div class="service-groups">')
# find matching close of service-groups - first </div></div></section> structure
# simpler: from service-groups to closing before section end
section_end = overview.rindex("</section>")
# find </div> that closes service-groups - last </div> before section container closes
# overview structure: section > container > header + service-groups > ...
groups_close = overview.rindex("</div>", 0, section_end)  # container
groups_close = overview.rindex("</div>", 0, groups_close)  # service-groups
static_cards = overview[groups_inner_start:groups_close].strip()
overview_header = overview[:groups_open].strip()
overview_footer = overview[groups_close:].strip()  # closes + section end

dynamic_block = r"""
        <div class="service-groups">
          @foreach (var (item, index) in Model.Select((x, i) => (x, i)))
          {
            var anchor = "service-" + item.Slug;
            <a class="service-group reveal" href="#@anchor">
              <span class="service-group__icon" aria-hidden="true">
                @if (!string.IsNullOrWhiteSpace(item.CoverImageUrl))
                {
                  <img src="@item.CoverImageUrl" alt="" width="32" height="32" style="width:2rem;height:2rem;object-fit:cover;border-radius:50%;" />
                }
                else
                {
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M12 2v4M12 18v4M4.9 4.9l2.8 2.8M16.3 16.3l2.8 2.8M2 12h4M18 12h4M4.9 19.1l2.8-2.8M16.3 7.7l2.8-2.8"/>
                    <circle cx="12" cy="12" r="3.2"/>
                  </svg>
                }
              </span>
              <span class="service-group__body">
                <span class="service-group__title">@item.Title</span>
                <span class="service-group__text">@(string.IsNullOrWhiteSpace(item.Excerpt) ? (item.CategoryName ?? "خدمت تخصصی نووا کلینیک") : item.Excerpt)</span>
              </span>
              <span class="service-group__action" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M19 12H5M12 5l-7 7 7 7"/>
                </svg>
              </span>
            </a>
          }
        </div>
"""

dynamic_details = r"""
    @foreach (var (item, index) in Model.Select((x, i) => (x, i)))
    {
      var anchor = "service-" + item.Slug;
      <section class="service-detail@(index % 2 == 1 ? " service-detail--alt" : null)" id="@anchor" aria-labelledby="@(anchor)-heading">
        <div class="container">
          <div class="service-detail__grid reveal">
            <div class="service-detail__content">
              <p class="section-kicker">خدمت @(index + 1)</p>
              <h2 class="section-heading" id="@(anchor)-heading">@item.Title</h2>
              <p class="service-detail__lead">
                @(string.IsNullOrWhiteSpace(item.Excerpt) ? "برای جزئیات کامل این خدمت، صفحه اختصاصی را ببینید یا درخواست مشاوره ثبت کنید." : item.Excerpt)
              </p>
              @if (!string.IsNullOrWhiteSpace(item.CategoryName))
              {
                <ul class="service-detail__list">
                  <li>دسته: @item.CategoryName</li>
                </ul>
              }
              <div class="service-detail__actions">
                <a class="btn-secondary-custom" asp-controller="Services" asp-action="Details" asp-route-slug="@item.Slug">
                  <span class="ui-icon btn-icon" aria-hidden="true">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M19 12H5M12 5l-7 7 7 7"/>
                    </svg>
                  </span>
                  صفحه کامل خدمت
                </a>
                <button class="btn-primary-custom" type="button" data-booking-open>
                  <span class="ui-icon btn-icon" aria-hidden="true">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
                      <rect x="3" y="4" width="18" height="18" rx="2"/>
                      <path d="M16 2v4M8 2v4M3 10h18"/>
                    </svg>
                  </span>
                  رزرو مشاوره
                </button>
              </div>
            </div>
            <aside class="service-detail__aside" aria-label="اطلاعات خدمت">
              @if (!string.IsNullOrWhiteSpace(item.CoverImageUrl))
              {
                <img src="@item.CoverImageUrl" alt="@item.Title" style="width:100%;border-radius:12px;margin-bottom:1rem;object-fit:cover;aspect-ratio:4/3;" />
              }
              else
              {
                <span class="service-detail__aside-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="9"/>
                    <path d="M12 8h.01M11 12h1v4h1"/>
                  </svg>
                </span>
              }
              <h3 class="service-detail__aside-title">مشاوره تخصصی</h3>
              <p>
                برای بررسی شرایط شما و انتخاب بهترین مسیر درمان، با تیم نووا کلینیک در ارتباط باشید.
              </p>
            </aside>
          </div>
        </div>
      </section>
    }
"""

# Fix overview lead text when CMS
overview_header_dynamic = overview_header.replace(
    "خدمات نووا کلینیک در هفت گروه تخصصی متمرکز است؛ برای جزئیات هر حوزه، مورد مربوط را انتخاب کنید.",
    "@(hasCmsServices ? \"خدمات منتشرشده کلینیک را ببینید و برای جزئیات، مورد مربوط را انتخاب کنید.\" : \"خدمات نووا کلینیک در هفت گروه تخصصی متمرکز است؛ برای جزئیات هر حوزه، مورد مربوط را انتخاب کنید.\")",
)

cshtml = f"""@model IReadOnlyList<PublicServiceItemSummaryDto>
@using CMS.Application.Settings
@inject ISiteSettingsService SiteSettings
@{{
    ViewData["Title"] = "خدمات";
    ViewData["NavActive"] = "services";
    var settings = await SiteSettings.GetAsync();
    var phoneRaw = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "02191093492" : settings.ContactPhone;
    var phoneDigits = System.Text.RegularExpressions.Regex.Replace(phoneRaw, @"[^\\d+]", "");
    if (string.IsNullOrWhiteSpace(phoneDigits)) {{ phoneDigits = "02191093492"; }}
    var phoneDisplay = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "۰۲۱-۹۱۰۹۳۴۹۲" : settings.ContactPhone;
    var telHref = "tel:" + phoneDigits;
    var contactEmail = string.IsNullOrWhiteSpace(settings.ContactEmail) ? "info@nova-clinic.org" : settings.ContactEmail;
    var address = string.IsNullOrWhiteSpace(settings.Address)
        ? "شریعتی، بالاتر از خواجه عبدالله، نبش کوچه ذکایی (جنب آرش موتورز)، ساختمان کسری، طبقه ۴، واحد ۱۵"
        : settings.Address;
    var hasCmsServices = Model is {{ Count: > 0 }};
}}

{hero}

{overview_header_dynamic}
@if (hasCmsServices)
{{{dynamic_block}
@Html.Raw(overview_footer.lstrip() if False else "")
}}
else
{{
        <div class="service-groups">
{static_cards}
        </div>
}}
      </div>
    </section>

@if (hasCmsServices)
{{{dynamic_details}
}}
else
{{
{details}
}}

{cta}
"""

# Fix the broken overview_footer injection - I already closed section manually
cshtml = f"""@model IReadOnlyList<PublicServiceItemSummaryDto>
@using CMS.Application.Settings
@inject ISiteSettingsService SiteSettings
@{{
    ViewData["Title"] = "خدمات";
    ViewData["NavActive"] = "services";
    var settings = await SiteSettings.GetAsync();
    var phoneRaw = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "02191093492" : settings.ContactPhone;
    var phoneDigits = System.Text.RegularExpressions.Regex.Replace(phoneRaw, @"[^\\d+]", "");
    if (string.IsNullOrWhiteSpace(phoneDigits)) {{ phoneDigits = "02191093492"; }}
    var phoneDisplay = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "۰۲۱-۹۱۰۹۳۴۹۲" : settings.ContactPhone;
    var telHref = "tel:" + phoneDigits;
    var contactEmail = string.IsNullOrWhiteSpace(settings.ContactEmail) ? "info@nova-clinic.org" : settings.ContactEmail;
    var address = string.IsNullOrWhiteSpace(settings.Address)
        ? "شریعتی، بالاتر از خواجه عبدالله، نبش کوچه ذکایی (جنب آرش موتورز)، ساختمان کسری، طبقه ۴، واحد ۱۵"
        : settings.Address;
    var hasCmsServices = Model is {{ Count: > 0 }};
}}

{hero}

<section class="services-overview" aria-labelledby="services-overview-heading">
  <div class="container">
    <div class="section-header reveal">
      <p class="section-kicker">گروه‌های تخصصی</p>
      <h2 class="section-heading" id="services-overview-heading">حوزه‌های درمانی کلینیک</h2>
      <p class="section-lead">
        @(hasCmsServices
            ? "خدمات منتشرشده کلینیک را ببینید و برای جزئیات، مورد مربوط را انتخاب کنید."
            : "خدمات نووا کلینیک در هفت گروه تخصصی متمرکز است؛ برای جزئیات هر حوزه، مورد مربوط را انتخاب کنید.")
      </p>
    </div>

@if (hasCmsServices)
{{{dynamic_block}
}}
else
{{
        <div class="service-groups">
{static_cards}
        </div>
}}
  </div>
</section>

@if (hasCmsServices)
{{{dynamic_details}
}}
else
{{
{details}
}}

{cta}
"""

OUT.write_text(cshtml, encoding="utf-8", newline="\n")
print("OK", OUT, "bytes", OUT.stat().st_size)
