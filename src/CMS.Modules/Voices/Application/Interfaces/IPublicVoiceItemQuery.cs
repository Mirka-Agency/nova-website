using CMS.Modules.Voices.Application.VoiceItems;

namespace CMS.Modules.Voices.Application.Interfaces;

public interface IPublicVoiceItemQuery
{
    Task<IReadOnlyList<PublicVoiceItemDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
}
