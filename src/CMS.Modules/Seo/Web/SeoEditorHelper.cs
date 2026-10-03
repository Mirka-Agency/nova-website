using CMS.Modules.Seo.Application.Documents;
using CMS.Modules.Seo.Domain.Enums;
using CMS.Modules.Seo.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Seo.Web;

public static class SeoEditorHelper
{
    public static SeoEditorFieldsViewModel CreateFields(
        string contentType,
        Guid? contentId = null,
        SeoDocumentDto? document = null,
        string? previewTitle = null,
        string? previewDescription = null,
        string? previewUrl = null)
    {
        var schema = document?.SchemaType;
        return new SeoEditorFieldsViewModel
        {
            ContentType = contentType,
            ContentId = contentId,
            FocusKeyword = document?.FocusKeyword,
            RobotsIndex = document?.RobotsIndex ?? true,
            RobotsFollow = document?.RobotsFollow ?? true,
            SchemaType = schema,
            SeoScore = document?.SeoScore,
            PreviewTitle = previewTitle,
            PreviewDescription = previewDescription,
            PreviewUrl = previewUrl,
            SchemaTypeOptions = BuildSchemaOptions(schema)
        };
    }

    public static void EnsureSchemaOptions(SeoEditorFieldsViewModel model)
    {
        model.SchemaTypeOptions = BuildSchemaOptions(model.SchemaType);
    }

    public static SaveSeoDocumentCommand ToSaveCommand(SeoEditorFieldsViewModel model, Guid contentId) =>
        new(
            model.ContentType,
            contentId,
            model.FocusKeyword,
            model.RobotsIndex,
            model.RobotsFollow,
            model.SchemaType,
            SchemaJson: null,
            SeoScore: model.SeoScore,
            AnalysisJson: null);

    public static string FormatRobotsMeta(bool index, bool follow)
    {
        var indexToken = index ? "index" : "noindex";
        var followToken = follow ? "follow" : "nofollow";
        return $"{indexToken}, {followToken}";
    }

    private static List<SelectListItem> BuildSchemaOptions(string? selected)
    {
        var items = new List<SelectListItem>
        {
            new("—", string.Empty, string.IsNullOrWhiteSpace(selected))
        };
        foreach (var type in SeoSchemaTypes.Known.OrderBy(x => x))
            items.Add(new SelectListItem(type, type, string.Equals(type, selected, StringComparison.OrdinalIgnoreCase)));
        return items;
    }
}
