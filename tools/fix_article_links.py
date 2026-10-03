# -*- coding: utf-8 -*-
from pathlib import Path
import re

path = Path(__file__).resolve().parents[1] / "src" / "CMS.Web" / "Views" / "Home" / "Index.cshtml"
text = path.read_text(encoding="utf-8")
text2 = re.sub(
    r'(<a class="article-item[^>]*?)asp-controller="Home" asp-action="Index"',
    r'\1asp-controller="Blog" asp-action="Index"',
    text,
)
path.write_text(text2, encoding="utf-8", newline="\n")
print("changed" if text != text2 else "unchanged")
