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
            </comments>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var items = _parser.Parse(stream);

        items.Should().HaveCount(2);
        items[0].ExternalId.Should().Be("10");
        items[0].ExternalParentId.Should().BeNull();
        items[0].Status.Should().Be(CommentStatus.Approved);
        items[0].Body.Should().Be("Hello world");
        items[0].PublishedAtUtc.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        items[0].PageUrl.Should().Contain("sample-post");

        items[1].ExternalParentId.Should().Be("10");
        items[1].Status.Should().Be(CommentStatus.Rejected);
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
