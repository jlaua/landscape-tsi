## Purpose

Evaluar cada accion protegida mediante permisos y politicas contextuales que incluyan estado del flujo, organizacion, asignacion y separacion de funciones.

## ADDED Requirements

### Requirement: Evaluacion integral de autorizacion
El sistema MUST autorizar una accion solo cuando el usuario este autenticado y activo, posea el permiso atomico, cumpla el alcance aplicable, satisfaga las restricciones del estado y no viole separacion de funciones.

#### Scenario: Todas las condiciones satisfechas
- **WHEN** el actor cumple permiso, alcance, asignacion, estado y segregacion requeridos
- **THEN** el sistema autoriza la accion y conserva la decision cuando sea auditable

#### Scenario: Una politica falla
- **WHEN** cualquiera de las condiciones obligatorias falla
- **THEN** el sistema aplica denegacion predeterminada y no ejecuta parcialmente la accion

### Requirement: Restricciones por estado del flujo
El sistema MUST evaluar el estado actual de la solicitud para permisos dependientes del flujo y MUST impedir transiciones no declaradas.

#### Scenario: Aprobacion fuera de estado
- **WHEN** un Punto de Gobierno intenta aprobar una solicitud que no esta pendiente de decision
- **THEN** el sistema deniega `Governance.Approve` aunque el actor posea ese permiso

### Requirement: Separacion de funciones
El sistema MUST impedir combinaciones incompatibles sobre el mismo caso, incluyendo autoaprobacion, autovalidacion de evidencia, aprobacion de excepcion propia y autoelevacion administrativa.

#### Scenario: Solicitante intenta aprobar
- **WHEN** la misma persona que creo o envio una solicitud intenta aprobarla
- **THEN** el sistema deniega la accion y registra la regla de segregacion aplicada

#### Scenario: Evaluador valida su propia evidencia critica
- **WHEN** quien cargo evidencia critica intenta validarla
- **THEN** el sistema exige un actor independiente

### Requirement: Acceso administrativo de emergencia
El sistema MUST separar el acceso de emergencia de los permisos ordinarios y exigir justificacion, referencia de incidente, duracion limitada y auditoria reforzada.

#### Scenario: Activacion valida de emergencia
- **WHEN** un Administrador autorizado activa acceso de emergencia con aprobacion y vencimiento
- **THEN** el sistema limita la elevacion al alcance y periodo aprobados y genera una alerta auditable

#### Scenario: Emergencia sin justificacion
- **WHEN** se intenta activar acceso de emergencia sin datos obligatorios
- **THEN** el sistema deniega la activacion

### Requirement: Coherencia entre interfaz y servidor
La interfaz MUST mostrar u ocultar acciones segun permisos efectivos, pero el servidor MUST volver a evaluar toda politica en cada operacion protegida.

#### Scenario: Solicitud manipulada desde el cliente
- **WHEN** un cliente envia directamente una accion que la interfaz no habilito
- **THEN** el servidor aplica las mismas politicas y deniega la accion no autorizada
