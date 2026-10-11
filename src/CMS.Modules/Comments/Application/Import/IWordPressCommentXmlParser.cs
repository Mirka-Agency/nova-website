namespace CMS.Modules.Comments.Application.Import;

public interface IWordPressCommentXmlParser
{
    IReadOnlyList<WordPressCommentXmlItem> Parse(Stream xmlStream);
}
