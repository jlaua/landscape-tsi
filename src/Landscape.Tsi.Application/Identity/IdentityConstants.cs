namespace Landscape.Tsi.Application.Identity;

public static class SystemRoles
{
    public const string AdministratorCode = "SYSTEM_ADMINISTRATOR";
    public const string AdministratorName = "Administrador del Sistema";

    public const string SecurityArchitectCode = "SECURITY_ARCHITECT";
    public const string SecurityArchitectName = "Arquitecto de Seguridad";

    public const string TsiEngineerCode = "TSI_ENGINEER";
    public const string TsiEngineerName = "Ingeniero de TSI";

    public const string GovernmentSpocCode = "GOVERNMENT_SPOC";
    public const string GovernmentSpocName = "Punto de Contacto del Gobierno";

    public static readonly IReadOnlyDictionary<string, string> InitialRoles =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [AdministratorCode] = AdministratorName,
            [SecurityArchitectCode] = SecurityArchitectName,
            [TsiEngineerCode] = TsiEngineerName,
            [GovernmentSpocCode] = GovernmentSpocName
        };
}

public static class Permissions
{
    public const string CatalogView = "Catalogos.Ver";
    public const string CatalogCreate = "Catalogos.Crear";
    public const string CatalogEdit = "Catalogos.Editar";
    public const string CatalogDeactivate = "Catalogos.Desactivar";
    public const string UserManage = "Administration.UserManage";
    public const string RoleManage = "Administration.RoleManage";
    public const string AuditView = "Audit.View";

    public static readonly IReadOnlyDictionary<string, string> AdministratorPermissions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CatalogView] = "Consultar catálogos maestros",
            [CatalogCreate] = "Crear registros en catálogos autorizados",
            [CatalogEdit] = "Editar registros en catálogos autorizados",
            [CatalogDeactivate] = "Desactivar registros cuando exista una estrategia aprobada",
            [UserManage] = "Administrar usuarios",
            [RoleManage] = "Administrar roles y permisos",
            [AuditView] = "Consultar auditoría autorizada"
        };
}

public static class CustomClaimTypes
{
    public const string Permission = "landscape_tsi_permission";
    public const string AuthenticationMethod = "landscape_tsi_authentication_method";
}
