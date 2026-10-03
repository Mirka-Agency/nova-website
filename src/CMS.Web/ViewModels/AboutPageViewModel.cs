using CMS.Modules.Honors.Application.HonorItems;
using CMS.Modules.Team.Application.TeamItems;

namespace CMS.Web.ViewModels;

public sealed class AboutPageViewModel
{
    public IReadOnlyList<PublicTeamItemSummaryDto> Team { get; init; } =
        Array.Empty<PublicTeamItemSummaryDto>();

    public IReadOnlyList<PublicHonorItemDto> Honors { get; init; } =
        Array.Empty<PublicHonorItemDto>();
}
