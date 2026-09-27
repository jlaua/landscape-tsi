# Diseño Técnico: Procesos de Implementación y Gestión de Vendors / Tecnologías TSI

## 1. Arquitectura de Dominio y Datos

### 1.1 Extensiones de Base de Datos

#### Script: `scripts/database/evolve-implementation-process-schema.sql`
- **`dbo.TProcesoAdopcionTSI`**:
  - `tipoProceso NVARCHAR(50) NOT NULL DEFAULT 'EVALUACION'` (admite `'EVALUACION'` e `'IMPLEMENTACION'`).
  - `fechaInicioVigenciaContrato DATETIME2 NULL`.
  - `fechaFinVigenciaContrato DATETIME2 NULL`.
  - `idTecnologiaEstandar INT NULL` con FK a `dbo.TTecnologiaTSI(idTecnologiaTSI)`.
- **`dbo.TProcesoAdopcionEmpresa`**:
  - `fechaImplementacion DATETIME2 NULL`.
  - `idContactoPartner INT NULL` con FK a `dbo.TContactoPartner(idContactoPartner)`.
  - `urlArquitectura NVARCHAR(1000) NULL`.
  - `archivoArquitecturaNombre NVARCHAR(255) NULL`.
  - `archivoArquitecturaContenido VARBINARY(MAX) NULL`.
  - `archivoArquitecturaContentType NVARCHAR(100) NULL`.
  - `idTecnologiaAsIs INT NULL` con FK a `dbo.TTecnologiaTSI(idTecnologiaTSI)`.

#### Script: `scripts/database/evolve-vendor-and-technology-version.sql`
- **`dbo.TTecnologiaTSI`**:
  - `idVendor INT NULL` con FK a `dbo.TMVendor(idVendor)`.
  - `idVersionDesplegada INT NULL` con FK a `dbo.TMVersionDesplegada(idVersionDesplegada)`.
- **`dbo.TTecnologiaTSIimplementadaSubsidiaria`**:
  - `idVersionDesplegada INT NULL` con FK a `dbo.TMVersionDesplegada(idVersionDesplegada)` y backfill automático desde `TMVersionDesplegada` por coincidencia de texto.

### 1.2 Capa de Aplicación e Infraestructura
- **`AdoptionProcessService.Implementations.cs`**:
  - `GetImplementationProcessAsync`: Carga del proceso de implementación con empresas, especialistas (subsidiaria y partner), contratos y drivers.
  - `UpdateTechnicalProgressAsync`: Persistencia del avance técnico por subsidiaria, incluyendo tecnología AS-IS, fechas, especialista dual y adjuntos/URLs de arquitectura.
  - `DownloadArchitectureAttachmentAsync`: Descarga segura del archivo binario de arquitectura.
  - Reportería con inicialización en `0` para volumetrías sin datos capturados.
- **`CatalogManagementService.cs`**:
  - Endpoints para obtener tecnologías asociadas a un Vendor (`GetVendorTechnologiesAsync`), asociar tecnología (`AssociateVendorTechnologyAsync`), desasociar tecnología (`DisassociateVendorTechnologyAsync`), y registro rápido de tecnología (`CreateTechnologyForVendorAsync`).

### 1.3 Capa Web y Presentación
- Vistas Razor:
  - `CreateImplementation.cshtml`: Formulario escalonado con selectores de familia, subfamilia, building block, fechas de vigencia contractual, tecnología decretada y empresas convocadas.
  - `Implementations.cshtml`: Bandeja ejecutiva de procesos de implementación con indicadores de estado y avance.
  - `ImplementationDetails.cshtml`: Detalle del proceso, resumen con tecnología estándar ("Por definir" si ausente), listado de subsidiarias con tecnología AS-IS, semáforos verde/amarillo/rojo de alineamiento, y modal de actualización técnica.
  - `ImplementationReports.cshtml`: Reportería ejecutiva de implementación con 4 pestañas y cabeceras a 90 grados (`writing-mode: vertical-lr; transform: rotate(180deg)`).
  - `_VendorTechnologiesModal.cshtml`: Modal reutilizable con pestañas para tecnologías asociadas, búsqueda/asociación y alta rápida.
- JavaScript:
  - `vendor-admin.js`: Lógica client-side para modal de tecnologías por vendor, debouncing de búsqueda, confirmaciones de desasociación y notificaciones Toastr/SweetAlert.
