/* Aplicado únicamente en db-landscape-tsi-dev-v2 tras aprobación explícita. */
IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;

IF EXISTS (SELECT 1
           FROM dbo.TBuildingBlockVsTTecnologiaTSI
           GROUP BY idBuildingBlock, idTecnologiaTSI
           HAVING COUNT_BIG(*) > 1)
    THROW 50002, 'Operación bloqueada: existen pares duplicados.', 1;

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.TBuildingBlockVsTTecnologiaTSI')
                 AND name = N'UX_TBuildingBlockVsTTecnologiaTSI_BuildingBlock_Technology')
    CREATE UNIQUE INDEX UX_TBuildingBlockVsTTecnologiaTSI_BuildingBlock_Technology
        ON dbo.TBuildingBlockVsTTecnologiaTSI (idBuildingBlock, idTecnologiaTSI);
