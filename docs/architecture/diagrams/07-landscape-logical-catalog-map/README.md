# Diagrama lógico interactivo del catálogo Landscape TSI

Esta carpeta contiene la fuente Archify y el artefacto navegable del modelo
lógico que alimenta la pantalla `Mapa del Catálogo Landscape TSI`.

- Fuente Archify: `landscape-logical-catalog-map.architecture.json`.
- Artefacto generado: `landscape-logical-catalog-map.html`.
- Fuente Web única: `MasterCatalogRegistry.EntityMetadata` y
  `MasterCatalogRegistry.LogicalRelationships`.
- La interfaz Web utiliza Cytoscape.js 3.31.2 para el grafo. Chart.js continúa
  reservado para reportería y dashboards estadísticos.

## Convenciones

Los nombres funcionales se muestran en la vista lógica y los nombres físicos
SQL en la vista física. La insignia `M` significa tabla maestra y `T` entidad
transaccional. Las relaciones y sus cardinalidades se basan en las FK
verificadas del modelo SQL; no se crean relaciones por similitud de nombres.

La tabla puente `TBuildingBlockVsTTecnologiaTSI` se representa como entidad
explícita cuando el control **Tablas puente** está activo. Cuando se oculta,
la interfaz muestra la relación conceptual `Building Block N:M Tecnología TSI`.
La tabla continúa sin PK/UNIQUE: `mapear-relacion-buildingblock-tecnologia`
es una deuda técnica independiente y no forma parte de este cambio.

## Entidades administrables

La agrupación funcional única de `CatalogEntityMetadata.Group` contiene 10
entidades en Arquitectura de seguridad, 2 en Tecnología, 2 en Organización y
2 en Operación. Las relaciones pueden cruzar grupos sin alterar esta
clasificación.

| Tabla física | Nombre lógico | M/T | Grupo | Ruta |
|---|---|:---:|---|---|
| TMDominio | Dominio | M | Arquitectura de seguridad | `/Administration/MasterTables/Domain` |
| TBuildingBlock | Building Block | T | Arquitectura de seguridad | `/Administration/MasterTables/Catalog?catalogRoute=building-block` |
| TCapacidadDeSeguridad | Capacidad de Seguridad | T | Arquitectura de seguridad | `/Administration/MasterTables/Catalog?catalogRoute=capacidad-seguridad` |
| TMEstadoCapacidad | Estado de Capacidad | M | Arquitectura de seguridad | `/Administration/MasterTables/Catalog?catalogRoute=estado-capacidad` |
| TFuncionalidad | Funcionalidad | T | Arquitectura de seguridad | `/Administration/MasterTables/Catalog?catalogRoute=funcionalidad` |
| TMEstadoFuncionalidad | Estado de Funcionalidad | M | Arquitectura de seguridad | `/Administration/MasterTables/Catalog?catalogRoute=estado-funcionalidad` |
| TEstadoFaseAdopcion | Fase de Adopción | T | Arquitectura de seguridad | `/Administration/MasterTables/Catalog?catalogRoute=fase-adopcion` |
| TTecnologiaTSI | Tecnología TSI | T | Tecnología | `/Administration/MasterTables/Catalog?catalogRoute=tecnologia-tsi` |
| TCasosDeUso | Casos de Uso | T | Tecnología | `/Administration/MasterTables/Catalog?catalogRoute=casos-uso` |
| TEmpresaSubsidiaria | Empresa / Subsidiaria | T | Organización | `/Administration/MasterTables/Catalog?catalogRoute=empresa-subsidiaria` |
| TCISO | CISO | T | Organización | `/Administration/MasterTables/Catalog?catalogRoute=ciso` |
| TMFamilia | Familia | M | Arquitectura de seguridad | `/Administration/MasterTables/Catalog?catalogRoute=familia` |
| TMPosturaRoadmap | Postura Roadmap | M | Arquitectura de seguridad | `/Administration/MasterTables/Catalog?catalogRoute=postura-roadmap` |
| TMEstadoAdopcionTSI | Estado de Adopción TSI | M | Arquitectura de seguridad | `/Administration/MasterTables/Catalog?catalogRoute=estado-adopcion-tsi` |
| TModalidadLaboral | Modalidad Laboral | M | Operación | `/Administration/MasterTables/Catalog?catalogRoute=modalidad-laboral` |
| TTipoOperacion | Tipo de Operación | M | Operación | `/Administration/MasterTables/Catalog?catalogRoute=tipo-operacion` |

La metadata también conserva entidades relacionadas no administrables desde el
mapa: `TTecnologiaTSIimplementadaSubsidiaria`, `TVendor`,
`TRegulacionAplicable`, `TContactoEmpresaSubsidiaria`, `TContactoVendor`,
`TContactoPartner`, `TDriver`, `TModeloDeOperacion` y la tabla puente.

## Excepciones de clasificación M/T

- `TTipoOperacion` es catálogo maestro explícito aunque no empiece por `TM`.
- `TModalidadLaboral` se clasifica explícitamente como maestra porque contiene
  el catálogo controlado de modalidades laborales usado por el modelo de
  operación; no depende de una inferencia por prefijo.
- `TModeloDeOperacion` se clasifica explícitamente como transaccional porque
  representa el contexto operativo relacionado con una implementación, no un
  conjunto pequeño de valores maestros; tampoco depende de una inferencia por
  prefijo.

## Relaciones verificadas

| Origen | Cardinalidad | Destino | FK / puente |
|---|---:|---|---|
| Dominio | 1:N | Building Block | `FK_TBuildingBlock_TDominio` |
| Fase de Adopción | 1:N | Building Block | `FK_TBuildingBlock_TEstadoFaseAdopcion` |
| Building Block | 1:N | Capacidad de Seguridad | `FK_TCapacidadDeSeguridad_TBuildingBlock` |
| Estado de Capacidad | 1:N | Capacidad de Seguridad | `FK_TCapacidadDeSeguridad_TMEstadoCapacidad` |
| Capacidad de Seguridad | 1:N | Funcionalidad | `FK_TFuncionalidad_TCapacidadDeSeguridad` |
| Estado de Funcionalidad | 1:N | Funcionalidad | `FK_TFuncionalidad_TMEstadoFuncionalidad` |
| Familia | 1:N | Tecnología TSI | `FK_TTecnologiaTSI_TMFamilia` |
| Estado de Adopción TSI | 1:N | Tecnología TSI | `FK_TTecnologiaTSI_TMEstadoAdopcionTSI` |
| Postura Roadmap | 1:N | Tecnología TSI | `FK_TTecnologiaTSI_TMPosturaRoadmap` |
| Tecnología TSI | 1:N | Casos de Uso | `FK_TCasosDeUso_TTecnologiaTSI` |
| Empresa / Subsidiaria | 1:N | CISO | `FK_TCISO_TEmpresaSubsidiaria` |
| Building Block | N:M | Tecnología TSI | `TBuildingBlockVsTTecnologiaTSI` |
| Empresa / Subsidiaria | 1:N | Contacto / Regulación / Tecnología implementada | FK reales |
| Tecnología TSI | 1:N | Vendor / Tecnología implementada | FK reales |
| Tecnología implementada | 1:N | Driver / Modelo de Operación | FK reales |
| Modalidad Laboral / Tipo de Operación | 1:N | Modelo de Operación | FK reales |

Las relaciones Web se serializan desde esta metadata al cliente. La
autorización de las rutas se mantiene en el servidor; el grafo no concede
permisos.

