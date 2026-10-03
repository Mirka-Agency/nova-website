# -*- coding: utf-8 -*-
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
HOME = ROOT / "src" / "CMS.Web" / "Views" / "Home"

LINK_MAP = [
    (r'href="pages/services\.html#parathyroid"', 'asp-controller="Services" asp-action="Index" asp-fragment="parathyroid"'),
    (r'href="pages/services\.html#adrenal"', 'asp-controller="Services" asp-action="Index" asp-fragment="adrenal"'),
    (r'href="pages/services\.html#pancreas"', 'asp-controller="Services" asp-action="Index" asp-fragment="pancreas"'),
    (r'href="pages/services\.html"', 'asp-controller="Services" asp-action="Index"'),
    (r'href="pages/service-single\.html"', 'asp-controller="Services" asp-action="Index"'),
    (r'href="pages/videos\.html"', 'asp-controller="Videos" asp-action="Index"'),
    (r'href="pages/video-single\.html"', 'asp-controller="Videos" asp-action="Index"'),
    (r'href="pages/articles\.html"', 'asp-controller="News" asp-action="Index"'),
    (r'href="pages/article-single\.html"', 'asp-controller="News" asp-action="Index"'),
    (r'href="pages/blog\.html"', 'asp-controller="Blog" asp-action="Index"'),
    (r'href="pages/blog-single\.html"', 'asp-controller="Blog" asp-action="Index"'),
    (r'href="pages/about\.html"', 'asp-controller="Home" asp-action="About"'),
    (r'href="pages/contact\.html"', 'asp-controller="Home" asp-action="Contact"'),
    (r'href="pages/doctors\.html"', 'asp-controller="Teams" asp-action="Index"'),
    (r'href="pages/doctor-single\.html"', 'asp-controller="Teams" asp-action="Index"'),
    (r'href="pages/doctor-takyar\.html"', 'asp-controller="Teams" asp-action="Index"'),
    (r'href="pages/events\.html"', 'asp-controller="News" asp-action="Index"'),
    (r'href="pages/event-single\.html"', 'asp-controller="News" asp-action="Index"'),
    (r'href="index\.html"', 'asp-controller="Home" asp-action="Index"'),
]

for path in HOME.glob("*.cshtml"):
    text = path.read_text(encoding="utf-8")
    original = text
    for pattern, repl in LINK_MAP:
        text = re.sub(pattern, repl, text)
    # assets that may have been missed
    text = text.replace('../assets/', '~/template/assets/')
    text = re.sub(r'(src|href|poster)="assets/', r'\1="~/template/assets/', text)
    if text != original:
        path.write_text(text, encoding="utf-8", newline="\n")
        print("updated", path.name)
    else:
        print("ok", path.name)
