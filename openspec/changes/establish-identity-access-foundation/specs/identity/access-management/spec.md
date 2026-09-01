## Purpose

Administrar usuarios, roles y permisos atomicos con minimo privilegio, aprobacion controlada y trazabilidad de todas las asignaciones de acceso de Landscape TSI.

## ADDED Requirements

### Requirement: Usuarios funcionales
El sistema MUST mantener un perfil funcional unico por identidad externa con estado, vigencia y atributos minimos necesarios para autorizacion y auditoria.

#### Scenario: Alta de usuario
- **WHEN** un Administrador autorizado registra una identidad corporativa no existente
- **THEN** el sistema crea un usuario sin permisos empresariales implicitos y audita el alta

#### Scenario: Suspension de usuario
- **WHEN** un Administrador suspende un usuario con justificacion
- **THEN** el sistema impide nuevas acciones protegidas sin eliminar su historial

### Requirement: Permisos atomicos
El sistema MUST representar cada capacidad protegida mediante un identificador de permiso atomico y MUST aplicar denegacion predeterminada cuando el usuario no lo posea.

#### Scenario: Permiso concedido
- **WHEN** un usuario posee el permiso requerido y satisface las politicas contextuales
- **THEN** el sistema permite la accion

#### Scenario: Permiso ausente
- **WHEN** ningun rol vigente del usuario concede el permiso requerido
- **THEN** el sistema deniega la accion aunque la interfaz haya mostrado su control

### Requirement: Roles como paquetes de permisos
El sistema MUST proporcionar inicialmente los roles Administrador, Arquitecto de Seguridad, Ingeniero de TSI y Punto de Contacto del Gobierno, y MUST tratarlos como agrupaciones administrables de permisos.

#### Scenario: Asignacion de rol inicial
- **WHEN** una asignacion aprobada concede un rol inicial a un usuario
- **THEN** el usuario obtiene solo los permisos vigentes incluidos en ese rol y dentro de sus alcances

### Requirement: Administracion controlada de acceso
El sistema MUST exigir autorizacion, justificacion y auditoria para altas, suspensiones, asignaciones de rol, cambios de permisos y cambios de alcance.

#### Scenario: Intento de autoelevacion
- **WHEN** un Administrador intenta aprobar o ejecutar una elevacion de sus propios permisos o alcance
- **THEN** el sistema deniega la operacion por separacion de funciones

#### Scenario: Cambio autorizado de rol
- **WHEN** una persona distinta del beneficiario autoriza una asignacion valida y el Administrador la ejecuta
- **THEN** el sistema aplica la asignacion con vigencia y registra antes, despues, justificacion y aprobacion

### Requirement: Administracion accesible y adaptable
Las pantallas de usuarios, roles y permisos MUST ser compatibles con escritorio, tableta y movil, cumplir WCAG 2.2 AA y mostrar claramente permisos efectivos, alcance y vigencia.

#### Scenario: Revision de permisos en pantalla pequena
- **WHEN** un Administrador consulta un usuario desde un dispositivo movil
- **THEN** la interfaz presenta roles, permisos y alcances mediante contenido adaptable sin perder etiquetas ni relaciones
