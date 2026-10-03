namespace CMS.Modules.Shop.Application.Locations;

public sealed record IranProvinceDto(Guid Id, string Name);

public sealed record IranCityDto(Guid Id, Guid ProvinceId, string Name);

public interface IIranLocationService
{
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IranProvinceDto>> ListProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IranCityDto>> ListCitiesAsync(Guid provinceId, CancellationToken cancellationToken = default);
    Task<bool> IsValidLocationAsync(string provinceName, string cityName, CancellationToken cancellationToken = default);
}
