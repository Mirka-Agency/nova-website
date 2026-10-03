# -*- coding: utf-8 -*-
"""Port template services.html main into Services/Index.cshtml with MVC links."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "src" / "CMS.Web" / "wwwroot" / "template" / "pages" / "services.html"
OUT = ROOT / "src" / "CMS.Modules" / "Services" / "Web" / "Views" / "Services" / "Index.cshtml"


def escape_at(html: str) -> str:
    return html.replace("@", "@@")


def convert_links(html: str) -> str:
    replacements = [
        ('href="../index.html"', 'asp-controller="Home" asp-action="Index"'),
        ('href="service-single.html"', 'asp-controller="Services" asp-action="Index" asp-fragment="thyroid"'),
        ('href="services.html#parathyroid"', 'href="#parathyroid"'),
        ('href="services.html#adrenal"', 'href="#adrenal"'),
        ('href="services.html#pancreas"', 'href="#pancreas"'),
        ('href="doctors.html"', 'asp-controller="Teams" asp-action="Index"'),
        ('href="articles.html"', 'asp-controller="News" asp-action="Index"'),
        ('href="../index.html#journey"', 'asp-controller="Home" asp-action="Index" asp-fragment="journey"'),
        ('href="../index.html#faq"', 'asp-controller="Home" asp-action="Index" asp-fragment="faq"'),
        ('href="mailto:info@nova-clinic.org"', 'href="mailto:@(contactEmail)"'),
    ]
    for old, new in replacements:
        html = html.replace(old, new)
    # first service-single in groups should point to thyroid detail anchor / first service
    html = html.replace(
        'href="service-single.html"',
        'href="#thyroid"',
    )
    return html


def main() -> None:
    text = TEMPLATE.read_text(encoding="utf-8")
    s = text.index("<main>")
    e = text.index("</main>")
    body = text[s + len("<main>") : e].strip()
    body = convert_links(body)
    body = escape_at(body)

    # Restore intentional Razor tokens after escape
    body = body.replace("mailto:@@(contactEmail)", "mailto:@(contactEmail)")
    body = body.replace("info@@nova-clinic.org", "@contactEmail")
    body = body.replace('href="tel:02191093492"', 'href="@telHref"')
    body = body.replace("۰۲۱-۹۱۰۹۳۴۹۲", "@phoneDisplay")
    body = body.replace(
        "شریعتی، بالاتر از خواجه عبدالله، نبش کوچه ذکایی (جنب آرش موتورز)، ساختمان کسری، طبقه ۴، واحد ۱۵",
        "@address",
    )

    # Point first group card (thyroid) to #thyroid after broken service-single replace
    body = body.replace(
        '<a class="service-group reveal" asp-controller="Services" asp-action="Index">',
        '<a class="service-group reveal" href="#thyroid">',
        1,
    )

    razor = f"""@model IReadOnlyList<PublicServiceItemSummaryDto>
@using CMS.Application.Settings
@using System.Net
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
    var hasCmsServices = Model.Count > 0;
}}

{body}
"""

    # If CMS has services, we need dynamic blocks. Inject after page-hero a conditional.
    # Simpler: replace the static overview+details with a partial marker and write a smarter view manually.
    OUT.write_text(razor, encoding="utf-8", newline="\n")
    print("wrote", OUT, "chars", len(razor))


if __name__ == "__main__":
    main()
