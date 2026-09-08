## Purpose

Permitir restaurar eliminaciones confirmadas mediante snapshots completos y transacciones nuevas, sin simular un rollback de una transacción ya comprometida.

## ADDED Requirements

### Requirement: Snapshot atómico antes de DELETE
El sistema SHALL capturar, dentro de la misma transacción que elimina, todos los registros raíz, descendientes y tablas puente, incluidos datos de columnas y relaciones necesarias para reconstruirlos.

#### Scenario: Falla al capturar snapshot
- **WHEN** cualquier snapshot no puede persistirse
- **THEN** la transacción revierte y no se elimina ningún registro.

### Requirement: Orden de eliminación y restauración
El sistema SHALL guardar `DeleteOrder` y `RestoreOrder`, eliminando hojas antes de padres y restaurando padres antes de hijos. No SHALL requerir conservar las PK originales.

#### Scenario: Cascada completa
- **WHEN** se elimina una entidad con dependencias multinivel
- **THEN** todos los registros se eliminan en orden seguro y pueden restaurarse en orden inverso sin deshabilitar FK.

### Requirement: Remapeo de claves y foreign keys
El sistema SHALL restaurar mediante `INSERT` normales, generar nuevas PK cuando SQL Server lo requiera y mantener un mapa `OldPrimaryKey -> NewPrimaryKey` por tabla y operación. Antes de insertar cada hijo SHALL sustituir sus FK antiguas por las PK nuevas de padres restaurados; si el padre no fue eliminado, SHALL conservar la FK original.

#### Scenario: Padre e hijo restaurados
- **WHEN** se restaura una Capacidad antigua 50 y una Funcionalidad antigua 2309 que referenciaba 50
- **THEN** se insertan nuevas PK, por ejemplo 181 y 5012, y la funcionalidad queda con `idCapacidad = 181`, conservando el mapa 50→181 y 2309→5012.

#### Scenario: Padre no eliminado
- **WHEN** solo se restaura una Funcionalidad y su Capacidad original continúa existente
- **THEN** la nueva funcionalidad conserva la FK hacia la PK original de la Capacidad y no se crea un padre duplicado.

#### Scenario: Tabla puente parcialmente remapeada
- **WHEN** una relación N:M tiene un extremo restaurado y otro no eliminado
- **THEN** la fila puente usa la nueva PK del extremo restaurado y la PK original del otro extremo.

#### Scenario: Restricciones de identidad
- **WHEN** se ejecuta un RESTORE
- **THEN** no se utiliza `SET IDENTITY_INSERT`, `ALTER TABLE`, `NOCHECK CONSTRAINT` ni `ON DELETE CASCADE` automático.

### Requirement: Preview y confirmación de restore
El sistema SHALL comprobar snapshots completos, retención, compatibilidad de esquema, conflictos de PK/UK/FK y estado de restauración antes de permitir `Deshacer`.

#### Scenario: Snapshot restaurable
- **WHEN** el usuario autorizado confirma un restore válido
- **THEN** el sistema restaura todos los registros en una única transacción y registra una operación RESTORE vinculada al DELETE original.

#### Scenario: Conflicto de clave
- **WHEN** una PK o restricción necesaria ya está ocupada
- **THEN** el restore se rechaza sin sobrescribir datos y sin cambios parciales.

### Requirement: Estados append-only y retención
El sistema SHALL conservar el DELETE original, marcar el restore como operación separada y calcular `CanUndo` según `Audit:UndoRetentionDays`; no borrará automáticamente historial.

#### Scenario: Operación ya restaurada
- **WHEN** se consulta un DELETE que ya tiene RESTORE exitoso
- **THEN** `Deshacer` queda deshabilitado y se muestra quién y cuándo restauró.

### Requirement: Autorización de restauración
El sistema SHALL exigir `Auditoria.Restaurar`, alcance organizacional y las comprobaciones de segregación de funciones configuradas.

#### Scenario: Usuario solo lector
- **WHEN** un usuario con `Auditoria.Ver` pero sin `Auditoria.Restaurar` abre un DELETE
- **THEN** puede ver el detalle, pero no puede ejecutar ni confirmar el restore.
