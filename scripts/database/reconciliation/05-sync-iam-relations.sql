/* NO EJECUTAR SIN APROBACIÓN. Plantilla; preserva IDs y FK de Identity. */
SET XACT_ABORT ON;
IF SESSION_CONTEXT(N'reconciliation_approved') <> 1
    THROW 51000, 'Falta aprobación explícita de reconciliación.', 1;
IF DB_NAME() <> N'db-landscape-tsi-reconcile'
    THROW 51001, 'Destino no autorizado.', 1;
USE [db-landscape-tsi-reconcile];
BEGIN TRANSACTION;
-- Conteos esperados: IamRolPermiso=18, IamUsuarioRol=2 y 0 en claims,
-- login externo, organización y tokens.
-- Sincronizar, después del esquema y maestros: IamRolPermiso, IamUsuarioRol,
-- IamUsuarioClaim, IamUsuarioLoginExterno, IamUsuarioOrganizacion y tokens.
-- Usar columnas/PK/FK obtenidas de sys y comandos parametrizados.
-- Nunca mostrar PasswordHash, tokens o valores de secretos.
ROLLBACK TRANSACTION;
