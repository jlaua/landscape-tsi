## MODIFIED Requirements

### Requirement: Detalle sin información técnica irrelevante
La interfaz MUST usar diálogo para registros simples y pantalla de detalle para registros complejos, mostrando etiquetas funcionales, relaciones e información pertinente sin IDs ni metadatos internos; y en el detalle de Building Block MUST presentar las Funcionalidades relacionadas contextualizadas con su Capacidad de Seguridad correspondiente.

#### Scenario: Visualización de Tecnología TSI
- **WHEN** el usuario abre el detalle de una Tecnología TSI
- **THEN** ve secciones de identificación, clasificación, adopción, fechas, licenciamiento, responsables y referencias con valores relacionados legibles

#### Scenario: Visualización de Funcionalidades en detalle de Building Block
- **WHEN** el usuario abre el detalle de un Building Block
- **THEN** la sección de Funcionalidades presenta columnas obligatorias `Capacidad`, `Funcionalidad`, `Estado de funcionalidad` y `Acciones`, asociando cada funcionalidad a su capacidad contenedora sin mostrar identificadores técnicos numéricos

## ADDED Requirements

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
