using CMS.Application.Security;
using FluentAssertions;

namespace CMS.Application.Tests.Security;

public class AdminLoginCaptchaProvidersTests
{
    [Theory]
    [InlineData(null, "none")]
    [InlineData("", "none")]
    [InlineData("off", "none")]
    [InlineData("disabled", "none")]
    [InlineData("Turnstile", "turnstile")]
    [InlineData("recaptcha", "recaptcha")]
    [InlineData("HCAPTCHA", "hcaptcha")]
    public void Normalize_MapsAliases(string? input, string expected)
    {
        AdminLoginCaptchaProviders.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Options_DefaultProvider_IsDisabled()
    {
        var options = new AdminLoginCaptchaOptions();
        options.IsEnabled.Should().BeFalse();
        options.NormalizedProvider.Should().Be(AdminLoginCaptchaProviders.None);
    }

    [Fact]
    public void Options_Turnstile_IsEnabled()
    {
        var options = new AdminLoginCaptchaOptions { Provider = "turnstile" };
        options.IsEnabled.Should().BeTrue();
        options.ForProvider("turnstile").Should().NotBeNull();
    }
}
