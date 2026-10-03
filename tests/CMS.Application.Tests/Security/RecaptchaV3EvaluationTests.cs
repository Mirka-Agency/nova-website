using CMS.Application.Security;
using FluentAssertions;

namespace CMS.Application.Tests.Security;

public class RecaptchaV3EvaluationTests
{
    [Fact]
    public void Accepts_Score_At_Or_Above_Threshold_And_Matching_Action()
    {
        RecaptchaV3Evaluation.IsAccepted(
            success: true,
            score: 0.5,
            action: "login",
            expectedAction: SharedCaptchaActions.Login,
            minScore: 0.5,
            allowMissingScore: false).Should().BeTrue();
    }

    [Fact]
    public void Rejects_Low_Score()
    {
        RecaptchaV3Evaluation.IsAccepted(
            success: true,
            score: 0.49,
            action: "form",
            expectedAction: SharedCaptchaActions.Form,
            minScore: 0.5,
            allowMissingScore: false).Should().BeFalse();
    }

    [Fact]
    public void Rejects_Action_Mismatch()
    {
        RecaptchaV3Evaluation.IsAccepted(
            success: true,
            score: 0.9,
            action: "comment",
            expectedAction: SharedCaptchaActions.Login,
            minScore: 0.5,
            allowMissingScore: false).Should().BeFalse();
    }

    [Fact]
    public void Missing_Score_Passes_Only_For_Test_Secret()
    {
        RecaptchaV3Evaluation.IsAccepted(
            success: true,
            score: null,
            action: null,
            expectedAction: SharedCaptchaActions.Login,
            minScore: 0.5,
            allowMissingScore: true).Should().BeTrue();

        RecaptchaV3Evaluation.IsAccepted(
            success: true,
            score: null,
            action: null,
            expectedAction: SharedCaptchaActions.Login,
            minScore: 0.5,
            allowMissingScore: false).Should().BeFalse();

        RecaptchaV3Evaluation.AllowsMissingScore(RecaptchaV3Evaluation.GoogleTestSecret).Should().BeTrue();
        RecaptchaV3Evaluation.AllowsMissingScore("real-secret").Should().BeFalse();
    }

    [Fact]
    public void Coalesce_Prefers_Shared_Key_Then_Legacy_Sections()
    {
        SharedCaptchaKeys.Coalesce(null, " forms-key ", "comments-key")
            .Should().Be("forms-key");

        SharedCaptchaKeys.Coalesce("  ", null, "comments-key", "admin-key")
            .Should().Be("comments-key");

        SharedCaptchaKeys.Coalesce("shared-key", "forms-key")
            .Should().Be("shared-key");
    }

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(-1, 0.5)]
    [InlineData(1.5, 0.5)]
    [InlineData(0.3, 0.3)]
    public void ClampMinScore_Uses_Default_Outside_Range(double input, double expected)
    {
        RecaptchaV3Evaluation.ClampMinScore(input).Should().Be(expected);
    }
}
