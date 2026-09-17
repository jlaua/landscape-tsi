## Context

`AuditController.Index` acepta `empresaSubsidiariaId` y transforma `UnauthorizedAccessException` en `NotFound`, causando el 404 observado. La consulta actual solo cubre `IamEventoAuditoriaAutorizacion` y escribe un evento de consulta. Las eliminaciones actuales usan `IDeletionImpactService` y no persisten snapshots completos.

## Goals / Non-Goals

**Goals:** consolidar trazabilidad funcional, corregir semántica HTTP, capturar snapshots atómicos y restaurar cascadas con autorización y auditoría append-only.

**Non-Goals:** aplicar DDL, migraciones, cambios de FK, purga de auditoría, restauración automática de datos existentes o modificación de SQL Server en esta etapa.

## Decisions

- Separar `audit.Operation` y `audit.RecordSnapshot` de las tablas IAM de autenticación/autorización; la UI puede proyectar una vista consolidada.
- Usar un identificador de operación (GUID), `CorrelationId`, snapshots JSON de PK/FK/row data y nombres funcionales snapshot del actor y registro.
- Reemplazar el parámetro técnico de subsidiaria por opciones funcionales autorizadas y filtros server-side.
- Convertir autorización fallida en 403; reservar 404 para recursos inexistentes.
- Mantener `IDeletionImpactService` como fuente del grafo y orden; el snapshot se captura antes de ejecutar los DELETE dentro de la misma transacción.
- No se restaurarán las PK/IDENTITY originales. Cada restore usará `INSERT` normal y mantendrá un mapa temporal y persistido de trazabilidad `OldPrimaryKey -> NewPrimaryKey`, identificado por operación y tabla.
- La restauración procesará padres antes que hijos: después de insertar cada padre se capturará la nueva PK; antes de insertar cada hijo se sustituirán sus FK antiguas por las nuevas. Si un padre no fue eliminado, se conservará su FK original. Las tablas puente N:M se restaurarán al final usando la PK nueva de cada extremo restaurado y la PK original de cada extremo no eliminado.
- Quedan expresamente prohibidos `SET IDENTITY_INSERT`, `ALTER TABLE`, `NOCHECK CONSTRAINT` y cambios automáticos a `ON DELETE CASCADE`.
- No permitir restore si existe conflicto de PK/UK/FK, esquema incompatible, snapshot incompleto, expiración o restauración previa.
- Permisos separados `Auditoria.Ver` y `Auditoria.Restaurar`; eventos sin UPDATE/DELETE desde la aplicación.
- Índices propuestos: OccurredAtUtc, ActorUserId, ActionType, EntityCode, EmpresaSubsidiariaId, CorrelationId y ReversesOperationId.

## Consulta inicial y filtros

`AuditQuery` usa valores nullable para filtros opcionales. Cuando no se
proporciona `dateFrom`/`dateTo`, la capa de aplicación obtiene la fecha local
configurada y construye un rango exclusivo del día siguiente; ambos límites
se convierten a UTC antes de componer la consulta parametrizada.

`AuditEntityRegistry` se deriva de `CatalogEntityMetadata` y solo expone
entidades marcadas como auditables y visibles. Las listas de acciones,
usuarios y subsidiarias también se resuelven en servidor dentro del alcance
autorizado. No se envía `*` a SQL y no se consulta `sys.tables` para poblar la
UI.

## Risks / Trade-offs

- [Snapshots JSON grandes] → limitar payload a columnas necesarias, comprimir solo tras medir y paginar detalle.
- [Cambios de esquema posteriores] → guardar SchemaVersion y validar columnas/tipos antes de restaurar.
- [IDENTITY y FK complejas] → insertar normalmente y validar el mapa OldId/NewId por tabla; no otorgar ALTER ni usar IDENTITY_INSERT desde runtime.
- [Restauración de datos recreados] → detectar conflictos y rechazar sin sobrescribir.
- [Alcance organizacional] → reevaluar autorización en consulta, preview y commit.

## Migration Plan

1. Aprobar OpenSpec y el diseño de tablas/permisos.
2. Crear entidades EF y migración propuesta sin aplicarla automáticamente.
3. Revisar índices, schema `audit` y privilegios mínimos.
4. Implementar auditoría y snapshots en Development con pruebas transaccionales.
5. Validar restore simple y cascada en base temporal.
6. Promover esquema y código por el flujo DEV → pruebas → PROD.
7. Rollback de código mediante despliegue anterior; rollback de datos mediante transacción fallida o restauración controlada, nunca mediante rollback ficticio de un commit anterior.
