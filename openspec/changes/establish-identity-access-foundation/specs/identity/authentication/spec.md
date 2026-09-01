## Purpose

Proporcionar autenticacion empresarial federada y sesiones seguras para identificar de forma confiable a cada actor de Landscape TSI sin administrar contrasenas locales.

## ADDED Requirements

### Requirement: Autenticacion federada
El sistema MUST autenticar a los usuarios mediante un proveedor OpenID Connect configurado y MUST denegar el acceso empresarial a identidades no autenticadas.

#### Scenario: Inicio de sesion satisfactorio
- **WHEN** el proveedor valida una identidad y devuelve las reclamaciones requeridas
- **THEN** el sistema establece una sesion autenticada vinculada al perfil local correspondiente

#### Scenario: Autenticacion fallida
- **WHEN** el proveedor rechaza la autenticacion o no entrega una identidad valida
- **THEN** el sistema no crea una sesion y muestra un resultado seguro que no revela detalles internos

### Requirement: Vinculacion con usuario local
El sistema MUST vincular la identidad externa con un unico usuario local activo utilizando emisor y sujeto estables, y no solamente correo o nombre visible.

#### Scenario: Identidad conocida y activa
- **WHEN** una identidad autenticada coincide con un usuario local activo
- **THEN** el sistema permite continuar y carga sus permisos y alcances vigentes

#### Scenario: Usuario ausente o inactivo
- **WHEN** la identidad no tiene usuario local habilitado o el usuario esta suspendido
- **THEN** el sistema deniega el acceso empresarial y registra la decision

### Requirement: Proteccion de credenciales y tokens
El sistema MUST NOT almacenar contrasenas locales ni tokens federados en texto claro y MUST proteger las credenciales de sesion contra divulgacion y reutilizacion.

#### Scenario: Persistencia del perfil
- **WHEN** se crea o actualiza el perfil local
- **THEN** solo se conservan identificadores y atributos minimos necesarios, nunca la contrasena del proveedor

### Requirement: Ciclo de vida de la sesion
El sistema MUST finalizar la sesion por cierre explicito, expiracion o invalidacion del usuario, y MUST volver a evaluar acceso sensible cuando cambien permisos o alcances.

#### Scenario: Cierre de sesion
- **WHEN** el usuario solicita cerrar sesion
- **THEN** el sistema invalida la sesion local y ejecuta el cierre federado cuando el proveedor lo soporte

#### Scenario: Usuario suspendido durante una sesion
- **WHEN** se detecta que el usuario autenticado fue suspendido
- **THEN** el sistema impide nuevas acciones protegidas y exige una nueva autenticacion valida

### Requirement: Experiencia accesible de autenticacion
La interfaz de inicio, cierre y denegacion de acceso MUST ser adaptable, operable por teclado, comprensible y conforme con WCAG 2.2 AA.

#### Scenario: Acceso desde dispositivo movil con teclado
- **WHEN** una persona navega por la experiencia de autenticacion en una pantalla pequena usando teclado
- **THEN** el foco, las instrucciones y los mensajes permanecen visibles, ordenados y accionables sin depender solo del color
