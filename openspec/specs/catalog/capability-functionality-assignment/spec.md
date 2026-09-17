# catalog/capability-functionality-assignment Specification

## Purpose
Permitir la administración gobernada de las relaciones jerárquicas 1:N entre Building Blocks, Capacidades de Seguridad y Funcionalidades, habilitando la asociación de entidades huérfanas y la reasignación de entidades padre con cálculo preventivo de impacto, control de concurrencia optimista y auditoría transaccional fail-closed sin alterar el esquema físico de SQL Server.

## Requirements

### Requirement: Asociación de Capacidad huérfana a Building Block
El sistema MUST permitir asociar una Capacidad de Seguridad huérfana (`TCapacidadDeSeguridad.idBuildingBlock = NULL`) a un Building Block destino válido, actualizando únicamente su clave foránea en el servidor previa validación de existencia.

#### Scenario: Asociación exitosa de Capacidad huérfana
- **WHEN** un usuario autorizado selecciona una Capacidad huérfana y un Building Block destino existente
- **THEN** el sistema asigna `idBuildingBlock` en `dbo.TCapacidadDeSeguridad`, registra el evento de auditoría y refleja la Capacidad dentro del Building Block

#### Scenario: Intento de asociar a Building Block inexistente
- **WHEN** el cliente envía un identificador de Building Block que no existe en la base de datos
- **THEN** el servidor rechaza la operación con error de validación y no modifica la Capacidad

### Requirement: Reasignación de Capacidad entre Building Blocks con cálculo de impacto
El sistema MUST permitir reasignar una Capacidad de Seguridad de su Building Block actual a otro Building Block destino, calculando y presentando de forma previa el impacto (cantidad de Funcionalidades hijas que cambiarán de contexto y Tecnologías/Casos de uso vinculados al Building Block origen), y exigiendo confirmación explícita del usuario.

#### Scenario: Cálculo y visualización de impacto previo a la reasignación
- **WHEN** el usuario inicia la reasignación de una Capacidad con Funcionalidades asociadas hacia otro Building Block
- **THEN** el sistema calcula e informa el número exacto de Funcionalidades hijas que cambiarán indirectamente de Building Block, las dependencias vinculadas y solicita confirmación

#### Scenario: Confirmación de reasignación de Capacidad
- **WHEN** el usuario confirma explícitamente la reasignación con impacto advertido
- **THEN** el sistema actualiza `TCapacidadDeSeguridad.idBuildingBlock`, conserva intactas las filas hijas en `TFuncionalidad` (preservando su `idCapacidad`) y registra la auditoría con el Building Block anterior y el nuevo

#### Scenario: Cancelación de reasignación de Capacidad
- **WHEN** el usuario cancela el diálogo de advertencia de impacto
- **THEN** el sistema cierra el diálogo sin persistir cambios en la base de datos y mantiene la asignación original

#### Scenario: Intento de reasignar al mismo Building Block
- **WHEN** el usuario selecciona como destino el mismo Building Block al que ya pertenece la Capacidad
- **THEN** el sistema informa que la entidad ya se encuentra asignada a dicho Building Block y no ejecuta ninguna mutación ni evento de auditoría

### Requirement: Asociación de Funcionalidad huérfana a Capacidad
El sistema MUST permitir asociar una Funcionalidad huérfana (`TFuncionalidad.idCapacidad = NULL`) a una Capacidad de Seguridad destino válida, actualizando únicamente `TFuncionalidad.idCapacidad` previa validación de existencia en el servidor.

#### Scenario: Asociación exitosa de Funcionalidad huérfana
- **WHEN** un usuario autorizado selecciona una Funcionalidad huérfana y una Capacidad destino existente
- **THEN** el sistema actualiza `idCapacidad` en `dbo.TFuncionalidad`, registra el evento de auditoría y refleja la Funcionalidad dentro de la Capacidad

#### Scenario: Intento de asociar Funcionalidad a Capacidad inexistente
- **WHEN** el cliente envía un identificador de Capacidad inexistente
- **THEN** el servidor rechaza la solicitud con error de validación y no modifica la Funcionalidad

### Requirement: Reasignación de Funcionalidad entre Capacidades con advertencia de contexto
El sistema MUST permitir reasignar una Funcionalidad de una Capacidad origen a una Capacidad destino, identificando e informando si la reasignación ocurre dentro del mismo Building Block o representa un cambio hacia otro Building Block, y exigiendo confirmación explícita.

#### Scenario: Reasignación dentro del mismo Building Block
- **WHEN** el usuario reasigna una Funcionalidad hacia otra Capacidad que pertenece al mismo Building Block
- **THEN** el sistema informa el cambio de Capacidad indicando que el Building Block se mantiene, solicita confirmación y actualiza `idCapacidad` al confirmar

#### Scenario: Reasignación hacia Capacidad de un Building Block distinto
- **WHEN** el usuario reasigna una Funcionalidad hacia una Capacidad que pertenece a un Building Block diferente
- **THEN** el sistema advierte explícitamente el cambio de Capacidad y el cambio de Building Block resultante, solicita confirmación y actualiza `idCapacidad` al confirmar

#### Scenario: Cancelación de reasignación de Funcionalidad
- **WHEN** el usuario cancela la advertencia de impacto de la reasignación
- **THEN** el sistema cancela la operación sin modificar `dbo.TFuncionalidad`

### Requirement: Concurrencia optimista mediante token de estado
El sistema MUST verificar un token de concurrencia optimista (hash SHA-256 del estado de la entidad) antes de persistir cualquier asociación o reasignación. Si el estado del registro cambió en la base de datos entre la lectura y el intento de guardado, la operación MUST ser rechazada sin sobrescribir.

#### Scenario: Reasignación sin conflicto concurrente
- **WHEN** el token de concurrencia enviado coincide con el estado actual del registro en la base de datos
- **THEN** la operación procede y se completa exitosamente

#### Scenario: Conflicto de concurrencia detectado
- **WHEN** otro usuario o proceso modificó la Capacidad o Funcionalidad antes de confirmar la reasignación
- **THEN** el sistema rechaza la operación, no persiste cambios y solicita al usuario recargar la vista para ver los datos vigentes

### Requirement: Integridad transaccional y auditoría fail-closed
Toda mutación de asociación o reasignación MUST ejecutarse dentro de una transacción que incluya el registro inmutable en el log de auditoría del sistema (`audit.Operation` o log estructurado equivalente), capturando la entidad modificada, el valor de clave foránea anterior, el valor nuevo, la justificación, el usuario autenticado y la fecha/hora UTC. Si el registro de auditoría falla, la transacción MUST realizar rollback completo (fail-closed).

#### Scenario: Transacción completada con auditoría inmutable
- **WHEN** se ejecuta una reasignación válida
- **THEN** el sistema persiste la actualización de la clave foránea y el registro de auditoría con `ValorAnterior`, `ValorNuevo`, `Entidad`, `Usuario` y `TimestampUtc` en una única transacción confirmada

#### Scenario: Falla en el servicio de auditoría provoca rollback total
- **WHEN** ocurre un error al persistir el registro de auditoría
- **THEN** la transacción se revierte en su totalidad, la clave foránea permanece en su valor original y se notifica el fallo de la operación de forma segura

### Requirement: Preservación del modelo relacional físico
El sistema MUST gestionar las relaciones jerárquicas 1:N utilizando exclusivamente las columnas de clave foránea existentes `dbo.TCapacidadDeSeguridad.idBuildingBlock` y `dbo.TFuncionalidad.idCapacidad`, sin requerir tablas puente, modificaciones DDL ni migraciones de esquema en la base de datos (`DDL_REQUIRED=NO`, `MIGRATION_REQUIRED=NO`).

#### Scenario: Persistencia directa en claves foráneas físicas
- **WHEN** se confirma una asociación o reasignación
- **THEN** la actualización impacta directamente sobre la fila existente en `dbo.TCapacidadDeSeguridad` o `dbo.TFuncionalidad` sin alterar restricciones ni crear tablas adicionales
