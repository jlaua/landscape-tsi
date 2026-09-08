## Why

El módulo `/Audit` solo consulta eventos de autorización y convierte un alcance no autorizado en HTTP 404. Además, las eliminaciones confirmadas no tienen trazabilidad funcional ni un mecanismo seguro de restauración. Se necesita una auditoría consolidada y restauración transaccional basada en snapshots previos al borrado.

## What Changes

- Corregir el contrato de `/Audit` para que filtros válidos nunca produzcan 404 y devolver 403 cuando corresponda.
- Reemplazar el formulario basado en IDs por filtros funcionales, paginación server-side y alcance organizacional.
- Incorporar eventos funcionales CREATE, UPDATE, DELETE y RESTORE, consolidables con autenticación/autorización.
- Capturar snapshots completos antes de eliminaciones simples o cascadas.
- Añadir preview, autorización y restauración transaccional append-only mediante remapeo de PK antiguas a nuevas PK generadas por SQL Server.
- Mantener la eliminación y restauración protegidas por permisos separados.
- No modificar DDL ni aplicar migraciones en esta propuesta.

### Mejora: consulta inicial y filtros funcionales

- `/Audit` debe cargar automáticamente los eventos del día actual autorizado.
- Las fechas visibles representan el día local, pero los límites se convierten
  a UTC como `[hoy 00:00 local, mañana 00:00 local)`.
- Entidad, acción, usuario y subsidiaria se seleccionan mediante opciones
  funcionales permitidas; los valores vacíos no agregan predicados SQL.
- La consulta mantiene paginación server-side y orden `OccurredAtUtc DESC`.

## Capabilities

### New Capabilities

- `audit/functional-traceability`: consulta consolidada, detalle, filtros, paginación y alcance organizacional.
- `audit/reversible-deletions`: snapshots pre-delete, preview de restore y restauración transaccional.

### Modified Capabilities

- Ninguna; las capacidades nuevas se implementarán sin alterar los contratos existentes hasta aprobación.

## Impact

- `AuditController`, modelos Razor y navegación de auditoría.
- Servicios de auditoría funcional y `IDeletionImpactService`.
- Nuevas entidades persistentes de auditoría y una migración EF propuesta, no aplicada.
- Policies `Auditoria.Ver` y `Auditoria.Restaurar`.
- Índices, permisos de esquema y estrategia de remapeo de identidad/restore que requieren aprobación operativa.
