using CMS.Modules.News.Application.Articles;
using CMS.Modules.Services.Application.ServiceItems;
using CMS.Modules.Team.Application.TeamItems;

namespace CMS.Web.ViewModels;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<PublicServiceItemSummaryDto> Services { get; init; } =
        Array.Empty<PublicServiceItemSummaryDto>();

    public IReadOnlyList<PublicTeamItemSummaryDto> Team { get; init; } =
        Array.Empty<PublicTeamItemSummaryDto>();

    public IReadOnlyList<PublicArticleSummaryDto> Articles { get; init; } =
        Array.Empty<PublicArticleSummaryDto>();
}
