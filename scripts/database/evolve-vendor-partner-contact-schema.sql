/* =========================================================================
   Script: evolve-vendor-partner-contact-schema.sql
   Propósito: Evolución aditiva y segura para Landscape TSI:
              - Agregar columnas ROL (NVARCHAR(100) NULL) y NOTAS (NVARCHAR(MAX) NULL)
                a las tablas [dbo].[TContactoVendor] y [dbo].[TContactoPartner] si no existen.
              - Mantener la integridad de las columnas existentes:
                idContacto*, idVendor, nombreContacto*, email, telefono, otro.
   Seguridad: Solo ejecutable en db-landscape-tsi-dev-v2 tras validación estricta de DB_NAME().
   ========================================================================= */

IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. TContactoVendor: Columnas ROL, nombreContactoVendor, email, telefono, otro, NOTAS
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TContactoVendor') 
      AND name = N'ROL'
)
BEGIN
    ALTER TABLE dbo.TContactoVendor ADD ROL NVARCHAR(100) NULL;
    PRINT N'Columna ROL agregada a dbo.TContactoVendor.';
END;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TContactoVendor') 
      AND name = N'otro'
)
BEGIN
    ALTER TABLE dbo.TContactoVendor ADD otro NVARCHAR(250) NULL;
    PRINT N'Columna otro agregada a dbo.TContactoVendor.';
END;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TContactoVendor') 
      AND name = N'NOTAS'
)
BEGIN
    ALTER TABLE dbo.TContactoVendor ADD NOTAS NVARCHAR(MAX) NULL;
    PRINT N'Columna NOTAS agregada a dbo.TContactoVendor.';
END;

-- 2. TContactoPartner: Columnas ROL, nombreContactoPartner, email, telefono, otro, NOTAS
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TContactoPartner') 
      AND name = N'ROL'
)
BEGIN
    ALTER TABLE dbo.TContactoPartner ADD ROL NVARCHAR(100) NULL;
    PRINT N'Columna ROL agregada a dbo.TContactoPartner.';
END;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TContactoPartner') 
      AND name = N'otro'
)
BEGIN
    ALTER TABLE dbo.TContactoPartner ADD otro NVARCHAR(250) NULL;
    PRINT N'Columna otro agregada a dbo.TContactoPartner.';
END;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.TContactoPartner') 
      AND name = N'NOTAS'
)
BEGIN
    ALTER TABLE dbo.TContactoPartner ADD NOTAS NVARCHAR(MAX) NULL;
    PRINT N'Columna NOTAS agregada a dbo.TContactoPartner.';
END;

COMMIT TRANSACTION;
PRINT N'Evolución de columnas de contactos (Vendor y Partner) completada exitosamente.';
