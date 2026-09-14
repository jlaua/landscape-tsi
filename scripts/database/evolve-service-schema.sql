/* =========================================================================
   Script: evolve-service-schema.sql
   Propósito: Evolución aditiva del esquema para Landscape TSI:
              - Catálogo de Tipos de Servicio (TTipoServicio)
              - Entidad Cabecera de Servicios (TServicioTecnologia)
              - Tarifario por Rango de Horas (TTarifarioProyectoHoras - Impl & Migr)
              - Catálogo de Actividades por Nivel de Soporte (TActividadNivelSoporte)
              - Matriz de Tarifario de Operación (TTarifarioOperacion - N1, N2, N3)
   Seguridad: Solo ejecutable en db-landscape-tsi-dev-v2 tras validación estricta de DB_NAME().
   ========================================================================= */

IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. TTipoServicio
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TTipoServicio' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TTipoServicio (
        idTipoServicio INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TTipoServicio PRIMARY KEY CLUSTERED,
        codigo NVARCHAR(20) NOT NULL CONSTRAINT UX_TTipoServicio_Codigo UNIQUE,
        nombre NVARCHAR(100) NOT NULL,
        descripcion NVARCHAR(500) NULL,
        orden INT NOT NULL CONSTRAINT DF_TTipoServicio_Orden DEFAULT 1,
        esActivo BIT NOT NULL CONSTRAINT DF_TTipoServicio_Activo DEFAULT 1
    );

    INSERT INTO dbo.TTipoServicio (codigo, nombre, descripcion, orden, esActivo)
    VALUES 
        (N'IMPLEMENTACION', N'Implementación', N'Servicios de despliegue inicial, instalación, integración y puesta en producción.', 1, 1),
        (N'MIGRACION', N'Migración', N'Servicios de transición, migración de reglas, datos, políticas e identidades AS-IS.', 2, 1),
        (N'OPERACION', N'Operación', N'Servicios continuos de soporte técnico, monitoreo y administración delegada N1, N2 y N3.', 3, 1);
END;

-- 2. TServicioTecnologia
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TServicioTecnologia' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TServicioTecnologia (
        idServicio INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TServicioTecnologia PRIMARY KEY CLUSTERED,
        codigoServicio NVARCHAR(50) NOT NULL CONSTRAINT UX_TServicioTecnologia_Codigo UNIQUE,
        nombreServicio NVARCHAR(200) NOT NULL,
        descripcion NVARCHAR(1000) NULL,
        idTipoServicio INT NOT NULL,
        idTecnologiaTSI INT NULL,
        idTecnologiaTSIimplementadaSubsidiaria INT NULL,
        idEmpresaSubsidiaria INT NULL,
        idProcesoAdopcionTSI INT NULL,
        idVendor INT NULL,
        nombreProveedorServicio NVARCHAR(150) NULL,
        estadoServicio NVARCHAR(30) NOT NULL CONSTRAINT DF_TServicio_Estado DEFAULT N'EVALUACION',
        costoTotalEstimado DECIMAL(18,2) NOT NULL CONSTRAINT DF_TServicio_CostoTotal DEFAULT 0.00,
        moneda NVARCHAR(10) NOT NULL CONSTRAINT DF_TServicio_Moneda DEFAULT N'USD',
        fechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_TServicio_FechaCreacion DEFAULT SYSUTCDATETIME(),
        usuarioCreacion NVARCHAR(100) NOT NULL CONSTRAINT DF_TServicio_UsuarioCreacion DEFAULT N'SYSTEM',
        fechaModificacion DATETIME2(0) NOT NULL CONSTRAINT DF_TServicio_FechaModificacion DEFAULT SYSUTCDATETIME(),
        usuarioModificacion NVARCHAR(100) NOT NULL CONSTRAINT DF_TServicio_UsuarioModificacion DEFAULT N'SYSTEM'
    );

    ALTER TABLE dbo.TServicioTecnologia
        ADD CONSTRAINT FK_TServicioTecnologia_Tipo
        FOREIGN KEY (idTipoServicio) REFERENCES dbo.TTipoServicio(idTipoServicio);

    ALTER TABLE dbo.TServicioTecnologia
        ADD CONSTRAINT FK_TServicioTecnologia_TTecnologiaTSI
        FOREIGN KEY (idTecnologiaTSI) REFERENCES dbo.TTecnologiaTSI(idTecnologiaTSI);

    ALTER TABLE dbo.TServicioTecnologia
        ADD CONSTRAINT FK_TServicioTecnologia_Implementada
        FOREIGN KEY (idTecnologiaTSIimplementadaSubsidiaria) REFERENCES dbo.TTecnologiaTSIimplementadaSubsidiaria(idTecnologiaTSIimplementadaSubsidiaria);

    ALTER TABLE dbo.TServicioTecnologia
        ADD CONSTRAINT FK_TServicioTecnologia_Empresa
        FOREIGN KEY (idEmpresaSubsidiaria) REFERENCES dbo.TEmpresaSubsidiaria(idEmpresaSubsidiaria);

    ALTER TABLE dbo.TServicioTecnologia
        ADD CONSTRAINT FK_TServicioTecnologia_Proceso
        FOREIGN KEY (idProcesoAdopcionTSI) REFERENCES dbo.TProcesoAdopcionTSI(idProcesoAdopcionTSI);

    ALTER TABLE dbo.TServicioTecnologia
        ADD CONSTRAINT FK_TServicioTecnologia_Vendor
        FOREIGN KEY (idVendor) REFERENCES dbo.TVendor(idVendor);
END;

-- 3. TTarifarioProyectoHoras (Implementación y Migración por Complejidad y Horas)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TTarifarioProyectoHoras' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TTarifarioProyectoHoras (
        idTarifarioProyecto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TTarifarioProyectoHoras PRIMARY KEY CLUSTERED,
        idServicio INT NOT NULL,
        complejidad NVARCHAR(50) NOT NULL,
        rangoHorasDesde INT NOT NULL,
        rangoHorasHasta INT NULL,
        tarifaHora DECIMAL(18,2) NOT NULL,
        horasEstimadas DECIMAL(10,2) NOT NULL CONSTRAINT DF_TTarifarioProyecto_Horas DEFAULT 0.00,
        subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_TTarifarioProyecto_Subtotal DEFAULT 0.00,
        moneda NVARCHAR(10) NOT NULL CONSTRAINT DF_TTarifarioProyecto_Moneda DEFAULT N'USD',
        observaciones NVARCHAR(500) NULL
    );

    ALTER TABLE dbo.TTarifarioProyectoHoras
        ADD CONSTRAINT FK_TTarifarioProyecto_Servicio
        FOREIGN KEY (idServicio) REFERENCES dbo.TServicioTecnologia(idServicio)
        ON DELETE CASCADE;
END;

-- 4. TActividadNivelSoporte (Catálogo Maestro de Actividades de Operación N1, N2 y N3)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TActividadNivelSoporte' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TActividadNivelSoporte (
        idActividadSoporte INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TActividadNivelSoporte PRIMARY KEY CLUSTERED,
        nivelSoporte NVARCHAR(10) NOT NULL,
        descripcionActividad NVARCHAR(300) NOT NULL,
        ordenVisual INT NOT NULL CONSTRAINT DF_TActividad_Orden DEFAULT 1,
        esActivo BIT NOT NULL CONSTRAINT DF_TActividad_Activo DEFAULT 1
    );

    -- Nivel 1 (8 actividades)
    INSERT INTO dbo.TActividadNivelSoporte (nivelSoporte, descripcionActividad, ordenVisual, esActivo) VALUES
    (N'N1', N'Aprovisionamiento de licencias', 1, 1),
    (N'N1', N'Monitoreo de disponibilidad y estabilidad de la plataforma', 2, 1),
    (N'N1', N'Gestión de requerimientos de la plataforma', 3, 1),
    (N'N1', N'Gestión de incidentes de la plataforma', 4, 1),
    (N'N1', N'Proveer información operativa para investigaciones, auditorías y controles de la plataforma', 5, 1),
    (N'N1', N'Registro, cierre y documentación de tickets y monitoreo del ciclo de vida.', 6, 1),
    (N'N1', N'Gestión y escalamientos con grupos resolutores, proveedores/fabricantes.', 7, 1),
    (N'N1', N'Hardenización de la plataforma', 8, 1);

    -- Nivel 2 (12 actividades)
    INSERT INTO dbo.TActividadNivelSoporte (nivelSoporte, descripcionActividad, ordenVisual, esActivo) VALUES
    (N'N2', N'Aprovisionamiento y configuración "en la plataforma"', 1, 1),
    (N'N2', N'Gestión de los módulos administrativos de la plataforma', 2, 1),
    (N'N2', N'Actualización de la plataforma', 3, 1),
    (N'N2', N'Capacitación de la plataforma', 4, 1),
    (N'N2', N'Habilitación e Implementación de ambientes', 5, 1),
    (N'N2', N'Participación en flujos de cambios', 6, 1),
    (N'N2', N'Participación en flujos de requerimientos, incidentes, problemas, war room, eventos de seguridad, etc.', 7, 1),
    (N'N2', N'Participación en Demos, pilotos, PoC de corta duración (junior)', 8, 1),
    (N'N2', N'Participación en eventos de gestión de continuidad', 9, 1),
    (N'N2', N'Gestión de usuarios en la plataforma', 10, 1),
    (N'N2', N'Evolución de los aspectos de seguridad de la plataforma', 11, 1),
    (N'N2', N'Configuración de políticas de backup, restore, custodia de información', 12, 1);

    -- Nivel 3 (5 actividades)
    INSERT INTO dbo.TActividadNivelSoporte (nivelSoporte, descripcionActividad, ordenVisual, esActivo) VALUES
    (N'N3', N'Mejora continua y recomendaciones sobre los aspectos de configuración definidos en la plataforma (tunning)', 1, 1),
    (N'N3', N'Habilitación e implementación de nuevas capacidades de la plataforma', 2, 1),
    (N'N3', N'Assesment continuo', 3, 1),
    (N'N3', N'Participación en Demos, pilotos, PoC de complejidad alta o larga duración (senior)', 4, 1),
    (N'N3', N'Pedidos adicionales fuera de alcance con referencia a la plataforma y operación de la plataforma', 5, 1);
END;

-- 5. TTarifarioOperacion (Matriz de Operación: Nivel, Modalidad, Horas, Seniority, Locación)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TTarifarioOperacion' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.TTarifarioOperacion (
        idTarifarioOperacion INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TTarifarioOperacion PRIMARY KEY CLUSTERED,
        idServicio INT NOT NULL,
        nivelSoporte NVARCHAR(10) NOT NULL,
        modalidad NVARCHAR(50) NOT NULL,
        detalleModalidad NVARCHAR(250) NULL,
        horasBaseMensual INT NOT NULL CONSTRAINT DF_TTarifarioOperacion_HorasBase DEFAULT 0,
        expertise NVARCHAR(20) NOT NULL,
        locacion NVARCHAR(20) NOT NULL,
        tarifaHora DECIMAL(18,2) NULL,
        tarifaMensual DECIMAL(18,2) NULL,
        cantidadMeses INT NOT NULL CONSTRAINT DF_TTarifarioOperacion_Meses DEFAULT 1,
        horasEstimadas DECIMAL(10,2) NULL,
        subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_TTarifarioOperacion_Subtotal DEFAULT 0.00,
        moneda NVARCHAR(10) NOT NULL CONSTRAINT DF_TTarifarioOperacion_Moneda DEFAULT N'USD'
    );

    ALTER TABLE dbo.TTarifarioOperacion
        ADD CONSTRAINT FK_TTarifarioOperacion_Servicio
        FOREIGN KEY (idServicio) REFERENCES dbo.TServicioTecnologia(idServicio)
        ON DELETE CASCADE;
END;

COMMIT TRANSACTION;
