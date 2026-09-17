/* =========================================================================
   Script: evolve-company-capabilities-matrix.sql
   Propósito: Creación de la tabla TProcesoEmpresaCapacidad para persistir
              la cobertura técnica de capacidades (A, F, NA) por subsidiaria
              en cada proceso de adopción / evaluación TSI.
   ========================================================================= */

IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TProcesoEmpresaCapacidad' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TProcesoEmpresaCapacidad (
        idProcesoEmpresaCapacidad INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TProcesoEmpresaCapacidad PRIMARY KEY CLUSTERED,
        idProcesoAdopcionEmpresa INT NOT NULL,
        idCapacidad INT NOT NULL,
        estadoCobertura NVARCHAR(10) NOT NULL CONSTRAINT DF_TProcesoEmpresaCapacidad_Estado DEFAULT N'NA', -- 'A' (Activo), 'F' (Futuro), 'NA' (No aplica)
        comentario NVARCHAR(500) NULL,
        fechaModificacion DATETIME2(0) NOT NULL CONSTRAINT DF_TProcesoEmpresaCapacidad_FechaMod DEFAULT SYSUTCDATETIME(),
        usuarioModificacion NVARCHAR(100) NOT NULL CONSTRAINT DF_TProcesoEmpresaCapacidad_UserMod DEFAULT N'SYSTEM',
        CONSTRAINT FK_TProcesoEmpresaCapacidad_TProcesoAdopcionEmpresa FOREIGN KEY (idProcesoAdopcionEmpresa) REFERENCES dbo.TProcesoAdopcionEmpresa(idProcesoAdopcionEmpresa) ON DELETE CASCADE,
        CONSTRAINT FK_TProcesoEmpresaCapacidad_TCapacidadDeSeguridad FOREIGN KEY (idCapacidad) REFERENCES dbo.TCapacidadDeSeguridad(idCapacidad),
        CONSTRAINT UX_TProcesoEmpresaCapacidad UNIQUE (idProcesoAdopcionEmpresa, idCapacidad)
    );
    PRINT 'Tabla TProcesoEmpresaCapacidad creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'Tabla TProcesoEmpresaCapacidad ya existe.';
END;

-- Poblar capacidades iniciales para todas las empresas ya convocadas en procesos de adopción
-- Se toman las capacidades de seguridad pertenecientes al Building Block del proceso
INSERT INTO dbo.TProcesoEmpresaCapacidad (
    idProcesoAdopcionEmpresa,
    idCapacidad,
    estadoCobertura,
    comentario,
    fechaModificacion,
    usuarioModificacion
)
SELECT 
    pae.idProcesoAdopcionEmpresa,
    cap.idCapacidad,
    N'NA',
    NULL,
    SYSUTCDATETIME(),
    N'SYSTEM'
FROM dbo.TProcesoAdopcionEmpresa pae
INNER JOIN dbo.TProcesoAdopcionTSI p ON p.idProcesoAdopcionTSI = pae.idProcesoAdopcionTSI
INNER JOIN dbo.TCapacidadDeSeguridad cap ON cap.idBuildingBlock = p.idBuildingBlock
WHERE NOT EXISTS (
    SELECT 1 
    FROM dbo.TProcesoEmpresaCapacidad pec 
    WHERE pec.idProcesoAdopcionEmpresa = pae.idProcesoAdopcionEmpresa
      AND pec.idCapacidad = cap.idCapacidad
);

PRINT 'Capacidades iniciales sincronizadas para empresas convocadas.';

COMMIT TRANSACTION;
