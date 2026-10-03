using CMS.Modules.Services.Application.ServiceItems;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Infrastructure;

/// <summary>
/// Resolves template service-single links to CMS Details pages when published items exist.
/// </summary>
public static class NovaServiceNav
{
    public static PublicServiceItemSummaryDto? Find(
        IReadOnlyList<PublicServiceItemSummaryDto> items,
        params string[] needles)
    {
        foreach (var item in items)
        {
            var hay = $"{item.Slug} {item.Title} {item.CategoryName} {item.Excerpt}";
            foreach (var needle in needles)
            {
                if (hay.Contains(needle, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
        }

        return null;
    }

    public static string Href(
        IUrlHelper url,
        PublicServiceItemSummaryDto? match,
        string fragment)
    {
        if (match is not null)
            return url.Action("Details", "Services", new { slug = match.Slug })!;

        return url.Action("Index", "Services") + "#" + fragment;
    }
}
