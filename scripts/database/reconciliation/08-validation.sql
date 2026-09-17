/* Solo lectura. Ejecutar únicamente después de un cambio aprobado. */
SET NOCOUNT ON;
USE [db-landscape-tsi-reconcile];
SELECT DB_NAME() AS DatabaseName;
SELECT s.name AS SchemaName,t.name AS TableName FROM sys.tables t
JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.is_ms_shipped=0;
SELECT fk.name AS ForeignKeyName,OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ChildSchema,
OBJECT_NAME(fk.parent_object_id) AS ChildTable,OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ParentSchema,
OBJECT_NAME(fk.referenced_object_id) AS ParentTable FROM sys.foreign_keys fk;
DBCC CHECKCONSTRAINTS WITH NO_INFOMSGS;
SELECT N'Validar conteos IAM y seis funcionalidades promovidas contra el plan.' AS Status;
-- Las comprobaciones de filas y hashes deben ejecutarse con consultas
-- parametrizadas y no deben imprimir secretos IAM.
