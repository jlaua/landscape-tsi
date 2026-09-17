## Why

Landscape TSI ya dispone de una relación N:M entre `TBuildingBlock` y `TTecnologiaTSI`, pero no existe una experiencia administrativa dedicada para consultar, crear y desasociar esas relaciones. La tabla puente tampoco tiene actualmente una restricción de unicidad documentada, por lo que la aplicación necesita prevenir duplicados mientras se valida una protección definitiva en SQL Server.

## What Changes

- Crear una página administrativa de Mapeo de Tecnologías TSI con búsqueda, filtros, KPIs y paginación server-side.
- Permitir administrar relaciones N:M desde una Tecnología TSI y desde el detalle de Building Block.
- Mostrar estado Mapeada/Sin mapear derivado exclusivamente de `TBuildingBlockVsTTecnologiaTSI`.
- Permitir asociar varias relaciones en una operación y desasociar únicamente filas de la tabla puente.
- Reutilizar metadata, autorización, auditoría y rutas existentes; no crear CRUD genéricos ni URLs concatenadas.
- Ejecutar una prevalidación de solo lectura sobre FK, índices, unicidad y duplicados antes de proponer DDL.
- Proponer, sin aplicar automáticamente, una PK compuesta o índice UNIQUE sobre `(idBuildingBlock, idTecnologiaTSI)`.
- Manejar concurrencia y `DbUpdateException` sin eliminar Building Blocks ni Tecnologías.

## Capabilities

### New Capabilities

- `catalog/building-block-technology-mapping`: Administración segura y bidireccional de la relación N:M entre Building Blocks y Tecnologías TSI.

### Modified Capabilities

- Ninguna. La navegación existente se reutilizará; cualquier ajuste de rutas se validará como parte de la nueva capacidad.

## Impact

- Aplicación MVC/Razor: nueva página, filtros, selección múltiple, confirmación de desasociación y navegación bidireccional.
- Application/Infrastructure: consultas proyectadas y paginadas, servicio de relaciones, whitelist, autorización, auditoría y manejo de concurrencia.
- Metadata: extensión controlada de `CatalogEntityMetadata`/`CatalogRelationshipMetadata`.
- Base de datos: inicialmente solo inspección. Cualquier PK/UNIQUE o migration será una decisión posterior e independiente.
- Pruebas: routing, permisos, duplicados, paginación, asociación/desasociación y ausencia de efectos sobre las entidades principales.
