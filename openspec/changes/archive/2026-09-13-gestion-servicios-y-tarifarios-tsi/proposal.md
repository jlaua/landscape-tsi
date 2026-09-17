## Why

Actualmente, Landscape TSI no cuenta con un modelo ni tablas para registrar y dimensionar **Servicios** asociados a las tecnologías de ciberseguridad. En los procesos de adopción tecnológica y en la operación de las subsidiarias, el costo no solo radica en licenciamiento y hardware (TContratoTecnologia, TDriver), sino fundamentalmente en los **servicios de mano de obra** para:
1. **Implementación** de nuevas soluciones corporativas o locales.
2. **Migración** de reglas, políticas e identidades desde tecnologías AS-IS.
3. **Operación continua** y soporte en niveles N1, N2 y N3.

Esta capacidad permite a cada empresa definir sus propios servicios para sus tecnologías implementadas y, a nivel corporativo, proyectar y presupuestar los servicios evaluados o adjudicados dentro de los procesos de adopción TSI mediante tarifarios estructurados por horas y modalidades de operación.

## What Changes

- **Nuevo Catálogo Maestro TTipoServicio**: Identifica los tipos estándar: IMPLEMENTACION, MIGRACION y OPERACION.
- **Nueva Entidad Central TServicioTecnologia**: Admite asociar servicios tanto a nivel de tecnología de subsidiaria (TTecnologiaTSIimplementadaSubsidiaria / TEmpresaSubsidiaria) como corporativo (TTecnologiaTSI / TProcesoAdopcionTSI), vinculando proveedor (TVendor), estado y consolidación de costos.
- **Nuevo Tarifario de Proyectos por Horas TTarifarioProyectoHoras**: Estructura el tarifario de **Implementación** y **Migración** según las 4 complejidades de negocio: Baja (Por Hora), Proyecto menor (1-100 hrs), Proyecto intermedio (101-200 hrs) y Proyecto mayor (>201 hrs).
- **Nuevo Catálogo Maestro de Alcance Operativo TActividadNivelSoporte**: Modela las actividades estandarizadas de **Nivel 1** (8 actividades), **Nivel 2** (12 actividades) y **Nivel 3** (5 actividades).
- **Nuevo Tarifario de Operación TTarifarioOperacion**: Modela la matriz de servicios operativos cruzando Nivel de Soporte (N1, N2, N3), Modalidad (Pay-Per-Use, Mensual 8x5 176h, Mensual 24x7 720h), Expertise (Junior, Senior) y Locación (Remoto, Presencial).
- **Integración Transaccional y UI**: Nuevas vistas, endpoints y componentes interactivos para gestionar servicios y tarifarios desde las evaluaciones de adopción y fichas de tecnología.

## Capabilities

### New Capabilities
- doption/service-management: Gobierna la creación, configuración, cálculo de costos y auditoría de Servicios y sus Tarifarios asociados (proyectos por rangos de horas y contratos de operación por niveles de soporte).

### Modified Capabilities
<!-- No modified existing requirements; purely additive new capability -->

## Impact

- **Base de datos**: Nuevas tablas dbo.TTipoServicio, dbo.TServicioTecnologia, dbo.TTarifarioProyectoHoras, dbo.TActividadNivelSoporte, dbo.TTarifarioOperacion e índices/claves foráneas en scripts/database/evolve-service-schema.sql.
- **Capa Domain**: Nuevas entidades TTipoServicio, TServicioTecnologia, TTarifarioProyectoHoras, TActividadNivelSoporte, TTarifarioOperacion.
- **Capa Infrastructure**: Mapeo en EF Core y repositorio/servicio de datos para cálculo y persistencia de tarifarios.
- **Capa Application**: Contratos DTO, validaciones y servicio de gestión de servicios y tarifarios (IServiceManagementService).
- **Capa Web**: Controladores y vistas Razor/modales para cotizar y auditar servicios y tarifarios en Procesos de Adopción y Subsidiarias.
- **Pruebas**: Pruebas de integración de esquema DDL, pruebas unitarias de cálculo de tarifas y validación de reglas de negocio.
