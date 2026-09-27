/* =========================================================================
   Script: evolve-implementation-process-schema.sql
   Propósito: Evolución aditiva del esquema para el proceso de Implementaciones TSI:
              - TProcesoAdopcionTSI: tipoProceso, idFamilia, idTecnologiaEstandar,
                idProcesoEvaluacionOrigen, fechaInicioVigenciaContrato, fechaFinVigenciaContrato,
                numeroContratoCorporativo.
              - TProcesoAdopcionEmpresa: fechaInicioAdopcion, especialistaTecnico,
                idContactoEspecialista, urlArquitectura, rutaArchivoArquitectura,
                nombreArchivoArquitectura, estadoImplementacion.
   Seguridad: Solo ejecutable en db-landscape-tsi-dev-v2 tras validación estricta de DB_NAME().
   ========================================================================= */

IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. TProcesoAdopcionTSI: Extensiones no destructivas
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionTSI') AND name = N'tipoProceso')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD tipoProceso NVARCHAR(30) NOT NULL CONSTRAINT DF_TProcesoAdopcionTSI_TipoProceso DEFAULT N'EVALUACION';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionTSI') AND name = N'idFamilia')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD idFamilia INT NULL;
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD CONSTRAINT FK_TProcesoAdopcionTSI_TMFamilia FOREIGN KEY (idFamilia) REFERENCES dbo.TMFamilia(idFamilia);
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionTSI') AND name = N'idTecnologiaEstandar')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD idTecnologiaEstandar INT NULL;
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD CONSTRAINT FK_TProcesoAdopcionTSI_TTecnologiaTSI FOREIGN KEY (idTecnologiaEstandar) REFERENCES dbo.TTecnologiaTSI(idTecnologiaTSI);
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionTSI') AND name = N'idProcesoEvaluacionOrigen')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD idProcesoEvaluacionOrigen INT NULL;
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD CONSTRAINT FK_TProcesoAdopcionTSI_EvaluacionOrigen FOREIGN KEY (idProcesoEvaluacionOrigen) REFERENCES dbo.TProcesoAdopcionTSI(idProcesoAdopcionTSI);
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionTSI') AND name = N'fechaInicioVigenciaContrato')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD fechaInicioVigenciaContrato DATE NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionTSI') AND name = N'fechaFinVigenciaContrato')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD fechaFinVigenciaContrato DATE NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionTSI') AND name = N'numeroContratoCorporativo')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD numeroContratoCorporativo NVARCHAR(100) NULL;
END;

-- Permitir idBuildingBlock NULL para solicitudes de implementación basadas a nivel de tecnología/familia
ALTER TABLE dbo.TProcesoAdopcionTSI ALTER COLUMN idBuildingBlock INT NULL;

-- 2. TProcesoAdopcionEmpresa: Extensiones no destructivas para seguimiento de implementación por subsidiaria
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionEmpresa') AND name = N'fechaInicioAdopcion')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD fechaInicioAdopcion DATE NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionEmpresa') AND name = N'especialistaTecnico')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD especialistaTecnico NVARCHAR(150) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionEmpresa') AND name = N'idContactoEspecialista')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD idContactoEspecialista INT NULL;
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD CONSTRAINT FK_TProcesoAdopcionEmpresa_ContactoEspecialista FOREIGN KEY (idContactoEspecialista) REFERENCES dbo.TContactoEmpresaSubsidiaria(idContactoEmpresaSubsidiaria);
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionEmpresa') AND name = N'urlArquitectura')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD urlArquitectura NVARCHAR(500) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionEmpresa') AND name = N'rutaArchivoArquitectura')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD rutaArchivoArquitectura NVARCHAR(500) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionEmpresa') AND name = N'nombreArchivoArquitectura')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD nombreArchivoArquitectura NVARCHAR(255) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionEmpresa') AND name = N'estadoImplementacion')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD estadoImplementacion NVARCHAR(50) NOT NULL CONSTRAINT DF_TProcesoAdopcionEmpresa_EstadoImpl DEFAULT N'PENDIENTE';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TProcesoAdopcionEmpresa') AND name = N'tecnologiaAsIs')
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD tecnologiaAsIs NVARCHAR(255) NULL;
END;

COMMIT TRANSACTION;
