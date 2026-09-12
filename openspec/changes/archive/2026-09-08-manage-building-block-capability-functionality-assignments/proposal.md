## Why

La jerarquía fundamental del catálogo de Landscape TSI (`Dominio -> Building Block -> Capacidad de Seguridad -> Funcionalidad`) carece actualmente de mecanismos administrativos interactivos para asociar entidades huérfanas y reasignar de forma gobernada Capacidades y Funcionalidades entre sus entidades padre. Adicionalmente, la tabla de funcionalidades en el detalle de Building Block no contextualiza a qué Capacidad pertenece cada funcionalidad ni ofrece un ordenamiento jerárquico coherente, lo cual dificulta la navegación operativa y el gobierno del catálogo.

## What Changes

- **Visualización contextual de Funcionalidades en detalle de Building Block**:
  - Reestructuración de la tabla de funcionalidades para presentar obligatoriamente las columnas: `Capacidad` (nombre funcional), `Funcionalidad` (nombre funcional), `Estado de funcionalidad` (etiqueta de cobertura) y `Acciones`.
  - Ordenamiento predeterminado compuesto: `Capacidad ASC, Funcionalidad ASC`.
  - Soporte de ordenamiento interactivo server-side y en cliente mediante lista blanca (`capacity`, `functionality`, `status`) con atributos de accesibilidad `aria-sort`.

- **Asociación y reasignación gobernada de Capacidad a Building Block**:
  - Capacidad de asociar Capacidades de Seguridad huérfanas (`TCapacidadDeSeguridad.idBuildingBlock = NULL`) a un Building Block destino.
  - Flujo de reasignación entre Building Blocks con cálculo previo de impacto: número de Funcionalidades hijas que cambiarán indirectamente de contexto y Tecnologías/Casos de uso vinculados.
  - Diálogo de confirmación explícita antes de persistir mutaciones en `dbo.TCapacidadDeSeguridad.idBuildingBlock`.

- **Asociación y reasignación gobernada de Funcionalidad a Capacidad**:
  - Capacidad de asociar Funcionalidades huérfanas (`TFuncionalidad.idCapacidad = NULL`) a una Capacidad destino.
  - Flujo de reasignación entre Capacidades (dentro del mismo Building Block o hacia otro Building Block) con advertencia de impacto: Capacidad origen y destino, y Building Block origen y destino.
  - Diálogo de confirmación explícita antes de persistir mutaciones en `dbo.TFuncionalidad.idCapacidad`.

- **Garantías de integridad, concurrencia y auditoría fail-closed**:
  - Verificación de concurrencia optimista mediante token de estado SHA-256 para prevenir sobreescrituras concurrentes.
  - Registro de auditoría transaccional inmutable (`audit.Operation` / log estructurado) documentando entidad modificada, valor anterior (`idBuildingBlock` o `idCapacidad`), valor nuevo, justificación, usuario autenticado y timestamp UTC. Si el registro de auditoría falla, la transacción revierte por completo (fail-closed).
  - Conservación estricta de la integridad referencial física existente en SQL Server (`dbo.TCapacidadDeSeguridad` y `dbo.TFuncionalidad`), sin requerir DDL, tablas intermedias ni migraciones de esquema (`DDL_REQUIRED=NO`, `MIGRATION_REQUIRED=NO`).

- **Exclusión de Identity/OIDC**:
  - Ningún flujo de este cambio altera ni desbloquea tareas de Identity/OIDC, las cuales permanecen estrictamente `DEFERRED (36/56)`.

## Capabilities

### New Capabilities
- `catalog/capability-functionality-assignment`: Gobierna la asociación de entidades huérfanas, reasignación entre padres, cálculo preventivo de impacto en cascada, validación de concurrencia optimista y auditoría transaccional fail-closed para las relaciones 1:N entre Building Block, Capacidad de Seguridad y Funcionalidad.

### Modified Capabilities
- `catalogos-maestros/experiencia-usuario`: Actualiza la experiencia de la vista de detalle de Building Block para incluir la tabla reorganizada de Funcionalidades con columnas `Capacidad | Funcionalidad | Estado de funcionalidad | Acciones`, ordenamiento compuesto predeterminado, ordenamiento accesible server-side y diálogos modales de asociación y reasignación con advertencia de impacto y confirmación explícita.

## Impact

- **Código y servicios afectados**:
  - `src/Landscape.Tsi.Application/`: Nuevos contratos y modelos (`IAssociationImpactService`, `IAssignmentService`, DTOs de impacto y comandos de reasignación con token de concurrencia).
  - `src/Landscape.Tsi.Infrastructure/`: Implementación de los servicios de asignación y cálculo de impacto sobre `CatalogDbContext`, asegurando transaccionalidad con `audit.Operation`.
  - `src/Landscape.Tsi.Web/`: Controladores MVC (`BuildingBlockController`, `CapacidadDeSeguridadController`, `FuncionalidadController`), ViewModels de detalle, ordenamiento y modales interactivos compatibles con Material Design 3 y WCAG 2.2 AA.
- **Base de datos**:
  - Sin cambios de esquema (`DDL_REQUIRED=NO`, `MIGRATION_REQUIRED=NO`). Utiliza las claves foráneas existentes `TCapacidadDeSeguridad.idBuildingBlock` y `TFuncionalidad.idCapacidad`.
- **Dependencias**:
  - Ninguna dependencia nueva requerida.
