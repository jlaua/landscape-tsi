/* NO EJECUTAR SIN APROBACIÓN. Auditoría IAM; preservar timestamps UTC. */
SET XACT_ABORT ON;
IF SESSION_CONTEXT(N'reconciliation_approved') <> 1
    THROW 51000, 'Falta aprobación explícita de reconciliación.', 1;
IF DB_NAME() <> N'db-landscape-tsi-reconcile'
    THROW 51001, 'Destino no autorizado.', 1;
USE [db-landscape-tsi-reconcile];
BEGIN TRANSACTION;
-- Conteos esperados: IamEventoAutenticacion=60,
-- IamEventoAuditoriaAutorizacion=5, IamAccesoEmergencia=0.
-- Sincronizar únicamente IamEventoAutenticacion, IamEventoAuditoriaAutorizacion
-- e IamAccesoEmergencia según la política de retención aprobada.
-- No exponer BeforeJson/AfterJson si contienen datos sensibles en reportes.
ROLLBACK TRANSACTION;
