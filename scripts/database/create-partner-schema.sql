/* =========================================================================
   Script: create-partner-schema.sql
   Propósito: Creación de la entidad TPartner y vinculación en TContactoPartner:
              - Crear tabla [dbo].[TPartner] con idPartner, nombrePartner, descripcionPartner, idVendor, idTecnologiaTSI
              - Agregar columna idPartner a [dbo].[TContactoPartner] si no existe
              - Migrar el contacto existente (Electrodata) para vincularlo a un partner inicial
   Seguridad: Solo ejecutable en db-landscape-tsi-dev-v2 tras validación estricta de DB_NAME().
   ========================================================================= */

IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Crear tabla dbo.TPartner si no existe
IF OBJECT_ID(N'dbo.TPartner', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TPartner (
        idPartner INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TPartner PRIMARY KEY,
        nombrePartner NVARCHAR(200) NOT NULL,
        descripcionPartner NVARCHAR(500) NULL,
        idVendor INT NULL,
        idTecnologiaTSI INT NULL,
        CONSTRAINT FK_TPartner_TVendor FOREIGN KEY (idVendor) REFERENCES dbo.TVendor(idVendor),
        CONSTRAINT FK_TPartner_TTecnologiaTSI FOREIGN KEY (idTecnologiaTSI) REFERENCES dbo.TTecnologiaTSI(idTecnologiaTSI)
    );
    PRINT N'Tabla dbo.TPartner creada exitosamente.';

    -- Semilla inicial basada en datos existentes
    INSERT INTO dbo.TPartner (nombrePartner, descripcionPartner, idVendor, idTecnologiaTSI)
    VALUES (N'Electrodata', N'Partner integrador de soluciones de seguridad y tecnología', 1, 1);
    PRINT N'Registro inicial Electrodata insertado en dbo.TPartner.';
END;

-- 2. Agregar columna idPartner a dbo.TContactoPartner si no existe
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TContactoPartner') 
      AND name = N'idPartner'
)
BEGIN
    ALTER TABLE dbo.TContactoPartner ADD idPartner INT NULL;
    PRINT N'Columna idPartner agregada a dbo.TContactoPartner.';

    -- Vincular contactos existentes de Electrodata al partner creado
    UPDATE cp
    SET cp.idPartner = p.idPartner
    FROM dbo.TContactoPartner cp
    INNER JOIN dbo.TPartner p ON p.nombrePartner = N'Electrodata'
    WHERE cp.email LIKE N'%electrodata%';
    PRINT N'Contacto existente vinculado a Electrodata.';
END;

COMMIT TRANSACTION;
PRINT N'Script create-partner-schema.sql ejecutado con éxito.';
