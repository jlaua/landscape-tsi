/* Solo lectura. Ejecutar exclusivamente en db-landscape-tsi-dev-v2. */
IF DB_NAME() <> N'db-landscape-tsi-dev-v2'
    THROW 50001, 'Operación bloqueada: este script solo admite db-landscape-tsi-dev-v2.', 1;

SELECT COUNT(*) AS TotalEmpresas FROM dbo.TEmpresaSubsidiaria;
SELECT COUNT(*) AS TotalCiso FROM dbo.TCISO;

WITH Cardinality AS (
    SELECT e.idEmpresaSubsidiaria, COUNT(c.idCiso) AS CisoCount
    FROM dbo.TEmpresaSubsidiaria e
    LEFT JOIN dbo.TCISO c ON c.idEmpresaSubsidiaria = e.idEmpresaSubsidiaria
    GROUP BY e.idEmpresaSubsidiaria
)
SELECT SUM(CASE WHEN CisoCount = 0 THEN 1 ELSE 0 END) AS EmpresasSinCiso,
       SUM(CASE WHEN CisoCount = 1 THEN 1 ELSE 0 END) AS EmpresasConUnCiso,
       SUM(CASE WHEN CisoCount > 1 THEN 1 ELSE 0 END) AS EmpresasConVariosCiso
FROM Cardinality;

SELECT idEmpresaSubsidiaria, COUNT(*) AS Representantes
FROM dbo.TCISO
WHERE Representante = 1
GROUP BY idEmpresaSubsidiaria
HAVING COUNT(*) > 1;

SELECT e.idEmpresaSubsidiaria, e.nombreEmpresa
FROM dbo.TEmpresaSubsidiaria e
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.TCISO c
    WHERE c.idEmpresaSubsidiaria = e.idEmpresaSubsidiaria
      AND c.Representante = 1);

SELECT e.idEmpresaSubsidiaria, e.nombreEmpresa, e.contactoCiso,
       STRING_AGG(c.nombreCISO, N' | ') AS NombresCiso,
       CASE WHEN EXISTS (
           SELECT 1 FROM dbo.TCISO c2
           WHERE c2.idEmpresaSubsidiaria = e.idEmpresaSubsidiaria
             AND NULLIF(LTRIM(RTRIM(c2.nombreCISO)), N'') = NULLIF(LTRIM(RTRIM(e.contactoCiso)), N''))
            THEN N'COINCIDE' ELSE N'DIFIERE_O_FALTA' END AS Comparacion
FROM dbo.TEmpresaSubsidiaria e
LEFT JOIN dbo.TCISO c ON c.idEmpresaSubsidiaria = e.idEmpresaSubsidiaria
GROUP BY e.idEmpresaSubsidiaria, e.nombreEmpresa, e.contactoCiso;
