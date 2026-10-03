using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Domain.Entities;

public class FormDefinition : BaseEntity
{
    private readonly List<FormField> _fields = [];
    private readonly List<FormSubmission> _submissions = [];
    private readonly List<FormVersion> _versions = [];

    private FormDefinition()
    {
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Stable machine key for Theme/API references. Immutable after create.</summary>
    public string Key { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public FormStatus Status { get; private set; } = FormStatus.Draft;

    public Guid? PublishedVersionId { get; private set; }
    public Guid? DraftVersionId { get; private set; }

    /// <summary>Code-seeded form; Key/Slug and system fields are protected in admin.</summary>
    public bool IsSystem { get; private set; }

    public bool IsPublished => Status == FormStatus.Published;
    public bool IsEnabled => Status == FormStatus.Published;

    public IReadOnlyCollection<FormField> Fields => _fields;
    public IReadOnlyCollection<FormSubmission> Submissions => _submissions;
    public IReadOnlyCollection<FormVersion> Versions => _versions;

    public static FormDefinition Create(string name, string key, string slug, string? description)
    {
        ValidateName(name);
        ValidateKey(key);
        ValidateSlug(slug);
        return new FormDefinition
        {
            Name = name.Trim(),
            Key = NormalizeKey(key),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = FormStatus.Draft
        };
    }

    /// <summary>Backward-compatible factory: key defaults from slug.</summary>
    public static FormDefinition Create(string name, string slug, string? description) =>
        Create(name, slug, slug, description);

    public void MarkAsSystem()
    {
        IsSystem = true;
        Touch();
    }

    public void Update(string name, string slug, string? description, FormStatus status)
    {
        ValidateName(name);
        if (!Enum.IsDefined(status))
            throw new DomainException("وضعیت فرم نامعتبر است.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = status;

        // System forms keep a stable slug for hardcoded URLs / embeds.
        if (!IsSystem)
        {
            ValidateSlug(slug);
            Slug = slug.Trim().ToLowerInvariant();
        }

        Touch();
    }

    public void SetStatus(FormStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new DomainException("وضعیت فرم نامعتبر است.");
        Status = status;
        Touch();
    }

    public void Publish() => SetStatus(FormStatus.Published);
    public void Unpublish() => SetStatus(FormStatus.Draft);
    public void Disable() => SetStatus(FormStatus.Disabled);

    public void SetVersionPointers(Guid? publishedVersionId, Guid? draftVersionId)
    {
        PublishedVersionId = publishedVersionId;
        DraftVersionId = draftVersionId;
        Touch();
    }

    public FormVersion AddVersion(int versionNumber, string schemaJson, FormVersionState state)
    {
        if (_versions.Any(v => v.VersionNumber == versionNumber))
            throw new DomainException("نسخه‌ای با این شماره از قبل وجود دارد.");

        var version = FormVersion.Create(Id, versionNumber, schemaJson, state);
        version.BindToForm(this);
        _versions.Add(version);
        Touch();
        return version;
    }

    public FormField AddField(
        string key,
        string label,
        FormFieldType fieldType,
        bool isRequired,
        string? optionsCsv,
        int sortOrder,
        string? placeholder = null,
        string? helpText = null,
        string? settingsJson = null,
        bool isSystem = false)
    {
        EnsureUniqueFieldKey(key, null);
        var field = FormField.Create(
            Id, key, label, fieldType, isRequired, optionsCsv, sortOrder, placeholder, helpText, settingsJson, isSystem);
        field.BindToForm(this);
        _fields.Add(field);
        Touch();
        return field;
    }

    public void UpdateField(
        Guid fieldId,
        string key,
        string label,
        FormFieldType fieldType,
        bool isRequired,
        string? optionsCsv,
        int sortOrder,
        string? placeholder = null,
        string? helpText = null,
        string? settingsJson = null)
    {
        var field = _fields.FirstOrDefault(f => f.Id == fieldId)
            ?? throw new DomainException("فیلد یافت نشد.");

        if (field.IsSystem)
        {
            // Structural contract stays fixed; only presentation extras may change.
            key = field.Key;
            fieldType = field.FieldType;
            isRequired = field.IsRequired;
            optionsCsv = field.OptionsCsv;
        }

        EnsureUniqueFieldKey(key, fieldId);
        field.Update(key, label, fieldType, isRequired, optionsCsv, sortOrder, placeholder, helpText, settingsJson);
        Touch();
    }

    public FormField DuplicateField(Guid fieldId)
    {
        var field = _fields.FirstOrDefault(f => f.Id == fieldId)
            ?? throw new DomainException("فیلد یافت نشد.");

        var candidate = $"{field.Key}_copy";
        var suffix = 2;
        while (_fields.Any(f => f.Key.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{field.Key}_copy{suffix}";
            suffix++;
        }

        var sortOrder = _fields.Count == 0 ? 1 : _fields.Max(f => f.SortOrder) + 1;
        return AddField(
            candidate,
            field.Label,
            field.FieldType,
            field.IsRequired,
            field.OptionsCsv,
            sortOrder,
            field.Placeholder,
            field.HelpText,
            field.SettingsJson);
    }

    public void RemoveField(Guid fieldId)
    {
        var field = _fields.FirstOrDefault(f => f.Id == fieldId)
            ?? throw new DomainException("فیلد یافت نشد.");
        if (field.IsSystem)
            throw new DomainException("فیلدهای سیستمی قابل حذف نیستند.");
        _fields.Remove(field);
        Touch();
    }

    public void ReorderFields(IReadOnlyList<Guid> orderedIds)
    {
        if (orderedIds.Count != _fields.Count || orderedIds.Distinct().Count() != orderedIds.Count)
            throw new DomainException("ترتیب فیلدها نامعتبر است.");

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var field = _fields.FirstOrDefault(f => f.Id == orderedIds[i])
                ?? throw new DomainException("فیلد یافت نشد.");
            field.SetSortOrder(i + 1);
        }

        Touch();
    }

    /// <summary>Copies meta + fields only. Schema/settings live on FormVersion and are copied by FormService.DuplicateAsync.</summary>
    public FormDefinition Clone(string newName, string newKey, string newSlug)
    {
        ValidateName(newName);
        ValidateKey(newKey);
        ValidateSlug(newSlug);
        var clone = Create(newName, newKey, newSlug, Description);

        foreach (var field in _fields.OrderBy(f => f.SortOrder))
        {
            clone.AddField(
                field.Key,
                field.Label,
                field.FieldType,
                field.IsRequired,
                field.OptionsCsv,
                field.SortOrder,
                field.Placeholder,
                field.HelpText,
                field.SettingsJson);
        }

        return clone;
    }

    /// <summary>Backward-compatible clone: new key derived from newSlug.</summary>
    public FormDefinition Clone(string newName, string newSlug) =>
        Clone(newName, newSlug, newSlug);

    public FormSubmission Submit(
        Guid formVersionId,
        string? ipAddress,
        string? userAgent,
        string? dataJson = null,
        string? contextJson = null)
    {
        if (Status != FormStatus.Published)
            throw new DomainException("فرم منتشر نشده است.");
        if (formVersionId == Guid.Empty)
            throw new DomainException("نسخه فرم برای ثبت ارسال الزامی است.");

        var submission = FormSubmission.Create(Id, formVersionId, ipAddress, userAgent, dataJson, contextJson);
        _submissions.Add(submission);
        return submission;
    }

    private void EnsureUniqueFieldKey(string key, Guid? excludeFieldId)
    {
        if (_fields.Any(f =>
                f.Key.Equals(key.Trim(), StringComparison.OrdinalIgnoreCase)
                && (!excludeFieldId.HasValue || f.Id != excludeFieldId.Value)))
            throw new DomainException("فیلدی با این کلید از قبل وجود دارد.");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام فرم الزامی است.");
        if (name.Trim().Length > 200)
            throw new DomainException("نام فرم خیلی طولانی است.");
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک فرم الزامی است.");
        if (slug.Trim().Length > 200)
            throw new DomainException("نامک فرم خیلی طولانی است.");
    }

    public static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("کلید فرم الزامی است.");
        var normalized = key.Trim();
        if (normalized.Length > 100)
            throw new DomainException("کلید فرم خیلی طولانی است.");
        if (!normalized.All(c => char.IsLetterOrDigit(c) || c is '_' or '-'))
            throw new DomainException("کلید فرم فقط می‌تواند شامل حرف، عدد، خط فاصله یا خط تیره باشد.");
    }

    public static string NormalizeKey(string key) => key.Trim().ToLowerInvariant().Replace('-', '_');
}
