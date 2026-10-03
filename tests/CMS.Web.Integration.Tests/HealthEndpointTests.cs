using System.Net;
using FluentAssertions;

namespace CMS.Web.Integration.Tests;

[Collection(CmsWebCollection.Name)]
public sealed class HealthEndpointTests
{
    private readonly CmsWebFixture _fixture;

    public HealthEndpointTests(CmsWebFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Health_Live_Returns_Ok()
    {
        _fixture.EnsureAvailable();
        var client = _fixture.Factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Health_Ready_Returns_Ok_When_Postgres_Up()
    {
        _fixture.EnsureAvailable();
        var client = _fixture.Factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
