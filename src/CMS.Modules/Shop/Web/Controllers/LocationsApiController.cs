using CMS.Modules.Shop.Application.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Modules.Shop.Web.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/locations")]
public sealed class LocationsApiController : ControllerBase
{
    private readonly IIranLocationService _locations;

    public LocationsApiController(IIranLocationService locations)
    {
        _locations = locations;
    }

    [HttpGet("provinces")]
    [ProducesResponseType(typeof(IReadOnlyList<IranProvinceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Provinces(CancellationToken cancellationToken)
    {
        return Ok(await _locations.ListProvincesAsync(cancellationToken));
    }

    [HttpGet("provinces/{provinceId:guid}/cities")]
    [ProducesResponseType(typeof(IReadOnlyList<IranCityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cities(Guid provinceId, CancellationToken cancellationToken)
    {
        return Ok(await _locations.ListCitiesAsync(provinceId, cancellationToken));
    }
}
