## Purpose

Permitir administrar de forma segura, auditable y bidireccional la relación N:M entre Building Blocks y Tecnologías TSI, identificando cobertura y evitando duplicados sin alterar las entidades principales.

## ADDED Requirements

### Requirement: Vista de mapeo con filtros y estado derivado
El sistema SHALL ofrecer una vista administrativa de Tecnologías TSI con búsqueda, filtro por estado de mapeo, Building Block, Familia y paginación server-side. El estado Mapeada/Sin mapear SHALL derivarse exclusivamente de la existencia de filas relacionadas en `TBuildingBlockVsTTecnologiaTSI`.

#### Scenario: Tecnología mapeada
- **WHEN** una Tecnología TSI tiene al menos una relación válida en la tabla puente
- **THEN** la vista muestra estado Mapeada y los Building Blocks funcionales relacionados.

#### Scenario: Tecnología sin mapear
- **WHEN** una Tecnología TSI no tiene filas en la tabla puente
- **THEN** la vista muestra estado Sin mapear y permite iniciar su administración sin inventar una relación.

#### Scenario: Paginación y filtros
- **WHEN** el usuario aplica búsqueda, Familia, Building Block o estado
- **THEN** el servidor filtra y pagina los resultados sin cargar el catálogo completo en el navegador.

### Requirement: Administración bidireccional de relaciones
El sistema SHALL permitir seleccionar uno o varios Building Blocks para una Tecnología TSI y seleccionar una o varias Tecnologías para un Building Block. Guardar SHALL crear únicamente las relaciones faltantes de la tabla puente y desasociar SHALL eliminar únicamente las filas puente seleccionadas.

#### Scenario: Asociar múltiples Building Blocks
- **WHEN** un usuario autorizado confirma varias asociaciones nuevas
- **THEN** se crean las filas puente correspondientes sin modificar `TBuildingBlock` ni `TTecnologiaTSI`.

#### Scenario: Desasociar una relación
- **WHEN** un usuario autorizado confirma la desasociación
- **THEN** se elimina solo la relación N:M y la UI explica que ninguna entidad principal será eliminada.

#### Scenario: Relación ya existente
- **WHEN** una asociación enviada ya existe
- **THEN** el sistema no crea un duplicado y devuelve un resultado idempotente o un conflicto controlado.

### Requirement: Unicidad y concurrencia
El sistema SHALL validar en aplicación que la combinación `(idBuildingBlock, idTecnologiaTSI)` no se duplique y SHALL manejar una violación concurrente de unicidad sin dejar relaciones parciales. La adopción de PK compuesta o índice UNIQUE en SQL Server SHALL requerir análisis de duplicados y aprobación independiente.

#### Scenario: Dos usuarios asocian simultáneamente
- **WHEN** dos solicitudes intentan crear la misma relación
- **THEN** como máximo una relación queda persistida y la otra recibe un error controlado sin modificar entidades principales.

#### Scenario: Duplicados preexistentes
- **WHEN** la prevalidación detecta duplicados en la tabla puente
- **THEN** la aplicación bloquea nuevas operaciones ambiguas y muestra el conteo para decisión, sin eliminar automáticamente filas.

### Requirement: Autorización, alcance y auditoría
El sistema SHALL proteger consulta y administración mediante permisos/policies server-side, respetar el alcance organizacional aplicable y registrar asociación y desasociación con actor, fecha UTC, entidades, resultado y cantidad de relaciones afectadas. La UI no SHALL confiar únicamente en ocultar botones.

#### Scenario: Usuario sin permiso
- **WHEN** un usuario sin el permiso de administración solicita guardar o desasociar
- **THEN** el servidor responde 403 y no cambia la tabla puente.

#### Scenario: Usuario autorizado
- **WHEN** un usuario autorizado opera dentro de su alcance
- **THEN** puede consultar y modificar relaciones permitidas y la acción queda auditada sin secretos.

### Requirement: Navegación y accesibilidad
Cada Tecnología TSI y Building Block administrable SHALL enlazar mediante rutas generadas por backend a sus detalles reales. La vista SHALL ser responsive, navegable por teclado y no depender únicamente del color para indicar estado.

#### Scenario: Navegación bidireccional
- **WHEN** el usuario selecciona Ver Tecnología o Ver Building Block
- **THEN** la ruta válida abre el detalle correspondiente sin HTTP 404.

#### Scenario: Uso móvil y teclado
- **WHEN** el usuario opera desde móvil o teclado
- **THEN** filtros, selección múltiple, confirmación y estados Mapeada/Sin mapear permanecen utilizables y tienen etiquetas accesibles.

### Requirement: Indicadores de cobertura
La vista SHALL mostrar totales de Tecnologías, Tecnologías mapeadas, Tecnologías sin mapear, Building Blocks, Building Blocks con Tecnología, Building Blocks sin Tecnología y porcentaje de cobertura calculados mediante consultas server-side.

#### Scenario: Cobertura calculada
- **WHEN** se carga el dashboard
- **THEN** los KPIs coinciden con las relaciones existentes y no incluyen entidades inexistentes ni valores hardcodeados.
