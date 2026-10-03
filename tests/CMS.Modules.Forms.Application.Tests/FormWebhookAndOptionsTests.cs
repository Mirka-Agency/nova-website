using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Domain.Fields;
using FluentAssertions;

namespace CMS.Modules.Forms.Application.Tests;

public class FormFieldOptionParserTests
{
    [Fact]
    public void Parses_Value_Label_Pairs()
    {
        var pairs = FormFieldOptionParser.Parse("yes:بله|no=خیر|plain");
        pairs.Should().HaveCount(3);
        pairs[0].Should().Be(new FormFieldOptionParser.OptionPair("yes", "بله"));
        pairs[1].Should().Be(new FormFieldOptionParser.OptionPair("no", "خیر"));
        pairs[2].Should().Be(new FormFieldOptionParser.OptionPair("plain", "plain"));
    }

    [Fact]
    public void Normalize_Keeps_Pair_Syntax()
    {
        FormFieldOptionParser.Normalize("yes:بله|same")
            .Should().Be("yes:بله|same");
    }

    [Fact]
    public void ToDisplayLabels_Maps_Single_And_Multi_Values()
    {
        var options = FormFieldOptionParser.Parse("yes:بله|no:خیر|maybe:شاید");

        FormFieldOptionParser.ToDisplayLabels("yes", options).Should().Be("بله");
        FormFieldOptionParser.ToDisplayLabels("no|maybe", options).Should().Be("خیر|شاید");
        FormFieldOptionParser.ToDisplayLabels("unknown", options).Should().Be("unknown");
    }
}

public class FormWebhookPayloadTests
{
    [Fact]
    public void ComputeSignatureHex_Is_Stable()
    {
        var a = FormWebhookPayload.ComputeSignatureHex("""{"ok":true}""", "secret");
        var b = FormWebhookPayload.ComputeSignatureHex("""{"ok":true}""", "secret");
        a.Should().Be(b);
        a.Should().HaveLength(64);
    }
}
