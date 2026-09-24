/* =========================================================================
   Script: evolve-proceso-orden-empresas.sql
   Propósito: Agregar columna ordenEmpresas a TProcesoAdopcionTSI
              para persistir en base de datos la secuencia y orden visual
              de las empresas subsidiarias convocadas en el proceso de evaluación,
              sincronizándolo con la sección de Convocatoria y los reportes de seguimiento.
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
      AND name = N'ordenEmpresas'
)
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI 
        ADD ordenEmpresas NVARCHAR(2000) NULL;
    PRINT 'Columna ordenEmpresas agregada a TProcesoAdopcionTSI.';
END
ELSE
BEGIN
    PRINT 'Columna ordenEmpresas ya existia en TProcesoAdopcionTSI.';
END

COMMIT TRANSACTION;
