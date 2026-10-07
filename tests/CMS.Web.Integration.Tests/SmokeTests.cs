using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace CMS.Web.Integration.Tests;

[Collection(CmsWebCollection.Name)]
public sealed class SmokeTests
{
    private readonly CmsWebFixture _fixture;

    public SmokeTests(CmsWebFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Anonymous_AdminDashboard_RedirectsToLogin()
    {
        _fixture.EnsureAvailable();
        var client = _fixture.Factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/admin/account/login");
    }

    [SkippableFact]
    public async Task Anonymous_Media_RedirectsToLogin()
    {
        _fixture.EnsureAvailable();
        var client = _fixture.Factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin/media");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/admin/account/login");
    }

    [SkippableFact]
    public async Task Admin_Login_ReachesDashboard()
    {
        _fixture.EnsureAvailable();
        var client = _fixture.Factory.CreateClient(new() { AllowAutoRedirect = true });

        await LoginAsync(client, CmsWebFixture.AdminEmail, CmsWebFixture.AdminPassword);

        var response = await client.GetAsync("/admin/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        html.Should().Contain("داشبورد");
        html.Should().Contain(CmsWebFixture.AdminEmail);
    }

    [SkippableFact]
    public async Task Shop_FeatureOff_ReturnsNotFound_FeatureOn_ReturnsOk()
    {
        _fixture.EnsureAvailable();
        var client = _fixture.Factory.CreateClient(new() { AllowAutoRedirect = false });

        await _fixture.SetShopFeatureAsync(enabled: false);
        var disabled = await client.GetAsync("/shop");
        disabled.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await _fixture.SetShopFeatureAsync(enabled: true);
        var enabled = await client.GetAsync("/shop");
        enabled.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Editor_CannotAccess_ShopProducts_ShopManager_Can()
    {
        _fixture.EnsureAvailable();
        await _fixture.SetShopFeatureAsync(enabled: true);

        var editorClient = _fixture.Factory.CreateClient(new() { AllowAutoRedirect = false });
        await LoginAsync(editorClient, CmsWebFixture.EditorEmail, CmsWebFixture.EditorPassword);
        var editorResponse = await editorClient.GetAsync("/admin/products");
        editorResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
        if (editorResponse.StatusCode == HttpStatusCode.Redirect)
            editorResponse.Headers.Location!.ToString().Should().Contain("accessdenied");

        var shopClient = _fixture.Factory.CreateClient(new() { AllowAutoRedirect = false });
        await LoginAsync(shopClient, CmsWebFixture.ShopManagerEmail, CmsWebFixture.ShopManagerPassword);
        var shopResponse = await shopClient.GetAsync("/admin/products");
        shopResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Viewer_CanBrowse_Posts_CannotCreate()
    {
        _fixture.EnsureAvailable();
        var client = _fixture.Factory.CreateClient(new() { AllowAutoRedirect = false });
        await LoginAsync(client, CmsWebFixture.ViewerEmail, CmsWebFixture.ViewerPassword);

        var list = await client.GetAsync("/admin/posts");
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        var create = await client.GetAsync("/admin/posts/create");
        create.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var loginGet = await client.GetAsync("/admin/account/login");
        loginGet.EnsureSuccessStatusCode();
        var html = await loginGet.Content.ReadAsStringAsync();

        var token = ExtractAntiforgeryToken(html);
        token.Should().NotBeNullOrWhiteSpace();

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token!
        });
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var loginPost = await client.PostAsync("/admin/account/login", content);
        loginPost.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found);
    }

    private static string? ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            """name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)""",
            RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(
            html,
            """name="__RequestVerificationToken"[^>]*value="([^"]+)""",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}
