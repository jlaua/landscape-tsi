/* =========================================================================
   Script: evolve-vencimiento-drivers-config.sql
   Propósito: Agregar columna driversReporteVencimiento a TProcesoAdopcionTSI
              para persistir en base de datos la selección personalizada de drivers
              proyectados en el Reporte de Vencimiento de Contratos.
   Seguridad: No destructivo, idempotente y aditivo.
   ========================================================================= */

IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[TProcesoAdopcionTSI]') 
      AND name = N'driversReporteVencimiento'
)
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI 
        ADD driversReporteVencimiento NVARCHAR(1000) NULL;
    PRINT 'Columna driversReporteVencimiento agregada a TProcesoAdopcionTSI.';
END
ELSE
BEGIN
    PRINT 'Columna driversReporteVencimiento ya existia en TProcesoAdopcionTSI.';
END

COMMIT TRANSACTION;
