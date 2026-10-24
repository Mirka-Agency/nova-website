using System.Text;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Application.Import;
using CMS.Modules.Comments.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Comments.Application.Tests;

public class WordPressCommentXmlParserTests
{
    private readonly WordPressCommentXmlParser _parser = new();

    [Fact]
    public void Parse_extracts_comments_and_maps_status_parent_and_page_url()
    {
        const string xml =
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <comments>
              <comment>
                <comment_id>10</comment_id>
                <comment_author>Ali</comment_author>
                <comment_author_email>ali@example.com</comment_author_email>
                <comment_content><![CDATA[Hello <b>world</b>]]></comment_content>
                <comment_date_gmt>2023-01-02 03:04:05</comment_date_gmt>
                <comment_approved>1</comment_approved>
                <comment_parent>0</comment_parent>
                <page_url>https://old.example.com/sample-post/</page_url>
              </comment>
              <comment>
                <comment_id>11</comment_id>
                <comment_author>Sara</comment_author>
                <comment_author_email>sara@example.com</comment_author_email>
                <comment_content>Reply</comment_content>
                <comment_date_gmt>2023-01-03 03:04:05</comment_date_gmt>
                <comment_approved>spam</comment_approved>
                <comment_parent>10</comment_parent>
                <page_url>https://old.example.com/sample-post/</page_url>
              </comment>
              <comment>
                <comment_id>12</comment_id>
                <comment_author>Guest</comment_author>
                <comment_content>Waiting for moderation</comment_content>
                <comment_date_gmt>2023-01-04 03:04:05</comment_date_gmt>
                <comment_approved>0</comment_approved>
                <comment_parent>0</comment_parent>
                <page_url>https://old.example.com/sample-post/</page_url>
              </comment>
            </comments>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var items = _parser.Parse(stream);

        items.Should().HaveCount(3);
        items[0].ExternalId.Should().Be("10");
        items[0].ExternalParentId.Should().BeNull();
        items[0].Status.Should().Be(CommentStatus.Approved);
        items[0].Body.Should().Be("Hello world");
        items[0].PublishedAtUtc.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        items[0].PageUrl.Should().Contain("sample-post");

        items[1].ExternalParentId.Should().Be("10");
        items[1].Status.Should().Be(CommentStatus.Rejected);

        items[2].ExternalId.Should().Be("12");
        items[2].Status.Should().Be(CommentStatus.Pending);
    }

    [Fact]
    public void Parse_maps_wp_namespace_comment_parent()
    {
        const string xml =
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <comments>
              <comment>
                <wp:comment_id xmlns:wp="http://wordpress.org/export/1.2/">1</wp:comment_id>
                <wp:comment_author xmlns:wp="http://wordpress.org/export/1.2/">Parent</wp:comment_author>
                <wp:comment_content xmlns:wp="http://wordpress.org/export/1.2/">Root</wp:comment_content>
                <wp:comment_date_gmt xmlns:wp="http://wordpress.org/export/1.2/">2023-01-01 00:00:00</wp:comment_date_gmt>
                <wp:comment_approved xmlns:wp="http://wordpress.org/export/1.2/">1</wp:comment_approved>
                <wp:comment_parent xmlns:wp="http://wordpress.org/export/1.2/">0</wp:comment_parent>
                <page_url>https://old.example.com/a/</page_url>
              </comment>
              <comment>
                <wp:comment_id xmlns:wp="http://wordpress.org/export/1.2/">2</wp:comment_id>
                <wp:comment_author xmlns:wp="http://wordpress.org/export/1.2/">Child</wp:comment_author>
                <wp:comment_content xmlns:wp="http://wordpress.org/export/1.2/">Reply</wp:comment_content>
                <wp:comment_date_gmt xmlns:wp="http://wordpress.org/export/1.2/">2023-01-02 00:00:00</wp:comment_date_gmt>
                <wp:comment_approved xmlns:wp="http://wordpress.org/export/1.2/">1</wp:comment_approved>
                <wp:comment_parent xmlns:wp="http://wordpress.org/export/1.2/">1</wp:comment_parent>
                <page_url>https://old.example.com/a/</page_url>
              </comment>
            </comments>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var items = _parser.Parse(stream);

        items.Should().HaveCount(2);
        items[0].ExternalParentId.Should().BeNull();
        items[1].ExternalParentId.Should().Be("1");
    }

    [Fact]
    public void Parse_sample_xml_applies_comment_approved_to_status()
    {
        const string xml =
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <comments>
            <comment>
            <comment_id>101</comment_id>
            <comment_author>علی رضایی</comment_author>
            <comment_author_email>ali@example.com</comment_author_email>
            <comment_content>مقاله مفیدی بود، ممنون.</comment_content>
            <comment_date_gmt>2023-05-10 08:30:00</comment_date_gmt>
            <comment_approved>1</comment_approved>
            <comment_parent>0</comment_parent>
            <page_url>https://old.example.com/sample-post/</page_url>
            </comment>
            <comment>
            <comment_id>102</comment_id>
            <comment_author>سارا محمدی</comment_author>
            <comment_author_email>sara@example.com</comment_author_email>
            <comment_content>کاملاً موافقم.</comment_content>
            <comment_date_gmt>2023-05-11 09:15:00</comment_date_gmt>
            <comment_approved>1</comment_approved>
            <comment_parent>101</comment_parent>
            <page_url>https://old.example.com/sample-post/</page_url>
            </comment>
            <comment>
            <comment_id>201</comment_id>
            <comment_author>مهمان</comment_author>
            <comment_author_email/>
            <comment_content>این URL در CMS وجود ندارد و باید دستی وصل شود.</comment_content>
            <comment_date_gmt>2023-06-01 12:00:00</comment_date_gmt>
            <comment_approved>0</comment_approved>
            <comment_parent>0</comment_parent>
            <page_url>https://old.example.com/missing-page/</page_url>
            </comment>
            </comments>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var items = _parser.Parse(stream);

        items.Should().HaveCount(3);
        items[0].ExternalId.Should().Be("101");
        items[0].Status.Should().Be(CommentStatus.Approved);
        items[1].ExternalId.Should().Be("102");
        items[1].Status.Should().Be(CommentStatus.Approved);
        items[1].ExternalParentId.Should().Be("101");
        items[2].ExternalId.Should().Be("201");
        items[2].Status.Should().Be(CommentStatus.Pending);
        items[2].AuthorEmail.Should().BeNull();
    }

    [Theory]
    [InlineData("1", CommentStatus.Approved)]
    [InlineData("۰", CommentStatus.Pending)]
    [InlineData("۱", CommentStatus.Approved)]
    [InlineData("0", CommentStatus.Pending)]
    [InlineData("hold", CommentStatus.Pending)]
    [InlineData("spam", CommentStatus.Rejected)]
    [InlineData("approved", CommentStatus.Approved)]
    public void Parse_maps_comment_approved_values(string approved, CommentStatus expected)
    {
        var xml =
            $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <comments>
              <comment>
                <comment_id>1</comment_id>
                <comment_author>x</comment_author>
                <comment_content>body</comment_content>
                <comment_date_gmt>2023-01-01 00:00:00</comment_date_gmt>
                <comment_approved>{approved}</comment_approved>
                <comment_parent>0</comment_parent>
                <page_url>https://old.example.com/a/</page_url>
              </comment>
            </comments>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var items = _parser.Parse(stream);

        items.Should().ContainSingle();
        items[0].Status.Should().Be(expected);
    }

    [Fact]
    public void Parse_rejects_xxe_external_dtd()
    {
        const string xml =
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE foo [ <!ENTITY xxe SYSTEM "file:///etc/passwd"> ]>
            <comments>
              <comment>
                <comment_id>1</comment_id>
                <comment_author>x</comment_author>
                <comment_content>&xxe;</comment_content>
                <comment_date_gmt>2023-01-01 00:00:00</comment_date_gmt>
                <comment_approved>1</comment_approved>
                <page_url>https://example.com/a/</page_url>
              </comment>
            </comments>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var act = () => _parser.Parse(stream);
        act.Should().Throw<DomainException>();
    }
}
