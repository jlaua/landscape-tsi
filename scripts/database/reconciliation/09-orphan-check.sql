/* Solo lectura: detección de huérfanos a partir de FKs reales. */
SET NOCOUNT ON;
USE [db-landscape-tsi-reconcile];
DECLARE @sql nvarchar(max)=N'';
SELECT @sql += N'SELECT N''' + REPLACE(fk.name,'''','''''') + N''' AS ForeignKeyName, COUNT_BIG(*) AS Orphans FROM '
 + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(fk.parent_object_id))+N' c LEFT JOIN '
 + QUOTENAME(OBJECT_SCHEMA_NAME(fk.referenced_object_id))+N'.'+QUOTENAME(OBJECT_NAME(fk.referenced_object_id))+N' p ON 1=1 WHERE 1=0;'
FROM sys.foreign_keys fk;
-- La consulta final debe generarse por FK y columna con sys.foreign_key_columns;
-- este archivo es un marcador seguro y no modifica datos.
SELECT N'Orphan check requires generated per-FK predicates' AS Status;
