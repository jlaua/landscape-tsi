/* NO EJECUTAR SIN APROBACIÓN FINAL. Promueve solo DEV PK 2,3,4,5,6,8. */
SET XACT_ABORT ON;
IF SESSION_CONTEXT(N'reconciliation_approved') <> 1
    THROW 51000, 'Falta aprobación explícita de reconciliación.', 1;
IF DB_NAME() <> N'db-landscape-tsi-reconcile'
    THROW 51001, 'Destino no autorizado.', 1;
USE [db-landscape-tsi-reconcile];
BEGIN TRANSACTION;
-- Lista blanca deliberada: solo seis PK origen; DEV PK 1 y 7 excluidos.
-- Resolver las FK por nombre funcional en el destino y abortar ante ambigüedad.
-- Insertar sin idFuncionalidad para que RECONCILE genere IDENTITY.
-- Requiere la tabla de trazabilidad aprobada; no crearla aquí.
-- Lista blanca deliberada: no aceptar nombres de tabla desde parámetros.
-- Solo después de aprobar el data-gap se podrán sincronizar catálogos como
-- TMDominio, estados y nuevos registros; nunca sobrescribir automáticamente
-- conflictos de TCISO, TBuildingBlock o TFuncionalidad.
-- Preservar TCISO.LineaDeNegocio y TCISO.Representante.
ROLLBACK TRANSACTION;
