# Plan de corte propuesto (no ejecutado)

1. Aprobar formalmente `schema-gap.md` y `data-gap.md`, incluyendo conflictos
   de `TCISO`, `TBuildingBlock` y `TFuncionalidad`.
2. Respaldar y congelar escrituras en el destino; verificar nuevamente que el
   destino es `db-landscape-tsi-reconcile`.
3. Aplicar el esquema IAM mediante las migraciones EF, nunca con DDL manual.
4. Validar tablas, columnas, PK, FK, índices, defaults y checks.
5. Sincronizar maestros IAM y relaciones conservando GUID/IDs, stamps,
   timestamps y FKs. Ejecutar dentro de transacciones y con `IDENTITY_INSERT`
   únicamente donde el inventario lo requiera.
6. Sincronizar auditoría según retención aprobada.
7. Resolver manualmente cada conflicto funcional; preservar siempre columnas
   exclusivas de RECONCILE/PROD (`TCISO.LineaDeNegocio`, `TCISO.Representante`).
8. Ejecutar `08-validation.sql` y `09-orphan-check.sql`; revisar conteos y
   huérfanos.
9. Realizar pruebas de login, autorización, reportes y CRUD contra la base
   canónica.
10. Solo tras aprobación, planificar el cutover de la aplicación. Las bases
    `db-landscape-tsi` y `db-landscape-tsi-dev` no se modifican como parte de
    este plan.
