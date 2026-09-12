# Experiencia de Usuario de Catalogos Maestros Specification

## Purpose

Ofrecer una experiencia empresarial, accesible y adaptable para seleccionar, consultar y mantener catálogos sin exponer detalles técnicos de la base de datos.

## Requirements

### Requirement: Pantalla principal de selección
La interfaz MUST mostrar una pantalla titulada `Administración de Tablas Maestras` con breadcrumbs, agrupación comprensible, descripción y acceso a cada catálogo permitido según capacidades efectivas.

#### Scenario: Acceso en escritorio
- **WHEN** un usuario autorizado abre el módulo en una pantalla amplia
- **THEN** ve navegación lateral o agrupada y tarjetas/listado de catálogos con nombres funcionales, sin nombres SQL como información principal

#### Scenario: Acceso sin capacidad de mantenimiento
- **WHEN** el usuario solo posee `Catalogos.Ver`
- **THEN** puede entrar a catálogos visibles, pero las acciones de mutación no se presentan y siguen denegadas por el servidor

### Requirement: Listado operativo de catálogo
La interfaz MUST mostrar nombre funcional, descripción, cantidad de registros, buscador, filtros aplicables, acción Nuevo cuando esté autorizada, listado paginado y acciones Ver y Editar; la acción de retiro MUST aparecer solo cuando la definición la permita.

#### Scenario: Carga y resultado
- **WHEN** el usuario consulta un catálogo
- **THEN** la interfaz comunica carga, resultado y paginación sin desplazar el foco de forma inesperada

#### Scenario: Error recuperable
- **WHEN** falla la consulta
- **THEN** la interfaz muestra un mensaje comprensible, una acción de reintento y una referencia de soporte sin detalles sensibles

### Requirement: Claves técnicas ocultas
La interfaz MUST NOT presentar PK o FK numéricas como columnas normales, títulos de formulario o etiquetas funcionales, aunque MUST conservarlas internamente para navegación y comandos.

#### Scenario: Detalle relacionado
- **WHEN** el usuario visualiza un Building Block
- **THEN** ve el nombre del Dominio y de la Fase de Adopción, no sus identificadores técnicos

### Requirement: Formulario según complejidad
La interfaz MUST utilizar diálogo o panel lateral para registros simples y una pantalla independiente para registros complejos, conservando validaciones, ayuda contextual, acciones Guardar/Cancelar y prevención de pérdida accidental.

#### Scenario: Catálogo simple
- **WHEN** se crea o edita Familia, Postura Roadmap, Modalidad Laboral, Tipo de Operación o un catálogo simple habilitado
- **THEN** la interfaz usa un diálogo o panel compacto con nombre y descripción

#### Scenario: Registro complejo
- **WHEN** se crea o edita Dominio, Building Block, Empresa/Subsidiaria o Tecnología TSI
- **THEN** la interfaz usa una pantalla independiente con secciones, campos multilínea y relaciones funcionales

#### Scenario: Salida con cambios pendientes
- **WHEN** el usuario intenta cerrar o navegar fuera de un formulario modificado
- **THEN** la interfaz solicita confirmación antes de descartar los cambios

### Requirement: Detalle sin información técnica irrelevante
La interfaz MUST usar diálogo para registros simples y pantalla de detalle para registros complejos, mostrando etiquetas funcionales, relaciones e información pertinente sin IDs ni metadatos internos; y en el detalle de Building Block MUST presentar las Funcionalidades relacionadas contextualizadas con su Capacidad de Seguridad correspondiente.

#### Scenario: Visualización de Tecnología TSI
- **WHEN** el usuario abre el detalle de una Tecnología TSI
- **THEN** ve secciones de identificación, clasificación, adopción, fechas, licenciamiento, responsables y referencias con valores relacionados legibles

#### Scenario: Visualización de Funcionalidades en detalle de Building Block
- **WHEN** el usuario abre el detalle de un Building Block
- **THEN** la sección de Funcionalidades presenta columnas obligatorias `Capacidad`, `Funcionalidad`, `Estado de funcionalidad` y `Acciones`, asociando cada funcionalidad a su capacidad contenedora sin mostrar identificadores técnicos numéricos

### Requirement: Confirmación segura de retiro
La interfaz MUST solicitar confirmación antes de cualquier desactivación o eliminación permitida y MUST explicar el efecto, las dependencias conocidas y si la operación es reversible.

#### Scenario: Retiro no disponible
- **WHEN** el catálogo carece de estrategia de vigencia aprobada
- **THEN** la interfaz no ofrece confirmación destructiva y explica que el retiro requiere aprobación adicional

#### Scenario: Confirmación cancelada
- **WHEN** el usuario cancela el diálogo de retiro
- **THEN** la interfaz vuelve al contexto anterior sin efectuar ninguna operación

### Requirement: Diseño responsive
La interfaz MUST adaptarse a escritorio, tableta y móvil, MUST priorizar campos funcionales y MUST sustituir tablas inutilizables por cards o vistas resumidas sin exigir desplazamiento horizontal excesivo.

#### Scenario: Listado en móvil
- **WHEN** el ancho disponible no permite una tabla legible
- **THEN** cada registro se presenta como card o resumen con nombre, relación principal, estado funcional y menú de acciones accesible

#### Scenario: Formulario en tableta
- **WHEN** el formulario se usa en una tableta
- **THEN** los controles se reorganizan sin perder etiquetas, errores, orden de lectura ni acciones principales

### Requirement: Accesibilidad WCAG 2.2 AA
La interfaz MUST ser operable por teclado, exponer nombres y errores accesibles, mantener foco visible, satisfacer contraste AA y no depender solo del color para comunicar estados o resultados.

#### Scenario: Validación con lector de pantalla
- **WHEN** un formulario contiene errores y se envía usando tecnología de asistencia
- **THEN** el resumen de errores recibe foco, identifica cada campo afectado y permite navegar hacia él

#### Scenario: Diálogo accesible
- **WHEN** se abre un diálogo de creación, detalle o confirmación
- **THEN** el foco queda contenido y anunciado, Escape o Cancelar lo cierra cuando corresponda y el foco retorna al control que lo abrió

### Requirement: Retroalimentación de operaciones
La interfaz MUST comunicar éxito, error, carga y ausencia de resultados mediante texto claro y componentes no intrusivos, preservando el contexto del catálogo y la búsqueda.

#### Scenario: Guardado exitoso
- **WHEN** una operación termina correctamente
- **THEN** la interfaz confirma el resultado, actualiza el listado y mantiene filtros y página cuando sigan siendo válidos

### Requirement: Visualización y ordenamiento de Funcionalidades en detalle de Building Block
La tabla de Funcionalidades dentro del detalle de Building Block MUST estructurarse con las columnas obligatorias `Capacidad`, `Funcionalidad`, `Estado de funcionalidad` y `Acciones`, MUST aplicar por defecto un ordenamiento compuesto por `Capacidad ASC, Funcionalidad ASC`, y MUST permitir al usuario ordenar interactivamente por las columnas permitidas (`capacity`, `functionality`, `status`) manteniendo consistencia visual y accesibilidad con atributos `aria-sort`.

#### Scenario: Ordenamiento predeterminado de Funcionalidades
- **WHEN** el usuario carga la vista de detalle de un Building Block con múltiples funcionalidades
- **THEN** la tabla se presenta ordenada primero por nombre funcional de Capacidad en orden ascendente y en segundo lugar por nombre funcional de Funcionalidad en orden ascendente

#### Scenario: Ordenamiento interactivo por columna
- **WHEN** el usuario activa el encabezado de ordenamiento de una columna permitida (Capacidad, Funcionalidad o Estado)
- **THEN** el sistema actualiza el orden de la tabla según la columna seleccionada, alterna la dirección ascendente/descendente y actualiza el atributo `aria-sort` en el encabezado correspondiente

#### Scenario: Parámetro de ordenamiento no permitido
- **WHEN** se proporciona un parámetro de ordenamiento fuera de la lista blanca aprobada
- **THEN** el sistema recurre al orden predeterminado (`Capacidad ASC, Funcionalidad ASC`) sin generar errores ni ejecutar consultas no validadas

#### Scenario: Building Block sin Funcionalidades asociadas
- **WHEN** un Building Block no tiene Capacidades con Funcionalidades o sus Capacidades carecen de Funcionalidades
- **THEN** la tabla presenta un estado vacío comprensible indicando la ausencia de funcionalidades y ofreciendo acciones para asociar Capacidades o Funcionalidades

### Requirement: Interacción accesible para asignación y reasignación en detalle
La interfaz de detalle de Building Block y de los catálogos de Capacidad y Funcionalidad MUST proporcionar controles accesibles (diálogos modales o paneles deslizantes) para asociar entidades huérfanas y reasignar entidades padre, desplegando advertencias de impacto antes de confirmar y garantizando operabilidad completa por teclado y lectores de pantalla según WCAG 2.2 AA.

#### Scenario: Apertura de diálogo de asociación de Capacidad
- **WHEN** el usuario selecciona la acción para asociar una Capacidad al Building Block
- **THEN** se abre un diálogo accesible que lista las Capacidades huérfanas disponibles por su nombre funcional, manteniendo el foco atrapado en el diálogo

#### Scenario: Advertencia de impacto en reasignación
- **WHEN** el usuario inicia la reasignación de una Capacidad o Funcionalidad
- **THEN** la interfaz presenta un resumen claro del impacto (número de dependencias hijas afectadas o cambio de Building Block), requiere confirmación explícita mediante un botón de confirmación secundario antes de proceder y permite cancelar retornando el foco al control de origen

#### Scenario: Operación por teclado y foco accesible
- **WHEN** el usuario navega y confirma o cancela un diálogo de asignación o reasignación usando solo el teclado
- **THEN** la tecla Escape cierra el diálogo, el orden de tabulación permanece dentro del diálogo mientras esté abierto y tras cerrarse el foco regresa al botón que activó la acción
