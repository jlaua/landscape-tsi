## Why

La reportería actual resume cantidades por catálogo, pero todavía no ofrece una exploración progresiva de registros, relaciones y métricas contextuales. Se necesita convertirla en un `Landscape TSI Explorer` para que los usuarios autorizados puedan recorrer el catálogo mediante drill-down sin recargas completas, comenzando por un vertical slice seguro y verificable de `Dominio`.

## What Changes

- Evolucionar `/reporteria` hacia un dashboard progresivo con filtros, KPIs generales y selección interactiva del gráfico principal.
- Sustituir visualmente la tabla resumen redundante por un estado inicial guiado y una grilla contextual al seleccionar `Dominio`.
- Implementar únicamente el vertical slice `Dominio -> Building Block` en esta iteración: detalle, búsqueda, ordenamiento, paginación, selección, breadcrumb, KPI directo y grilla de Building Blocks relacionados.
- Mostrar la relación `Building Block -> Capacidad de Seguridad -> Funcionalidad` y la rama tecnológica solo como fases posteriores; no presentar `Funcionalidad -> Tecnología TSI` como relación directa.
- Validar relaciones y la tabla puente `TBuildingBlockVsTTecnologiaTSI` mediante metadatos de SQL Server antes de habilitar KPIs o drill-down tecnológicos.
- Mantener una lista blanca de códigos funcionales y relaciones; nunca aceptar nombres arbitrarios de tablas, columnas o relaciones desde HTTP.
- Mantener Chart.js como librería estándar, con actualización, destrucción controlada de instancias, selección, tooltips y estados de carga, vacío y error.
- Mantener la reportería autenticada, autorizada por `CatalogView`, exclusivamente de lectura y sin cambios de esquema o datos.

## Capabilities

### New Capabilities

- `reporting/landscape-explorer`: Dashboard de reportería interactivo con drill-down controlado, KPIs contextuales y navegación por relaciones reales del catálogo.

### Modified Capabilities

- Ninguna. No existen especificaciones principales previas de reportería en este repositorio.

## Impact

- Aplicación: contratos y servicios de lectura de `Application/Reporting`, acceso controlado a datos de catálogo y DTOs.
- Web: `ReportingController`, vista `/reporteria`, componentes Chart.js, grillas, estado de selección y breadcrumb analítico.
- Datos: consultas de metadatos de solo lectura contra `db-landscape-tsi-dev`; no se requieren migraciones, DDL ni DML.
- Seguridad: se conserva `Permissions.CatalogView`, se validan códigos mediante whitelist y se respeta el alcance organizacional cuando aplique.
- Pruebas: cobertura de carga inicial, selección, detalle, filtros, paginación, ordenamiento permitido, autorización, registros inexistentes y relaciones con/sin hijos.
