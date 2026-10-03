using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Schema;
using FluentAssertions;

namespace CMS.Modules.Forms.Application.Tests;

public class FormFieldVisibilityEvaluatorTests
{
    [Fact]
    public void Missing_Or_Empty_Conditions_Always_Visible()
    {
        var field = Field("target", visibility: null);
        var fields = new List<FormSchemaField> { field };

        FormFieldVisibilityEvaluator.IsVisible(field, new Dictionary<string, string?>(), fields)
            .Should().BeTrue();

        field = Field("target", new FormFieldVisibilitySchema { Mode = "all", Conditions = [] });
        FormFieldVisibilityEvaluator.IsVisible(field, new Dictionary<string, string?>(), [field])
            .Should().BeTrue();
    }

    [Fact]
    public void Mode_All_Requires_Every_Condition()
    {
        var depA = Field("a");
        var depB = Field("b");
        var target = Field("target", new FormFieldVisibilitySchema
        {
            Mode = "all",
            Conditions =
            [
                Cond(depA.Id, "equals", "yes"),
                Cond(depB.Key, "equals", "1")
            ]
        });
        var all = new List<FormSchemaField> { depA, depB, target };
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [depA.Id] = "yes",
            [depB.Key] = "0"
        };

        FormFieldVisibilityEvaluator.IsVisible(target, values, all).Should().BeFalse();

        values[depB.Key] = "1";
        FormFieldVisibilityEvaluator.IsVisible(target, values, all).Should().BeTrue();
    }

    [Fact]
    public void Mode_Any_Requires_One_Condition()
    {
        var dep = Field("choice");
        var target = Field("target", new FormFieldVisibilitySchema
        {
            Mode = "any",
            Conditions =
            [
                Cond(dep.Id, "equals", "a"),
                Cond(dep.Id, "equals", "b")
            ]
        });
        var all = new List<FormSchemaField> { dep, target };
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [dep.Key] = "b"
        };

        FormFieldVisibilityEvaluator.IsVisible(target, values, all).Should().BeTrue();

        values[dep.Key] = "c";
        FormFieldVisibilityEvaluator.IsVisible(target, values, all).Should().BeFalse();
    }

    [Fact]
    public void Equals_Is_Case_Insensitive()
    {
        var dep = Field("status");
        var target = Field("detail", new FormFieldVisibilitySchema
        {
            Mode = "all",
            Conditions = [Cond(dep.Key, "equals", "Open")]
        });
        var all = new List<FormSchemaField> { dep, target };
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [dep.Id] = "open"
        };

        FormFieldVisibilityEvaluator.IsVisible(target, values, all).Should().BeTrue();
    }

    [Fact]
    public void Is_Empty_And_Is_Not_Empty()
    {
        var dep = Field("notes");
        var whenEmpty = Field("hint", new FormFieldVisibilitySchema
        {
            Mode = "all",
            Conditions = [Cond(dep.Id, "is_empty", null)]
        });
        var whenFilled = Field("extra", new FormFieldVisibilitySchema
        {
            Mode = "all",
            Conditions = [Cond(dep.Key, "is_not_empty", null)]
        });
        var all = new List<FormSchemaField> { dep, whenEmpty, whenFilled };

        var empty = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { [dep.Key] = "  " };
        FormFieldVisibilityEvaluator.IsVisible(whenEmpty, empty, all).Should().BeTrue();
        FormFieldVisibilityEvaluator.IsVisible(whenFilled, empty, all).Should().BeFalse();

        var filled = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { [dep.Key] = "x" };
        FormFieldVisibilityEvaluator.IsVisible(whenEmpty, filled, all).Should().BeFalse();
        FormFieldVisibilityEvaluator.IsVisible(whenFilled, filled, all).Should().BeTrue();
    }

    private static FormSchemaField Field(string key, FormFieldVisibilitySchema? visibility = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("D"),
            Key = key,
            Type = FormFieldTypeIds.Text,
            Label = key,
            Visibility = visibility
        };

    private static FormVisibilityConditionSchema Cond(string fieldId, string op, string? value) =>
        new() { FieldId = fieldId, Operator = op, Value = value };
}

public class FormFieldVisibilitySettingsRoundTripTests
{
    [Fact]
    public void BuildSettingsJson_RoundTrips_Multiple_Conditions()
    {
        var extras = new FormFieldFormExtras
        {
            Visibility = new FormFieldVisibilitySchema
            {
                Mode = "any",
                Conditions =
                [
                    new FormVisibilityConditionSchema
                    {
                        FieldId = "status",
                        Operator = "equals",
                        Value = "open"
                    },
                    new FormVisibilityConditionSchema
                    {
                        FieldId = "notes",
                        Operator = "is_not_empty",
                        Value = null
                    }
                ]
            }
        };

        var json = FormFieldSchemaFactory.BuildSettingsJson(extras);
        json.Should().NotBeNullOrWhiteSpace();

        var parsed = FormFieldSchemaFactory.ParseExtras(json);
        parsed.Visibility.Should().NotBeNull();
        parsed.Visibility!.Mode.Should().Be("any");
        parsed.Visibility.Conditions.Should().HaveCount(2);
        parsed.Visibility.Conditions[0].FieldId.Should().Be("status");
        parsed.Visibility.Conditions[0].Operator.Should().Be("equals");
        parsed.Visibility.Conditions[0].Value.Should().Be("open");
        parsed.Visibility.Conditions[1].FieldId.Should().Be("notes");
        parsed.Visibility.Conditions[1].Operator.Should().Be("is_not_empty");
    }
}
