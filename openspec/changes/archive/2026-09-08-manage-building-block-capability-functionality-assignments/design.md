## Context

Landscape TSI modela la jerarquía de gobierno de seguridad de la información como:
`Dominio -> Building Block -> Capacidad de Seguridad -> Funcionalidad`

La inspección del esquema físico en SQL Server (`dbo.TCapacidadDeSeguridad`, `dbo.TFuncionalidad`, `dbo.TBuildingBlock`) y su mapeo en `src/Landscape.Tsi.Infrastructure/Catalogs/CatalogDbContext.cs` confirma:
- `dbo.TCapacidadDeSeguridad.idBuildingBlock` es una clave foránea nullable (`int?`) hacia `dbo.TBuildingBlock`.
- `dbo.TFuncionalidad.idCapacidad` es una clave foránea nullable (`int?`) hacia `dbo.TCapacidadDeSeguridad`.
- Ambas relaciones son físicamente 1:N directas. No existen tablas puente para estas jerarquías ni se requieren nuevas estructuras en la base de datos (`DDL_REQUIRED=NO`, `MIGRATION_REQUIRED=NO`).

Actualmente, la vista de detalle de Building Block (`/Administration/MasterTables/building-block/details/{id}`) lista funcionalidades pero sin explicitar su Capacidad contenedora ni permitir ordenamiento jerárquico. Asimismo, no existen interfaces ni servicios dedicados para asociar registros huérfanos o reasignar padres con análisis de impacto.

Ver `proposal.md` para la justificación funcional.

## Goals / Non-Goals

**Goals:**
- Implementar un servicio de cálculo de impacto (`IAssociationImpactService`) para proyectar el efecto de reasignaciones antes de su confirmación.
- Implementar un servicio de gestión de asignaciones (`IAssignmentService`) que ejecute transaccionalmente las asociaciones y reasignaciones de Capacidades y Funcionalidades.
- Proveer validación de concurrencia optimista mediante tokens de estado SHA-256 sin requerir columnas `ROWVERSION` en SQL Server.
- Garantizar auditoría inmutable fail-closed: el registro de auditoría (`audit.Operation` / AuditLog) se inserta dentro de la misma transacción que la mutación relacional; si la auditoría falla, se revierte la transacción.
- Reestructurar la tabla de Funcionalidades en el detalle de Building Block con columnas `Capacidad | Funcionalidad | Estado de funcionalidad | Acciones`, ordenamiento predeterminado `Capacidad ASC, Funcionalidad ASC`, y ordenamiento interactivo con lista blanca y `aria-sort`.
- Proveer modales y flujos interactivos accesibles conforme a Material Design 3 y WCAG 2.2 AA.

**Non-Goals:**
- Modificar o crear tablas, columnas, índices o restricciones en SQL Server (`DDL_REQUIRED=NO`).
- Implementar o desbloquear flujos de autenticación/federación OIDC (`DEFERRED 36/56`).
- Permitir eliminación o mutación física en cascada de entidades hijas (al reasignar una Capacidad, sus Funcionalidades asociadas se trasladan de contexto transitivamente sin alterar sus filas).
- Integrar la reasignación en el motor de restauración de borrados `AuditRestoreService` (este último está diseñado exclusivamente para snapshots de `DELETE` con mapeo de PKs; las reasignaciones son `UPDATE` y se auditan registrando `idBuildingBlock`/`idCapacidad` previo y nuevo).

## Decisions

### 1. Reutilización directa de claves foráneas físicas existentes
- **Decisión**: Usar exclusivamente `dbo.TCapacidadDeSeguridad.idBuildingBlock` y `dbo.TFuncionalidad.idCapacidad`.
- **Racional**: La base de datos existente en SQL Server ya implementa la cardinalidad 1:N mediante estas columnas nullables. No hay justificación técnica para introducir tablas intermedias.
- **Alternativas descartadas**:
  - *Tablas puente N:N*: Descartadas porque violan la jerarquía canónica del dominio y agregarían complejidad de sincronización redundante.

### 2. Preservación transitiva de entidades hijas al reasignar Capacidad
- **Decisión**: Al reasignar una Capacidad a un nuevo Building Block, únicamente se actualiza `TCapacidadDeSeguridad.idBuildingBlock`. Sus filas hijas en `dbo.TFuncionalidad` mantienen su `idCapacidad` sin cambios.
- **Racional**: Al depender funcionalmente de `idCapacidad`, las funcionalidades cambian transitivamente de contexto de Building Block sin necesidad de mutar masivamente registros hijos en la base de datos.
- **Alternativas descartadas**:
  - *Re-creación o mutación de filas de Funcionalidad*: Descartada por innecesaria y propensa a inconsistencias.

### 3. Separación del cálculo de impacto (`IAssociationImpactService`)
- **Decisión**: Exponer el cálculo de impacto como una consulta de solo lectura independiente previa al comando de mutación.
- **Racional**: Permite a la interfaz de usuario consultar y presentar las advertencias de impacto (p. ej. cuántas funcionalidades hijas cambian de contexto, qué tecnologías/casos de uso se relacionan al BB origen) de forma desacoplada antes de solicitar la confirmación explícita al usuario.
- **Alternativas descartadas**:
  - *Calcular impacto dentro del mismo POST de guardado sin confirmación interactiva*: Descartada porque el usuario debe visualizar y consentir el impacto antes de persistir.

### 4. Concurrencia optimista basada en hash SHA-256
- **Decisión**: Generar un token de estado determinístico `ComputeStateHash(entity)` compuesto por `id + parentId + nombre + estadoId` al cargar los datos y verificar su coincidencia en el momento de la confirmación.
- **Racional**: Las tablas de catálogo en SQL Server no cuentan con columnas `rowversion`/`timestamp`. El hash en memoria garantiza detección de colisiones concurrentes sin alterar el DDL de la base.
- **Alternativas descartadas**:
  - *Bloqueo pesimista de filas*: Descartada porque retendría transacciones abiertas durante la interacción del usuario con los modales.

### 5. Auditoría transaccional fail-closed
- **Decisión**: Ejecutar la mutación de la entidad y la persistencia del registro en `audit.Operation` bajo `IDbContextTransaction`. Si la inserción del log falla, se ejecuta `RollbackAsync()`.
- **Racional**: Garantiza que ninguna relación de catálogo sea alterada sin dejar traza inmutable verificable de `ValorAnterior`, `ValorNuevo`, `Entidad`, `Usuario` y `TimestampUtc`.
- **Alternativas descartadas**:
  - *Auditoría asíncrona fire-and-forget*: Descartada porque permitiría estados huérfanos de auditoría ante caídas del servidor.

### 6. Estrategia de ordenamiento en grilla de Funcionalidades
- **Decisión**: Enriquecer el ViewModel de detalle de Building Block para estructurar las funcionalidades con `CapacidadNombre`, `FuncionalidadNombre`, `EstadoNombre` y soportar ordenamiento predeterminado `Capacidad ASC, Funcionalidad ASC` junto con ordenamiento dinámico por lista blanca (`capacity`, `functionality`, `status`).
- **Racional**: Asegura consistencia jerárquica para la lectura humana y protege contra inyección de nombres de columnas arbitrarias. En la interfaz se emiten atributos `aria-sort` (`ascending`, `descending`, `none`) para lectores de pantalla.

## Risks / Trade-offs

- **[Riesgo: Modificación concurrente de la entidad entre la advertencia de impacto y la confirmación]**  
  → *Mitigación*: Validación obligatoria del token SHA-256 dentro de la transacción de guardado; rechazo con código HTTP 409 / error de concurrencia y solicitud de recarga si el hash no coincide.

- **[Riesgo: Funcionalidad reasignada entre Capacidades de distintos Building Blocks sin que el operador lo note]**  
  → *Mitigación*: El servicio de impacto evalúa si `originCapability.idBuildingBlock != targetCapability.idBuildingBlock`; si difieren, la UI resalta una advertencia visual destacada indicando el cambio de Building Block.

- **[Riesgo: Reversibilidad de mutaciones tipo UPDATE ante la limitación de `AuditRestoreService`]**  
  → *Mitigación*: El registro de auditoría almacena explícitamente el identificador del padre anterior en el campo `Metadata/Details`; en caso de error operacional, el administrador puede revertir la reasignación mediante una reasignación inversa gobernada.

- **[Riesgo: Regresiones en endpoints existentes de administración de catálogos]**  
  → *Mitigación*: `IBuildingBlockRelatedService` y `ICatalogManagementService` preservan sus contratos existentes; las nuevas operaciones se incorporan mediante extensiones modulares y pruebas de regresión unitarias e integrales.

## Migration Plan

- **DDL & Esquema**: No se requiere migración de esquema (`DDL_REQUIRED=NO`, `MIGRATION_REQUIRED=NO`).
- **Despliegue**: Despliegue estándar de binarios de la aplicación (.NET 10).
- **Rollback**: En caso de reversión de versión de código, no se requiere ningún script SQL de rollback dado que el esquema permanece inalterado.

## Open Questions

- *Ninguna pregunta abierta pendiente*: El modelo físico y los requerimientos funcionales están completamente determinados y validados contra el repositorio.
