## Purpose

Proporcionar una trazabilidad funcional consolidada y segura de las acciones administrativas, autenticación y autorización dentro del alcance organizacional permitido.

## ADDED Requirements

### Requirement: Auditoría funcional consolidada
El sistema SHALL presentar eventos CREATE, UPDATE, DELETE y RESTORE, y podrá integrar LOGIN, LOGIN_FAILED, LOGOUT y AUTHORIZATION_DENIED sin exponer secretos.

#### Scenario: Consulta inicial
- **WHEN** un usuario autorizado visita `/Audit`
- **THEN** el sistema muestra el dashboard sin exigir un identificador técnico de subsidiaria.

### Requirement: Filtros funcionales y paginación
El sistema SHALL filtrar por texto, acción, entidad, usuario, subsidiaria funcional y rango UTC, aplicando paginación y orden descendente por fecha en servidor.

#### Scenario: Carga inicial del día
- **WHEN** un usuario autorizado visita `/Audit` sin parámetros
- **THEN** el sistema usa la fecha local actual como `dateFrom` y `dateTo`, consulta el intervalo UTC `[inicioDelDía, inicioDelDíaSiguiente)` y muestra la primera página ordenada por `OccurredAtUtc DESC`.

#### Scenario: Filtros opcionales vacíos
- **WHEN** búsqueda, acción, entidad, usuario o subsidiaria están vacíos o en `Todas`
- **THEN** el servidor no agrega el predicado correspondiente y conserva los demás filtros mediante parámetros.

#### Scenario: Entidad seleccionable
- **WHEN** se abre el filtro Entidad
- **THEN** solo aparecen entidades de `AuditEntityRegistry` marcadas como auditables y visibles; la opción `Todas` se representa internamente como `null`.

#### Scenario: Límites de fecha
- **WHEN** el usuario selecciona un día local
- **THEN** los eventos hasta `23:59:59.999...` de ese día se incluyen y el inicio del día siguiente se usa como límite exclusivo después de convertir ambos valores a UTC.

#### Scenario: Subsidiaria funcional
- **WHEN** el usuario selecciona una subsidiaria visible para su alcance
- **THEN** la consulta utiliza internamente su clave, pero la interfaz muestra el nombre funcional.

#### Scenario: Filtro no autorizado
- **WHEN** se solicita una subsidiaria fuera del alcance
- **THEN** el sistema responde 403 o muestra una consulta vacía segura, nunca 404 por convertir autorización en inexistencia.

### Requirement: Detalle inmutable de operación
El sistema SHALL ofrecer `/Audit/Operations/{operationId}` para mostrar actor, acción, entidad, registro, impacto, estado, correlación y subsidiaria, sin permitir editar ni borrar eventos.

#### Scenario: Operación inexistente
- **WHEN** se solicita un identificador de operación inexistente
- **THEN** el sistema responde 404 únicamente para ese recurso inexistente.

### Requirement: Autorización y alcance
El sistema SHALL proteger la consulta con `Auditoria.Ver` y aplicar alcance organizacional; la restauración SHALL requerir además `Auditoria.Restaurar`.

#### Scenario: Usuario sin permiso
- **WHEN** un usuario sin `Auditoria.Ver` solicita `/Audit`
- **THEN** el sistema responde 403 y no revela eventos.
