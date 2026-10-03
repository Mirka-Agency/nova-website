# -*- coding: utf-8 -*-
"""Convert wwwroot/template HTML into Razor views/partials for CMS.Web."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src" / "CMS.Web"
TEMPLATE = WEB / "wwwroot" / "template"
VIEWS_SHARED = WEB / "Views" / "Shared"
VIEWS_HOME = WEB / "Views" / "Home"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def slice_between(text: str, start: str, end: str, include_end: bool = False) -> str:
    i = text.index(start)
    j = text.index(end, i + len(start))
    if include_end:
        return text[i : j + len(end)]
    return text[i:j]


def convert_asset_paths(html: str) -> str:
    html = html.replace("../assets/", "~/template/assets/")
    html = re.sub(r'(src|href|poster)="assets/', r'\1="~/template/assets/', html)
    html = html.replace("url('assets/", "url('~/template/assets/")
    html = html.replace('url("assets/', 'url("~/template/assets/')
    return html


LINK_MAP = [
    ('href="index.html"', 'asp-controller="Home" asp-action="Index"'),
    ('href="../index.html"', 'asp-controller="Home" asp-action="Index"'),
    ('href="pages/services.html#parathyroid"', 'asp-controller="Services" asp-action="Index" asp-fragment="parathyroid"'),
    ('href="services.html#parathyroid"', 'asp-controller="Services" asp-action="Index" asp-fragment="parathyroid"'),
    ('href="pages/services.html#adrenal"', 'asp-controller="Services" asp-action="Index" asp-fragment="adrenal"'),
    ('href="services.html#adrenal"', 'asp-controller="Services" asp-action="Index" asp-fragment="adrenal"'),
    ('href="pages/services.html#pancreas"', 'asp-controller="Services" asp-action="Index" asp-fragment="pancreas"'),
    ('href="services.html#pancreas"', 'asp-controller="Services" asp-action="Index" asp-fragment="pancreas"'),
    ('href="pages/services.html"', 'asp-controller="Services" asp-action="Index"'),
    ('href="services.html"', 'asp-controller="Services" asp-action="Index"'),
    # service-single.html in the template set is the thyroid detail page
    ('href="pages/service-single.html"', 'asp-controller="Services" asp-action="Index" asp-fragment="thyroid"'),
    ('href="service-single.html"', 'asp-controller="Services" asp-action="Index" asp-fragment="thyroid"'),
    ('href="pages/videos.html"', 'asp-controller="Videos" asp-action="Index"'),
    ('href="videos.html"', 'asp-controller="Videos" asp-action="Index"'),
    ('href="pages/articles.html"', 'asp-controller="News" asp-action="Index"'),
    ('href="articles.html"', 'asp-controller="News" asp-action="Index"'),
    ('href="pages/blog.html"', 'asp-controller="Blog" asp-action="Index"'),
    ('href="blog.html"', 'asp-controller="Blog" asp-action="Index"'),
    ('href="pages/about.html"', 'asp-controller="Home" asp-action="About"'),
    ('href="about.html"', 'asp-controller="Home" asp-action="About"'),
    ('href="pages/contact.html"', 'asp-controller="Home" asp-action="Contact"'),
    ('href="contact.html"', 'asp-controller="Home" asp-action="Contact"'),
    ('href="pages/doctors.html"', 'asp-controller="Teams" asp-action="Index"'),
    ('href="doctors.html"', 'asp-controller="Teams" asp-action="Index"'),
    ('href="pages/events.html"', 'asp-controller="News" asp-action="Index"'),
    ('href="events.html"', 'asp-controller="News" asp-action="Index"'),
    ('href="pages/doctor-takyar.html"', 'asp-controller="Teams" asp-action="Index"'),
    ('href="doctor-takyar.html"', 'asp-controller="Teams" asp-action="Index"'),
    ('href="pages/doctor-single.html"', 'asp-controller="Teams" asp-action="Index"'),
    ('href="doctor-single.html"', 'asp-controller="Teams" asp-action="Index"'),
]


def convert_page_links(html: str) -> str:
    for old, new in LINK_MAP:
        html = html.replace(old, new)
    html = re.sub(r'href="pages/[^"]+\.html"', 'asp-controller="Home" asp-action="Index"', html)
    return html


def clean_mobile_nav_dupes(html: str) -> str:
    # Broken duplicate block in index.html mobile nav
    pattern = re.compile(
        r"\s*</li>\s*"
        r"<li><a href=\"pages/doctors\.html\">.*?</a></li>\s*"
        r"<li><a href=\"pages/events\.html\">.*?</a></li>\s*"
        r"<li><a href=\"pages/videos\.html\">.*?</a></li>\s*"
        r"<li><a href=\"pages/articles\.html\">.*?</a></li>\s*"
        r"<li><a href=\"pages/blog\.html\">.*?</a></li>\s*"
        r"<li><a href=\"pages/about\.html\">.*?</a></li>\s*"
        r"<li><a href=\"pages/contact\.html\">.*?</a></li>\s*"
        r"</ul>",
        re.DOTALL,
    )
    return pattern.sub("\n          </ul>", html, count=1)


def escape_razor_at(html: str) -> str:
    """Escape literal @ so Razor does not treat YouTube handles etc. as code."""
    return html.replace("@", "@@")


def process_shell(html: str) -> str:
    html = clean_mobile_nav_dupes(html)
    html = convert_page_links(html)
    html = convert_asset_paths(html)
    html = escape_razor_at(html)
    return html


def process_main(html: str) -> str:
    html = convert_page_links(html)
    html = convert_asset_paths(html)
    html = escape_razor_at(html)
    return html


def extract_main(page_path: Path) -> str:
    text = read(page_path)
    s = text.index("<main>")
    e = text.index("</main>")
    return text[s + len("<main>") : e].strip()


SETTINGS_HEADER = r"""@using CMS.Application.Common.Features
@using CMS.Application.Settings
@using Microsoft.FeatureManagement
@inject IFeatureManager FeatureManager
@inject ISiteSettingsService SiteSettings
@{
    var settings = await SiteSettings.GetAsync();
    var brand = string.IsNullOrWhiteSpace(settings.SiteName) ? "نووا کلینیک" : settings.SiteName;
    var logoUrl = string.IsNullOrWhiteSpace(settings.LogoUrl)
        ? Url.Content("~/template/assets/images/common/logo-nova-transparent.png")
        : settings.LogoUrl;
    var phoneRaw = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "02191093492" : settings.ContactPhone;
    var phoneDigits = System.Text.RegularExpressions.Regex.Replace(phoneRaw, @"[^\d+]", "");
    if (string.IsNullOrWhiteSpace(phoneDigits)) { phoneDigits = "02191093492"; }
    var phoneDisplay = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "۰۲۱-۹۱۰۹۳۴۹۲" : settings.ContactPhone;
    var telHref = "tel:" + phoneDigits;
    var nav = (ViewData["NavActive"] as string) ?? "";
    bool IsActive(string key) => string.Equals(nav, key, StringComparison.OrdinalIgnoreCase);
    var servicesOn = await FeatureManager.IsEnabledAsync(FeatureNames.Services);
    var videoOn = await FeatureManager.IsEnabledAsync(FeatureNames.Video);
    var newsOn = await FeatureManager.IsEnabledAsync(FeatureNames.News);
    var blogOn = await FeatureManager.IsEnabledAsync(FeatureNames.Blog);
    var teamOn = await FeatureManager.IsEnabledAsync(FeatureNames.Team);
}
"""

SETTINGS_PHONE = r"""@using CMS.Application.Settings
@inject ISiteSettingsService SiteSettings
@{
    var settings = await SiteSettings.GetAsync();
    var phoneRaw = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "02191093492" : settings.ContactPhone;
    var phoneDigits = System.Text.RegularExpressions.Regex.Replace(phoneRaw, @"[^\d+]", "");
    if (string.IsNullOrWhiteSpace(phoneDigits)) { phoneDigits = "02191093492"; }
    var phoneDisplay = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "۰۲۱-۹۱۰۹۳۴۹۲" : settings.ContactPhone;
    var telHref = "tel:" + phoneDigits;
}
"""

SETTINGS_FOOTER = r"""@using CMS.Application.Settings
@inject ISiteSettingsService SiteSettings
@{
    var settings = await SiteSettings.GetAsync();
    var brand = string.IsNullOrWhiteSpace(settings.SiteName) ? "نووا کلینیک" : settings.SiteName;
    var logoUrl = string.IsNullOrWhiteSpace(settings.LogoUrl)
        ? Url.Content("~/template/assets/images/common/logo-nova-transparent.png")
        : settings.LogoUrl;
    var phoneRaw = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "02191093492" : settings.ContactPhone;
    var phoneDigits = System.Text.RegularExpressions.Regex.Replace(phoneRaw, @"[^\d+]", "");
    if (string.IsNullOrWhiteSpace(phoneDigits)) { phoneDigits = "02191093492"; }
    var phoneDisplay = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "۰۲۱-۹۱۰۹۳۴۹۲" : settings.ContactPhone;
    var telHref = "tel:" + phoneDigits;
    var email = string.IsNullOrWhiteSpace(settings.ContactEmail) ? "info@nova-clinic.org" : settings.ContactEmail;
    var address = string.IsNullOrWhiteSpace(settings.Address)
        ? "شریعتی، نبش کوچه ذکایی، ساختمان کسری، طبقه ۴ واحد ۱۵"
        : settings.Address;
    var footerText = string.IsNullOrWhiteSpace(settings.FooterText)
        ? "اولین کلینیک فلوشیپ جراحی غدد درون‌ریز ایران، تحت نظارت دکتر کاوه همدانی."
        : settings.FooterText;
    var year = DateTime.Now.Year;
}
"""


def apply_phone_tokens(html: str) -> str:
    return (
        html.replace('href="tel:02191093492"', 'href="@telHref"')
        .replace("۰۲۱-۹۱۰۹۳۴۹۲", "@phoneDisplay")
    )


def enhance_header(html: str) -> str:
    html = html.replace(' class="is-active"', "")
    html = html.replace(' aria-current="page"', "")
    html = apply_phone_tokens(html)
    html = html.replace(
        'src="~/template/assets/images/common/logo-nova-transparent.png"',
        'src="@logoUrl"',
    )
    html = html.replace('alt="لوگوی نووا کلینیک"', 'alt="@brand"')
    html = html.replace(
        'aria-label="نووا کلینیک — صفحه اصلی"',
        'aria-label="@brand — صفحه اصلی"',
    )

    html = html.replace(
        '<li><a asp-controller="Home" asp-action="Index">صفحه اصلی</a></li>',
        '<li><a class="@(IsActive("home") ? "is-active" : null)" asp-controller="Home" asp-action="Index" aria-current="@(IsActive("home") ? "page" : null)">صفحه اصلی</a></li>',
    )
    html = html.replace(
        '<a asp-controller="Services" asp-action="Index" aria-haspopup="true"',
        '<a class="@(IsActive("services") ? "is-active" : null)" asp-controller="@(servicesOn ? "Services" : "Home")" asp-action="Index" aria-current="@(IsActive("services") ? "page" : null)" aria-haspopup="true"',
    )
    html = html.replace(
        '<li><a asp-controller="Videos" asp-action="Index">ویدئو ها</a></li>',
        '@if (videoOn)\n            {\n            <li><a class="@(IsActive("videos") ? "is-active" : null)" asp-controller="Videos" asp-action="Index" aria-current="@(IsActive("videos") ? "page" : null)">ویدئو ها</a></li>\n            }',
    )
    html = html.replace(
        '<li><a asp-controller="News" asp-action="Index">مقالات تخصصی</a></li>',
        '@if (newsOn)\n            {\n            <li><a class="@(IsActive("articles") ? "is-active" : null)" asp-controller="News" asp-action="Index" aria-current="@(IsActive("articles") ? "page" : null)">مقالات تخصصی</a></li>\n            }',
    )
    html = html.replace(
        '<li><a asp-controller="Blog" asp-action="Index">وبلاگ</a></li>',
        '@if (blogOn)\n            {\n            <li><a class="@(IsActive("blog") ? "is-active" : null)" asp-controller="Blog" asp-action="Index" aria-current="@(IsActive("blog") ? "page" : null)">وبلاگ</a></li>\n            }',
    )
    html = html.replace(
        '<a asp-controller="Home" asp-action="About" aria-haspopup="true"',
        '<a class="@(IsActive("about") || IsActive("contact") || IsActive("doctors") || IsActive("events") ? "is-active" : null)" asp-controller="Home" asp-action="About" aria-current="@(IsActive("about") ? "page" : null)" aria-haspopup="true"',
    )
    # Also handle about link that already had is-active stripped from class="is-active" on about pages
    html = html.replace(
        '<a class="is-active" asp-controller="Home" asp-action="About"',
        '<a asp-controller="Home" asp-action="About"',
    )
    return html


def enhance_footer(html: str) -> str:
    html = apply_phone_tokens(html)
    html = html.replace(
        'src="~/template/assets/images/common/logo-nova-transparent.png"',
        'src="@logoUrl"',
    )
    html = html.replace('aria-label="نووا کلینیک"', 'aria-label="@brand"')
    html = html.replace('alt="نووا کلینیک"', 'alt="@brand"')
    html = html.replace(
        "اولین کلینیک فلوشیپ جراحی غدد درون‌ریز ایران، تحت نظارت دکتر کاوه همدانی.",
        "@footerText",
    )
    html = html.replace("info@nova-clinic.org", "@email")
    html = html.replace(
        "شریعتی، نبش کوچه ذکایی، ساختمان کسری، طبقه ۴ واحد ۱۵",
        "@address",
    )
    html = html.replace("© نووا کلینیک · نظام پزشکی ۱۱۰۶۱۴", "© @year @brand")
    html = html.replace(
        'href="https://www.instagram.com/dr_kaveh_hamedani"',
        'href="@(settings.InstagramUrl ?? "https://www.instagram.com/dr_kaveh_hamedani")"',
    )
    html = html.replace(
        'href="https://www.linkedin.com/company/nova-clinic-iran"',
        'href="@(settings.LinkedInUrl ?? "https://www.linkedin.com/company/nova-clinic-iran")"',
    )
    html = html.replace(
        'href="https://www.youtube.com/@novaclinic_org"',
        'href="@(settings.YouTubeUrl ?? "https://www.youtube.com/@novaclinic_org")"',
    )
    html = html.replace(
        'href="https://www.aparat.com/"',
        'href="@(settings.AparatUrl ?? "https://www.aparat.com/")"',
    )
    return html


def main() -> None:
    index = read(TEMPLATE / "index.html")

    header = slice_between(index, '<header class="site-header">', "</header>", include_end=True)
    backdrop = slice_between(index, '<div class="nav-backdrop"', "</div>", include_end=True)
    booking = slice_between(index, '<div class="booking-modal"', "  <main>").rstrip() + "\n"
    footer = slice_between(index, '<footer class="site-footer">', "</footer>", include_end=True)

    float_start = index.index('<div class="float-call"')
    script_start = index.index('<script src="assets/js/main.js">')
    float_call = index[float_start:script_start].strip()

    main_start = index.index("<main>")
    main_end = index.index("</main>")
    home_main = index[main_start + len("<main>") : main_end].strip()

    header_html = enhance_header(process_shell(header))
    booking_html = apply_phone_tokens(process_shell(booking))
    footer_html = enhance_footer(process_shell(footer))
    float_html = apply_phone_tokens(process_shell(float_call))

    write(
        VIEWS_SHARED / "_NovaHeader.cshtml",
        "@* Nova template header *@\n" + SETTINGS_HEADER + header_html + "\n",
    )
    write(
        VIEWS_SHARED / "_NovaBookingModal.cshtml",
        "@* Nova booking modal *@\n" + SETTINGS_PHONE + booking_html + "\n",
    )
    write(
        VIEWS_SHARED / "_NovaFooter.cshtml",
        "@* Nova template footer *@\n" + SETTINGS_FOOTER + footer_html + "\n",
    )
    write(
        VIEWS_SHARED / "_NovaFloatCall.cshtml",
        SETTINGS_PHONE + float_html + "\n",
    )
    write(VIEWS_SHARED / "_NovaNavBackdrop.cshtml", backdrop + "\n")

    write(
        VIEWS_HOME / "Index.cshtml",
        '@{\n    ViewData["Title"] = "خانه";\n    ViewData["NavActive"] = "home";\n}\n'
        + process_main(home_main)
        + "\n",
    )

    about_main = extract_main(TEMPLATE / "pages" / "about.html")
    contact_main = extract_main(TEMPLATE / "pages" / "contact.html")

    write(
        VIEWS_HOME / "About.cshtml",
        '@{\n    ViewData["Title"] = "درباره ما";\n    ViewData["NavActive"] = "about";\n}\n'
        + process_main(about_main)
        + "\n",
    )
    write(
        VIEWS_HOME / "Contact.cshtml",
        '@{\n    ViewData["Title"] = "تماس با ما";\n    ViewData["NavActive"] = "contact";\n}\n'
        + process_main(contact_main)
        + "\n",
    )

    print("Wrote Nova Razor partials and Home views.")


if __name__ == "__main__":
    main()
