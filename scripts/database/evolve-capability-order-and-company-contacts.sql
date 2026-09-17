-- evolve-capability-order-and-company-contacts.sql
-- Agrega columna ordenVisualizacion en TCapacidadDeSeguridad y columna notas en TContactoEmpresaSubsidiaria

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[TCapacidadDeSeguridad]') 
      AND name = N'ordenVisualizacion'
)
BEGIN
    ALTER TABLE [dbo].[TCapacidadDeSeguridad] ADD [ordenVisualizacion] INT NULL;
    PRINT 'Columna ordenVisualizacion agregada a TCapacidadDeSeguridad.';
END
ELSE
BEGIN
    PRINT 'Columna ordenVisualizacion ya existia en TCapacidadDeSeguridad.';
END
GO

-- Inicializar ordenVisualizacion con idCapacidad para registros existentes sin orden asignado
UPDATE [dbo].[TCapacidadDeSeguridad]
SET [ordenVisualizacion] = [idCapacidad]
WHERE [ordenVisualizacion] IS NULL;
PRINT 'Orden visual inicializado para capacidades existentes.';
GO

-- Asegurar columna notas en TContactoEmpresaSubsidiaria si no existe
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[TContactoEmpresaSubsidiaria]') 
      AND name = N'notas'
)
BEGIN
    ALTER TABLE [dbo].[TContactoEmpresaSubsidiaria] ADD [notas] NVARCHAR(MAX) NULL;
    PRINT 'Columna notas agregada a TContactoEmpresaSubsidiaria.';
END
ELSE
BEGIN
    PRINT 'Columna notas ya existia en TContactoEmpresaSubsidiaria.';
END
GO
