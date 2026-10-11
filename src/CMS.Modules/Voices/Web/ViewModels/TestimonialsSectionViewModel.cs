using CMS.Modules.Voices.Application.VoiceItems;

namespace CMS.Modules.Voices.Web.ViewModels;

public sealed class TestimonialsSectionViewModel
{
    public required string Brand { get; init; }
    public IReadOnlyList<PublicVoiceItemDto> Voices { get; init; } =
        Array.Empty<PublicVoiceItemDto>();

    public bool HasCmsVoices => Voices.Count > 0;
}
