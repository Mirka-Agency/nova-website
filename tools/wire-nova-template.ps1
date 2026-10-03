# Converts wwwroot/template HTML into Razor views/partials for CMS.Web
$ErrorActionPreference = 'Stop'
$root = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { 'd:\work\nova-website' }
$web = Join-Path $root 'src\CMS.Web'
$template = Join-Path $web 'wwwroot\template'
$viewsShared = Join-Path $web 'Views\Shared'
$viewsHome = Join-Path $web 'Views\Home'

function Get-FileText([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))
}

function Write-Utf8([string]$path, [string]$content) {
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
    [System.IO.File]::WriteAllText($path, $content, [System.Text.UTF8Encoding]::new($false))
}

function Slice-Between([string]$text, [string]$startMarker, [string]$endMarker, [switch]$includeEnd) {
    $i = $text.IndexOf($startMarker)
    if ($i -lt 0) { throw "Start not found: $startMarker" }
    $j = $text.IndexOf($endMarker, $i + $startMarker.Length)
    if ($j -lt 0) { throw "End not found: $endMarker" }
    if ($includeEnd) {
        return $text.Substring($i, $j + $endMarker.Length - $i)
    }
    return $text.Substring($i, $j - $i)
}

function Convert-AssetPaths([string]$html) {
    $html = $html -replace '\.\./assets/', '~/template/assets/'
    $html = $html -replace '(src|href|poster)="assets/', '$1="~/template/assets/'
    $html = $html -replace "url\('assets/", "url('~/template/assets/"
    $html = $html -replace 'url\("assets/', 'url("~/template/assets/'
    return $html
}

function Convert-PageLinks([string]$html) {
    $map = [ordered]@{
        'href="index.html"' = 'asp-controller="Home" asp-action="Index"'
        'href="../index.html"' = 'asp-controller="Home" asp-action="Index"'
        'href="pages/services.html"' = 'asp-controller="Services" asp-action="Index"'
        'href="services.html"' = 'asp-controller="Services" asp-action="Index"'
        'href="../pages/services.html"' = 'asp-controller="Services" asp-action="Index"'
        'href="pages/services.html#parathyroid"' = 'asp-controller="Services" asp-action="Index" asp-fragment="parathyroid"'
        'href="services.html#parathyroid"' = 'asp-controller="Services" asp-action="Index" asp-fragment="parathyroid"'
        'href="pages/services.html#adrenal"' = 'asp-controller="Services" asp-action="Index" asp-fragment="adrenal"'
        'href="services.html#adrenal"' = 'asp-controller="Services" asp-action="Index" asp-fragment="adrenal"'
        'href="pages/services.html#pancreas"' = 'asp-controller="Services" asp-action="Index" asp-fragment="pancreas"'
        'href="services.html#pancreas"' = 'asp-controller="Services" asp-action="Index" asp-fragment="pancreas"'
        'href="pages/service-single.html"' = 'asp-controller="Services" asp-action="Index"'
        'href="service-single.html"' = 'asp-controller="Services" asp-action="Index"'
        'href="pages/videos.html"' = 'asp-controller="Videos" asp-action="Index"'
        'href="videos.html"' = 'asp-controller="Videos" asp-action="Index"'
        'href="pages/articles.html"' = 'asp-controller="News" asp-action="Index"'
        'href="articles.html"' = 'asp-controller="News" asp-action="Index"'
        'href="pages/blog.html"' = 'asp-controller="Blog" asp-action="Index"'
        'href="blog.html"' = 'asp-controller="Blog" asp-action="Index"'
        'href="pages/about.html"' = 'asp-controller="Home" asp-action="About"'
        'href="about.html"' = 'asp-controller="Home" asp-action="About"'
        'href="pages/contact.html"' = 'asp-controller="Home" asp-action="Contact"'
        'href="contact.html"' = 'asp-controller="Home" asp-action="Contact"'
        'href="pages/doctors.html"' = 'asp-controller="Teams" asp-action="Index"'
        'href="doctors.html"' = 'asp-controller="Teams" asp-action="Index"'
        'href="pages/events.html"' = 'asp-controller="News" asp-action="Index"'
        'href="events.html"' = 'asp-controller="News" asp-action="Index"'
        'href="pages/doctor-takyar.html"' = 'asp-controller="Teams" asp-action="Index"'
        'href="doctor-takyar.html"' = 'asp-controller="Teams" asp-action="Index"'
        'href="pages/doctor-single.html"' = 'asp-controller="Teams" asp-action="Index"'
        'href="doctor-single.html"' = 'asp-controller="Teams" asp-action="Index"'
    }

    foreach ($key in $map.Keys) {
        $attr = $map[$key]
        # Replace href="..." with tag helpers on <a ...>
        $html = $html.Replace($key, $attr)
    }

    # Fix anchors that became invalid: <a asp-controller=...> — already fine as attributes
    # Handle leftover relative page links
    $html = $html -replace 'href="pages/[^"]+\.html"', 'asp-controller="Home" asp-action="Index"'
    return $html
}

function Convert-PhoneAndContact([string]$html) {
    # Leave static Nova phone as fallback content; layout will inject settings where we use tokens
    return $html
}

function Clean-MobileNavDupes([string]$html) {
    # Template mobile nav has a broken duplicate block; keep first clean list only by removing known junk if present
    $junk = @'
            </li>
            <li><a href="pages/doctors.html">متخصصان کلینیک</a></li>
            <li><a href="pages/events.html">رویدادها</a></li>
            <li><a href="pages/videos.html">ویدئو ها</a></li>
            <li><a href="pages/articles.html">مقالات تخصصی</a></li>
            <li><a href="pages/blog.html">وبلاگ</a></li>
            <li><a href="pages/about.html">درباره ما</a></li>
            <li><a href="pages/contact.html">تماس باما</a></li>
          </ul>
'@
    $junk2 = $junk.Replace('pages/', '')
    if ($html.Contains($junk)) { $html = $html.Replace($junk, "          </ul>`r`n") }
    # After link conversion junk may already be converted — skip if not found
    return $html
}

$indexPath = Join-Path $template 'index.html'
$index = Get-FileText $indexPath

$header = Slice-Between $index '<header class="site-header">' '</header>' -includeEnd
$backdrop = Slice-Between $index '<div class="nav-backdrop"' '</div>' -includeEnd
$booking = Slice-Between $index '<div class="booking-modal"' "  <main>" 
$booking = $booking.TrimEnd() + "`r`n"
$footer = Slice-Between $index '<footer class="site-footer">' '</footer>' -includeEnd
$floatStart = $index.IndexOf('<div class="float-call"')
$scriptStart = $index.IndexOf('<script src="assets/js/main.js">')
$float = $index.Substring($floatStart, $scriptStart - $floatStart).Trim()

$mainStart = $index.IndexOf('<main>')
$mainEnd = $index.IndexOf('</main>')
$homeMain = $index.Substring($mainStart + '<main>'.Length, $mainEnd - $mainStart - '<main>'.Length).Trim()

function Process-Shell([string]$html) {
    $html = Clean-MobileNavDupes $html
    $html = Convert-PageLinks $html
    $html = Convert-AssetPaths $html
    return $html
}

$headerOut = Process-Shell $header
$bookingOut = Process-Shell $booking
$footerOut = Process-Shell $footer
$floatOut = Process-Shell $float
$backdropOut = $backdrop

# Active nav: strip static is-active; layout/partial can add via ViewData later — keep home active only when NavActive == home
$headerOut = $headerOut -replace ' class="is-active"' , ''
$headerOut = $headerOut -replace ' aria-current="page"' , ''

$partialHeader = @"
@* Generated from wwwroot/template — Nova header *@
@using CMS.Application.Common.Features
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

$headerOut
"@

# Replace hardcoded tel/logo in header with dynamic values via simple replacements after generation
$partialHeader = $partialHeader.Replace('href="tel:02191093492"', 'href="@telHref"')
$partialHeader = $partialHeader.Replace('۰۲۱-۹۱۰۹۳۴۹۲', '@phoneDisplay')
$partialHeader = $partialHeader.Replace(
    'src="~/template/assets/images/common/logo-nova-transparent.png"',
    'src="@logoUrl"')
$partialHeader = $partialHeader.Replace('alt="لوگوی نووا کلینیک"', 'alt="@brand"')
$partialHeader = $partialHeader.Replace('aria-label="نووا کلینیک — صفحه اصلی"', 'aria-label="@brand — صفحه اصلی"')

# Inject is-active markers on key links — do lightweight post-process with placeholders
$partialHeader = $partialHeader.Replace(
    '<li><a asp-controller="Home" asp-action="Index">صفحه اصلی</a></li>',
    '<li><a class="@(IsActive("home") ? "is-active" : null)" asp-controller="Home" asp-action="Index" aria-current="@(IsActive("home") ? "page" : null)">صفحه اصلی</a></li>')
$partialHeader = $partialHeader.Replace(
    '<a asp-controller="Services" asp-action="Index" aria-haspopup="true"',
    '<a class="@(IsActive("services") ? "is-active" : null)" asp-controller="Services" asp-action="Index" aria-current="@(IsActive("services") ? "page" : null)" aria-haspopup="true"')
$partialHeader = $partialHeader.Replace(
    '<li><a asp-controller="Videos" asp-action="Index">ویدئو ها</a></li>',
    '@if (videoOn) {<li><a class="@(IsActive("videos") ? "is-active" : null)" asp-controller="Videos" asp-action="Index" aria-current="@(IsActive("videos") ? "page" : null)">ویدئو ها</a></li>}')
$partialHeader = $partialHeader.Replace(
    '<li><a asp-controller="News" asp-action="Index">مقالات تخصصی</a></li>',
    '@if (newsOn) {<li><a class="@(IsActive("articles") ? "is-active" : null)" asp-controller="News" asp-action="Index" aria-current="@(IsActive("articles") ? "page" : null)">مقالات تخصصی</a></li>}')
$partialHeader = $partialHeader.Replace(
    '<li><a asp-controller="Blog" asp-action="Index">وبلاگ</a></li>',
    '@if (blogOn) {<li><a class="@(IsActive("blog") ? "is-active" : null)" asp-controller="Blog" asp-action="Index" aria-current="@(IsActive("blog") ? "page" : null)">وبلاگ</a></li>}')
$partialHeader = $partialHeader.Replace(
    '<a class="is-active" href=',
    '<a href=') # safety

# Wrap services dropdown in feature flag — if services off, hide dropdown parent carefully
# Keep as-is for now; Services links remain even if feature off (controller will 404/disabled)

$partialBooking = @"
@* Generated from wwwroot/template — booking modal *@
@using CMS.Application.Settings
@inject ISiteSettingsService SiteSettings
@{
    var settings = await SiteSettings.GetAsync();
    var phoneRaw = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "02191093492" : settings.ContactPhone;
    var phoneDigits = System.Text.RegularExpressions.Regex.Replace(phoneRaw, @"[^\d+]", "");
    if (string.IsNullOrWhiteSpace(phoneDigits)) { phoneDigits = "02191093492"; }
    var phoneDisplay = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "۰۲۱-۹۱۰۹۳۴۹۲" : settings.ContactPhone;
    var telHref = "tel:" + phoneDigits;
}
$(Process-Shell $booking)
"@
$partialBooking = $partialBooking.Replace('href="tel:02191093492"', 'href="@telHref"')
$partialBooking = $partialBooking.Replace('۰۲۱-۹۱۰۹۳۴۹۲', '@phoneDisplay')

$partialFooter = @"
@* Generated from wwwroot/template — Nova footer *@
@using CMS.Application.Common.Features
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
    var email = string.IsNullOrWhiteSpace(settings.ContactEmail) ? "info@nova-clinic.org" : settings.ContactEmail;
    var address = string.IsNullOrWhiteSpace(settings.Address)
        ? "شریعتی، نبش کوچه ذکایی، ساختمان کسری، طبقه ۴ واحد ۱۵"
        : settings.Address;
    var footerText = string.IsNullOrWhiteSpace(settings.FooterText)
        ? "اولین کلینیک فلوشیپ جراحی غدد درون‌ریز ایران، تحت نظارت دکتر کاوه همدانی."
        : settings.FooterText;
    var year = DateTime.Now.Year;
}
$(Process-Shell $footer)
"@
$partialFooter = $partialFooter.Replace('href="tel:02191093492"', 'href="@telHref"')
$partialFooter = $partialFooter.Replace('۰۲۱-۹۱۰۹۳۴۹۲', '@phoneDisplay')
$partialFooter = $partialFooter.Replace(
    'src="~/template/assets/images/common/logo-nova-transparent.png"',
    'src="@logoUrl"')
$partialFooter = $partialFooter.Replace('aria-label="نووا کلینیک"', 'aria-label="@brand"')
$partialFooter = $partialFooter.Replace('alt="نووا کلینیک"', 'alt="@brand"')
$partialFooter = $partialFooter.Replace(
    'اولین کلینیک فلوشیپ جراحی غدد درون‌ریز ایران، تحت نظارت دکتر کاوه همدانی.',
    '@footerText')
$partialFooter = $partialFooter.Replace('info@nova-clinic.org', '@email')
$partialFooter = $partialFooter.Replace('mailto:@email', 'mailto:@email') # already ok
$partialFooter = $partialFooter.Replace(
    'شریعتی، نبش کوچه ذکایی، ساختمان کسری، طبقه ۴ واحد ۱۵',
    '@address')
$partialFooter = $partialFooter.Replace('© نووا کلینیک · نظام پزشکی ۱۱۰۶۱۴', '© @year @brand')

# Social links from settings when present
$partialFooter = $partialFooter -replace 'href="https://www.instagram.com/dr_kaveh_hamedani"', 'href="@(settings.InstagramUrl ?? "https://www.instagram.com/dr_kaveh_hamedani")"'
$partialFooter = $partialFooter -replace 'href="https://www.linkedin.com/company/nova-clinic-iran"', 'href="@(settings.LinkedInUrl ?? "https://www.linkedin.com/company/nova-clinic-iran")"'
$partialFooter = $partialFooter -replace 'href="https://www.youtube.com/@novaclinic_org"', 'href="@(settings.YouTubeUrl ?? "https://www.youtube.com/@novaclinic_org")"'
$partialFooter = $partialFooter -replace 'href="https://www.aparat.com/"', 'href="@(settings.AparatUrl ?? "https://www.aparat.com/")"'

$partialFloat = @"
@using CMS.Application.Settings
@inject ISiteSettingsService SiteSettings
@{
    var settings = await SiteSettings.GetAsync();
    var phoneRaw = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "02191093492" : settings.ContactPhone;
    var phoneDigits = System.Text.RegularExpressions.Regex.Replace(phoneRaw, @"[^\d+]", "");
    if (string.IsNullOrWhiteSpace(phoneDigits)) { phoneDigits = "02191093492"; }
    var phoneDisplay = string.IsNullOrWhiteSpace(settings.ContactPhone) ? "۰۲۱-۹۱۰۹۳۴۹۲" : settings.ContactPhone;
    var telHref = "tel:" + phoneDigits;
}
$(Process-Shell $float)
"@
$partialFloat = $partialFloat.Replace('href="tel:02191093492"', 'href="@telHref"')
$partialFloat = $partialFloat.Replace('۰۲۱-۹۱۰۹۳۴۹۲', '@phoneDisplay')

$homeView = @"
@{
    ViewData["Title"] = "خانه";
    ViewData["NavActive"] = "home";
}
$($(Convert-AssetPaths (Convert-PageLinks $homeMain)))
"@

function Extract-Main([string]$pagePath) {
    $text = Get-FileText $pagePath
    $s = $text.IndexOf('<main>')
    $e = $text.IndexOf('</main>')
    if ($s -lt 0 -or $e -lt 0) { throw "main not found in $pagePath" }
    return $text.Substring($s + 6, $e - $s - 6).Trim()
}

$aboutMain = Extract-Main (Join-Path $template 'pages\about.html')
$contactMain = Extract-Main (Join-Path $template 'pages\contact.html')

$aboutView = @"
@{
    ViewData["Title"] = "درباره ما";
    ViewData["NavActive"] = "about";
}
$($(Convert-AssetPaths (Convert-PageLinks $aboutMain)))
"@

$contactView = @"
@{
    ViewData["Title"] = "تماس با ما";
    ViewData["NavActive"] = "contact";
}
$($(Convert-AssetPaths (Convert-PageLinks $contactMain)))
"@

Write-Utf8 (Join-Path $viewsShared '_NovaHeader.cshtml') $partialHeader
Write-Utf8 (Join-Path $viewsShared '_NovaBookingModal.cshtml') $partialBooking
Write-Utf8 (Join-Path $viewsShared '_NovaFooter.cshtml') $partialFooter
Write-Utf8 (Join-Path $viewsShared '_NovaFloatCall.cshtml') $partialFloat
Write-Utf8 (Join-Path $viewsShared '_NovaNavBackdrop.cshtml') $backdropOut
Write-Utf8 (Join-Path $viewsHome 'Index.cshtml') $homeView
Write-Utf8 (Join-Path $viewsHome 'About.cshtml') $aboutView
Write-Utf8 (Join-Path $viewsHome 'Contact.cshtml') $contactView

Write-Host 'Wrote Nova Razor partials and Home views.'
