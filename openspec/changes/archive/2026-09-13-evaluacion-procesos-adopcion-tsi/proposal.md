## Why

Las organizaciones del Grupo Credicorp requieren definir estándares tecnológicos corporativos para cada Building Block de seguridad, pero cada empresa subsidiaria opera actualmente con contratos, tecnologías y proveedores locales vigentes (AS-IS). Para evitar la dispersión tecnológica y sobrecostos por rescisiones tempranas, se requiere un módulo especializado de **Evaluaciones** dentro de los **Procesos de Adopción TSI** que permita convocar masivamente a las empresas mediante selección con checkboxes, registrar su diagnóstico actual (contratos, fechas de fin, drivers 1:N, modelo operativo, vendor y partner) y fijar el estándar corporativo con su cronograma de convergencia progresiva.

## What Changes

- **Página de Inicio & Navegación:** Botón directo "Procesos de Adopción TSI" en el Home a la misma altura de "Explorar vista de entidades", y menú superior con dropdown que incluye "Exploraciones", "Evaluaciones" (activo) e "Implementaciones".
- **Gestión de Evaluaciones:** Bandeja administrativa para registrar nuevas solicitudes, actualizar evaluaciones en curso y dar de baja evaluaciones canceladas con registro inmutable en auditoría.
- **Definición de Evaluación vinculada a Tablas Maestras:** Selección de Dominio (`TMDominio`) y Building Block (`TBuildingBlock`), con visualización reactiva de las Capacidades (`TCapacidadDeSeguridad`) y Funcionalidades (`TFuncionalidad`) asociadas sin duplicar tablas.
- **Selección Múltiple de Subsidiarias (Checkboxes):** Interfaz para marcar múltiples empresas de `TEmpresaSubsidiaria` simultáneamente y registrar en lote en `TProcesoAdopcionEmpresa` con su contacto técnico focal (`TContactoEmpresaSubsidiaria`).
- **Diagnóstico AS-IS por Subsidiaria:** Registro de tecnología implementada actual (`TTecnologiaTSIimplementadaSubsidiaria`), contrato vigente con fecha de vencimiento (`TContratoTecnologia`), drivers múltiples de dimensionamiento (`TDriver`) y modelo de operación (`TModeloDeOperacion`).
- **Proveedores Unificados:** Reutilización del modelo de Vendor, Partner y contactos para soportar tanto la tecnología corporativa como las tecnologías locales implementadas en subsidiarias.
- **Alineación a Catálogos Maestros de Estados:** Fase del proceso gobernada por `dbo.TEstadoFaseAdopcion` (Fase 2: `EVALUACION`) y estado tecnológico por `dbo.TMEstadoAdopcionTSI`.

## Capabilities

### Modified Capabilities
- `adoption/process-management`: Se expande la especificación para incorporar el flujo integral de evaluación con selección múltiple con checkboxes, visualización de capacidades de Building Block, diagnóstico AS-IS por subsidiaria (contrato, drivers, modelo de operación, vendor/partner) y regla de convergencia por vencimiento contractual.

## Impact

- Capa Web: Controlador `AdoptionProcessController`, vistas Razor `Evaluations.cshtml`, `CreateEvaluation.cshtml`, `EditEvaluation.cshtml`, navegación en `_Layout.cshtml` y botón en `Home/Index.cshtml`.
- Capa de Dominio y Aplicación: Comandos y contratos en `IAdoptionProcessService` para creación multi-paso, convocatoria masiva, diagnóstico AS-IS y baja auditada.
- Capa de Infraestructura: `CatalogDbContext` y `AdoptionProcessService` conectando las entidades transaccionales y maestras.
- Base de datos: Reutilización estricta de las tablas físicas existentes en `db-landscape-tsi-dev-v2` (`TProcesoAdopcionTSI`, `TProcesoAdopcionEmpresa`, `TTecnologiaTSIimplementadaSubsidiaria`, `TContratoTecnologia`, `TDriver`, `TModeloDeOperacion`, etc.) sin introducir cambios DDL disruptivos.
