## Purpose

Proporcionar autenticacion corporativa y local de respaldo con sesiones seguras, una identidad interna comun y proteccion rigurosa de credenciales para cada actor de Landscape TSI.

## ADDED Requirements

### Requirement: Autenticacion federada
El sistema MUST autenticar a los usuarios mediante un proveedor OpenID Connect configurado y MUST denegar el acceso empresarial a identidades no autenticadas.

#### Scenario: Inicio de sesion satisfactorio
- **WHEN** el proveedor valida una identidad y devuelve las reclamaciones requeridas
- **THEN** el sistema establece una sesion autenticada vinculada al perfil local correspondiente

#### Scenario: Autenticacion fallida
- **WHEN** el proveedor rechaza la autenticacion o no entrega una identidad valida
- **THEN** el sistema no crea una sesion y muestra un resultado seguro que no revela detalles internos

### Requirement: Autenticacion local con ASP.NET Core Identity
El sistema MUST permitir autenticacion local mediante usuario y contrasena usando ASP.NET Core Identity, MUST almacenar exclusivamente el hash producido por Identity y MUST aplicar politicas configurables de contrasena, bloqueo por intentos fallidos y proteccion contra fuerza bruta.

#### Scenario: Inicio local satisfactorio
- **WHEN** un usuario local activo presenta credenciales validas y no esta bloqueado
- **THEN** el sistema establece una sesion vinculada a la misma identidad interna utilizada por OAuth

#### Scenario: Credenciales locales invalidas
- **WHEN** el usuario no existe, la contrasena es incorrecta o la cuenta no puede iniciar sesion
- **THEN** el sistema no crea una sesion, contabiliza el intento conforme a la politica y muestra `Usuario o contraseña incorrectos.` sin distinguir la causa

#### Scenario: Cuenta bloqueada
- **WHEN** se supera el umbral configurable de intentos fallidos
- **THEN** el sistema bloquea temporalmente el acceso local y mantiene una respuesta que no facilita enumerar usuarios

### Requirement: Identidad interna comun
El sistema MUST hacer que OAuth y la autenticacion local produzcan la misma representacion interna de usuario, permisos y alcance, y MUST NOT crear un modelo paralelo de roles para cuentas OAuth o locales.

#### Scenario: Autorizacion independiente del mecanismo
- **WHEN** una persona se autentica mediante cualquiera de los mecanismos y esta vinculada al mismo usuario interno
- **THEN** el servidor evalua los mismos roles funcionales, permisos, politicas y alcances

### Requirement: Vinculacion con usuario local
El sistema MUST vincular la identidad externa con un unico usuario local activo utilizando emisor y sujeto estables, y no solamente correo o nombre visible.

#### Scenario: Identidad conocida y activa
- **WHEN** una identidad autenticada coincide con un usuario local activo
- **THEN** el sistema permite continuar y carga sus permisos y alcances vigentes

#### Scenario: Usuario ausente o inactivo
- **WHEN** la identidad no tiene usuario local habilitado o el usuario esta suspendido
- **THEN** el sistema deniega el acceso empresarial y registra la decision

### Requirement: Proteccion de credenciales y tokens
El sistema MUST NOT almacenar contrasenas, secretos de bootstrap ni tokens federados en texto claro y MUST proteger hashes, credenciales de sesion y tokens contra divulgacion y reutilizacion.

#### Scenario: Persistencia del perfil
- **WHEN** se crea o actualiza el perfil local
- **THEN** solo se conservan identificadores y atributos minimos necesarios, nunca la contrasena del proveedor

#### Scenario: Persistencia de credencial local
- **WHEN** se crea o cambia una contrasena local
- **THEN** solo se persiste el hash generado por ASP.NET Core Identity y ningun log o evento contiene la contrasena

### Requirement: Recuperacion administrativa local fuera de la interfaz
La utilidad administrativa aislada MUST operar solo con una politica explicita de ambiente y base autorizada, MUST utilizar `UserManager` para recuperar la cuenta local y MUST rechazar cualquier ambiente o base no declarados. MUST solicitar la nueva contraseña interactivamente, sin argumentos, eco, almacenamiento ni registro.

#### Scenario: Recuperacion en Development o Staging autorizado
- **WHEN** la utilidad se ejecuta en Development con `db-landscape-tsi-dev` o en Staging con la misma base explicitamente autorizada
- **THEN** muestra ambiente, servidor, base y usuario, solicita confirmacion reforzada fuera de Development y ejecuta `FindByNameAsync`, `GeneratePasswordResetTokenAsync` y `ResetPasswordAsync` mediante ASP.NET Core Identity

#### Scenario: Production sin base autorizada
- **WHEN** la utilidad se ejecuta con `ASPNETCORE_ENVIRONMENT=Production` sin una entrada productiva explicita en la politica
- **THEN** rechaza la operacion antes de buscar al usuario o solicitar la contraseña

#### Scenario: Credencial no expuesta
- **WHEN** se completa o falla una recuperacion administrativa
- **THEN** la contraseña, confirmacion, hash y token no aparecen en consola, logs, argumentos, archivos ni auditoria

### Requirement: Bootstrap administrativo condicionado
El sistema MUST poder crear idempotentemente los usuarios locales `jean` y `administrador` con el rol funcional `Administrador del Sistema` solo en entornos expresamente autorizados y solo cuando la contrasena se obtiene de User Secrets o `LANDSCAPE_TSI_BOOTSTRAP_ADMIN_PASSWORD`.

#### Scenario: Primera ejecucion autorizada
- **WHEN** el bootstrap esta habilitado, existe el secreto y ninguno de los usuarios existe
- **THEN** el sistema crea ambos mediante ASP.NET Core Identity, asigna el rol funcional y audita el resultado sin registrar el secreto

#### Scenario: Ejecucion repetida
- **WHEN** un usuario creado por bootstrap ya existe
- **THEN** el sistema no lo duplica, no sobrescribe su contrasena y conserva la asignacion aprobada del rol

#### Scenario: Usuario preexistente administrado manualmente
- **WHEN** `jean` o `administrador` ya existe y no consta como creado por bootstrap
- **THEN** el sistema no cambia su contrasena ni eleva silenciosamente sus privilegios y registra una advertencia administrativa para revision

#### Scenario: Secreto ausente
- **WHEN** no existe una fuente de contrasena configurada
- **THEN** la aplicacion continua de forma segura o detiene solo el bootstrap, registra una instruccion administrativa sin valor sensible y nunca genera una contrasena predeterminada

#### Scenario: Produccion sin habilitacion explicita
- **WHEN** la aplicacion se ejecuta en produccion sin una autorizacion de bootstrap explicita
- **THEN** el sistema no crea ni modifica usuarios locales aunque exista la variable de entorno

### Requirement: Auditoria de autenticacion
El sistema MUST registrar login exitoso y fallido con usuario o identificador seguro, fecha y hora, mecanismo y resultado, y MUST NOT almacenar contrasenas, secretos, hashes ni tokens OAuth completos.

#### Scenario: Fallo local auditado
- **WHEN** falla un inicio de sesion local
- **THEN** el sistema registra mecanismo local y resultado sin registrar la contrasena ni revelar al cliente si el usuario existe

#### Scenario: Login corporativo auditado
- **WHEN** termina un intento OAuth satisfactorio o fallido
- **THEN** el sistema registra mecanismo corporativo y resultado sin almacenar el token completo

### Requirement: Ciclo de vida de la sesion
El sistema MUST finalizar la sesion por cierre explicito, expiracion o invalidacion del usuario, y MUST volver a evaluar acceso sensible cuando cambien permisos o alcances.

#### Scenario: Cierre de sesion
- **WHEN** el usuario solicita cerrar sesion
- **THEN** el sistema invalida la sesion local y ejecuta el cierre federado cuando el proveedor lo soporte

#### Scenario: Usuario suspendido durante una sesion
- **WHEN** se detecta que el usuario autenticado fue suspendido
- **THEN** el sistema impide nuevas acciones protegidas y exige una nueva autenticacion valida

### Requirement: Experiencia accesible de autenticacion
La interfaz de inicio, cierre y denegacion de acceso MUST aplicar Material Design 3, ser adaptable para escritorio, tableta y movil, operable por teclado, comprensible y conforme con WCAG 2.2 AA, mostrando claramente ingreso corporativo e ingreso local.

#### Scenario: Acceso desde dispositivo movil con teclado
- **WHEN** una persona navega por la experiencia de autenticacion en una pantalla pequena usando teclado
- **THEN** el foco, las instrucciones y los mensajes permanecen visibles, ordenados y accionables sin depender solo del color

#### Scenario: Eleccion de mecanismo
- **WHEN** una persona abre la pantalla de acceso
- **THEN** encuentra `Continuar con cuenta corporativa` y un formulario local con Usuario, Contraseña e `Ingresar`, con nombres accesibles y orden de foco coherente
