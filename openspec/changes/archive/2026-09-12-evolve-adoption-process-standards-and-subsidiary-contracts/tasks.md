# Tareas: Evolución del Proceso de Adopción TSI, Estándares Corporativos Históricos y Contratos de Subsidiarias

## 1. Modelo de Datos y Scripts SQL Gobernados (DEV-v2)

- [x] 1.1 Diseñar script DDL aditivo en `scripts/database/evolve-adoption-process-schema.sql` para crear `TProcesoAdopcionTSI`, `TProcesoAdopcionEmpresa`, `TEstandarTecnologiaHistorico` y `TContratoTecnologia`, verificando validación `SELECT DB_NAME() = 'db-landscape-tsi-dev-v2'`.
- [x] 1.2 Extender script DDL para incorporar columnas nullables en `TBuildingBlock` (`idFamilia`), `TTecnologiaTSIimplementadaSubsidiaria` (`idBuildingBlock`, `idProcesoAdopcionEmpresa`, `esTecnologiaPrimaria`), `TDriver` (`unidadMedida`, `cantidad`, `precioUnitario`, `moneda`) y `TCasosDeUso` (`idEstandarTecnologia`), verificando integridad referencial en DEV-v2.
- [x] 1.3 Ejecutar y certificar script DDL aditivo en `db-landscape-tsi-dev-v2` utilizando pruebas de integración con identificadores `ITEST_*` y verificar que las 26 tablas existentes y sus datos se mantengan íntegros.

## 2. Entidades de Dominio y Mapeo en Infraestructura (EF Core)

- [x] 2.1 Crear clases de entidad en `Landscape.Tsi.Domain/Adoption/` (`TProcesoAdopcionTSI`, `TProcesoAdopcionEmpresa`, `TEstandarTecnologiaHistorico`, `TContratoTecnologia`) y enriquecer `TBuildingBlock`, `TTecnologiaTSIimplementadaSubsidiaria` y `TDriver`.
- [x] 2.2 Configurar `CatalogDbContext` y mapeos Fluent API en `Landscape.Tsi.Infrastructure` para las nuevas entidades y relaciones foráneas sin alterar las exclusiones de migración de catálogos maestros.
- [x] 2.3 Implementar pruebas unitarias de mapeo EF Core en `Landscape.Tsi.Tests` verificando nombres físicos de tabla, columnas, nullabilidad y relaciones foráneas.

## 3. Lógica de Dominio y Servicios de Aplicación

- [x] 3.1 Implementar `IAdoptionProcessService` y `AdoptionProcessService` en `Landscape.Tsi.Application` con operaciones para apertura de proceso de adopción, convocatoria de empresas y asignación de contactos focales (`TContactoEmpresaSubsidiaria`).
- [x] 3.2 Implementar gestión de estándares corporativos con histórico inmutable (`TEstandarTecnologiaHistorico`), garantizando que al asignar un nuevo estándar principal el anterior pase atómicamente a `HISTORICO_REEMPLAZADO` con fecha de término.
- [x] 3.3 Implementar gestión de contratos y adendas jerárquicas (1:N) en `TContratoTecnologia`, validando en servidor que las adendas requieran un contrato principal válido perteneciente a la misma tecnología implementada.
- [x] 3.4 Implementar soporte de múltiples drivers en `TDriver` con cálculo de costo proyectado (`cantidad * precioUnitario`) y vinculación de `TModeloDeOperacion` con `TTipoOperacion` y `TModalidadLaboral`.
- [x] 3.5 Implementar sincronización automática de compatibilidad hacia la tabla puente `TBuildingBlockVsTTecnologiaTSI` para mantener operativos los controladores preexistentes.
- [x] 3.6 Implementar pruebas unitarias en `Landscape.Tsi.Tests` para validar invariantes de transición histórica, validación de adendas, cálculo de drivers y autorización por roles.

## 4. Capa Web MVC y Experiencia de Usuario

- [x] 4.1 Implementar `AdoptionProcessController` y vistas para administración del ciclo de vida de los procesos de adopción y convocatoria de empresas subsidiarias con focales.
- [x] 4.2 Enriquecer la vista de detalle de Building Block (`/Administration/MasterTables/building-block/details/{id}`) incorporando pestaña de Adopción TSI, visualización de estándares corporativos vigentes vs históricos y relación con familia.
- [x] 4.3 Implementar interfaz y modales para registro de tecnologías implementadas por subsidiarias, incluyendo gestión de contratos/adendas y matriz de drivers con cálculo dinámico.
- [x] 4.4 Implementar pruebas funcionales e HTTP en `Landscape.Tsi.Tests/Web` verificando renderizado de vistas, protección antiforgery y autorización por rol/ámbito.

## 5. Verificación Integral y Documentación

- [x] 5.1 Ejecutar suite completa de pruebas `dotnet test --configuration Release` verificando que todas las pruebas pasen sin regresiones.
- [x] 5.2 Actualizar diagramas lógicos y documentación de arquitectura en `docs/` reflejando el modelo ampliado de adopción, estándares, contratos y drivers.
