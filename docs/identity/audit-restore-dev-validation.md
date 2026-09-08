# Validación DEV de alcance y Restore auditado

Fecha: 2026-09-08  
Base: `db-landscape-tsi-dev-v2`

`IamUsuarioOrganizacion` representa el alcance corporativo con
`IsCorporateScope = 1` y `EmpresaSubsidiariaId = NULL`, protegido por
`CK_IamUsuarioOrganizacion_Alcance`. La autorización continúa siendo la
combinación de permiso vigente y alcance vigente; no existe bypass para
`SYSTEM_ADMINISTRATOR`.

Se asignó mediante `IOrganizationScopeAdministration` (idempotente y con
evento append-only `OrganizationScopeAssigned`) alcance corporativo vigente a
`jean` y `administrador`. El aprobador se valida como usuario activo con rol
`SYSTEM_ADMINISTRATOR` y no puede ser el beneficiario.

La operación DELETE temporal `6c30c13d-8283-44e3-b28b-b2448efa5cab` fue
previsualizada y restaurada con autorización efectiva. Resultado observado:

- `PreviewAsync`: `CanUndo=true` antes de restaurar.
- `RestoreAsync`: éxito y evento `RESTORE` creado.
- `AuditRecordKeyMap`: un mapeo `OldPrimaryKey -> NewPrimaryKey` creado.
- segundo Restore: rechazado con “La operación ya fue restaurada”.
- no se usó `IDENTITY_INSERT`, `NOCHECK CONSTRAINT` ni DDL.

El formulario de administración de usuario ahora permite consultar alcance,
asignar alcance corporativo con aprobador y vigencia, y retirar asignaciones,
sin exponer secretos.
