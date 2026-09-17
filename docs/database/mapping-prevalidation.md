# Prevalidación del mapeo Building Block–Tecnología TSI

La prevalidación es exclusivamente de lectura y está definida en
`scripts/database/mapping-prevalidation.sql`. El script exige que
`DB_NAME()` sea exactamente `db-landscape-tsi-dev-v2`; no ejecuta DDL ni DML.

Debe registrar, sin exponer secretos:

- columnas y tipos de `dbo.TBuildingBlockVsTTecnologiaTSI`;
- FK reales y acciones de borrado;
- índices, PK y restricciones UNIQUE existentes;
- pares duplicados `(idBuildingBlock, idTecnologiaTSI)`;
- filas totales frente a pares distintos.

## Decisión de esquema pendiente

No se aplica ninguna modificación de esquema en este cambio. Si la consulta
confirma que no hay duplicados, se recomienda una restricción `UNIQUE` sobre
`(idBuildingBlock, idTecnologiaTSI)` como cambio mínimo y compatible con la
tabla puente existente. Una PK compuesta solo debe elegirse después de revisar
el modelo EF y cualquier consumidor que trate la tabla puente como entidad.

Si existen duplicados, deben resolverse explícitamente antes de proponer DDL.

## Ejecución de prevalidación (db-landscape-tsi-dev-v2)

La consulta se ejecutó en modo solo lectura con protección de `DB_NAME()` y
devolvió:

- columnas: `idBuildingBlock int NOT NULL` e `idTecnologiaTSI int NOT NULL`;
- FK: una hacia `TBuildingBlock` y una hacia `TTecnologiaTSI`, ambas con
  acción `NO_ACTION`;
- índices: no hay índices, PK ni UNIQUE en la tabla puente;
- filas de la tabla puente: 1;
- pares distintos: 1;
- grupos duplicados: 0.

No se ejecutó ningún `INSERT`, `UPDATE`, `DELETE`, `ALTER`, `CREATE` ni
`DROP`.

Con el estado actual, la recomendación pendiente es un índice `UNIQUE` sobre
`(idBuildingBlock, idTecnologiaTSI)`. No se aplica en este cambio. La
aplicación mantiene además la comprobación idempotente y la transacción
`Serializable` para evitar duplicados mientras la deuda de esquema siga
abierta.

La propuesta SQL original está en
`scripts/database/mapping-unique-index-proposal.sql`. Tras aprobación explícita
se aplicó únicamente el índice `UX_TBuildingBlockVsTTecnologiaTSI_BuildingBlock_Technology`
en `db-landscape-tsi-dev-v2`, mediante el script protegido
`scripts/database/apply-mapping-unique-index.sql`. No se generó una migración
EF para este índice porque la tabla puente no está modelada como entidad EF.

Validación posterior:

- base: `db-landscape-tsi-dev-v2`;
- índice: presente;
- `is_unique`: `1`;
- bases `db-landscape-tsi`, `db-landscape-tsi-reconcile` y
  `db-landscape-tsi-dev`: no modificadas.
