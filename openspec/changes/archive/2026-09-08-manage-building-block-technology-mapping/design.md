## Context

La relación existente es una tabla puente física `dbo.TBuildingBlockVsTTecnologiaTSI` con FKs hacia `dbo.TBuildingBlock` y `dbo.TTecnologiaTSI`. La documentación de reconciliación registra que puede carecer de PK/UNIQUE, por lo que el primer paso será una inspección SQL de solo lectura en `db-landscape-tsi-dev-v2`; ninguna decisión de constraint se asumirá por nombres.

## Goals / Non-Goals

**Goals:**

- Exponer una única capacidad de administración bidireccional con metadata y rutas backend compartidas.
- Consultar Tecnologías, Building Blocks y Familias con proyección, filtros y paginación SQL.
- Crear y desasociar solo filas puente, de forma transaccional, auditable e idempotente.
- Prevenir duplicados en aplicación y preparar una protección de base después de revisar datos reales.
- Mantener N:M, autorización server-side, alcance organizacional y accesibilidad.

**Non-Goals:**

- No agregar columnas a `TTecnologiaTSI` ni `TBuildingBlock`.
- No aplicar PK, UNIQUE, índices, migrations ni limpieza automática en esta etapa de propuesta.
- No eliminar entidades principales al desasociar.
- No permitir SQL dinámico o nombres de tablas recibidos desde HTTP.

## Decisions

1. **Servicio de consulta y comando controlado.** Un servicio de aplicación usará códigos whitelist (`building-block`, `tecnologia-tsi`) y la relación registrada en `CatalogRelationshipMetadata`. La UI recibirá DTOs funcionales y rutas generadas por backend. Se descarta un endpoint genérico que acepte nombres físicos.

2. **Paginación y KPIs en SQL.** La lista principal ejecutará `AsNoTracking`, proyección, filtros y `Skip/Take`; los KPIs usarán `Count`/`CountDistinct` controlados. Se evita cargar todas las relaciones o hacer N+1.

3. **Comandos idempotentes y transaccionales.** El guardado recibirá únicamente IDs validados pertenecientes a la whitelist. Se volverá a consultar el estado dentro de una transacción, se insertarán solo pares faltantes y se eliminarán solo pares confirmados. Una carrera concurrente se traducirá a un resultado controlado y no afectará entidades principales.

4. **Unicidad de la tabla puente.** Primero se ejecutarán consultas de solo lectura sobre `sys.foreign_keys`, `sys.indexes`, `sys.index_columns`, `sys.key_constraints` y duplicados por par. Si no hay duplicados, se presentará una migration separada para PK compuesta o UNIQUE; la elección dependerá de compatibilidad EF y del modelo actual. No se aplicará automáticamente.

5. **Autorización y auditoría.** Se reutilizarán las policies de catálogos para lectura y edición, con un permiso explícito para administrar relaciones si la matriz actual lo requiere. Asociación y desasociación registrarán actor, UTC, pares afectados y resultado sin secretos. La desasociación tendrá confirmación reforzada y texto explícito de que no elimina entidades.

6. **Navegación.** `Url.Action`/Tag Helpers/LinkGenerator resolverán rutas de detalle reales. El frontend no construirá slugs ni nombres físicos.

7. **UI adaptable.** La página será un dashboard compacto con KPIs, filtros, tabla y un panel de selección múltiple; el mismo componente se reutilizará desde detalles de Building Block y Tecnología. Se proporcionará alternativa de teclado y etiquetas que no dependan del color.

## Risks / Trade-offs

- **Duplicados preexistentes** → bloquear comandos ambiguos, reportar conteos y esperar decisión; nunca borrar automáticamente.
- **Carreras sin constraint SQL** → transacción, reconsulta y manejo de `DbUpdateException`; la garantía definitiva requiere DDL aprobado.
- **Divergencia de rutas** → generar enlaces en backend y agregar pruebas que acepten 200/302/403, pero no 404.
- **Alcance organizacional** → resolver Building Blocks/Tecnologías visibles según la autorización antes de mostrar o modificar pares.
- **Tabla puente sin PK** → usar el par de FKs como clave lógica en la primera iteración y mantener la deuda técnica documentada.
- **Volumen de relaciones** → índices existentes y proyecciones; medir consultas antes de proponer nuevos índices.

## Migration Plan

1. Ejecutar prevalidación de solo lectura en `db-landscape-tsi-dev-v2` y documentar FKs, índices, duplicados y conteos.
2. Implementar UI, servicios, permisos y pruebas sin DDL.
3. Presentar decisión PK compuesta/UNIQUE, migration y plan de limpieza si existen duplicados.
4. Solo tras aprobación independiente, aplicar la migration exclusivamente al ambiente autorizado.
5. Rollback de código mediante despliegue anterior; rollback de DDL mediante migration reversa aprobada. No usar restore de bases productivas.
