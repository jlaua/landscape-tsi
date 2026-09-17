/* NO EJECUTAR SIN APROBACIÓN EXPLÍCITA. Solo lectura. */
SET NOCOUNT ON;
DECLARE @reconcile sysname = N'db-landscape-tsi-reconcile';
DECLARE @dev sysname = N'db-landscape-tsi-dev';

-- Ejecutar este bloque por cada base y exportar el resultado; no contiene DDL.
SELECT DB_NAME() AS DatabaseName, s.name AS SchemaName, t.name AS TableName
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name;
SELECT s.name AS SchemaName,t.name AS TableName,c.column_id,c.name AS ColumnName,
       ty.name AS TypeName,c.max_length,c.precision,c.scale,c.is_nullable,
       c.is_identity,dc.definition AS DefaultDefinition
FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id
JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.parent_object_id=c.object_id AND dc.parent_column_id=c.column_id
WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name,c.column_id;
SELECT fk.name AS ForeignKeyName, OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ChildSchema,
       OBJECT_NAME(fk.parent_object_id) AS ChildTable, cp.name AS ChildColumn,
       OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ParentSchema,
       OBJECT_NAME(fk.referenced_object_id) AS ParentTable, cr.name AS ParentColumn,
       fk.delete_referential_action_desc,fk.update_referential_action_desc
FROM sys.foreign_key_columns fkc JOIN sys.foreign_keys fk ON fk.object_id=fkc.constraint_object_id
JOIN sys.columns cp ON cp.object_id=fkc.parent_object_id AND cp.column_id=fkc.parent_column_id
JOIN sys.columns cr ON cr.object_id=fkc.referenced_object_id AND cr.column_id=fkc.referenced_column_id
ORDER BY ChildSchema,ChildTable,ForeignKeyName,fkc.constraint_column_id;
SELECT * FROM sys.indexes WHERE object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped=0);
SELECT * FROM sys.check_constraints;

-- La clasificación RECONCILE/DEV se realiza fuera de SQL comparando estos
-- inventarios. No usar nombres de tabla recibidos desde HTTP.
