/* NO EJECUTAR SIN APROBACIÓN. Plantilla de sincronización DEV -> RECONCILE. */
SET XACT_ABORT ON;
IF SESSION_CONTEXT(N'reconciliation_approved') <> 1
    THROW 51000, 'Falta aprobación explícita de reconciliación.', 1;
IF DB_NAME() <> N'db-landscape-tsi-reconcile'
    THROW 51001, 'Destino no autorizado.', 1;
USE [db-landscape-tsi-reconcile];
BEGIN TRANSACTION;
-- Conteos esperados de DEV: IamPermiso=15, IamRol=4, IamUsuario=2.
-- Generar aquí, tras revisar el data-gap, MERGE/INSERT parametrizados para
-- dbo.IamPermiso, dbo.IamRol y dbo.IamUsuario. Las columnas PasswordHash,
-- SecurityStamp y ConcurrencyStamp se copian como datos, nunca se imprimen.
-- Mantener los GUID/UserId/RoleId/PermissionId originales.
-- No incluir valores literales ni SELECT de secretos en la salida.
ROLLBACK TRANSACTION; -- plantilla deliberadamente no aplicable
