using CMS.Domain.Common;

namespace CMS.Modules.Shop.Domain.Entities;

public class IranProvince : BaseEntity
{
    private readonly List<IranCity> _cities = [];

    private IranProvince()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public IReadOnlyCollection<IranCity> Cities => _cities;

    public static IranProvince Create(string name, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Province name is required.", nameof(name));

        return new IranProvince
        {
            Name = name.Trim(),
            SortOrder = Math.Max(0, sortOrder)
        };
    }

    public IranCity AddCity(string name, int sortOrder)
    {
        var city = IranCity.Create(Id, name, sortOrder);
        _cities.Add(city);
        return city;
    }
}

public class IranCity : BaseEntity
{
    private IranCity()
    {
    }

    public Guid ProvinceId { get; private set; }
    public IranProvince Province { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public static IranCity Create(Guid provinceId, string name, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("City name is required.", nameof(name));

        return new IranCity
        {
            ProvinceId = provinceId,
            Name = name.Trim(),
            SortOrder = Math.Max(0, sortOrder)
        };
    }
}
