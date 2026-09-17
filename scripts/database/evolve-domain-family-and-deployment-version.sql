-- ============================================================================
-- Migration: Evolve Domain Family and Deployment Version Master Table
-- Database: db-landscape-tsi-dev-v2
-- ============================================================================

-- 1. Agregar columna idFamilia a TMDominio si no existe
IF NOT EXISTS (
    SELECT 1 FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = 'TMDominio' AND c.name = 'idFamilia'
)
BEGIN
    ALTER TABLE [dbo].[TMDominio] ADD [idFamilia] INT NULL;
    ALTER TABLE [dbo].[TMDominio] ADD CONSTRAINT [FK_TMDominio_TMFamilia] FOREIGN KEY ([idFamilia]) REFERENCES [dbo].[TMFamilia]([idFamilia]);
    PRINT 'Added column idFamilia and FK_TMDominio_TMFamilia to dbo.TMDominio.';
END
ELSE
BEGIN
    PRINT 'Column idFamilia already exists in dbo.TMDominio.';
END
GO

-- 2. Crear tabla TMVersionDesplegada si no existe
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TMVersionDesplegada')
BEGIN
    CREATE TABLE [dbo].[TMVersionDesplegada] (
        [idVersionDesplegada] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [nombreVersionDesplegada] NVARCHAR(100) NOT NULL,
        [descripcion] NVARCHAR(250) NULL,
        [orden] INT NULL,
        [esActivo] BIT NOT NULL CONSTRAINT [DF_TMVersionDesplegada_esActivo] DEFAULT 1
    );
    PRINT 'Created table dbo.TMVersionDesplegada.';
END
ELSE
BEGIN
    PRINT 'Table dbo.TMVersionDesplegada already exists.';
END
GO

-- 3. Cargar valores iniciales en TMVersionDesplegada
MERGE [dbo].[TMVersionDesplegada] AS target
USING (VALUES
    (N'SaaS', N'Software as a Service', 1),
    (N'PaaS', N'Platform as a Service', 2),
    (N'IaaS', N'Infrastructure as a Service', 3),
    (N'On-Premises', N'Instalación local / On-Premises', 4),
    (N'End-user', N'Software de usuario final / endpoint', 5),
    (N'Híbrido', N'Despliegue mixto / híbrido', 6)
) AS source ([nombreVersionDesplegada], [descripcion], [orden])
ON target.[nombreVersionDesplegada] = source.[nombreVersionDesplegada]
WHEN MATCHED THEN
    UPDATE SET [descripcion] = source.[descripcion], [orden] = source.[orden]
WHEN NOT MATCHED THEN
    INSERT ([nombreVersionDesplegada], [descripcion], [orden], [esActivo])
    VALUES (source.[nombreVersionDesplegada], source.[descripcion], source.[orden], 1);
PRINT 'Seeded dbo.TMVersionDesplegada.';
GO

-- 4. Limpieza de registros dummy residuales de integración (ITEST_%)
DELETE FROM [dbo].[TBuildingBlockVsTTecnologiaTSI] WHERE [idBuildingBlock] IN (SELECT [idBuildingBlock] FROM [dbo].[TBuildingBlock] WHERE [nombreBuildingBlock] LIKE 'ITEST_%');
DELETE FROM [dbo].[TCapacidadDeSeguridad] WHERE [idBuildingBlock] IN (SELECT [idBuildingBlock] FROM [dbo].[TBuildingBlock] WHERE [nombreBuildingBlock] LIKE 'ITEST_%');
DELETE FROM [dbo].[TBuildingBlock] WHERE [nombreBuildingBlock] LIKE 'ITEST_%';
DELETE FROM [dbo].[TMDominio] WHERE [dominio] LIKE 'ITEST_%';
DELETE FROM [dbo].[TMFamilia] WHERE [nombreFamilia] LIKE 'ITEST_%';
PRINT 'Cleaned up ITEST_% records.';
GO
