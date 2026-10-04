using CMS.Application;
using CMS.Application.Settings;
using CMS.Application.Sms;
using CMS.Application.Storage;
using FluentAssertions;

namespace CMS.Application.Tests;

public class ImageUploadRulesTests
{
    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void Allowed_ContentTypes_Pass(string contentType)
    {
        ImageUploadRules.IsAllowedContentType(contentType).Should().BeTrue();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("")]
    [InlineData(null)]
    public void Disallowed_ContentTypes_Fail(string? contentType)
    {
        ImageUploadRules.IsAllowedContentType(contentType).Should().BeFalse();
    }

    [Fact]
    public void Size_Within_Limit_Passes()
    {
        ImageUploadRules.IsWithinSizeLimit(1024).Should().BeTrue();
        ImageUploadRules.IsWithinSizeLimit(ImageUploadRules.MaxBytes).Should().BeTrue();
    }

    [Fact]
    public void Size_Over_Limit_Fails()
    {
        ImageUploadRules.IsWithinSizeLimit(0).Should().BeFalse();
        ImageUploadRules.IsWithinSizeLimit(ImageUploadRules.MaxBytes + 1).Should().BeFalse();
    }
}

public class UpdateSiteSettingsCommandValidatorTests
{
    private readonly UpdateSiteSettingsCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new UpdateSiteSettingsCommand(
            "میرکا", "tag", "info@example.com", null, null, "۹–۱۷", "footer",
            "/media/logo.png", "/favicon.ico", "عنوان", "توضیح", "/media/og.png",
            "https://instagram.com/mirka", "https://t.me/mirka", "https://x.com/mirka", "https://linkedin.com/company/mirka",
            "https://www.aparat.com/mirka", "https://facebook.com/mirka", "https://youtube.com/@mirka", "https://wa.me/989121234567",
            null, false, null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_SiteName_Fails()
    {
        var result = _validator.Validate(EmptyCommand with { SiteName = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSiteSettingsCommand.SiteName));
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var result = _validator.Validate(EmptyCommand with { ContactEmail = "bad-email" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSiteSettingsCommand.ContactEmail));
    }

    [Fact]
    public void Relative_Logo_Passes_Absolute_Social_Required()
    {
        var ok = _validator.Validate(EmptyCommand with { LogoUrl = "/uploads/a.png" });
        ok.IsValid.Should().BeTrue();

        var badSocial = _validator.Validate(EmptyCommand with { InstagramUrl = "/not-absolute" });
        badSocial.IsValid.Should().BeFalse();
        badSocial.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateSiteSettingsCommand.InstagramUrl));
    }

    private static UpdateSiteSettingsCommand EmptyCommand =>
        new("میرکا", null, null, null, null, null, null,
            null, null, null, null, null,
            null, null, null, null,
            null, null, null, null,
            null, false, null);
}

public class FileContentSnifferTests
{
    [Theory]
    [InlineData("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    [InlineData("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData("image/gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })]
    public void Valid_Image_Signatures_Pass(string contentType, byte[] header)
    {
        using var stream = new MemoryStream(header);
        FileContentSniffer.MatchesDeclaredImage(stream, contentType).Should().BeTrue();
    }

    [Fact]
    public void Mismatched_ContentType_Fails()
    {
        using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        FileContentSniffer.MatchesDeclaredImage(stream, "image/jpeg").Should().BeFalse();
    }

    [Fact]
    public void ImageUploadRules_Validate_Uses_Sniffer()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(png);
        ImageUploadRules.Validate(stream, "image/png", png.Length).Should().BeTrue();
    }
}

public class SmsPhoneNormalizerTests
{
    [Theory]
    [InlineData("09121234567", "09121234567")]
    [InlineData("+98 912 123 4567", "09121234567")]
    [InlineData("989121234567", "09121234567")]
    [InlineData("9121234567", "09121234567")]
    public void Normalizes_Iranian_Mobiles(string input, string expected)
    {
        SmsPhoneNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void Invalid_Returns_Null(string? input)
    {
        SmsPhoneNormalizer.Normalize(input).Should().BeNull();
    }
}
