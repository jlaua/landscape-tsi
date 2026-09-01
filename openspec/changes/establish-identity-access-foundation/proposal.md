## Why

Landscape TSI necesita una identidad empresarial y una autorizacion consistente antes de habilitar catalogos editables y el flujo de adopcion. La base de datos de produccion solo ofrece usuarios y roles tecnicos de SQL Server; no contiene usuarios funcionales, permisos atomicos, alcance por subsidiaria, separacion de funciones ni auditoria de decisiones de autorizacion.

## What Changes

- Introducir autenticacion federada mediante un proveedor OpenID Connect configurable, sin almacenar contrasenas locales en Landscape TSI.
- Crear un perfil local de usuario vinculado a la identidad corporativa y administrar su estado, vigencia y datos minimos.
- Definir los roles iniciales Administrador, Arquitecto de Seguridad, Ingeniero de TSI y Punto de Contacto del Gobierno (SPOC) como paquetes de permisos, no como la unica fuente de autorizacion.
- Definir permisos atomicos para catalogo, tecnologia, adopcion, evaluacion, gobernanza, auditoria y administracion.
- Evaluar politicas contextuales que combinen permiso, organizacion o subsidiaria, asignacion del caso, propiedad, estado del flujo y separacion de funciones.
- Registrar de forma inmutable las decisiones de autorizacion relevantes, los rechazos de acciones privilegiadas y todas las elevaciones administrativas.
- Proponer las estructuras persistentes minimas para usuarios, roles, permisos, asignaciones organizacionales y auditoria, preservando las tablas funcionales existentes.
- Mantener fuera de alcance la ejecucion de migraciones y cualquier cambio sobre la base de datos de produccion.

## Capabilities

### New Capabilities

- `identity/authentication`: Inicio y cierre de sesion federados, vinculacion de identidad y control del ciclo de vida de la sesion.
- `identity/access-management`: Usuarios funcionales, roles, permisos y administracion segura de asignaciones.
- `identity/organization-scope`: Restriccion de acceso por organizacion o subsidiaria, propiedad y asignacion de caso.
- `identity/policy-authorization`: Evaluacion de permisos, estado del flujo y reglas de separacion de funciones para cada accion protegida.
- `identity/authorization-audit`: Registro y consulta controlada de decisiones y cambios de autorizacion.

### Modified Capabilities

- Ninguna. No existen especificaciones principales previas que deban modificarse.

## Impact

- **Modulos:** se introduce un modulo de Identidad y Acceso con contratos consumidos por Catalogo, Tecnologia, Organizacion, Adopcion, Evaluacion, Gobernanza, Informes y Administracion dentro del monolito modular.
- **Aplicacion:** ASP.NET Core MVC requerira autenticacion OIDC, autorizacion por politicas, manejo de sesiones, filtros de alcance y servicios de auditoria. Entity Framework Core administrara el modelo local de autorizacion.
- **Base de datos:** se anticipan nuevas tablas para usuario, rol, permiso, membresia de rol, alcance organizacional y auditoria de autorizacion, ademas de claves e indices asociados. La propuesta no altera las 25 tablas funcionales actuales salvo referencias opcionales futuras hacia actores; cualquier DDL requerira revision y autorizacion operativa independiente.
- **Seguridad:** minimo privilegio, denegacion predeterminada, separacion entre administracion y decisiones empresariales, control de cuatro ojos para elevaciones y acciones de emergencia auditadas.
- **Privacidad:** se almacenara solo el identificador externo y perfil minimo necesario; no se persistiran contrasenas ni tokens de acceso en texto claro.
- **UI/UX:** se necesitaran inicio/cierre de sesion accesibles, navegacion y paneles segun permisos, administracion adaptable de usuarios y roles, mensajes claros de acceso denegado y cumplimiento de WCAG 2.2 AA.
- **Dependencias:** proveedor OIDC corporativo configurable; la seleccion concreta del proveedor y sus reclamaciones se validara antes de implementar integraciones de produccion.
