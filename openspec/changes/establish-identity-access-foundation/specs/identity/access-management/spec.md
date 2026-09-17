## Purpose

Administrar usuarios, roles y permisos atomicos con minimo privilegio, aprobacion controlada y trazabilidad de todas las asignaciones de acceso de Landscape TSI.

## ADDED Requirements

### Requirement: Usuarios funcionales
El sistema MUST mantener un perfil funcional unico capaz de asociar identidad externa y credencial local opcional, con estado, vigencia y atributos minimos necesarios para autenticacion, autorizacion y auditoria.

#### Scenario: Alta de usuario
- **WHEN** un Administrador autorizado registra una identidad corporativa no existente
- **THEN** el sistema crea un usuario sin permisos empresariales implicitos y audita el alta

#### Scenario: Suspension de usuario
- **WHEN** un Administrador suspende un usuario con justificacion
- **THEN** el sistema impide nuevas acciones protegidas sin eliminar su historial

#### Scenario: Usuario con dos mecanismos
- **WHEN** un perfil local vincula un login corporativo y una credencial local
- **THEN** ambos mecanismos resuelven el mismo identificador interno y las mismas asignaciones empresariales

### Requirement: Permisos atomicos
El sistema MUST representar cada capacidad protegida mediante un identificador de permiso atomico y MUST aplicar denegacion predeterminada cuando el usuario no lo posea.

#### Scenario: Permiso concedido
- **WHEN** un usuario posee el permiso requerido y satisface las politicas contextuales
- **THEN** el sistema permite la accion

#### Scenario: Permiso ausente
- **WHEN** ningun rol vigente del usuario concede el permiso requerido
- **THEN** el sistema deniega la accion aunque la interfaz haya mostrado su control

### Requirement: Roles como paquetes de permisos
El sistema MUST proporcionar inicialmente los roles Administrador del Sistema, Arquitecto de Seguridad, Ingeniero de TSI y Punto de Contacto del Gobierno, y MUST tratarlos como agrupaciones administrables de permisos independientes del mecanismo de autenticacion.

#### Scenario: Asignacion de rol inicial
- **WHEN** una asignacion aprobada concede un rol inicial a un usuario
- **THEN** el usuario obtiene solo los permisos vigentes incluidos en ese rol y dentro de sus alcances

#### Scenario: Identity no crea roles paralelos
- **WHEN** una cuenta local se autentica mediante ASP.NET Core Identity
- **THEN** sus permisos se obtienen de los roles funcionales y politicas de Landscape TSI, no de un segundo catalogo de roles de autenticacion

### Requirement: Administradores locales iniciales
El sistema MUST asignar a los usuarios bootstrap `jean` y `administrador` el rol `Administrador del Sistema`, cuyos permisos se definen mediante la matriz autorizada y se evaluan siempre en el servidor.

#### Scenario: Permisos administrativos efectivos
- **WHEN** un administrador bootstrap inicia sesion y su asignacion esta activa
- **THEN** puede ejercer administracion de catalogos, usuarios y roles, consultar auditoria y ejecutar solo las funciones administrativas concedidas por permisos y politicas

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
