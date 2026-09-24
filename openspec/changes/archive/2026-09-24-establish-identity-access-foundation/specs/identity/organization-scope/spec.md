## Purpose

Restringir el acceso empresarial a las organizaciones y subsidiarias autorizadas, incorporando tambien propiedad y asignacion del caso sin alterar el catalogo organizacional existente.

## ADDED Requirements

### Requirement: Alcance organizacional explicito
El sistema MUST asociar cada asignacion empresarial con una o mas organizaciones o con un alcance corporativo explicito, y MUST negar acceso fuera de ese limite.

#### Scenario: Acceso a subsidiaria asignada
- **WHEN** el usuario posee el permiso requerido y una asignacion vigente para la subsidiaria del recurso
- **THEN** el sistema permite continuar con las demas politicas de autorizacion

#### Scenario: Acceso cruzado no autorizado
- **WHEN** el usuario intenta consultar o modificar un recurso perteneciente a una subsidiaria no asignada
- **THEN** el sistema deniega la accion sin revelar datos del recurso

### Requirement: Alcance corporativo controlado
El sistema MUST conceder acceso global solo mediante una asignacion corporativa explicita y auditable, nunca por inferencia a partir del rol.

#### Scenario: Punto de Gobierno con alcance limitado
- **WHEN** un Punto de Contacto del Gobierno tiene asignadas solo dos subsidiarias
- **THEN** el sistema limita sus acciones empresariales a esas subsidiarias aunque el rol pueda operar globalmente en otros contextos

### Requirement: Propiedad y asignacion de caso
El sistema MUST poder restringir acciones a registros propios o solicitudes asignadas ademas del alcance organizacional.

#### Scenario: Evaluador no asignado
- **WHEN** un Arquitecto posee el permiso de evaluar pero no esta asignado a la solicitud
- **THEN** el sistema permite como maximo la consulta autorizada y deniega la ejecucion de la evaluacion

### Requirement: Cambios de alcance protegidos
El sistema MUST impedir que una persona amplie su propio alcance y MUST conservar vigencia y trazabilidad de cada asignacion organizacional.

#### Scenario: Autoampliacion de alcance
- **WHEN** un Administrador intenta agregarse una subsidiaria sin aprobacion independiente
- **THEN** el sistema deniega la modificacion y registra el intento privilegiado

### Requirement: Consultas e informes acotados
Las busquedas, paneles, conteos y exportaciones MUST aplicar el mismo alcance organizacional que las operaciones sobre registros individuales.

#### Scenario: Panel por rol
- **WHEN** un usuario abre su panel empresarial
- **THEN** los indicadores y elementos visibles incluyen unicamente datos dentro de sus permisos y alcances efectivos
