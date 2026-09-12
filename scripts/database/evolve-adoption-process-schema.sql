/* =========================================================================
   Script: evolve-adoption-process-schema.sql
   Propósito: Evolución aditiva del esquema para Landscape TSI:
              - Proceso de Adopción TSI (TProcesoAdopcionTSI)
              - Empresas participantes en el Proceso (TProcesoAdopcionEmpresa)
              - Estándares Corporativos con Histórico (TEstandarTecnologiaHistorico)
              - Contratos y Adendas 1:N (TContratoTecnologia)
              - Extensión no destructiva en TBuildingBlock, TTecnologiaTSIimplementadaSubsidiaria, TDriver, TCasosDeUso
   Seguridad: Solo ejecutable en db-landscape-tsi-dev-v2 tras validación estricta de DB_NAME().
   ========================================================================= */

IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. TProcesoAdopcionTSI
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TProcesoAdopcionTSI' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TProcesoAdopcionTSI (
        idProcesoAdopcionTSI INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TProcesoAdopcionTSI PRIMARY KEY CLUSTERED,
        codigoProceso NVARCHAR(50) NOT NULL,
        nombreProceso NVARCHAR(200) NOT NULL,
        idBuildingBlock INT NOT NULL,
        idEstadoAdopcionTSI INT NOT NULL,
        objetivo NVARCHAR(1000) NULL,
        alcance NVARCHAR(1000) NULL,
        liderCorporativoTSI NVARCHAR(150) NULL,
        fechaInicio DATE NOT NULL,
        fechaEstimadaCierre DATE NULL,
        fechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_TProcesoAdopcionTSI_FechaCreacion DEFAULT SYSUTCDATETIME(),
        usuarioCreacion NVARCHAR(100) NOT NULL CONSTRAINT DF_TProcesoAdopcionTSI_UsuarioCreacion DEFAULT N'SYSTEM'
    );

    ALTER TABLE dbo.TProcesoAdopcionTSI
        ADD CONSTRAINT FK_TProcesoAdopcionTSI_TBuildingBlock
        FOREIGN KEY (idBuildingBlock) REFERENCES dbo.TBuildingBlock(idBuildingBlock);

    ALTER TABLE dbo.TProcesoAdopcionTSI
        ADD CONSTRAINT FK_TProcesoAdopcionTSI_TMEstadoAdopcionTSI
        FOREIGN KEY (idEstadoAdopcionTSI) REFERENCES dbo.TMEstadoAdopcionTSI(idEstadoAdopcionTSI);

    CREATE UNIQUE NONCLUSTERED INDEX UX_TProcesoAdopcionTSI_Codigo
        ON dbo.TProcesoAdopcionTSI(codigoProceso);
END;

-- 2. TProcesoAdopcionEmpresa
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TProcesoAdopcionEmpresa' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TProcesoAdopcionEmpresa (
        idProcesoAdopcionEmpresa INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TProcesoAdopcionEmpresa PRIMARY KEY CLUSTERED,
        idProcesoAdopcionTSI INT NOT NULL,
        idEmpresaSubsidiaria INT NOT NULL,
        idContactoEmpresaSubsidiaria INT NULL,
        aplica BIT NOT NULL CONSTRAINT DF_TProcesoAdopcionEmpresa_Aplica DEFAULT 1,
        justificacionNoAplica NVARCHAR(1000) NULL,
        fechaIncorporacion DATE NOT NULL CONSTRAINT DF_TProcesoAdopcionEmpresa_Fecha DEFAULT CAST(SYSUTCDATETIME() AS DATE),
        fechaModificacion DATETIME2(0) NOT NULL CONSTRAINT DF_TProcesoAdopcionEmpresa_FechaMod DEFAULT SYSUTCDATETIME(),
        usuarioModificacion NVARCHAR(100) NOT NULL CONSTRAINT DF_TProcesoAdopcionEmpresa_UserMod DEFAULT N'SYSTEM'
    );

    ALTER TABLE dbo.TProcesoAdopcionEmpresa
        ADD CONSTRAINT FK_TProcesoAdopcionEmpresa_TProcesoAdopcionTSI
        FOREIGN KEY (idProcesoAdopcionTSI) REFERENCES dbo.TProcesoAdopcionTSI(idProcesoAdopcionTSI);

    ALTER TABLE dbo.TProcesoAdopcionEmpresa
        ADD CONSTRAINT FK_TProcesoAdopcionEmpresa_TEmpresaSubsidiaria
        FOREIGN KEY (idEmpresaSubsidiaria) REFERENCES dbo.TEmpresaSubsidiaria(idEmpresaSubsidiaria);

    ALTER TABLE dbo.TProcesoAdopcionEmpresa
        ADD CONSTRAINT FK_TProcesoAdopcionEmpresa_TContactoEmpresaSubsidiaria
        FOREIGN KEY (idContactoEmpresaSubsidiaria) REFERENCES dbo.TContactoEmpresaSubsidiaria(idContactoEmpresaSubsidiaria);

    CREATE UNIQUE NONCLUSTERED INDEX UX_TProcesoAdopcionEmpresa_Proceso_Empresa
        ON dbo.TProcesoAdopcionEmpresa(idProcesoAdopcionTSI, idEmpresaSubsidiaria);
END;

-- 3. TEstandarTecnologiaHistorico
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TEstandarTecnologiaHistorico' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TEstandarTecnologiaHistorico (
        idEstandarTecnologia INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TEstandarTecnologiaHistorico PRIMARY KEY CLUSTERED,
        idBuildingBlock INT NOT NULL,
        idTecnologiaTSI INT NOT NULL,
        idProcesoAdopcionTSI INT NULL,
        rolEstandar NVARCHAR(30) NOT NULL CONSTRAINT DF_TEstandar_Rol DEFAULT N'PRINCIPAL',
        estadoVigencia NVARCHAR(30) NOT NULL CONSTRAINT DF_TEstandar_Estado DEFAULT N'ACTIVO_VIGENTE',
        fechaInicioVigencia DATE NOT NULL,
        fechaFinVigencia DATE NULL,
        motivoCambio NVARCHAR(1000) NULL,
        sustentoArquitectura NVARCHAR(MAX) NULL,
        fechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_TEstandar_FechaReg DEFAULT SYSUTCDATETIME(),
        usuarioRegistro NVARCHAR(100) NOT NULL CONSTRAINT DF_TEstandar_UserReg DEFAULT N'SYSTEM'
    );

    ALTER TABLE dbo.TEstandarTecnologiaHistorico
        ADD CONSTRAINT FK_TEstandarTecnologiaHistorico_TBuildingBlock
        FOREIGN KEY (idBuildingBlock) REFERENCES dbo.TBuildingBlock(idBuildingBlock);

    ALTER TABLE dbo.TEstandarTecnologiaHistorico
        ADD CONSTRAINT FK_TEstandarTecnologiaHistorico_TTecnologiaTSI
        FOREIGN KEY (idTecnologiaTSI) REFERENCES dbo.TTecnologiaTSI(idTecnologiaTSI);

    ALTER TABLE dbo.TEstandarTecnologiaHistorico
        ADD CONSTRAINT FK_TEstandarTecnologiaHistorico_TProcesoAdopcionTSI
        FOREIGN KEY (idProcesoAdopcionTSI) REFERENCES dbo.TProcesoAdopcionTSI(idProcesoAdopcionTSI);

    CREATE UNIQUE NONCLUSTERED INDEX UX_TEstandar_Principal_Vigente
        ON dbo.TEstandarTecnologiaHistorico(idBuildingBlock)
        WHERE rolEstandar = N'PRINCIPAL' AND estadoVigencia = N'ACTIVO_VIGENTE';
END;

-- 4. TContratoTecnologia (1:N hacia TTecnologiaTSIimplementadaSubsidiaria con adendas autorreferenciadas)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TContratoTecnologia' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TContratoTecnologia (
        idContratoTecnologia INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TContratoTecnologia PRIMARY KEY CLUSTERED,
        idTecnologiaTSIimplementadaSubsidiaria INT NOT NULL,
        numeroContrato NVARCHAR(100) NOT NULL,
        esAdenda BIT NOT NULL CONSTRAINT DF_TContrato_EsAdenda DEFAULT 0,
        idContratoPadre INT NULL,
        fechaInicio DATE NOT NULL,
        fechaFin DATE NOT NULL,
        fechaAdjudicacion DATE NULL,
        rutaDocumentoContrato NVARCHAR(500) NULL,
        montoContratado DECIMAL(18,2) NULL,
        moneda NVARCHAR(10) NOT NULL CONSTRAINT DF_TContrato_Moneda DEFAULT N'USD',
        observaciones NVARCHAR(MAX) NULL,
        fechaRegistro DATETIME2(0) NOT NULL CONSTRAINT DF_TContrato_FechaReg DEFAULT SYSUTCDATETIME(),
        usuarioRegistro NVARCHAR(100) NOT NULL CONSTRAINT DF_TContrato_UserReg DEFAULT N'SYSTEM'
    );

    ALTER TABLE dbo.TContratoTecnologia
        ADD CONSTRAINT FK_TContratoTecnologia_Implementada
        FOREIGN KEY (idTecnologiaTSIimplementadaSubsidiaria) REFERENCES dbo.TTecnologiaTSIimplementadaSubsidiaria(idTecnologiaTSIimplementadaSubsidiaria);

    ALTER TABLE dbo.TContratoTecnologia
        ADD CONSTRAINT FK_TContratoTecnologia_Padre
        FOREIGN KEY (idContratoPadre) REFERENCES dbo.TContratoTecnologia(idContratoTecnologia);
END;

-- 5. Extensiones No Destructivas en Tablas Existentes

-- 5.1 TBuildingBlock: idFamilia
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TBuildingBlock') AND name = N'idFamilia')
BEGIN
    ALTER TABLE dbo.TBuildingBlock ADD idFamilia INT NULL;
    ALTER TABLE dbo.TBuildingBlock ADD CONSTRAINT FK_TBuildingBlock_TMFamilia FOREIGN KEY (idFamilia) REFERENCES dbo.TMFamilia(idFamilia);
END;

-- 5.2 TTecnologiaTSIimplementadaSubsidiaria: idBuildingBlock, idProcesoAdopcionEmpresa, esTecnologiaPrimaria, versionDesplegada
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TTecnologiaTSIimplementadaSubsidiaria') AND name = N'idBuildingBlock')
BEGIN
    ALTER TABLE dbo.TTecnologiaTSIimplementadaSubsidiaria ADD idBuildingBlock INT NULL;
    ALTER TABLE dbo.TTecnologiaTSIimplementadaSubsidiaria ADD CONSTRAINT FK_TTecnologiaTSIimplementadaSubsidiaria_TBuildingBlock FOREIGN KEY (idBuildingBlock) REFERENCES dbo.TBuildingBlock(idBuildingBlock);
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TTecnologiaTSIimplementadaSubsidiaria') AND name = N'idProcesoAdopcionEmpresa')
BEGIN
    ALTER TABLE dbo.TTecnologiaTSIimplementadaSubsidiaria ADD idProcesoAdopcionEmpresa INT NULL;
    ALTER TABLE dbo.TTecnologiaTSIimplementadaSubsidiaria ADD CONSTRAINT FK_TTecnologiaTSIimplementadaSubsidiaria_TProcesoAdopcionEmpresa FOREIGN KEY (idProcesoAdopcionEmpresa) REFERENCES dbo.TProcesoAdopcionEmpresa(idProcesoAdopcionEmpresa);
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TTecnologiaTSIimplementadaSubsidiaria') AND name = N'esTecnologiaPrimaria')
BEGIN
    ALTER TABLE dbo.TTecnologiaTSIimplementadaSubsidiaria ADD esTecnologiaPrimaria BIT NOT NULL CONSTRAINT DF_TTecnologiaImpl_EsPrimaria DEFAULT 1;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TTecnologiaTSIimplementadaSubsidiaria') AND name = N'versionDesplegada')
BEGIN
    ALTER TABLE dbo.TTecnologiaTSIimplementadaSubsidiaria ADD versionDesplegada NVARCHAR(50) NULL;
END;

-- 5.3 TDriver: unidadMedida, cantidad, precioUnitario, moneda
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TDriver') AND name = N'unidadMedida')
BEGIN
    ALTER TABLE dbo.TDriver ADD unidadMedida NVARCHAR(50) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TDriver') AND name = N'cantidad')
BEGIN
    ALTER TABLE dbo.TDriver ADD cantidad DECIMAL(18,2) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TDriver') AND name = N'precioUnitario')
BEGIN
    ALTER TABLE dbo.TDriver ADD precioUnitario DECIMAL(18,2) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TDriver') AND name = N'moneda')
BEGIN
    ALTER TABLE dbo.TDriver ADD moneda NVARCHAR(10) NOT NULL CONSTRAINT DF_TDriver_Moneda DEFAULT N'USD';
END;

-- 5.4 TCasosDeUso: idEstandarTecnologia
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TCasosDeUso') AND name = N'idEstandarTecnologia')
BEGIN
    ALTER TABLE dbo.TCasosDeUso ADD idEstandarTecnologia INT NULL;
    ALTER TABLE dbo.TCasosDeUso ADD CONSTRAINT FK_TCasosDeUso_TEstandarTecnologiaHistorico FOREIGN KEY (idEstandarTecnologia) REFERENCES dbo.TEstandarTecnologiaHistorico(idEstandarTecnologia);
END;

COMMIT TRANSACTION;
