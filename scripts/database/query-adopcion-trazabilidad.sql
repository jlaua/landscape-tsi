/* ==============================================================================================
   SCRIPT DE NAVEGACIÓN Y TRAZABILIDAD - PROCESO DE ADOPCIÓN / EVALUACIÓN TSI (WAAP)
   Base de Datos: Landscape TSI (SQL Server)
   Propósito: Navegar de punta a punta a través de los datos del proceso, empresas subsidiarias,
              contratos, volumetría/drivers, modelos operativos y capacidades del Building Block.
   ============================================================================================== */

-- Variable de contexto: ID del Proceso de Adopción (Modificar según se requiera, ej: 1)
DECLARE @ProcesoId INT = 1;

-- Si no conoces el ID, busca por nombre o código:
-- SELECT @ProcesoId = idProcesoAdopcionTSI FROM dbo.TProcesoAdopcionTSI WHERE codigoProceso = 'PROC-SEC-001' OR nombreProceso LIKE '%WAAP%';

PRINT '====================================================================================';
PRINT '1. INFORMACIÓN MAESTRA DEL PROCESO DE ADOPCIÓN / EVALUACIÓN';
PRINT '====================================================================================';
SELECT 
    p.idProcesoAdopcionTSI          AS [ID Proceso],
    p.codigoProceso                 AS [Código],
    p.nombreProceso                 AS [Evaluación TSI],
    d.dominio                       AS [Dominio TSI],
    bb.idBuildingBlock              AS [ID BB],
    bb.nombreBuildingBlock          AS [Building Block],
    f.nombreFamilia                 AS [Familia],
    est.nombreEstadoAdopcionTSI     AS [Estado Evaluación],
    p.liderCorporativoTSI           AS [Líder Corporativo],
    p.fechaInicio                   AS [Fecha Inicio],
    p.fechaEstimadaCierre           AS [Fecha Est. Cierre],
    p.objetivo                      AS [Objetivo],
    p.alcance                       AS [Alcance]
FROM dbo.TProcesoAdopcionTSI p
INNER JOIN dbo.TBuildingBlock bb ON bb.idBuildingBlock = p.idBuildingBlock
LEFT JOIN dbo.TMDominio d ON d.iddominio = bb.idDominio
LEFT JOIN dbo.TMFamilia f ON f.idFamilia = bb.idFamilia
LEFT JOIN dbo.TMEstadoAdopcionTSI est ON est.idEstadoAdopcionTSI = p.idEstadoAdopcionTSI
WHERE p.idProcesoAdopcionTSI = @ProcesoId;


PRINT '====================================================================================';
PRINT '2. ESTÁNDARES CORPORATIVOS TSI (Vigente Principal, Alternativos e Históricos)';
PRINT '====================================================================================';
SELECT 
    eh.idEstandarTecnologia         AS [ID Estándar],
    eh.rolEstandar                  AS [Rol (PRINCIPAL / ALTERNATIVA)],
    eh.estadoVigencia               AS [Vigencia (ACTIVO_VIGENTE / HISTORICO)],
    t.idTecnologiaTSI               AS [ID Tecnología],
    t.[nombreTecnologiaAlternativa1-Corporativo] AS [Tecnología Corporativa],
    v.nombreVendor                  AS [Vendor],
    cp.nombreContactoPartner        AS [Contacto Partner],
    eh.fechaInicioVigencia          AS [Inicio Vigencia],
    eh.fechaFinVigencia             AS [Fin Vigencia],
    eh.sustentoArquitectura         AS [Sustento Arquitectura]
FROM dbo.TEstandarTecnologiaHistorico eh
INNER JOIN dbo.TTecnologiaTSI t ON t.idTecnologiaTSI = eh.idTecnologiaTSI
LEFT JOIN dbo.TVendor v ON v.idTecnologiaTSI = t.idTecnologiaTSI
LEFT JOIN dbo.TContactoPartner cp ON cp.idVendor = v.idVendor
WHERE eh.idProcesoAdopcionTSI = @ProcesoId 
   OR eh.idBuildingBlock = (SELECT idBuildingBlock FROM dbo.TProcesoAdopcionTSI WHERE idProcesoAdopcionTSI = @ProcesoId)
ORDER BY eh.rolEstandar DESC, eh.fechaInicioVigencia DESC;


PRINT '====================================================================================';
PRINT '3. REPORTE A: ALCANCE POR SUBSIDIARIA, WAF AS-IS, CONTRATOS, PARTNER Y OPERACIÓN';
PRINT '====================================================================================';
SELECT 
    emp.idEmpresaSubsidiaria        AS [ID Empresa],
    emp.nombreEmpresa               AS [Empresa / Subsidiaria],
    emp.Pais                        AS [País],
    pae.aplica                      AS [Aplica Adopción?],
    pae.justificacionNoAplica       AS [Motivo No Aplica],
    -- Tecnología Implementada AS-IS
    impl.idTecnologiaTSIimplementadaSubsidiaria AS [ID Tech Impl],
    COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], t.[nombreTecnologiaAlternativa2-Local], N'No definida') AS [WAF AS-IS],
    impl.versionDesplegada          AS [Versión Desplegada],
    impl.esTecnologiaPrimaria       AS [Es Primaria?],
    t.modeloEsquemaLicenciamientoSubscripcion AS [Tipo Contrato / Licenciamiento],
    -- Contrato más próximo
    ct.idContratoTecnologia         AS [ID Contrato],
    ct.numeroContrato               AS [N° Contrato],
    ct.fechaInicio                  AS [Inicio Contrato],
    ct.fechaFin                     AS [Vencimiento Contrato],
    ct.montoContratado              AS [Monto Contrato],
    ct.moneda                       AS [Moneda],
    ct.observaciones                AS [Partner / Observaciones],
    -- Modelo de Operación
    topr.TipoModeloDeOperacion      AS [Tipo Operación],
    mlab.TipoModalidadLaboral       AS [Modalidad Laboral]
FROM dbo.TProcesoAdopcionEmpresa pae
INNER JOIN dbo.TEmpresaSubsidiaria emp ON emp.idEmpresaSubsidiaria = pae.idEmpresaSubsidiaria
LEFT JOIN dbo.TTecnologiaTSIimplementadaSubsidiaria impl 
       ON (impl.idProcesoAdopcionEmpresa = pae.idProcesoAdopcionEmpresa 
           OR (impl.idEmpresaSubsidiaria = pae.idEmpresaSubsidiaria AND impl.idBuildingBlock = (SELECT idBuildingBlock FROM dbo.TProcesoAdopcionTSI WHERE idProcesoAdopcionTSI = @ProcesoId)))
LEFT JOIN dbo.TTecnologiaTSI t ON t.idTecnologiaTSI = impl.idTecnologiaTSI
OUTER APPLY (
    SELECT TOP 1 c.* 
    FROM dbo.TContratoTecnologia c 
    WHERE c.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
    ORDER BY c.fechaFin ASC
) ct
LEFT JOIN dbo.TModeloDeOperacion moper ON moper.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
LEFT JOIN dbo.TTipoOperacion topr ON topr.idTipoModeloOperacion = moper.idTipoModeloDeOperacion
LEFT JOIN dbo.TModalidadLaboral mlab ON mlab.idModalidadLaboral = moper.idModalidadLaboral
WHERE pae.idProcesoAdopcionTSI = @ProcesoId
ORDER BY ct.fechaFin ASC, emp.nombreEmpresa ASC;


PRINT '====================================================================================';
PRINT '4. REPORTE B: LÍNEA TEMPORAL DE VENCIMIENTOS Y VOLUMETRÍA BASE POR EMPRESA';
PRINT '====================================================================================';
WITH MetricasBase AS (
    SELECT 
        emp.idEmpresaSubsidiaria,
        emp.nombreEmpresa,
        COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'WAF AS-IS') AS ProductoActual,
        ct.fechaFin AS FechaVencimiento,
        ct.esAdenda,
        -- Drivers agregados
        COALESCE(SUM(CASE WHEN d.descripcionDriver LIKE '%through%' OR d.descripcionDriver LIKE '%gb%' THEN d.cantidad ELSE 0 END), 0) AS ThroughputGbMes,
        COALESCE(SUM(CASE WHEN d.descripcionDriver LIKE '%fqdn%' OR d.descripcionDriver LIKE '%app%' THEN CAST(d.cantidad AS INT) ELSE 0 END), 0) AS CantidadAppFqdn,
        COALESCE(SUM(CASE WHEN d.descripcionDriver LIKE '%request%' OR d.descripcionDriver LIKE '%waf%' THEN d.cantidad ELSE 0 END), 0) AS RequestWafMillonesMes
    FROM dbo.TProcesoAdopcionEmpresa pae
    INNER JOIN dbo.TEmpresaSubsidiaria emp ON emp.idEmpresaSubsidiaria = pae.idEmpresaSubsidiaria
    LEFT JOIN dbo.TTecnologiaTSIimplementadaSubsidiaria impl 
           ON (impl.idProcesoAdopcionEmpresa = pae.idProcesoAdopcionEmpresa 
               OR (impl.idEmpresaSubsidiaria = pae.idEmpresaSubsidiaria AND impl.idBuildingBlock = (SELECT idBuildingBlock FROM dbo.TProcesoAdopcionTSI WHERE idProcesoAdopcionTSI = @ProcesoId)))
    LEFT JOIN dbo.TTecnologiaTSI t ON t.idTecnologiaTSI = impl.idTecnologiaTSI
    LEFT JOIN dbo.TContratoTecnologia ct ON ct.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
    LEFT JOIN dbo.TDriver d ON d.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
    WHERE pae.idProcesoAdopcionTSI = @ProcesoId
    GROUP BY emp.idEmpresaSubsidiaria, emp.nombreEmpresa, t.[nombreTecnologiaAlternativa1-Corporativo], ct.fechaFin, ct.esAdenda
)
SELECT 
    ROW_NUMBER() OVER (ORDER BY CASE WHEN FechaVencimiento IS NULL THEN 1 ELSE 0 END, FechaVencimiento ASC) AS [# Secuencia],
    nombreEmpresa                   AS [Subsidiaria],
    ProductoActual                  AS [Producto Actual],
    ThroughputGbMes                 AS [Throughput GB/mes],
    CantidadAppFqdn                 AS [Cant. App FQDN],
    RequestWafMillonesMes           AS [Request WAF M/mes],
    FORMAT(FechaVencimiento, 'dd/MM/yyyy') AS [Fecha Vencimiento],
    -- Hitos Temporales: Marca X
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '12-2026' THEN 'X' ELSE '' END AS [Dic-26],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '01-2027' THEN 'X' ELSE '' END AS [Ene-27],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '03-2027' THEN 'X' ELSE '' END AS [Mar-27],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '07-2027' THEN 'X' ELSE '' END AS [Jul-27],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '08-2027' THEN 'X' ELSE '' END AS [Ago-27],
    CASE WHEN FORMAT(FechaVencimiento, 'MM-yyyy') = '09-2027' THEN 'X' ELSE '' END AS [Set-27],
    CASE WHEN FechaVencimiento IS NULL THEN 'X' ELSE '' END AS [PAYG / Sin Venc.]
FROM MetricasBase
ORDER BY [# Secuencia];


PRINT '====================================================================================';
PRINT '5. REPORTE 3: DETALLE DE DRIVERS Y VOLUMETRÍA TÉCNICA REGISTRADA';
PRINT '====================================================================================';
SELECT 
    emp.nombreEmpresa               AS [Empresa],
    COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'WAF AS-IS') AS [Tecnología AS-IS],
    d.idDriver                      AS [ID Driver],
    d.descripcionDriver             AS [Driver de Consumo],
    d.unidadMedida                  AS [Unidad de Medida],
    d.cantidad                      AS [Volumen / Cantidad],
    d.precioUnitario                AS [Tarifa Unitaria],
    d.moneda                        AS [Moneda],
    CAST((COALESCE(d.cantidad, 0) * COALESCE(d.precioUnitario, 0)) AS DECIMAL(18,2)) AS [Costo Estimado Mensual]
FROM dbo.TProcesoAdopcionEmpresa pae
INNER JOIN dbo.TEmpresaSubsidiaria emp ON emp.idEmpresaSubsidiaria = pae.idEmpresaSubsidiaria
INNER JOIN dbo.TTecnologiaTSIimplementadaSubsidiaria impl 
        ON (impl.idProcesoAdopcionEmpresa = pae.idProcesoAdopcionEmpresa 
            OR (impl.idEmpresaSubsidiaria = pae.idEmpresaSubsidiaria AND impl.idBuildingBlock = (SELECT idBuildingBlock FROM dbo.TProcesoAdopcionTSI WHERE idProcesoAdopcionTSI = @ProcesoId)))
LEFT JOIN dbo.TTecnologiaTSI t ON t.idTecnologiaTSI = impl.idTecnologiaTSI
LEFT JOIN dbo.TDriver d ON d.idTecnologiaTSIimplementadaSubsidiaria = impl.idTecnologiaTSIimplementadaSubsidiaria
WHERE pae.idProcesoAdopcionTSI = @ProcesoId
ORDER BY emp.nombreEmpresa ASC, d.descripcionDriver ASC;


PRINT '====================================================================================';
PRINT '6. REPORTE 4: MATRIZ DE CAPACIDADES Y FUNCIONALIDADES DEL BUILDING BLOCK';
PRINT '====================================================================================';
SELECT 
    bb.nombreBuildingBlock          AS [Building Block],
    c.idCapacidad                   AS [ID Capacidad],
    c.nombreCapacidad               AS [Capacidad de Seguridad],
    estCap.nombreEstadoCapacidad    AS [Estado Capacidad],
    f.idFuncionalidad               AS [ID Funcionalidad],
    f.nombreFuncionalidad           AS [Funcionalidad / Módulo],
    estFunc.nombreEstadoFuncionalidad AS [Cobertura Funcional]
FROM dbo.TProcesoAdopcionTSI p
INNER JOIN dbo.TBuildingBlock bb ON bb.idBuildingBlock = p.idBuildingBlock
INNER JOIN dbo.TCapacidadDeSeguridad c ON c.idBuildingBlock = bb.idBuildingBlock
LEFT JOIN dbo.TMEstadoCapacidad estCap ON estCap.idEstadoCapacidad = c.idEstadoCapacidad
LEFT JOIN dbo.TFuncionalidad f ON f.idCapacidad = c.idCapacidad
LEFT JOIN dbo.TMEstadoFuncionalidad estFunc ON estFunc.idEstadoCoberturaFuncionalidad = f.idEstadoCoberturaFuncionalidad
WHERE p.idProcesoAdopcionTSI = @ProcesoId
ORDER BY c.nombreCapacidad ASC, f.nombreFuncionalidad ASC;
