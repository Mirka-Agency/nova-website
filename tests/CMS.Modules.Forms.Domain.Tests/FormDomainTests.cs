using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Forms.Domain.Tests;

public class FormDefinitionTests
{
    [Fact]
    public void Create_StartsAsDraft()
    {
        var form = FormDefinition.Create("Contact", "contact", "desc");
        form.Status.Should().Be(FormStatus.Draft);
        form.IsPublished.Should().BeFalse();
        form.Key.Should().Be("contact");
    }

    [Fact]
    public void Create_WithExplicitKey_NormalizesKey()
    {
        var form = FormDefinition.Create("Contact", "Contact-Us", "contact-us", null);
        form.Key.Should().Be("contact_us");
        form.Slug.Should().Be("contact-us");
    }

    [Fact]
    public void Update_SetsMetaOnly()
    {
        var form = FormDefinition.Create("Contact", "contact", null);
        form.Update("Contact Us", "contact-us", "Hello", FormStatus.Published);

        form.Name.Should().Be("Contact Us");
        form.Key.Should().Be("contact");
        form.Slug.Should().Be("contact-us");
        form.Description.Should().Be("Hello");
        form.Status.Should().Be(FormStatus.Published);
        form.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void AddField_And_Reorder_Works()
    {
        var form = FormDefinition.Create("F", "f", null);
        var a = form.AddField("name", "Name", FormFieldType.Text, true, null, 1, "Your name", "Help");
        var b = form.AddField("email", "Email", FormFieldType.Email, true, null, 2);
        form.AddField("topic", "Topic", FormFieldType.Radio, true, "A|B", 3);

        form.Fields.Should().HaveCount(3);
        a.Placeholder.Should().Be("Your name");
        a.HelpText.Should().Be("Help");

        form.ReorderFields([b.Id, a.Id, form.Fields.Last().Id]);
        form.Fields.OrderBy(f => f.SortOrder).Select(f => f.Key).Should().Equal("email", "name", "topic");
    }

    [Fact]
    public void AddVersion_StoresSchemaAndPointers()
    {
        var form = FormDefinition.Create("F", "f_key", "f", null);
        var version = form.AddVersion(1, """{"fields":[]}""", FormVersionState.Published);
        form.SetVersionPointers(version.Id, version.Id);

        form.Versions.Should().ContainSingle();
        version.VersionNumber.Should().Be(1);
        version.State.Should().Be(FormVersionState.Published);
        form.PublishedVersionId.Should().Be(version.Id);
        form.DraftVersionId.Should().Be(version.Id);
    }

    [Fact]
    public void Clone_CopiesFieldsAsDraft_WithNewKey()
    {
        var form = FormDefinition.Create("F", "f", "d");
        form.Publish();
        form.AddField("name", "Name", FormFieldType.Text, true, null, 1);

        var clone = form.Clone("F copy", "f_copy", "f-copy");
        clone.Status.Should().Be(FormStatus.Draft);
        clone.IsPublished.Should().BeFalse();
        clone.Key.Should().Be("f_copy");
        clone.Fields.Should().HaveCount(1);
        clone.Fields.First().Key.Should().Be("name");
        clone.Slug.Should().Be("f-copy");
    }

    [Fact]
    public void Submit_WhenDisabled_Throws()
    {
        var form = FormDefinition.Create("F", "f", null);
        form.Disable();
        var act = () => form.Submit(Guid.NewGuid(), null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Submit_RequiresFormVersionId()
    {
        var form = FormDefinition.Create("F", "f", null);
        form.Publish();
        var act = () => form.Submit(Guid.Empty, null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Submit_WhenPublished_CreatesNewSubmissionWithDataJson()
    {
        var form = FormDefinition.Create("F", "f", null);
        form.Publish();
        var version = form.AddVersion(1, "{}", FormVersionState.Published);
        form.SetVersionPointers(version.Id, version.Id);
        form.AddField("email", "Email", FormFieldType.Email, true, null, 1);

        var dataJson = """{"fields":[{"key":"email","label":"Email","value":"a@b.com"}]}""";
        var submission = form.Submit(version.Id, "1.1.1.1", "ua", dataJson);
        submission.Status.Should().Be(SubmissionStatus.New);
        submission.FormVersionId.Should().Be(version.Id);
        submission.DataJson.Should().Be(dataJson);
        submission.ContextJson.Should().Be("{}");
    }

    [Fact]
    public void RemoveField_Works()
    {
        var form = FormDefinition.Create("F", "f", null);
        var field = form.AddField("name", "Name", FormFieldType.Text, false, null, 1);
        form.RemoveField(field.Id);
        form.Fields.Should().BeEmpty();
    }

    [Fact]
    public void AddField_DuplicateKey_Throws()
    {
        var form = FormDefinition.Create("F", "f", null);
        form.AddField("name", "Name", FormFieldType.Text, false, null, 1);
        var act = () => form.AddField("name", "Other", FormFieldType.Text, false, null, 2);
        act.Should().Throw<DomainException>();
    }
}

public class FormSubmissionTests
{
    [Fact]
    public void MarkRead_New_Archive_Flow()
    {
        var submission = FormSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), null, null);
        submission.Status.Should().Be(SubmissionStatus.New);
        submission.DataJson.Should().Be("""{"fields":[]}""");
        submission.ContextJson.Should().Be("{}");

        submission.MarkRead();
        submission.Status.Should().Be(SubmissionStatus.Read);

        submission.MarkUnread();
        submission.Status.Should().Be(SubmissionStatus.New);

        submission.Archive();
        submission.Status.Should().Be(SubmissionStatus.Archived);

        submission.Unarchive();
        submission.Status.Should().Be(SubmissionStatus.Read);
    }

    [Fact]
    public void Create_WithDataAndContext_StoresJson()
    {
        var data = """{"fields":[{"key":"email","value":"a@b.com"}]}""";
        var context = """{"page":"/contact","utm_source":"newsletter"}""";
        var submission = FormSubmission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.2.3.4",
            "ua",
            data,
            context);

        submission.Status.Should().Be(SubmissionStatus.New);
        submission.DataJson.Should().Be(data);
        submission.ContextJson.Should().Be(context);
        submission.IpAddress.Should().Be("1.2.3.4");
    }

    [Fact]
    public void AddFile_And_MarkProcessed_Spam()
    {
        var submission = FormSubmission.Create(Guid.NewGuid(), Guid.NewGuid(), null, null);
        var file = submission.AddFile(
            Guid.NewGuid(),
            "resume",
            "cv.pdf",
            "application/pdf",
            1024,
            "forms/contact/cv.pdf",
            "https://cdn.example/cv.pdf");

        submission.Files.Should().ContainSingle();
        file.OriginalFileName.Should().Be("cv.pdf");
        file.PublicUrl.Should().Be("https://cdn.example/cv.pdf");

        submission.MarkProcessed();
        submission.Status.Should().Be(SubmissionStatus.Processed);

        submission.MarkSpam();
        submission.Status.Should().Be(SubmissionStatus.Spam);
    }

    [Fact]
    public void Create_RequiresFormVersionId()
    {
        var act = () => FormSubmission.Create(Guid.NewGuid(), Guid.Empty, null, null);
        act.Should().Throw<DomainException>();
    }
}

public class FormFieldTests
{
    [Fact]
    public void Radio_RequiresOptions()
    {
        var act = () => FormField.Create(Guid.NewGuid(), "r", "R", FormFieldType.Radio, true, null, 1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CheckboxGroup_RequiresOptions()
    {
        var act = () => FormField.Create(Guid.NewGuid(), "g", "G", FormFieldType.CheckboxGroup, true, null, 1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetSortOrder_Works()
    {
        var field = FormField.Create(Guid.NewGuid(), "n", "N", FormFieldType.Text, false, null, 1);
        field.SetSortOrder(5);
        field.SortOrder.Should().Be(5);
    }

    [Fact]
    public void OptionsCsv_Supports_Value_Label_Pairs()
    {
        var field = FormField.Create(
            Guid.NewGuid(),
            "choice",
            "Choice",
            FormFieldType.Select,
            true,
            "yes:بله|no:خیر",
            1);

        field.OptionsCsv.Should().Be("yes:بله|no:خیر");
        field.GetOptions().Should().Equal("yes", "no");
        field.GetOptionPairs().Should().Equal(("yes", "بله"), ("no", "خیر"));
    }
}

public class FormFieldDuplicateTests
{
    [Fact]
    public void DuplicateField_CreatesNewKey()
    {
        var form = FormDefinition.Create("F", "f", null);
        var field = form.AddField("name", "Name", FormFieldType.Text, true, null, 1, "ph", "help", """{"defaultValue":"x"}""");

        var clone = form.DuplicateField(field.Id);

        clone.Id.Should().NotBe(field.Id);
        clone.Key.Should().Be("name_copy");
        clone.Label.Should().Be(field.Label);
        clone.FieldType.Should().Be(field.FieldType);
        clone.IsRequired.Should().Be(field.IsRequired);
        clone.SettingsJson.Should().Be(field.SettingsJson);
        form.Fields.Should().HaveCount(2);
    }

    [Fact]
    public void DuplicateField_SuffixesWhenCopyExists()
    {
        var form = FormDefinition.Create("F", "f", null);
        var field = form.AddField("name", "Name", FormFieldType.Text, false, null, 1);
        form.AddField("name_copy", "Copy", FormFieldType.Text, false, null, 2);

        var clone = form.DuplicateField(field.Id);
        clone.Key.Should().Be("name_copy2");
    }
}
