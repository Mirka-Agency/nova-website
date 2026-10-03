using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Forms.Application.Tests;

public class FormTemplateRendererTests
{
    [Fact]
    public void Renders_Field_And_System_Placeholders_With_HtmlEncode()
    {
        var form = FormDefinition.Create("Contact", "contact_form", "contact", null);
        form.Publish();
        var version = form.AddVersion(1, "{}", FormVersionState.Published);
        form.SetVersionPointers(version.Id, version.Id);
        var submission = form.Submit(version.Id, null, null);

        var context = new FormActionExecutionContext
        {
            Form = form,
            Submission = submission,
            Schema = new FormSchemaDocument(),
            FieldValuesByKey = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["email"] = "a@b.com",
                ["name"] = "Ali <script>"
            },
            FieldValuesById = new Dictionary<string, string?>(),
            SiteName = "Mirka",
            PageUrl = "/contact"
        };

        var rendered = FormTemplateRenderer.Render(
            "Hi {{name}} from {{site.name}} / {email}",
            context,
            htmlEncode: true);

        rendered.Should().Contain("Ali &lt;script&gt;");
        rendered.Should().Contain("Mirka");
        rendered.Should().Contain("a@b.com");
        rendered.Should().NotContain("<script>");
    }
}

public class FormSubmitBehaviorResolverTests
{
    [Fact]
    public void Redirect_Uses_Url()
    {
        var schema = new FormSchemaDocument
        {
            SubmitBehavior = new FormSubmitBehaviorSchema
            {
                Type = FormSubmitBehaviorTypes.Redirect,
                Url = "/thanks",
                Message = "ok"
            }
        };

        var (message, url) = FormSubmitBehaviorResolver.Resolve(schema, "legacy", "/old");
        message.Should().Be("ok");
        url.Should().Be("/thanks");
    }

    [Fact]
    public void Message_Clears_Redirect()
    {
        var schema = new FormSchemaDocument
        {
            SubmitBehavior = new FormSubmitBehaviorSchema
            {
                Type = FormSubmitBehaviorTypes.Message,
                Message = "done"
            }
        };

        var (message, url) = FormSubmitBehaviorResolver.Resolve(schema, "legacy", "/old");
        message.Should().Be("done");
        url.Should().BeNull();
    }

    [Fact]
    public void Page_Uses_Url_As_Relative_Path()
    {
        var schema = new FormSchemaDocument
        {
            SubmitBehavior = new FormSubmitBehaviorSchema
            {
                Type = FormSubmitBehaviorTypes.Page,
                Url = "/thanks",
                Message = "ok"
            }
        };

        var (message, url) = FormSubmitBehaviorResolver.Resolve(schema, "legacy", "/old");
        message.Should().Be("ok");
        url.Should().Be("/thanks");
    }
}

public class FormActionConfigReaderTests
{
    [Fact]
    public void Reads_String_List_From_Array_And_Single()
    {
        var config = new Dictionary<string, object?>
        {
            ["to"] = new object?[] { "a@b.com", "c@d.com" }
        };

        FormActionConfigReader.GetStringList(config, "to").Should().Equal("a@b.com", "c@d.com");
        FormActionConfigReader.GetStringList(
            new Dictionary<string, object?> { ["to"] = "solo@x.com" },
            "to").Should().Equal("solo@x.com");
    }
}
