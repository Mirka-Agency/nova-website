using CMS.Modules.Shop.Application.Locations;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Shop.Infrastructure.Locations;

public sealed class IranLocationService : IIranLocationService
{
    private readonly ShopDbContext _db;
    private readonly ILogger<IranLocationService> _logger;

    public IranLocationService(ShopDbContext db, ILogger<IranLocationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        var existingProvinceCount = await _db.IranProvinces.CountAsync(cancellationToken);
        if (existingProvinceCount >= IranLocationsCatalog.All.Count)
            return;

        _logger.LogInformation("Seeding Iran provinces/cities ({Existing}/{Expected} provinces present)",
            existingProvinceCount, IranLocationsCatalog.All.Count);

        var existingByName = await _db.IranProvinces
            .Include(p => p.Cities)
            .ToDictionaryAsync(p => p.Name, StringComparer.Ordinal, cancellationToken);

        var sort = 0;
        foreach (var (provinceName, cities) in IranLocationsCatalog.All)
        {
            if (!existingByName.TryGetValue(provinceName, out var province))
            {
                province = IranProvince.Create(provinceName, sort);
                _db.IranProvinces.Add(province);
                existingByName[provinceName] = province;
            }

            var existingCities = province.Cities
                .Select(c => c.Name)
                .ToHashSet(StringComparer.Ordinal);

            var citySort = 0;
            foreach (var cityName in cities)
            {
                if (existingCities.Contains(cityName))
                {
                    citySort++;
                    continue;
                }

                _db.IranCities.Add(IranCity.Create(province.Id, cityName, citySort));
                existingCities.Add(cityName);
                citySort++;
            }

            sort++;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IranProvinceDto>> ListProvincesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.IranProvinces
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new IranProvinceDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IranCityDto>> ListCitiesAsync(Guid provinceId, CancellationToken cancellationToken = default)
    {
        return await _db.IranCities
            .AsNoTracking()
            .Where(c => c.ProvinceId == provinceId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new IranCityDto(c.Id, c.ProvinceId, c.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsValidLocationAsync(string provinceName, string cityName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provinceName) || string.IsNullOrWhiteSpace(cityName))
            return false;

        var province = provinceName.Trim();
        var city = cityName.Trim();

        return await _db.IranCities
            .AsNoTracking()
            .AnyAsync(
                c => c.Name == city && c.Province.Name == province,
                cancellationToken);
    }
}
