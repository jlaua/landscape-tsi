-- ============================================================================
-- Migration: Evolve Vendor Association and Deployment Version for Technologies
-- Database: db-landscape-tsi-dev-v2
-- ============================================================================

IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;
GO

-- 1. Agregar idVendor e idVersionDesplegada a TTecnologiaTSI si no existen
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TTecnologiaTSI') 
      AND name = N'idVendor'
)
BEGIN
    ALTER TABLE [dbo].[TTecnologiaTSI] ADD [idVendor] INT NULL;
    PRINT N'Columna idVendor agregada a dbo.TTecnologiaTSI.';
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = N'FK_TTecnologiaTSI_TVendor'
)
BEGIN
    ALTER TABLE [dbo].[TTecnologiaTSI]
    ADD CONSTRAINT [FK_TTecnologiaTSI_TVendor]
    FOREIGN KEY ([idVendor]) REFERENCES [dbo].[TVendor]([idVendor]);
    PRINT N'Constraint FK_TTecnologiaTSI_TVendor agregada.';
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TTecnologiaTSI') 
      AND name = N'idVersionDesplegada'
)
BEGIN
    ALTER TABLE [dbo].[TTecnologiaTSI] ADD [idVersionDesplegada] INT NULL;
    PRINT N'Columna idVersionDesplegada agregada a dbo.TTecnologiaTSI.';
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = N'FK_TTecnologiaTSI_TMVersionDesplegada'
)
BEGIN
    ALTER TABLE [dbo].[TTecnologiaTSI]
    ADD CONSTRAINT [FK_TTecnologiaTSI_TMVersionDesplegada]
    FOREIGN KEY ([idVersionDesplegada]) REFERENCES [dbo].[TMVersionDesplegada]([idVersionDesplegada]);
    PRINT N'Constraint FK_TTecnologiaTSI_TMVersionDesplegada agregada.';
END;
GO

-- Backfill idVendor en TTecnologiaTSI desde TVendor
UPDATE t
SET t.[idVendor] = v.[idVendor]
FROM [dbo].[TTecnologiaTSI] t
INNER JOIN [dbo].[TVendor] v ON v.[idTecnologiaTSI] = t.[idTecnologiaTSI]
WHERE t.[idVendor] IS NULL;
PRINT N'Backfill idVendor en TTecnologiaTSI completado.';
GO

-- 2. Agregar idVersionDesplegada a TTecnologiaTSIimplementadaSubsidiaria si no existe
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TTecnologiaTSIimplementadaSubsidiaria') 
      AND name = N'idVersionDesplegada'
)
BEGIN
    ALTER TABLE [dbo].[TTecnologiaTSIimplementadaSubsidiaria] ADD [idVersionDesplegada] INT NULL;
    PRINT N'Columna idVersionDesplegada agregada a dbo.TTecnologiaTSIimplementadaSubsidiaria.';
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = N'FK_TTecnologiaTSIimplementada_TMVersionDesplegada'
)
BEGIN
    ALTER TABLE [dbo].[TTecnologiaTSIimplementadaSubsidiaria]
    ADD CONSTRAINT [FK_TTecnologiaTSIimplementada_TMVersionDesplegada]
    FOREIGN KEY ([idVersionDesplegada]) REFERENCES [dbo].[TMVersionDesplegada]([idVersionDesplegada]);
    PRINT N'Constraint FK_TTecnologiaTSIimplementada_TMVersionDesplegada agregada.';
END;
GO

-- Backfill idVersionDesplegada en TTecnologiaTSIimplementadaSubsidiaria
UPDATE i
SET i.[idVersionDesplegada] = v.[idVersionDesplegada]
FROM [dbo].[TTecnologiaTSIimplementadaSubsidiaria] i
INNER JOIN [dbo].[TMVersionDesplegada] v ON LOWER(RTRIM(LTRIM(i.[versionDesplegada]))) = LOWER(RTRIM(LTRIM(v.[nombreVersionDesplegada])))
WHERE i.[idVersionDesplegada] IS NULL;
PRINT N'Backfill idVersionDesplegada en TTecnologiaTSIimplementadaSubsidiaria completado.';
GO

PRINT N'Migración evolve-vendor-and-technology-version completada exitosamente.';
