using System.ComponentModel.DataAnnotations;

using Landscape.Tsi.Application.Identity;

namespace Landscape.Tsi.Web.Models;

public sealed class LocalUsersQueryModel
{
    public string? Search { get; set; }
    public string? RoleCode { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
}

public sealed record LocalUsersPageViewModel(LocalUserPage Page, IReadOnlyList<LocalRoleOption> Roles, LocalUsersQueryModel Query);
public sealed record LocalUserDetailViewModel(LocalUserDetail User, IReadOnlyList<LocalRoleOption> Roles, IReadOnlyList<LocalUserAuditRow> Audit, IReadOnlyList<OrganizationScopeRow> Scopes, IReadOnlyList<OrganizationScopeApprover> Approvers);

public sealed class LocalUserCreateViewModel
{
    [Required, StringLength(256)] public string UserName { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    [Required, StringLength(1024)] public string Justification { get; set; } = string.Empty;
}

public sealed class LocalUserEditViewModel
{
    public Guid UserId { get; set; }
    [Required, StringLength(256)] public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    [Required, StringLength(1024)] public string Justification { get; set; } = string.Empty;
}