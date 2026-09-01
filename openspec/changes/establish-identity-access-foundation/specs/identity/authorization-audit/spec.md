## Purpose

Proporcionar trazabilidad inmutable y consultable de cambios y decisiones de autorizacion para investigacion, cumplimiento y rendicion de cuentas.

## ADDED Requirements

### Requirement: Auditoria de cambios de acceso
El sistema MUST registrar altas y suspensiones de usuario, cambios de rol o permiso, cambios de alcance, excepciones y elevaciones administrativas.

#### Scenario: Asignacion de rol
- **WHEN** se crea, modifica, revoca o vence una asignacion de rol
- **THEN** el sistema registra actor, beneficiario, valores anteriores y nuevos, justificacion, aprobacion, fecha y correlacion

### Requirement: Auditoria de decisiones de autorizacion
El sistema MUST registrar las autorizaciones empresariales de alto impacto, las denegaciones por segregacion de funciones y los intentos privilegiados fallidos.

#### Scenario: Aprobacion permitida
- **WHEN** una politica permite una aprobacion empresarial
- **THEN** el sistema registra el permiso, alcance, estado, actor y resultado antes de confirmar la operacion

#### Scenario: Autoaprobacion denegada
- **WHEN** una accion se deniega por separacion de funciones
- **THEN** el sistema registra la regla aplicada sin almacenar secretos ni contenido innecesario

### Requirement: Inmutabilidad y disponibilidad
Los eventos de auditoria MUST ser append-only para usuarios de la aplicacion y MUST permanecer disponibles segun la politica de retencion.

#### Scenario: Intento de editar auditoria
- **WHEN** cualquier usuario intenta modificar o eliminar directamente un evento retenido
- **THEN** el sistema deniega la operacion

### Requirement: Consulta restringida de auditoria
El sistema MUST exigir `Audit.View` y un alcance de auditoria valido, y MUST auditar las consultas sensibles y exportaciones.

#### Scenario: Consulta dentro del alcance
- **WHEN** un actor autorizado consulta eventos de una subsidiaria incluida en su alcance
- **THEN** el sistema presenta los eventos permitidos y registra el acceso sensible

#### Scenario: Consulta fuera del alcance
- **WHEN** un actor solicita eventos de una subsidiaria no autorizada
- **THEN** el sistema deniega la consulta sin revelar su existencia o contenido

### Requirement: Presentacion accesible de auditoria
La interfaz de auditoria MUST ser adaptable, navegable por teclado y conforme con WCAG 2.2 AA, con estado y resultado comunicados mediante texto ademas de recursos visuales.

#### Scenario: Revision de evento con lector de pantalla
- **WHEN** una persona revisa el detalle de un evento usando tecnologia de asistencia
- **THEN** actor, accion, resultado, fecha, alcance y justificacion tienen nombres accesibles y un orden comprensible
