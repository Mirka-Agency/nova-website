using CMS.Domain.Exceptions;
using CMS.Modules.Seo.Domain.Entities;
using CMS.Modules.Seo.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Seo.Domain.Tests;

public class SeoDocumentTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var id = Guid.NewGuid();
        var doc = SeoDocument.Create(
            SeoContentTypes.BlogPost,
            id,
            "کلمه کلیدی",
            robotsIndex: true,
            robotsFollow: false,
            SeoSchemaTypes.Article,
            seoScore: 80);

        doc.ContentType.Should().Be(SeoContentTypes.BlogPost);
        doc.ContentId.Should().Be(id);
        doc.FocusKeyword.Should().Be("کلمه کلیدی");
        doc.RobotsIndex.Should().BeTrue();
        doc.RobotsFollow.Should().BeFalse();
        doc.SchemaType.Should().Be(SeoSchemaTypes.Article);
        doc.SeoScore.Should().Be(80);
    }

    [Fact]
    public void Create_WithUnknownContentType_Throws()
    {
        var act = () => SeoDocument.Create("unknown", Guid.NewGuid(), null, true, true, null);
        act.Should().Throw<DomainException>().WithMessage("*نوع محتوا*");
    }

    [Fact]
    public void Create_WithInvalidScore_Throws()
    {
        var act = () => SeoDocument.Create(
            SeoContentTypes.BlogPost,
            Guid.NewGuid(),
            null,
            true,
            true,
            null,
            seoScore: 150);
        act.Should().Throw<DomainException>().WithMessage("*امتیاز*");
    }
}

public class SeoRedirectTests
{
    [Fact]
    public void Create_NormalizesPath()
    {
        var redirect = SeoRedirect.Create("/old/", "https://example.com/new", 301, true, null);
        redirect.FromPath.Should().Be("/old");
        redirect.StatusCode.Should().Be(301);
    }

    [Fact]
    public void Create_WithoutLeadingSlash_AddsSlash()
    {
        var redirect = SeoRedirect.Create("old-page", "/new-page", 302, true, "note");
        redirect.FromPath.Should().Be("/old-page");
        redirect.StatusCode.Should().Be(302);
        redirect.Note.Should().Be("note");
    }

    [Fact]
    public void Create_WithInvalidStatus_Throws()
    {
        var act = () => SeoRedirect.Create("/a", "/b", 307, true, null);
        act.Should().Throw<DomainException>().WithMessage("*۳۰۱*");
    }

    [Fact]
    public void Create_WithRewriteStatus_NormalizesInternalTarget()
    {
        var redirect = SeoRedirect.Create("/pretty/", "/real-page/?x=1", 200, true, null);
        redirect.FromPath.Should().Be("/pretty");
        redirect.ToUrl.Should().Be("/real-page?x=1");
        redirect.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Create_WithRewriteToAbsoluteUrl_Throws()
    {
        var act = () => SeoRedirect.Create("/pretty", "https://example.com/page", 200, true, null);
        act.Should().Throw<DomainException>().WithMessage("*بازنویسی*");
    }

    [Fact]
    public void Create_WithRewriteSamePath_Throws()
    {
        var act = () => SeoRedirect.Create("/same", "/same/", 200, true, null);
        act.Should().Throw<DomainException>().WithMessage("*یکسان*");
    }

    [Fact]
    public void Create_WithEmptyFromPath_Throws()
    {
        var act = () => SeoRedirect.Create("  ", "/b", 301, true, null);
        act.Should().Throw<DomainException>();
    }
}
