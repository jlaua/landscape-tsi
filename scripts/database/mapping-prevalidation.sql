/*
   Solo lectura. Ejecutar exclusivamente contra db-landscape-tsi-dev-v2
   para la prevalidación de TBuildingBlockVsTTecnologiaTSI.
   Este script no crea, altera ni modifica objetos o datos.
*/
IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: la prevalidación solo admite db-landscape-tsi-dev-v2.', 1;

SELECT DB_NAME() AS DatabaseName;

SELECT
    c.name AS ColumnName,
    ty.name AS SqlType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.tables AS t
JOIN sys.columns AS c ON c.object_id = t.object_id
JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
WHERE t.schema_id = SCHEMA_ID(N'dbo')
  AND t.name = N'TBuildingBlockVsTTecnologiaTSI'
ORDER BY c.column_id;

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) + N'.' + OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    pc.name AS ChildColumn,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) + N'.' + OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    rc.name AS ParentColumn,
    fk.delete_referential_action_desc AS DeleteAction
FROM sys.foreign_keys AS fk
JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns AS pc ON pc.object_id = fk.parent_object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.columns AS rc ON rc.object_id = fk.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.TBuildingBlockVsTTecnologiaTSI')
   OR fk.referenced_object_id = OBJECT_ID(N'dbo.TBuildingBlockVsTTecnologiaTSI');

SELECT
    i.name AS IndexName,
    i.is_primary_key AS IsPrimaryKey,
    i.is_unique AS IsUnique,
    ic.key_ordinal AS KeyOrdinal,
    c.name AS ColumnName
FROM sys.indexes AS i
JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.TBuildingBlockVsTTecnologiaTSI')
ORDER BY i.name, ic.key_ordinal;

SELECT
    idBuildingBlock,
    idTecnologiaTSI,
    COUNT_BIG(*) AS DuplicateCount
FROM dbo.TBuildingBlockVsTTecnologiaTSI
GROUP BY idBuildingBlock, idTecnologiaTSI
HAVING COUNT_BIG(*) > 1
ORDER BY DuplicateCount DESC, idBuildingBlock, idTecnologiaTSI;

SELECT COUNT_BIG(*) AS TotalBridgeRows,
       COUNT_BIG(DISTINCT CONCAT(idBuildingBlock, N':', idTecnologiaTSI)) AS DistinctPairs
FROM dbo.TBuildingBlockVsTTecnologiaTSI;
