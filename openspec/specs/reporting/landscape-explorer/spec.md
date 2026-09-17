# reporting/landscape-explorer Specification

## Purpose
Permitir que usuarios autorizados exploren progresivamente el catálogo Landscape TSI mediante gráficos, grillas, KPIs contextuales y relaciones verificadas, comenzando con el vertical slice Dominio a Building Block.

## Requirements

### Requirement: Reportería progresiva y protegida
La reportería SHALL requerir autenticación y la policy `CatalogView`, SHALL ser únicamente de lectura y SHALL mostrar inicialmente el gráfico principal junto con el mensaje «Seleccione una barra para explorar la información.» sin presentar como componente principal la tabla redundante de catálogo, grupo y registros.

#### Scenario: Usuario autorizado abre la reportería
- **WHEN** un usuario autenticado con `CatalogView` solicita `/reporteria`
- **THEN** el sistema muestra los KPIs generales válidos, el gráfico `Registros por catálogo`, los filtros globales y el mensaje de selección progresiva

#### Scenario: Usuario no autorizado solicita la reportería
- **WHEN** un usuario autenticado sin `CatalogView` solicita `/reporteria` o cualquiera de sus APIs
- **THEN** el servidor deniega la operación y no entrega datos de catálogos

### Requirement: Selección funcional de Dominio
El sistema SHALL permitir seleccionar `Dominio` desde el gráfico principal o desde su alternativa accesible, SHALL mostrar «Analizando: Dominio», SHALL reemplazar el resumen visual por una grilla de dominios y SHALL mantener el código funcional `dominio` como único identificador público del catálogo.

#### Scenario: Selección de Dominio
- **WHEN** el usuario selecciona la barra `Dominio`
- **THEN** el sistema actualiza la vista sin recarga completa, muestra `Detalle de Dominios`, oculta la tabla resumen redundante y carga la grilla paginada

#### Scenario: Código de catálogo inválido
- **WHEN** el cliente solicita un código no incluido en la whitelist
- **THEN** el servidor responde como recurso no disponible o solicitud inválida sin construir SQL con el valor recibido

### Requirement: Grilla de Dominios
La grilla de Dominios SHALL soportar búsqueda, ordenamiento únicamente sobre columnas permitidas, paginación server-side, selección de fila, total de registros, estados Loading/Empty/Error y SHALL excluir PK técnicas, mostrando valores funcionales.

#### Scenario: Búsqueda y paginación
- **WHEN** el usuario introduce una búsqueda o cambia de página
- **THEN** el servidor devuelve únicamente la página solicitada y el total correspondiente sin cargar todos los registros en JavaScript

#### Scenario: Ordenamiento no permitido
- **WHEN** el cliente envía una columna de ordenamiento fuera de la whitelist
- **THEN** el servidor aplica el orden predeterminado o rechaza el parámetro sin interpolarlo en SQL

### Requirement: Contexto y breadcrumb analítico
El sistema SHALL mostrar el contexto seleccionado y un breadcrumb navegable con la forma `Reportería > Dominio > {NombreDominio}`, permitiendo regresar a niveles anteriores sin recargar toda la página.

#### Scenario: Selección de un dominio
- **WHEN** el usuario selecciona `Identity` en la grilla de Dominios
- **THEN** el sistema muestra `Reportería > Dominio > Identity` y conserva el estado de filtro, paginación y selección relevante

#### Scenario: Regreso al resumen
- **WHEN** el usuario activa `Reportería` o `Volver al resumen`
- **THEN** el sistema oculta el contexto seleccionado, destruye los gráficos secundarios activos y muestra nuevamente el estado inicial sin recarga completa

### Requirement: KPI directo de Building Blocks
Para un Dominio seleccionado, el sistema SHALL calcular y mostrar únicamente el KPI directo de Building Blocks mediante la relación real `TMDominio` a `TBuildingBlock` (`FK_TBuildingBlock_TDominio`), sin habilitar en este vertical slice KPIs de Capacidades, Funcionalidades o Tecnologías TSI.

#### Scenario: Dominio con Building Blocks
- **WHEN** el usuario selecciona un Dominio que tiene registros hijos
- **THEN** el sistema muestra el KPI `Building Blocks` con el conteo de hijos asociados por la FK real

#### Scenario: Dominio sin Building Blocks
- **WHEN** el usuario selecciona un Dominio sin registros hijos
- **THEN** el sistema muestra el KPI con valor cero y un estado vacío comprensible, sin error

### Requirement: Relación y grilla de Building Blocks
El sistema SHALL mostrar `Building Blocks por Dominio` mediante Chart.js y SHALL cargar la grilla `Building Blocks relacionados con {NombreDominio}` desde el servidor filtrando por la FK real, sin filtrar el conjunto completo en JavaScript.

#### Scenario: Selección de un Dominio con hijos
- **WHEN** el usuario selecciona una barra o fila correspondiente a `Identity`
- **THEN** el sistema actualiza el gráfico relacionado sin recarga completa y carga una grilla paginada de Building Blocks cuyo `idDominio` corresponde al registro seleccionado

#### Scenario: Dominio sin hijos
- **WHEN** el usuario selecciona un Dominio sin Building Blocks
- **THEN** el gráfico y la grilla muestran un estado vacío y no realizan consultas de detalle innecesarias

### Requirement: Chart.js controlado y accesible
Los gráficos SHALL usar Chart.js como librería estándar, SHALL actualizar datos con `chart.update()` cuando corresponda, SHALL destruir instancias reemplazadas con `chart.destroy()`, SHALL evitar múltiples instancias sobre el mismo canvas, SHALL ofrecer tooltips, selección visual, alternativa tabular accesible y estados Loading/Empty/Error.

#### Scenario: Cambio de datos del gráfico principal
- **WHEN** cambia un filtro global sin cambiar la estructura del gráfico
- **THEN** el sistema actualiza etiquetas y valores mediante la instancia existente

#### Scenario: Reemplazo de gráfico relacionado
- **WHEN** cambia el catálogo o relación seleccionada
- **THEN** el sistema destruye la instancia anterior antes de crear o mostrar la nueva

### Requirement: Relaciones controladas por metadatos reales
La reportería SHALL mantener una whitelist explícita de entidades y relaciones, SHALL validar las relaciones contra metadatos reales de SQL Server y SHALL reconocer como jerarquía principal únicamente `Dominio -> Building Block -> Capacidad de Seguridad -> Funcionalidad`; la rama tecnológica SHALL ser `Building Block -> Tecnología TSI -> Casos de Uso` y no SHALL presentarse `Funcionalidad -> Tecnología TSI` como relación directa.

#### Scenario: Relación principal verificada
- **WHEN** se valida el metadato `FK_TBuildingBlock_TDominio`
- **THEN** la relación Dominio a Building Block queda disponible para el vertical slice

#### Scenario: Relación tecnológica no verificada
- **WHEN** no se confirman PK, FK, columnas y cardinalidad de `TBuildingBlockVsTTecnologiaTSI`
- **THEN** el sistema no muestra KPIs ni drill-down de Tecnología TSI por Dominio

### Requirement: Lectura eficiente y sin escritura
Las consultas de reportería SHALL ser asíncronas, SHALL usar proyecciones y conteos agregados, SHALL usar `AsNoTracking` cuando aplique, SHALL evitar N+1 y SHALL no ejecutar POST, PUT, DELETE, DDL ni DML desde el dashboard.

#### Scenario: Consulta de detalle
- **WHEN** se solicita una página de Dominios o Building Blocks relacionados
- **THEN** el backend devuelve solo las columnas funcionales y filas de la página solicitada

#### Scenario: Solicitud AJAX obsoleta
- **WHEN** una respuesta antigua llega después de una selección más reciente
- **THEN** el cliente ignora la respuesta obsoleta y conserva el contexto más reciente

### Requirement: Responsive y WCAG 2.2 AA
La experiencia SHALL mantener Material Design 3, densidad compacta, foco visible, navegación por teclado, contenido equivalente accesible al gráfico y presentación adaptable en desktop, tablet y móvil.

#### Scenario: Visualización móvil
- **WHEN** la reportería se abre en una pantalla móvil
- **THEN** los KPIs, grillas y relaciones se apilan progresivamente, sin mostrar simultáneamente gráficos o tablas ilegibles

#### Scenario: Navegación por teclado
- **WHEN** el usuario navega con teclado por la alternativa accesible
- **THEN** puede seleccionar el catálogo, seleccionar una fila, regresar y consultar los estados anunciados por `aria-live`
