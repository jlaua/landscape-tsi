# catalog/building-block-technology-mapping Specification

## Purpose
Permitir administrar de forma segura, auditable y bidireccional la relación N:M entre Building Blocks y Tecnologías TSI, identificando cobertura y evitando duplicados sin alterar las entidades principales.

## Requirements

### Requirement: Vista de mapeo con filtros y estado derivado
El sistema SHALL ofrecer por defecto una vista cuya entidad principal sea Building Block, con búsqueda prioritaria por su nombre, filtros por Dominio, Fase de adopción, estado y Familia, opción Solo Building Blocks sin tecnología, ordenamiento y paginación server-side. Cada fila SHALL mostrar Building Block, Dominio, Fase, cantidad de Tecnologías TSI, estado y acciones, sin identificadores técnicos. Mapeado SHALL significar una o más filas relacionadas y Pendiente cero filas en TBuildingBlockVsTTecnologiaTSI.

#### Scenario: Building Block con tecnologías
- **WHEN** un Building Block tiene una o más relaciones válidas en la tabla puente
- **THEN** la vista lo muestra como entidad principal, estado Mapeado, cantidad de tecnologías y acción Administrar tecnologías.

#### Scenario: Building Block sin tecnologías
- **WHEN** un Building Block no tiene filas en la tabla puente
- **THEN** la vista muestra 0 tecnologías, estado Pendiente y permite iniciar su administración sin inventar una relación.

#### Scenario: Tecnología mapeada
- **WHEN** una Tecnología TSI tiene al menos una relación válida en la tabla puente
- **THEN** aparece en el detalle expandible de cada Building Block relacionado sin convertirse en la raíz del listado.

#### Scenario: Tecnología sin mapear
- **WHEN** una Tecnología TSI no tiene filas en la tabla puente
- **THEN** no se atribuye a ningún Building Block ni altera el estado de cobertura de entidades no relacionadas.

#### Scenario: Paginación y filtros
- **WHEN** el usuario aplica búsqueda, Dominio, Fase, Familia, estado o Solo Building Blocks sin tecnología
- **THEN** el servidor filtra y pagina Building Blocks mediante una consulta acotada sin cargar el catálogo completo en el navegador.

#### Scenario: Ordenamiento
- **WHEN** el usuario ordena por Building Block, Dominio, Fase, cantidad de Tecnologías o Estado
- **THEN** el servidor devuelve la página en el orden solicitado, usando Building Block ascendente como orden predeterminado.

#### Scenario: Página de Building Blocks
- **WHEN** existen más resultados que el tamaño de página
- **THEN** la interfaz muestra Anterior, Página X de Y, total de Building Blocks y Siguiente sin paginar por Tecnologías.

### Requirement: Administración bidireccional de relaciones
El sistema SHALL administrar desde un Building Block sus Tecnologías TSI actualmente asociadas y las disponibles para agregar. Asociar o desasociar SHALL modificar únicamente TBuildingBlockVsTTecnologiaTSI, impedir duplicados y no SHALL modificar las entidades principales ni relaciones adicionales existentes.

#### Scenario: Asociar múltiples Building Blocks
- **WHEN** una Tecnología sin asignar se asocia a un Building Block autorizado
- **THEN** se crea únicamente la fila puente faltante y la Tecnología aparece bajo el Building Block.

#### Scenario: Desasociar una relación
- **WHEN** un usuario autorizado desasocia una Tecnología desde el detalle de un Building Block
- **THEN** se elimina solo esa fila puente después de confirmación explícita y ninguna entidad principal es eliminada.

#### Scenario: Relación ya existente
- **WHEN** se intenta asociar una combinación ya existente
- **THEN** el sistema no crea un duplicado y devuelve un resultado idempotente o un conflicto controlado.

#### Scenario: Administración orientada al Building Block
- **WHEN** se abre Administrar Tecnologías TSI
- **THEN** se muestran el Building Block, Dominio, Tecnologías asociadas primero y Tecnologías disponibles después, con Familia, estado de adopción cuando corresponda y rutas reales de detalle.

#### Scenario: Cardinalidad física preservada
- **WHEN** una Tecnología está relacionada con más de un Building Block
- **THEN** la UI informa normalmente todas las relaciones y no elimina ni reasigna ninguna de forma automática.

### Requirement: Unicidad y concurrencia
El sistema SHALL validar en aplicación que la combinación (idBuildingBlock, idTecnologiaTSI) no se duplique y SHALL manejar una violación concurrente de unicidad sin dejar relaciones parciales. La adopción de PK compuesta o índice UNIQUE en SQL Server SHALL requerir análisis de duplicados y aprobación independiente.

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

#### Scenario: Usuario fuera de alcance
- **WHEN** un usuario intenta asignar o consultar datos fuera de su alcance organizacional
- **THEN** el servidor deniega la acción y no expone ni modifica relaciones.

### Requirement: Indicadores de cobertura
La vista SHALL mostrar Building Blocks totales, con Tecnología, sin Tecnología, Tecnologías TSI totales, filas de relación y porcentaje de cobertura. Cobertura SHALL ser Building Blocks con al menos una Tecnología / Total Building Blocks * 100 y no SHALL usar la cantidad de Tecnologías como denominador.

#### Scenario: Cobertura calculada
- **WHEN** se carga el dashboard
- **THEN** los KPIs coinciden con las relaciones existentes y el porcentaje usa exclusivamente Building Blocks como numerador y denominador.

### Requirement: Tecnologías sin asignar
La pantalla SHALL ofrecer una vista secundaria Tecnologías sin asignar, manteniendo Por Building Block como vista predeterminada, para listar Tecnologías TSI sin ninguna fila puente y permitir asociarlas a un Building Block autorizado.

#### Scenario: Detectar tecnología sin Building Block
- **WHEN** una Tecnología TSI no participa en TBuildingBlockVsTTecnologiaTSI
- **THEN** aparece en la vista secundaria con nombre, Familia y acción para asignarla.

#### Scenario: Asignar desde la vista secundaria
- **WHEN** un usuario autorizado elige un Building Block para una Tecnología sin asignar
- **THEN** se crea únicamente la relación faltante y la Tecnología deja de figurar como no asignada.

### Requirement: Permanencia del análisis por Familia
La vista SHALL mantener la sección Building Blocks por Familia después del listado principal, con sus indicadores, gráfico y exploración de detalle basados en las relaciones existentes.

#### Scenario: Orden visual de la página
- **WHEN** el usuario carga /Administration/TechnologyMapping
- **THEN** primero encuentra la jerarquía Building Block -> Tecnologías TSI y en la parte inferior encuentra Building Blocks por Familia.

#### Scenario: Uso adaptable y accesible
- **WHEN** el usuario navega desde escritorio, tableta, móvil o teclado
- **THEN** la jerarquía, estados, acciones y análisis por Familia siguen siendo comprensibles y operables conforme a WCAG 2.2 AA sin depender únicamente del color.

#### Scenario: Usuario sin permiso de consulta
- **WHEN** un usuario sin el permiso atómico requerido solicita la vista
- **THEN** el servidor deniega el acceso sin revelar Building Blocks ni Tecnologías TSI fuera de su alcance.

### Requirement: Detalle expandible y navegación funcional
Cada Building Block SHALL permitir consultar sus Tecnologías relacionadas mediante una fila expandible o detalle equivalente. Las rutas de Building Block y Tecnología SHALL ser generadas por backend y las acciones visibles SHALL conservar significado y foco accesibles.

#### Scenario: Expandir Building Block
- **WHEN** el usuario expande un Building Block con relaciones
- **THEN** ve cada Tecnología, su Familia, estado aplicable y acciones Ver y Desasociar, además de + Asociar tecnología.

#### Scenario: Navegación sin concatenación manual
- **WHEN** el usuario selecciona Ver Building Block o Ver Tecnología TSI
- **THEN** navega por la ruta real generada por backend sin exponer identificadores técnicos como contenido de UI.

#### Scenario: Building Block sin detalle relacionado
- **WHEN** el usuario expande un Building Block pendiente
- **THEN** recibe un estado vacío comprensible y una acción para asociar Tecnología.

