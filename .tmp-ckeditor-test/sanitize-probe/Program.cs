using CMS.Infrastructure.Security;
var s = new HtmlContentSanitizer();
var html = "<p>??? <span class=\"nova-scroll-highlight\" data-nova-scroll-highlight=\"1\" data-nova-sh-from=\"#6b7280\" data-nova-sh-to=\"#111827\" style=\"color:#6b7280\">???</span> ???</p>";
var outHtml = s.Sanitize(html);
Console.WriteLine(outHtml);
Console.WriteLine(outHtml.Contains("nova-scroll-highlight") ? "CLASS_OK" : "CLASS_GONE");
Console.WriteLine(outHtml.Contains("data-nova-sh-from") ? "DATA_OK" : "DATA_GONE");
Console.WriteLine(outHtml.Contains("color") ? "STYLE_OK" : "STYLE_GONE");
