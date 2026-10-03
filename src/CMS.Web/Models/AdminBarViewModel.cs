namespace CMS.Web.Models;

public sealed class AdminBarViewModel
{
    public required string DisplayName { get; init; }
    public AdminBarLink? EditLink { get; init; }
    public bool CanManageSettings { get; init; }
    public IReadOnlyList<AdminBarLink> ManageLinks { get; init; } = [];
    public IReadOnlyList<AdminBarLink> CreateLinks { get; init; } = [];
}

public sealed record AdminBarLink(
    string Label,
    string Controller,
    string Action,
    Guid? Id = null,
    string Area = "Admin",
    string? Icon = null);
