using CMS.Modules.Seo.Application.Documents;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Domain.Entities;
using CMS.Modules.Seo.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Seo.Infrastructure.Services;

public sealed class SeoDocumentService : ISeoDocumentService
{
    private readonly SeoDbContext _db;
    private readonly IValidator<SaveSeoDocumentCommand> _validator;

    public SeoDocumentService(SeoDbContext db, IValidator<SaveSeoDocumentCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<SeoDocumentDto?> GetAsync(string contentType, Guid contentId, CancellationToken cancellationToken = default)
    {
        var normalized = contentType.Trim().ToLowerInvariant();
        var doc = await _db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.ContentType == normalized && d.ContentId == contentId, cancellationToken);
        return doc is null ? null : Map(doc);
    }

    public async Task<SeoDocumentDto> UpsertAsync(SaveSeoDocumentCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

        var normalized = command.ContentType.Trim().ToLowerInvariant();
        var existing = await _db.Documents
            .FirstOrDefaultAsync(d => d.ContentType == normalized && d.ContentId == command.ContentId, cancellationToken);

        if (existing is null)
        {
            var created = SeoDocument.Create(
                command.ContentType,
                command.ContentId,
                command.FocusKeyword,
                command.RobotsIndex,
                command.RobotsFollow,
                command.SchemaType,
                command.SchemaJson,
                command.SeoScore,
                command.AnalysisJson);
            _db.Documents.Add(created);
            await _db.SaveChangesAsync(cancellationToken);
            return Map(created);
        }

        existing.Update(
            command.FocusKeyword,
            command.RobotsIndex,
            command.RobotsFollow,
            command.SchemaType,
            command.SchemaJson,
            command.SeoScore,
            command.AnalysisJson);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(existing);
    }

    private async Task ValidateAsync(SaveSeoDocumentCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (result.IsValid)
            return;

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        throw new DomainValidationException(errors);
    }

    private static SeoDocumentDto Map(SeoDocument doc) =>
        new(
            doc.Id,
            doc.ContentType,
            doc.ContentId,
            doc.FocusKeyword,
            doc.RobotsIndex,
            doc.RobotsFollow,
            doc.SchemaType,
            doc.SchemaJson,
            doc.SeoScore,
            doc.AnalysisJson,
            doc.CreatedAtUtc,
            doc.UpdatedAtUtc);
}
