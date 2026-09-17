## Purpose

Proteger toda consulta y mutación de tablas maestras mediante permisos, políticas del servidor, alcance corporativo y auditoría íntegra, con denegación predeterminada.

## ADDED Requirements

### Requirement: Permisos atómicos por operación
El sistema MUST exigir `Catalogos.Ver`, `Catalogos.Crear`, `Catalogos.Editar` o `Catalogos.Desactivar` según la acción y MUST evaluar permisos y políticas en el servidor, sin autorizar únicamente por nombre de rol.

#### Scenario: Rol inicial con permiso efectivo
- **WHEN** un Administrador del Sistema o Arquitecto de Seguridad Corporativo posee el permiso, alcance y condiciones vigentes para una acción
- **THEN** el servidor permite continuar con las validaciones de negocio

#### Scenario: Botón oculto manipulado
- **WHEN** un usuario sin el permiso requerido invoca directamente una ruta de mantenimiento
- **THEN** el servidor deniega la acción y no realiza cambios aunque el cliente haya construido la solicitud manualmente

### Requirement: Responsabilidades y separación de funciones
El sistema MUST reconocer al Arquitecto de Seguridad Corporativo como responsable funcional y al Administrador del Sistema como responsable técnico y operativo, y MUST impedir que una persona apruebe su propia elevación o altere sus propios permisos para mantener catálogos.

#### Scenario: Mantenimiento funcional autorizado
- **WHEN** el Arquitecto posee el permiso efectivo y modifica un catálogo funcional habilitado
- **THEN** el sistema registra la operación bajo su responsabilidad funcional

#### Scenario: Autoelevación administrativa
- **WHEN** un Administrador intenta concederse el permiso o alcance que necesita para ejecutar una operación actualmente denegada
- **THEN** el sistema bloquea la autoelevación conforme al fundamento de Identidad y Acceso

### Requirement: Alcance corporativo para catálogos compartidos
El sistema MUST exigir alcance corporativo explícito para modificar catálogos compartidos y `TEmpresaSubsidiaria`; un rol o acceso a una subsidiaria aislada MUST NOT implicar mantenimiento global.

#### Scenario: Alcance corporativo vigente
- **WHEN** el actor posee el permiso requerido y alcance corporativo explícito
- **THEN** el servidor evalúa las demás políticas antes de autorizar la mutación

#### Scenario: Alcance solo subsidiario
- **WHEN** un actor limitado a una subsidiaria intenta modificar el catálogo corporativo de empresas o cualquier catálogo compartido
- **THEN** el servidor deniega la operación sin ampliar su alcance por inferencia

### Requirement: Validación de catálogo y campos en servidor
El sistema MUST resolver códigos de catálogo, campos visibles, campos editables, permisos y estrategia de retiro desde una definición controlada por el servidor y MUST prevenir overposting y acceso a objetos fuera de la lista blanca.

#### Scenario: Campo no editable agregado a la solicitud
- **WHEN** el cliente agrega una PK, FK no expuesta o columna no declarada como editable
- **THEN** el servidor rechaza la entrada y no asigna el valor

### Requirement: Auditoría de mantenimiento
El sistema MUST registrar actor, fecha y hora, catálogo funcional, identificador interno protegido, operación, valores anteriores y nuevos permitidos, resultado y correlación para cada intento de creación, edición o retiro.

#### Scenario: Edición exitosa
- **WHEN** una modificación se confirma
- **THEN** el cambio y su evento de auditoría se conservan de forma atómica antes de responder éxito

#### Scenario: Mutación denegada
- **WHEN** una política, validación o regla de integridad rechaza una operación privilegiada
- **THEN** el sistema registra el intento y su motivo seguro sin credenciales, tokens ni contenido innecesario

### Requirement: Falla cerrada sin auditoría confiable
El sistema MUST impedir mutaciones si el servicio de autorización o el mecanismo de auditoría obligatoria no están disponibles; las consultas podrán continuar solo si su política y sensibilidad lo permiten.

#### Scenario: Auditoría no disponible
- **WHEN** un usuario intenta crear o editar y no puede garantizarse el evento de auditoría requerido
- **THEN** el sistema no confirma la mutación y presenta un error operativo trazable

### Requirement: Protección de secretos y producción
El sistema MUST obtener credenciales mediante configuración segura, MUST NOT registrar secretos y MUST NOT ejecutar migraciones ni comandos de mantenimiento automatizados contra la base SQL Server de producción.

#### Scenario: Registro de error de conexión
- **WHEN** falla una operación de infraestructura
- **THEN** los registros identifican la correlación y el tipo de fallo sin exponer cadena de conexión, usuario SQL ni contraseña

