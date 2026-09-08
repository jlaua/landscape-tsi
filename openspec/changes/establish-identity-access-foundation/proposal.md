## Why

Landscape TSI necesita una identidad empresarial y una autorizacion consistente antes de habilitar catalogos editables y el flujo de adopcion. La base de datos de produccion solo ofrece usuarios y roles tecnicos de SQL Server; no contiene usuarios funcionales, permisos atomicos, alcance por subsidiaria, separacion de funciones ni auditoria de decisiones de autorizacion.

## What Changes

- Introducir autenticacion dual: OAuth/OpenID Connect corporativo mediante un proveedor configurable y autenticacion local de respaldo mediante ASP.NET Core Identity.
- Crear una identidad interna unica capaz de vincular una identidad corporativa, una credencial local opcional o ambas, y administrar su estado, vigencia y datos minimos.
- Incorporar un bootstrap local idempotente y condicionado por ambiente para `jean` y `administrador`, sin contrasenas predeterminadas ni secretos versionados.
- Incorporar una utilidad CLI aislada de recuperacion administrativa de contrasena para `jean`, gobernada por una allowlist explicita de ambiente y base, confirmacion reforzada fuera de Development y sin bypass de autenticacion.
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
- **Aplicacion:** ASP.NET Core MVC requerira autenticacion OIDC y ASP.NET Core Identity, autorizacion por politicas, manejo comun de sesiones, filtros de alcance y servicios de auditoria. Entity Framework Core administrara el modelo local de autenticacion y autorizacion.
- **Base de datos:** se anticipan nuevas estructuras para usuario Identity, login externo, rol funcional, permiso, membresia de rol, alcance organizacional y auditoria, ademas de claves e indices asociados. La propuesta no altera las 25 tablas funcionales actuales salvo referencias opcionales futuras hacia actores; cualquier DDL requerira revision y autorizacion operativa independiente y nunca se ejecutara contra `db-landscape-tsi` desde este cambio.
- **Seguridad:** minimo privilegio, denegacion predeterminada, separacion entre administracion y decisiones empresariales, control de cuatro ojos para elevaciones y acciones de emergencia auditadas.
- **Privacidad:** se almacenaran identificadores externos, perfil minimo y exclusivamente hashes de contrasena producidos por ASP.NET Core Identity; nunca contrasenas, secretos ni tokens de acceso en texto claro.
- **Recuperacion administrativa:** la CLI utilizara UserManager y User Secrets, mostrara solamente ambiente, servidor, base y usuario objetivo, y registrara una auditoria tecnica sin contrasena, hash ni token. Development y Staging apuntaran explicitamente a `db-landscape-tsi-dev`; Production permanecera rechazado hasta contar con una base autorizada en la politica.
- **UI/UX:** se necesitara una pantalla de acceso empresarial Material Design 3 con alternativas corporativa y local, inicio/cierre de sesion accesibles, navegacion y paneles segun permisos, administracion adaptable de usuarios y roles y cumplimiento de WCAG 2.2 AA.
- **Dependencias:** proveedor OIDC corporativo configurable; la seleccion concreta del proveedor y sus reclamaciones se validara antes de implementar integraciones de produccion.

## Estado de implementación

Implementación pausada por decisión del Product Owner. La autenticación corporativa OIDC y las tareas IAM restantes se posponen para una fase futura.

Se conserva la autenticación local, los usuarios bootstrap existentes, los roles, permisos, alcance organizacional y auditoría ya implementados. El cambio permanece abierto con 36/56 tareas completadas y no se archiva.
