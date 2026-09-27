# Administracion de Catalogos Maestros Specification

## Purpose

Permitir la consulta y el mantenimiento gobernado de los catálogos oficiales de Landscape TSI, conservando las estructuras, identificadores y relaciones existentes en SQL Server.

## Requirements

### Requirement: Catálogos administrables mediante lista blanca
El sistema MUST admitir únicamente los 16 catálogos aprobados y MUST resolver cada catálogo desde un código funcional registrado por el servidor, nunca desde un nombre de tabla proporcionado libremente por el cliente.

#### Scenario: Selección de catálogo permitido
- **WHEN** un usuario selecciona un código de catálogo incluido en la lista blanca
- **THEN** el sistema resuelve su definición aprobada y muestra la administración correspondiente

#### Scenario: Código manipulado o desconocido
- **WHEN** el cliente envía un nombre físico, un código no registrado o caracteres destinados a alterar la consulta
- **THEN** el sistema rechaza la solicitud sin construir ni ejecutar SQL dinámico con ese valor

### Requirement: Inventario inicial de catálogos
El sistema MUST registrar como administrables `TMDominio`, `TBuildingBlock`, `TCapacidadDeSeguridad`, `TMEstadoCapacidad`, `TFuncionalidad`, `TMEstadoFuncionalidad`, `TEstadoFaseAdopcion`, `TTecnologiaTSI`, `TMFamilia`, `TCasosDeUso`, `TEmpresaSubsidiaria`, `TCISO`, `TMPosturaRoadmap`, `TMEstadoAdopcionTSI`, `TModalidadLaboral` y `TTipoOperacion`, con nombre funcional, descripción y capacidades habilitadas por catálogo.

#### Scenario: Inventario disponible
- **WHEN** un usuario autorizado abre Administración de Tablas Maestras
- **THEN** el sistema presenta únicamente los catálogos registrados y las acciones habilitadas para cada uno

### Requirement: Consulta funcional paginada
El sistema MUST permitir buscar, filtrar cuando exista un criterio aprobado, ordenar y paginar registros, y MUST mostrar el conteo correspondiente sin presentar las PK técnicas como columnas funcionales.

#### Scenario: Listado con datos
- **WHEN** el usuario abre un catálogo con registros
- **THEN** el sistema presenta campos funcionales priorizados, conteo, búsqueda, paginación y acciones permitidas

#### Scenario: Catálogo sin registros
- **WHEN** una consulta autorizada no obtiene registros
- **THEN** el sistema presenta un estado vacío comprensible y conserva disponibles las acciones autorizadas

### Requirement: Relaciones representadas por valores funcionales
El sistema MUST obtener las opciones de FK desde los catálogos relacionados, presentar sus etiquetas funcionales y validar en el servidor que cada identificador recibido exista y corresponda a la relación esperada.

#### Scenario: Selección válida de relación
- **WHEN** el usuario selecciona el nombre funcional de un Dominio para un Building Block
- **THEN** el sistema conserva internamente `idDominio` y presenta el nombre del Dominio, no el número técnico

#### Scenario: FK alterada por el cliente
- **WHEN** el cliente envía un identificador inexistente o de un catálogo diferente
- **THEN** el sistema rechaza la operación con un error de validación y no persiste cambios

### Requirement: Creación y edición explícitas
El sistema MUST aceptar solo campos editables declarados para el catálogo, MUST ignorar o rechazar propiedades técnicas no permitidas y MUST volver a leer el registro confirmado después de una operación exitosa.

#### Scenario: Creación válida
- **WHEN** un actor autorizado envía los campos permitidos y satisface las validaciones del catálogo
- **THEN** el sistema crea el registro, usa la PK generada internamente y devuelve su representación funcional

#### Scenario: Intento de asignar una PK
- **WHEN** el cliente intenta establecer o modificar una PK `IDENTITY`
- **THEN** el sistema rechaza el campo técnico y no altera el identificador

#### Scenario: Edición concurrente
- **WHEN** el registro cambió desde que el usuario abrió el formulario y no existe un token de concurrencia en la tabla
- **THEN** el sistema detecta la divergencia mediante la representación original aprobada, evita sobrescribir silenciosamente y solicita recargar

### Requirement: Obligatoriedad de negocio independiente de nulabilidad física
El sistema MUST aplicar reglas de obligatoriedad definidas por catálogo en el servidor y MUST documentar que la nulabilidad actual de SQL Server no equivale a opcionalidad funcional.

#### Scenario: Campo funcional obligatorio vacío
- **WHEN** el usuario omite el nombre funcional definido como obligatorio aunque la columna SQL permita `NULL`
- **THEN** el sistema rechaza la operación y señala el campo que debe completarse

### Requirement: Catálogos autoritativos protegidos
El sistema MUST tratar Estado de Adopción TSI, Fase de Adopción, Estado de Capacidad y Estado de Funcionalidad como fuentes autoritativas existentes y MUST mantenerlos en modo de consulta en este cambio.

#### Scenario: Consulta de catálogo autoritativo
- **WHEN** un usuario con `Catalogos.Ver` abre uno de los cuatro catálogos protegidos
- **THEN** el sistema muestra sus valores existentes sin hardcodearlos en la vista

#### Scenario: Intento de modificar valor autoritativo
- **WHEN** un cliente intenta crear, editar, desactivar o eliminar un valor protegido
- **THEN** el sistema deniega la mutación e informa que requiere un cambio OpenSpec y autorización operativa independientes

### Requirement: Conservación referencial y retiro controlado
El sistema MUST impedir el borrado físico desde el módulo inicial y MUST mostrar desactivación solo cuando exista una estrategia aprobada y soportada por la tabla correspondiente.

#### Scenario: Registro con dependencias
- **WHEN** un usuario intenta retirar un registro referenciado por otro catálogo o dato transaccional
- **THEN** el sistema conserva el registro, explica la dependencia de forma funcional y no ejecuta `DELETE`

#### Scenario: Tabla sin indicador de vigencia
- **WHEN** el catálogo no posee campo activo, vigencia o eliminación lógica aprobado
- **THEN** el sistema no ofrece una operación destructiva y remite a una aprobación OpenSpec posterior

### Requirement: Compatibilidad con la base existente
El sistema MUST mapear los nombres físicos, tipos, PK y FK existentes sin renombrar, recrear, sembrar ni alterar automáticamente las 16 tablas.

#### Scenario: Revisión de migración
- **WHEN** se genera una migración para revisión durante la implementación
- **THEN** la revisión verifica que no contenga DDL ni DML contra las 16 tablas y que no pueda ejecutarse contra producción como parte del flujo automatizado

### Requirement: Gestión de Tecnologías TSI Asociadas a Vendors
El catálogo de Vendors (`TMVendor`) MUST permitir visualizar, asociar, desasociar y crear tecnologías TSI (`TTecnologiaTSI`) vinculadas al proveedor mediante modales interactivos y pestañas dedicadas tanto desde el listado como desde el detalle del Vendor.

#### Scenario: Visualización de tecnologías asociadas y desasociación
- **WHEN** un usuario autorizado abre la gestión de tecnologías para un Vendor
- **THEN** el sistema lista las tecnologías actualmente asociadas (`idVendor = vendor.Id`) y ofrece una acción para desasociar la tecnología (estableciendo `idVendor = NULL` tras confirmación).

#### Scenario: Búsqueda y asociación de tecnología existente
- **WHEN** el usuario busca una tecnología TSI en la pestaña "Asociar Existente" del modal de Vendor
- **THEN** el sistema filtra las tecnologías del catálogo maestro en tiempo real y permite asociarla al Vendor en curso con un solo clic.

#### Scenario: Registro rápido de tecnología para el Vendor
- **WHEN** el usuario completa el formulario de alta rápida de tecnología TSI dentro del modal del Vendor
- **THEN** el sistema crea la nueva entidad `TTecnologiaTSI` asignándole automáticamente el `idVendor` del proveedor actual.

### Requirement: Estandarización de Relación de Vendor y Versión Desplegada en Catálogos
El catálogo de Tecnologías TSI (`TTecnologiaTSI`) MUST ofrecer selectores combo para Vendor (`TMVendor`) y Versión Desplegada (`TMVersionDesplegada`). Asimismo, el catálogo de Tecnologías Implementadas por Subsidiaria (`TTecnologiaTSIimplementadaSubsidiaria`) MUST presentar el campo "Versión Desplegada" como un desplegable relacional en lugar de entrada de texto libre.

#### Scenario: Selección de Vendor y Versión Desplegada en Tecnología TSI
- **WHEN** se crea o edita un registro en el catálogo de Tecnologías TSI
- **THEN** el sistema presenta combos dinámicos poblados con las opciones activas de `TMVendor` y `TMVersionDesplegada`.

#### Scenario: Selección de Versión Desplegada en Tecnología Implementada por Subsidiaria
- **WHEN** se administra una tecnología implementada en una subsidiaria
- **THEN** el sistema despliega el campo Versión Desplegada como selector vinculado a `TMVersionDesplegada`, persistiendo la clave foránea correspondiente.

