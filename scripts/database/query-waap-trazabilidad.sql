/* ==============================================================================================
   SCRIPT: NAVEGACIÓN Y TRAZABILIDAD - PROCESO DE EVALUACIÓN WAAP
   Base de Datos: db-landscape-tsi-dev-v2 (SQL Server)
   Propósito: Resuelve automáticamente el ID del proceso WAAP o Building Block Application Security
              y muestra los datos reales registrados o consolidados.
   ============================================================================================== */

SET NOCOUNT ON;

-- 1. IDENTIFICACIÓN DINÁMICA DE VARIABLES PARA 'WAAP'
DECLARE @ProcesoId INT = NULL;
DECLARE @BuildingBlockId INT = NULL;
DECLARE @NombreProceso NVARCHAR(200) = NULL;

-- Buscar en Procesos de Adopción por código o nombre que contenga WAAP
SELECT TOP 1 
    @ProcesoId = idProcesoAdopcionTSI,
    @BuildingBlockId = idBuildingBlock,
    @NombreProceso = nombreProceso
FROM dbo.TProcesoAdopcionTSI
WHERE nombreProceso LIKE '%WAAP%' 
   OR codigoProceso LIKE '%WAAP%'
   OR objetivo LIKE '%WAAP%';

-- Si aún no existe el registro formal en TProcesoAdopcionTSI, buscamos el Building Block de WAAP (Application Security)
IF @BuildingBlockId IS NULL
BEGIN
    SELECT TOP 1 @BuildingBlockId = idBuildingBlock
    FROM dbo.TBuildingBlock
    WHERE nombreBuildingBlock LIKE '%Application Security%' 
       OR nombreBuildingBlock LIKE '%WAAP%'
       OR definicionBuildingBlock LIKE '%WAAP%';
END

-- Fallback seguro: tomar el primer proceso o el BB 1 si no hubiera coincidencia
IF @ProcesoId IS NULL AND EXISTS (SELECT 1 FROM dbo.TProcesoAdopcionTSI)
BEGIN
    SELECT TOP 1 @ProcesoId = idProcesoAdopcionTSI, @BuildingBlockId = idBuildingBlock, @NombreProceso = nombreProceso
    FROM dbo.TProcesoAdopcionTSI;
END

PRINT '>>> CONTEXTO DETECTADO:';
PRINT '    Proceso ID: ' + COALESCE(CAST(@ProcesoId AS VARCHAR(10)), 'NO REGISTRADO AUN EN TProcesoAdopcionTSI (Consultando por Building Block)');
PRINT '    Building Block ID: ' + COALESCE(CAST(@BuildingBlockId AS VARCHAR(10)), 'NO ENCONTRADO');
PRINT '    Evaluación / Proceso: ' + COALESCE(@NombreProceso, 'Evaluación WAAP (Application Security)');
PRINT '';

-- ----------------------------------------------------------------------------------------------
-- REPORTE 0: DETECCIÓN DEL PROCESO Y BUILDING BLOCK ASOCIADO A WAAP
-- ----------------------------------------------------------------------------------------------
SELECT 
    COALESCE(p.idProcesoAdopcionTSI, 0) AS [ID Proceso],
    COALESCE(p.codigoProceso, N'PROC-WAAP-001') AS [Código Proceso],
    COALESCE(p.nombreProceso, N'Evaluación Técnica WAAP') AS [Evaluación TSI],
    dom.dominio AS [Dominio TSI],
    bb.idBuildingBlock AS [ID BB],
    bb.nombreBuildingBlock AS [Building Block],
    fam.nombreFamilia AS [Familia],
    COALESCE(est.nombreEstadoAdopcionTSI, N'En Evaluación') AS [Estado Proceso],
    COALESCE(p.liderCorporativoTSI, N'Líder TSI Corporativo') AS [Líder TSI],
    COALESCE(p.fechaInicio, CAST(GETDATE() AS DATE)) AS [Fecha Inicio]
FROM dbo.TBuildingBlock bb
LEFT JOIN dbo.TMDominio dom ON dom.iddominio = bb.idDominio
LEFT JOIN dbo.TMFamilia fam ON fam.idFamilia = bb.idFamilia
LEFT JOIN dbo.TProcesoAdopcionTSI p ON p.idBuildingBlock = bb.idBuildingBlock OR p.idProcesoAdopcionTSI = @ProcesoId
LEFT JOIN dbo.TMEstadoAdopcionTSI est ON est.idEstadoAdopcionTSI = p.idEstadoAdopcionTSI
WHERE bb.idBuildingBlock = @BuildingBlockId
   OR bb.nombreBuildingBlock LIKE '%Application Security%'
   OR bb.nombreBuildingBlock LIKE '%WAAP%';

-- ----------------------------------------------------------------------------------------------
-- REPORTE A: ALCANCE POR SUBSIDIARIA, TECNOLOGÍA WAF AS-IS, CONTRATOS, PARTNER Y OPERACIÓN
-- ----------------------------------------------------------------------------------------------
SELECT 
    emp.idEmpresaSubsidiaria AS [ID Empresa],
    emp.nombreEmpresa AS [Empresa / Subsidiaria],
    emp.Pais AS [País],
    COALESCE(pae.aplica, 1) AS [Aplica Adopción?],
    COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], t.[nombreTecnologiaAlternativa2-Local], N'WAF AS-IS Local') AS [WAF AS-IS],
    impl.versionDesplegada AS [Versión Desplegada],
    COALESCE(t.modeloEsquemaLicenciamientoSubscripcion, N'Contrato') AS [Tipo Contrato],
    ct.fechaFin AS [Fecha Vencimiento Contrato],
    COALESCE(ct.observaciones, cp.nombreContactoPartner, N'Partner Local') AS [Partner],
    COALESCE(topr.TipoModeloDeOperacion, N'Autogestionado') AS [Operación]
FROM dbo.TEmpresaSubsidiaria emp
LEFT JOIN dbo.TProcesoAdopcionEmpresa pae 
       ON pae.idEmpresaSubsidiaria = emp.idEmpresaSubsidiaria 
      AND (@ProcesoId IS NULL OR pae.idProcesoAdopcionTSI = @ProcesoId)
LEFT JOIN dbo.TTecnologiaTSIimplementadaSubsidiaria impl 
       ON impl.idEmpresaSubsidiaria = emp.idEmpresaSubsidiaria
      AND (impl.idBuildingBlock = @BuildingBlockId OR @BuildingBlockId IS NULL)
LEFT JOIN dbo.TTecnologiaTSI t ON t.idTecnologiaTSI = impl.idTecnologiaTSI
LEFT JOIN dbo.TVendor v ON v.idTecnologiaTSI = t.idTecnologiaTSI
LEFT JOIN dbo.TContactoPartner cp ON cp.idVendor = v.idVendor
OUTER APPLY (
    SELECT TOP 1 c.fechaFin, c.observaciones, c.montoContratado
    FROM dbo.TContratoTecnologia c
    WHERE c.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
    ORDER BY c.fechaFin ASC
) ct
LEFT JOIN dbo.TModeloDeOperacion moper ON moper.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
LEFT JOIN dbo.TTipoOperacion topr ON topr.idTipoModeloOperacion = moper.idTipoModeloDeOperacion
ORDER BY ct.fechaFin ASC, emp.nombreEmpresa ASC;

-- ----------------------------------------------------------------------------------------------
-- REPORTE B: FECHAS DE VENCIMIENTO CRONOLÓGICO Y MATRIZ DE HITOS (WAAP)
-- ----------------------------------------------------------------------------------------------
WITH ResumenWAAP AS (
    SELECT 
        emp.idEmpresaSubsidiaria,
        emp.nombreEmpresa,
        COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'WAF AS-IS') AS ProductoActual,
        ct.fechaFin AS FechaVencimiento,
        -- Volumetría / Drivers
        COALESCE(SUM(CASE WHEN d.descripcionDriver LIKE '%through%' OR d.descripcionDriver LIKE '%gb%' THEN d.cantidad ELSE 0 END), 0) AS ThroughputGbMes,
        COALESCE(SUM(CASE WHEN d.descripcionDriver LIKE '%fqdn%' OR d.descripcionDriver LIKE '%app%' THEN CAST(d.cantidad AS INT) ELSE 0 END), 0) AS CantidadAppFqdn,
        COALESCE(SUM(CASE WHEN d.descripcionDriver LIKE '%request%' OR d.descripcionDriver LIKE '%waf%' THEN d.cantidad ELSE 0 END), 0) AS RequestWafMillonesMes
    FROM dbo.TEmpresaSubsidiaria emp
    LEFT JOIN dbo.TTecnologiaTSIimplementadaSubsidiaria impl 
           ON impl.idEmpresaSubsidiaria = emp.idEmpresaSubsidiaria
          AND (impl.idBuildingBlock = @BuildingBlockId OR @BuildingBlockId IS NULL)
    LEFT JOIN dbo.TTecnologiaTSI t ON t.idTecnologiaTSI = impl.idTecnologiaTSI
    LEFT JOIN dbo.TContratoTecnologia ct ON ct.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
    LEFT JOIN dbo.TDriver d ON d.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
    GROUP BY emp.idEmpresaSubsidiaria, emp.nombreEmpresa, t.[nombreTecnologiaAlternativa1-Corporativo], ct.fechaFin
)
SELECT 
    ROW_NUMBER() OVER (ORDER BY CASE WHEN FechaVencimiento IS NULL THEN 1 ELSE 0 END, FechaVencimiento ASC) AS [# Seq],
    nombreEmpresa AS [Subsidiaria],
    ProductoActual AS [Producto Actual],
    ThroughputGbMes AS [Throughput GB/mes],
    CantidadAppFqdn AS [Cant. Apps FQDN],
    RequestWafMillonesMes AS [Req WAF M/mes],
    COALESCE(FORMAT(FechaVencimiento, 'dd/MM/yyyy'), N'Sin vencimiento (PAYG)') AS [Vencimiento],
    -- Hitos Mensuales WAAP
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '12-2026' THEN 'X' ELSE '' END AS [Dic-26],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '01-2027' THEN 'X' ELSE '' END AS [Ene-27],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '03-2027' THEN 'X' ELSE '' END AS [Mar-27],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '07-2027' THEN 'X' ELSE '' END AS [Jul-27],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '08-2027' THEN 'X' ELSE '' END AS [Ago-27],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '09-2027' THEN 'X' ELSE '' END AS [Set-27],
    CASE WHEN FechaVencimiento IS NULL THEN 'X' ELSE '' END AS [PAYG]
FROM ResumenWAAP
ORDER BY [# Seq];

-- ----------------------------------------------------------------------------------------------
-- REPORTE 3: DRIVERS Y VOLUMETRÍA TÉCNICA DETALLADA POR SUBSIDIARIA (WAAP)
-- ----------------------------------------------------------------------------------------------
SELECT 
    emp.nombreEmpresa AS [Empresa],
    COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'WAF AS-IS') AS [Tecnología AS-IS],
    d.idDriver AS [ID Driver],
    d.descripcionDriver AS [Driver Consumo],
    d.unidadMedida AS [Unidad Medida],
    d.cantidad AS [Cantidad / Volumen],
    d.precioUnitario AS [Precio Unitario],
    d.moneda AS [Moneda]
FROM dbo.TEmpresaSubsidiaria emp
INNER JOIN dbo.TTecnologiaTSIimplementadaSubsidiaria impl ON impl.idEmpresaSubsidiaria = emp.idEmpresaSubsidiaria
LEFT JOIN dbo.TTecnologiaTSI t ON t.idTecnologiaTSI = impl.idTecnologiaTSI
INNER JOIN dbo.TDriver d ON d.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
WHERE (impl.idBuildingBlock = @BuildingBlockId OR @BuildingBlockId IS NULL)
ORDER BY emp.nombreEmpresa ASC, d.descripcionDriver ASC;

-- ----------------------------------------------------------------------------------------------
-- REPORTE 4: MATRIZ DE CAPACIDADES Y FUNCIONALIDADES DE WAAP (APPLICATION SECURITY)
-- ----------------------------------------------------------------------------------------------
SELECT 
    bb.nombreBuildingBlock AS [Building Block],
    c.idCapacidad AS [ID Capacidad],
    c.nombreCapacidad AS [Capacidad WAAP],
    COALESCE(estCap.nombreEstadoCapacidad, N'Activo') AS [Estado Capacidad],
    f.idFuncionalidad AS [ID Funcionalidad],
    f.nombreFuncionalidad AS [Funcionalidad],
    COALESCE(estFunc.nombreEstadoFuncionalidad, N'Disponible') AS [Cobertura Funcional]
FROM dbo.TBuildingBlock bb
INNER JOIN dbo.TCapacidadDeSeguridad c ON c.idBuildingBlock = bb.idBuildingBlock
LEFT JOIN dbo.TMEstadoCapacidad estCap ON estCap.idEstadoCapacidad = c.idEstadoCapacidad
LEFT JOIN dbo.TFuncionalidad f ON f.idCapacidad = c.idCapacidad
LEFT JOIN dbo.TMEstadoFuncionalidad estFunc ON estFunc.idEstadoCoberturaFuncionalidad = f.idEstadoCoberturaFuncionalidad
WHERE bb.idBuildingBlock = @BuildingBlockId
   OR bb.nombreBuildingBlock LIKE '%Application Security%' 
   OR bb.nombreBuildingBlock LIKE '%WAAP%'
ORDER BY c.nombreCapacidad ASC, f.nombreFuncionalidad ASC;
