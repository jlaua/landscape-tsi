## 1. Contrato y routing de auditoría

- [x] 1.1 Corregir `AuditController` para distinguir 403, 404 y errores de consulta, y cubrir `/Audit` con filtros válidos mediante pruebas de routing.
- [x] 1.2 Definir DTO/ViewModel consolidado con filtros funcionales, nombres de entidad/subsidiaria y paginación server-side; verificar consultas SQL paginadas y sin IDs técnicos en la UI.
- [x] 1.2b Implementar carga inicial del día, conversión de límites locales a UTC, valores por defecto/limpiar filtros y filtros selectivos nullable sin usar `*` en SQL.
- [x] 1.3 Implementar detalle `/Audit/Operations/{operationId}` append-only y verificar autorización, alcance y respuesta 404 solo para operación inexistente.

## 2. Modelo persistente propuesto

- [x] 2.1 Crear entidades `audit.Operation` y `audit.RecordSnapshot` con SchemaVersion, correlación, actor snapshot, órdenes y referencias de reversión; verificar que no contengan secretos.
- [x] 2.2 Diseñar índices y restricciones FK/UK, documentar permisos mínimos del schema `audit` y verificar que no se cree ni altere SQL automáticamente.
- [x] 2.3 Generar una migración EF propuesta sin aplicarla; verificar que el script sea revisable y no modifique catalogos funcionales.

## 3. Auditoría de operaciones

- [x] 3.1 Extender `IDeletionImpactService` para capturar snapshots completos antes de DELETE dentro de la misma transacción; verificar rollback cuando falle un snapshot.
- [x] 3.2 Registrar CREATE, UPDATE y DELETE funcionales con nombres snapshot, impacto y resultado; verificar inmutabilidad de eventos.
- [x] 3.3 Incorporar configuración `Audit:UndoRetentionDays` y estados CanUndo/Restored/Expired/Error; verificar retención sin purga automática.

## 4. Restauración segura

- [x] 4.1 Implementar Restore Impact Preview con detección de snapshots incompletos, conflictos PK/UK/FK, esquema incompatible y restauraciones previas; verificar que no sobrescriba datos.
- [x] 4.2 Implementar el mapa `OldPrimaryKey -> NewPrimaryKey` por operación y tabla usando INSERT normales; verificar que no se use IDENTITY_INSERT, ALTER ni privilegios estructurales.
- [x] 4.3 Implementar restauración padres→hijos→puentes sustituyendo FK restauradas y conservando FK de padres no eliminados; verificar rollback completo y FK válidas.
- [x] 4.4 Aplicar permisos `Auditoria.Ver` y `Auditoria.Restaurar`, alcance organizacional y segregación de funciones; verificar 403 para usuarios no autorizados.

## 5. Interfaz y pruebas

- [x] 5.1 Rediseñar `/Audit` con filtros funcionales, badges accesibles, estados y paginación; verificar responsive y WCAG 2.2 AA.
- [x] 5.2 Añadir detalle expandible de impacto y botón Deshacer condicionado por CanUndo; verificar que DELETE original permanezca visible.
- [x] 5.3 Agregar pruebas de DELETE simple/cascada, snapshot, fallos transaccionales, restore, conflictos, retención, filtros, routing y auditoría inmutable.
- [x] 5.4 Ejecutar `dotnet build`, `dotnet test` y pruebas de integración contra base temporal; verificar que no se ejecuten escrituras en PROD.
