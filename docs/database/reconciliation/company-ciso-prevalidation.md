# Prevalidación Empresa/Subsidiaria–CISO

Fecha del análisis: 2026-09-08  
Alcance: solo lectura; no se ejecutaron DML, DDL ni migrations.

## Estado de acceso a datos

La conexión configurada para Development resolvió de forma segura a:

- Base: `db-landscape-tsi-dev-v2`
- Servidor: configurado en la aplicación (no se documentan credenciales)
- `Encrypt=True`
- `TrustServerCertificate=True`

La apertura con Microsoft.Data.SqlClient falló antes de ejecutar las consultas:

```text
SqlException Number=20, State=0, Class=20
La instancia de SQL Server con la que intenta conectar requiere cifrado,
pero este equipo no lo admite.
```

Por tanto, los conteos y comparaciones quedan **pendientes de ejecución
administrativa** desde un cliente que pueda abrir esa conexión. No se reportan
valores inventados y no se modificó ninguna base.

## Consultas de prevalidación pendientes

Estas consultas están preparadas para ejecutarse únicamente con `SELECT`:

```sql
SELECT COUNT(*) AS TotalEmpresas
FROM dbo.TEmpresaSubsidiaria;

SELECT COUNT(*) AS TotalCiso
FROM dbo.TCISO;

SELECT
    e.idEmpresaSubsidiaria,
    e.nombreEmpresa,
    COUNT(c.idCiso) AS TotalCiso,
    SUM(CASE WHEN c.Representante = 1 THEN 1 ELSE 0 END) AS Representantes
FROM dbo.TEmpresaSubsidiaria e
LEFT JOIN dbo.TCISO c
    ON c.idEmpresaSubsidiaria = e.idEmpresaSubsidiaria
GROUP BY e.idEmpresaSubsidiaria, e.nombreEmpresa;

SELECT
    e.idEmpresaSubsidiaria,
    e.nombreEmpresa,
    e.contactoCiso,
    c.nombreCISO
FROM dbo.TEmpresaSubsidiaria e
LEFT JOIN dbo.TCISO c
    ON c.idEmpresaSubsidiaria = e.idEmpresaSubsidiaria
ORDER BY e.nombreEmpresa, c.nombreCISO;
```

El análisis final deberá clasificar empresas con cero, uno, varios CISO,
varios representantes, ningún representante, coincidencia de texto,
diferencia y ausencia de `contactoCiso`.

## Dependencias encontradas en el repositorio

| Ubicación | Tipo | Uso |
|---|---|---|
| `src/Landscape.Tsi.Application/Catalogs/MasterCatalogRegistry.cs:28` | Metadata de catálogo | Expone `contactoCiso` como campo editable/consultable del catálogo de Empresa/Subsidiaria. |
| `openspec/changes/gestionar-tablas-maestras/design.md:33` | Documentación | Enumera `contactoCiso` dentro de los campos de la entidad. |

No se encontraron referencias adicionales en entidades C#, mapeos EF, servicios,
controllers, Razor, ViewModels, JavaScript, tests ni scripts SQL versionados.
La existencia de objetos SQL fuera del repositorio queda pendiente de validar
con metadatos de SQL Server cuando haya conectividad.

## Decisiones pendientes

1. Confirmar los conteos y discrepancias mediante las consultas anteriores.
2. Determinar si la regla de un único `Representante = 1` es válida.
3. Solo después proponer un índice único filtrado y una migration de retiro de
   `contactoCiso`.
4. No ejecutar `DROP COLUMN`, backfill, índice, migration ni cambios de datos
   sin aprobación independiente.
