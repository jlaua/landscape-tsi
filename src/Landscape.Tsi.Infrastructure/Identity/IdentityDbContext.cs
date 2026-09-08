using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Domain.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityUserContext<IamUsuario, Guid, IdentityUserClaim<Guid>, IamUsuarioLoginExterno, IdentityUserToken<Guid>>(options)
{
    public DbSet<IamRol> BusinessRoles => Set<IamRol>();
    public DbSet<IamPermiso> Permissions => Set<IamPermiso>();
    public DbSet<IamUsuarioRol> UserRoles => Set<IamUsuarioRol>();
    public DbSet<IamRolPermiso> RolePermissions => Set<IamRolPermiso>();
    public DbSet<IamEventoAutenticacion> AuthenticationEvents => Set<IamEventoAutenticacion>();
    public DbSet<IamUsuarioLoginExterno> ExternalLogins => Set<IamUsuarioLoginExterno>();
    public DbSet<IamUsuarioOrganizacion> UserOrganizations => Set<IamUsuarioOrganizacion>();
    public DbSet<IamAccesoEmergencia> EmergencyAccess => Set<IamAccesoEmergencia>();
    public DbSet<IamEventoAuditoriaAutorizacion> AuthorizationAuditEvents => Set<IamEventoAuditoriaAutorizacion>();
    public DbSet<AuditOperation> AuditOperations => Set<AuditOperation>();
    public DbSet<AuditDeletedRecordSnapshot> AuditRecordSnapshots => Set<AuditDeletedRecordSnapshot>();
    public DbSet<AuditRecordKeyMap> AuditRecordKeyMaps => Set<AuditRecordKeyMap>();
    public DbSet<TmDominio> Domains => Set<TmDominio>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAuditIsAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureAuditIsAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IamUsuario>().ToTable("IamUsuario");
        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("IamUsuarioClaim");
            entity.HasIndex(x => x.UserId);
        });
        builder.Entity<IamUsuarioLoginExterno>(entity =>
        {
            entity.ToTable("IamUsuarioLoginExterno");
            entity.Property(x => x.Issuer).HasMaxLength(512);
            entity.Property(x => x.Subject).HasMaxLength(512);
            entity.HasIndex(x => new { x.Issuer, x.Subject }).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<IdentityUserToken<Guid>>().ToTable("IamUsuarioToken");

        builder.Entity<IamUsuario>(entity =>
        {
            entity.Property(x => x.UserName).HasMaxLength(256);
            entity.Property(x => x.NormalizedUserName).HasMaxLength(256);
            entity.HasIndex(x => x.NormalizedUserName).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_IamUsuario_Vigencia",
                "[ValidUntilUtc] IS NULL OR [ValidFromUtc] IS NULL OR [ValidUntilUtc] > [ValidFromUtc]"));
        });

        builder.Entity<IamRol>(entity =>
        {
            entity.ToTable("IamRol");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(128);
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<IamPermiso>(entity =>
        {
            entity.ToTable("IamPermiso");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(512);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<IamUsuarioRol>(entity =>
        {
            entity.ToTable("IamUsuarioRol");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.RoleId);
            entity.HasIndex(x => x.UserId);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_IamUsuarioRol_Vigencia",
                "[ValidUntilUtc] IS NULL OR [ValidUntilUtc] > [ValidFromUtc]"));
        });

        builder.Entity<IamRolPermiso>(entity =>
        {
            entity.ToTable("IamRolPermiso");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne(x => x.Role).WithMany(x => x.Permissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Permission).WithMany(x => x.Roles).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.PermissionId);
        });

        builder.Entity<IamEventoAutenticacion>(entity =>
        {
            entity.ToTable("IamEventoAutenticacion");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserIdentifier).HasMaxLength(256);
            entity.Property(x => x.Mechanism).HasMaxLength(32);
            entity.Property(x => x.Result).HasMaxLength(32);
            entity.Property(x => x.EventType).HasMaxLength(64);
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.HasIndex(x => x.OccurredAtUtc);
        });

        builder.Entity<EmpresaSubsidiariaReference>(entity =>
        {
            entity.ToTable("TEmpresaSubsidiaria", table => table.ExcludeFromMigrations());
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("idEmpresaSubsidiaria");
            entity.Property(x => x.Name).HasColumnName("nombreEmpresa");
        });

        builder.Entity<IamUsuarioOrganizacion>(entity =>
        {
            entity.ToTable("IamUsuarioOrganizacion", table => table.HasCheckConstraint(
                "CK_IamUsuarioOrganizacion_Alcance",
                "([IsCorporateScope] = 1 AND [EmpresaSubsidiariaId] IS NULL) OR ([IsCorporateScope] = 0 AND [EmpresaSubsidiariaId] IS NOT NULL)"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Justification).HasMaxLength(1024);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.EmpresaSubsidiariaId);
            entity.HasIndex(x => new { x.UserId, x.EmpresaSubsidiariaId, x.IsCorporateScope, x.ValidFromUtc }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EmpresaSubsidiaria).WithMany().HasForeignKey(x => x.EmpresaSubsidiariaId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IamAccesoEmergencia>(entity =>
        {
            entity.ToTable("IamAccesoEmergencia", table => table.HasCheckConstraint(
                "CK_IamAccesoEmergencia_Vigencia",
                "[ExpiresAtUtc] > [StartsAtUtc]"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IncidentReference).HasMaxLength(128);
            entity.Property(x => x.Justification).HasMaxLength(1024);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.EmpresaSubsidiariaId);
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EmpresaSubsidiaria).WithMany().HasForeignKey(x => x.EmpresaSubsidiariaId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IamEventoAuditoriaAutorizacion>(entity =>
        {
            entity.ToTable("IamEventoAuditoriaAutorizacion");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(128);
            entity.Property(x => x.PermissionCode).HasMaxLength(160);
            entity.Property(x => x.Result).HasMaxLength(64);
            entity.Property(x => x.ResourceType).HasMaxLength(128);
            entity.Property(x => x.ResourceId).HasMaxLength(256);
            entity.Property(x => x.Justification).HasMaxLength(1024);
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => x.ActorUserId);
            entity.HasIndex(x => x.BeneficiaryUserId);
            entity.HasIndex(x => x.EmpresaSubsidiariaId);
        });

        builder.Entity<AuditOperation>(entity =>
        {
            entity.ToTable("Operation", "audit");
            entity.HasKey(x => x.OperationId);
            entity.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ActionType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.EntityCode).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PhysicalTableName).HasMaxLength(256);
            entity.Property(x => x.RootDisplayName).HasMaxLength(512);
            entity.Property(x => x.ActorUserNameSnapshot).HasMaxLength(256);
            entity.Property(x => x.Description).HasMaxLength(2048);
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => x.ActorUserId);
            entity.HasIndex(x => x.ActionType);
            entity.HasIndex(x => x.EntityCode);
            entity.HasIndex(x => x.EmpresaSubsidiariaId);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.ReversesOperationId);
        });

        builder.Entity<AuditDeletedRecordSnapshot>(entity =>
        {
            entity.ToTable("RecordSnapshot", "audit");
            entity.HasKey(x => x.SnapshotId);
            entity.Property(x => x.EntityCode).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PhysicalTableName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PrimaryKeyJson).IsRequired();
            entity.Property(x => x.ForeignKeysJson).IsRequired();
            entity.Property(x => x.RowDataJson).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(512);
            entity.HasIndex(x => new { x.OperationId, x.PhysicalTableName, x.DeleteOrder });
            entity.HasOne(x => x.Operation).WithMany(x => x.Snapshots)
                .HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditRecordKeyMap>(entity =>
        {
            entity.ToTable("RecordKeyMap", "audit");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PhysicalTableName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.OldPrimaryKeyJson).IsRequired();
            entity.Property(x => x.NewPrimaryKeyJson).IsRequired();
            entity.HasIndex(x => new { x.OperationId, x.PhysicalTableName });
            entity.HasOne(x => x.Operation).WithMany(x => x.KeyMaps)
                .HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TmDominio>(entity =>
        {
            entity.ToTable("TMDominio", "dbo", table => table.ExcludeFromMigrations());
            entity.HasKey(x => x.Id).HasName("PK_TDominio");
            entity.Property(x => x.Id).HasColumnName("iddominio").ValueGeneratedOnAdd();
            entity.Property(x => x.Dominio).HasColumnName("dominio");
            entity.Property(x => x.DescripcionDominio).HasColumnName("descripcionDominio");
            entity.Property(x => x.Referencias).HasColumnName("referencias");
            entity.Property(x => x.HomologacionDimensionSegunCiber).HasColumnName("homologacionDimensionSegunCiber");
            entity.Property(x => x.HomologacionDimensionSegunLineamiento).HasColumnName("homologacionDimensionSegunLineamiento");
            entity.Property(x => x.SubDominioCvt).HasColumnName("subDominioCVT");
            entity.Property(x => x.Ejemplos).HasColumnName("Ejemplos");
        });
    }

    private void EnsureAuditIsAppendOnly()
    {
        var forbidden = ChangeTracker.Entries()
            .Where(entry => entry.Entity is IamEventoAuditoriaAutorizacion or IamEventoAutenticacion or AuditOperation or AuditDeletedRecordSnapshot or AuditRecordKeyMap)
            .Where(entry => entry.State is EntityState.Modified or EntityState.Deleted)
            .Select(entry => entry.Metadata.DisplayName())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (forbidden.Length > 0)
        {
            throw new InvalidOperationException(
                $"Los eventos de auditoría son append-only: {string.Join(", ", forbidden)}.");
        }
    }
}
