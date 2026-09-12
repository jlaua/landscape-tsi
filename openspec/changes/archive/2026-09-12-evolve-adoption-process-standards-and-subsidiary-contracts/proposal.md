# Propuesta: Evolución del Proceso de Adopción TSI, Estándares Corporativos Históricos y Contratos de Subsidiarias

## Why

Actualmente, Landscape TSI mantiene desacopladas las tecnologías estándar corporativas (asociadas a nivel macro mediante `TBuildingBlockVsTTecnologiaTSI`) del inventario local de tecnologías de las empresas subsidiarias (`TTecnologiaTSIimplementadaSubsidiaria`), el cual carece de referencia al Building Block. Además, no existe una entidad formal que gobierne la iniciativa o ciclo de vida de adopción corporativa, ni un soporte relacional para contratos y adendas (1:N), histórico de estándares cuando se cambia de proveedor o dimensionamiento libre de drivers de costeo.

Esta evolución introduce la entidad gobernante `TProcesoAdopcionTSI` (cuyo ciclo de vida aprovecha `TMEstadoAdopcionTSI`), establece el registro histórico de tecnologías estándar corporativas (`TEstandarTecnologiaHistorico`), y enriquece de forma no destructiva la realidad de las subsidiarias permitiendo registrar contactos focales (`TContactoEmpresaSubsidiaria`), contratos con adendas (`TContratoTecnologia`), drivers con cantidad y precio unitario (`TDriver`) y modelos de operación (`TModeloDeOperacion`).

## What Changes

- **Evolución y Agnosticismo Arquitectónico:** Se reafirma que la jerarquía `Dominio -> Building Block -> Capacidad de Seguridad -> Funcionalidad` permanece agnóstica a empresas y marcas. Se vincula `TBuildingBlock` a `TMFamilia` para soportar la taxonomía de familias de seguridad.
- **Entidad de Gobernanza `TProcesoAdopcionTSI`:** Se crea la entidad que define qué Building Block se estandariza corporativamente, gestionando su evolución mediante los estados de `dbo.TMEstadoAdopcionTSI`.
- **Trazabilidad Histórica de Estándares Corporativos:** Se crea `TEstandarTecnologiaHistorico`, permitiendo que al sustituir una tecnología estándar corporativa, el registro saliente pase a estado histórico (`HISTORICO_REEMPLAZADO`) con fecha de término y justificación, mientras el nuevo se registra como vigente (`ACTIVO_VIGENTE`).
- **Casos de Uso Corporativos (`TCasosDeUso`):** Vinculación formal de los casos de uso a los estándares corporativos que debe soportar la solución tecnológica.
- **Convocatoria de Subsidiarias y Focales:** En el marco de un proceso de adopción, se registran las empresas subsidiarias participantes (`TProcesoAdopcionEmpresa`) junto al especialista técnico que acompaña el proceso (`TContactoEmpresaSubsidiaria`) y la justificación de excepciones (`No Aplica`).
- **Contextualización de Tecnologías Implementadas:** `TTecnologiaTSIimplementadaSubsidiaria` se enriquece con claves foráneas hacia el proceso de adopción y el Building Block, preservando intactas las claves hacia `TModeloDeOperacion` y `TDriver`.
- **Soporte de Contratos y Adendas (1:N):** Nueva entidad `TContratoTecnologia` asociada a la tecnología implementada en la subsidiaria, con soporte jerárquico de contrato original y adendas hijas, fechas, rutas de repositorio documental y montos.
- **Drivers Operativos con Dimensionamiento y Precio Unitario:** Enriquecimiento de `TDriver` con columnas para unidad de medida, cantidad volumétrica, precio unitario y moneda, permitiendo registrar múltiples drivers por tecnología implementada.
- **Preservación de Modelos de Operación:** Se mantiene la integridad relacional de `TModeloDeOperacion` con `TTipoOperacion` y `TModalidadLaboral`.
- **Compatibilidad Retrospectiva:** La tabla puente `TBuildingBlockVsTTecnologiaTSI` y los servicios preexistentes de catálogo y mapeo (`BuildingBlockTechnologyMappingService`) se mantienen operativos y sincronizados con los estándares vigentes.

## Capabilities

### New Capabilities
- `adoption/process-management`: Apertura, gobernanza y seguimiento del ciclo de vida de los procesos de adopción TSI (`TProcesoAdopcionTSI`) para Building Blocks, convocatoria de empresas subsidiarias y asignación de contactos focales (`TContactoEmpresaSubsidiaria`).
- `adoption/corporate-standards`: Gestión y auditoría histórica de tecnologías estándar corporativas principales y alternativas homologadas (`TEstandarTecnologiaHistorico`), vinculadas a casos de uso (`TCasosDeUso`), vendors y partners.
- `adoption/subsidiary-contracts-and-operation`: Registro contextualizado de tecnologías implementadas por subsidiarias, administración de contratos y adendas (1:N), dimensionamiento con drivers (cantidad, precio) y modelos operativos (`TModeloDeOperacion`).

### Modified Capabilities

## Impact

- **Base de Datos SQL Server (`db-landscape-tsi-dev-v2`):**
  - Nuevas tablas: `TProcesoAdopcionTSI`, `TProcesoAdopcionEmpresa`, `TEstandarTecnologiaHistorico`, `TContratoTecnologia`.
  - Columnas agregadas (nullable / default): `TBuildingBlock.idFamilia`, `TTecnologiaTSIimplementadaSubsidiaria.idBuildingBlock`, `TTecnologiaTSIimplementadaSubsidiaria.idProcesoAdopcionEmpresa`, `TDriver.unidadMedida`, `TDriver.cantidad`, `TDriver.precioUnitario`, `TDriver.moneda`.
  - Cero eliminación o alteración destructiva de las tablas o registros existentes.
- **Dominio y Persistencia (`Landscape.Tsi.Domain` y `Landscape.Tsi.Infrastructure`):**
  - Modelos de entidad y configuraciones en `CatalogDbContext` para las nuevas entidades y propiedades extendidas.
- **Aplicación y Web (`Landscape.Tsi.Application` y `Landscape.Tsi.Web`):**
  - Casos de uso y servicios para procesos de adopción, histórico de estándares y contratos de subsidiarias.
  - Vistas y controladores MVC enriquecidos (pestaña en detalle de Building Block, modales de contratos, matriz de drivers).
