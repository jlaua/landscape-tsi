namespace Landscape.Tsi.Application.Identity;

public sealed record AdministrationUserRow(
    Guid Id,
    string UserName,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Scopes);

public sealed record AdministrationRoleRow(
    string Name,
    IReadOnlyList<string> Permissions);

public sealed record IdentityAdministrationOverview(
    IReadOnlyList<AdministrationUserRow> Users,
    IReadOnlyList<AdministrationRoleRow> Roles);

public interface IIdentityAdministrationOverview
{
    Task<IdentityAdministrationOverview> GetAsync(CancellationToken cancellationToken = default);
}
