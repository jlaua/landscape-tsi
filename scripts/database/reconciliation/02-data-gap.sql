/* NO EJECUTAR SIN APROBACIÓN EXPLÍCITA. Solo lectura; no resuelve conflictos. */
SET NOCOUNT ON;
-- Ejecutar por cada tabla funcional aprobada, sustituyendo únicamente los
-- identificadores generados por el inventario (nunca valores de usuario).
-- La herramienta de reconciliación debe comparar por PK y producir cuatro
-- estados: SOLO RECONCILE, SOLO DEV, MISMO PK/VALORES IGUALES,
-- MISMO PK/VALORES DIFERENTES.
--
-- Ejemplo de consulta de conteo (sustituir por tabla inventariada):
-- SELECT COUNT_BIG(*) AS RowCount FROM [dbo].[TMDominio];
--
-- Para tablas sin PK (por ejemplo TBuildingBlockVsTTecnologiaTSI), comparar
-- exclusivamente el conjunto de columnas FK real y reportar duplicados.
-- No ejecutar SELECT * sobre tablas IAM desde este script.
