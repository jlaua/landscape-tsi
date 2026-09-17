# Plan de ejecución canónica (pendiente de aprobación final)

**Destino único autorizado para escrituras:** `db-landscape-tsi-reconcile`.
`db-landscape-tsi` y `db-landscape-tsi-dev` se consultan únicamente como
fuentes de lectura.

## Auditoría y trazabilidad

La auditoría existente `IamEventoAuditoriaAutorizacion` es append-only y
registra actor, permiso, recurso, resultado, correlación y JSON antes/después.
Es adecuada para auditar la operación administrativa, pero no para una
relación estable de cada PK origen con su nueva PK destino.

Por ello se propone, sin crearla todavía, una estructura mínima dedicada:

`dbo.ReconciliacionTrazabilidad`:

| Columna | Tipo propuesto | Regla |
|---|---|---|
| `Id` | `bigint IDENTITY` | PK técnica |
| `SourceDatabase` | `sysname` | siempre `db-landscape-tsi-dev` |
| `SourceSchema` | `sysname` | `dbo` |
| `SourceTable` | `sysname` | lista blanca |
| `SourcePrimaryKey` | `nvarchar(200)` | PK DEV serializada, no secreto |
| `TargetDatabase` | `sysname` | siempre `db-landscape-tsi-reconcile` |
| `TargetSchema` | `sysname` | `dbo` |
| `TargetTable` | `sysname` | lista blanca |
| `TargetPrimaryKey` | `nvarchar(200)` | PK canónica generada |
| `OccurredAtUtc` | `datetime2(7)` | UTC |
| `Operation` | `nvarchar(32)` | `PROMOTE` |
| `Result` | `nvarchar(32)` | `SUCCEEDED`/`FAILED` |
| `CorrelationId` | `uniqueidentifier` | correlación de ejecución |

Índice único recomendado: origen + tabla + PK + operación. La tabla no se
creará en esta etapa. Cada operación también deberá emitir un evento resumido
en `IamEventoAuditoriaAutorizacion`, sin contraseñas, hashes ni tokens.

## Secuencia controlada

| Paso | Script | Operación | Tablas potencialmente modificadas | Esperado | Transacción / rollback |
|---:|---|---|---|---:|---|
| 0 | preflight externo | Validar backup restaurable, servidor y base destino | ninguna | backup verificado | detener si falla |
| 1 | `03-apply-iam-schema.sql` | Ejecutar migración EF versionada | tablas IAM y `__EFMigrationsHistory` | migración `20260901215337_InitialIdentityAccess` | migración EF; restaurar backup si falla |
| 2 | `08-validation.sql` | Validar PK, FK, índices, checks y migración | ninguna | 13 tablas IAM + constraints | solo lectura |
| 3 | `04-sync-iam-master-data.sql` | Copiar permisos, roles y usuarios desde DEV | `IamPermiso`, `IamRol`, `IamUsuario` | 15, 4 y 2 filas | transacción; rollback completo |
| 4 | `05-sync-iam-relations.sql` | Copiar relaciones Identity | `IamRolPermiso`, `IamUsuarioRol`, claims, logins, org., tokens | 18, 2 y ceros en tablas vacías | transacción; rollback completo |
| 5 | `06-sync-iam-audit.sql` | Copiar auditoría histórica | `IamEventoAutenticacion`, `IamEventoAuditoriaAutorizacion`, `IamAccesoEmergencia` | 60, 5 y 0 filas | transacción; rollback completo |
| 6 | `07-sync-approved-functional-data.sql` | Promover funcionalidades DEV 2,3,4,5,6,8 | `TFuncionalidad`, trazabilidad propuesta, auditoría IAM | 6 nuevas filas | transacción; rollback completo |
| 7 | `09-orphan-check.sql` | Revisar huérfanos | ninguna | 0 huérfanos | solo lectura |
| 8 | validación final | `DBCC CHECKCONSTRAINTS` y conteos | ninguna | sin errores; conteos esperados | detener si falla |

## Guardas obligatorias

- Cada script de escritura exige `SESSION_CONTEXT('reconciliation_approved') = 1`.
- El script aborta si la base actual no es `db-landscape-tsi-reconcile`.
- No se aceptan nombres de tabla desde parámetros HTTP o consola.
- No se usa `IDENTITY_INSERT` para `TFuncionalidad`.
- Las FK de las seis funcionalidades se resuelven por nombre funcional en el
  destino y se aborta ante cero o múltiples coincidencias.
- No se copian DEV PK 1 ni 7.
- No se imprimen contraseñas, hashes, tokens ni cadenas de conexión.

## Rollback

Antes de iniciar: backup verificable del destino. Cada bloque de datos usa
`SET XACT_ABORT ON`, `BEGIN TRANSACTION` y `ROLLBACK` ante cualquier error.
Después de un commit parcial de migración EF, el rollback operativo es
restaurar el backup o aplicar una migración de reversión aprobada; no se hará
`DROP` manual de tablas IAM.

## Estado

Los scripts se han preparado y endurecido con guardas, pero no se ha ejecutado
ninguna escritura. La ejecución requiere aprobación final del usuario y
confirmación del diseño de `ReconciliacionTrazabilidad`.
