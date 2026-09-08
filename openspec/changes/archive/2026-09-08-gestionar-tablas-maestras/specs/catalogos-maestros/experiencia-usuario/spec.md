## Purpose

Ofrecer una experiencia empresarial, accesible y adaptable para seleccionar, consultar y mantener catálogos sin exponer detalles técnicos de la base de datos.

## ADDED Requirements

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
La interfaz MUST usar diálogo para registros simples y pantalla de detalle para registros complejos, mostrando etiquetas funcionales, relaciones e información pertinente sin IDs ni metadatos internos.

#### Scenario: Visualización de Tecnología TSI
- **WHEN** el usuario abre el detalle de una Tecnología TSI
- **THEN** ve secciones de identificación, clasificación, adopción, fechas, licenciamiento, responsables y referencias con valores relacionados legibles

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

