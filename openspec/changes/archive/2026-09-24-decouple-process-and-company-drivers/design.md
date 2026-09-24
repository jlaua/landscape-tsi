# Design: Desacoplamiento de Drivers a Nivel Proceso y Empresa

## 1. Modelo de Datos Físico

### `dbo.TDriverProceso` (Plantilla del Proceso)
- `idDriverProceso` INT IDENTITY PRIMARY KEY
- `idProcesoAdopcionTSI` INT NOT NULL FK -> `TProcesoAdopcionTSI`
- `descripcionDriver` NVARCHAR(250) NOT NULL
- `unidadMedida` NVARCHAR(50) NULL
- `ordenVisual` INT NOT NULL DEFAULT 0
- Campos de auditoría: `fechaCreacion`, `creadoPor`, `fechaModificacion`, `modificadoPor`

### `dbo.TDriverEmpresa` (Valores de Consumo por Subsidiaria)
- `idDriverEmpresa` INT IDENTITY PRIMARY KEY
- `idProcesoAdopcionEmpresa` INT NOT NULL FK -> `TProcesoAdopcionEmpresa`
- `idDriverProceso` INT NOT NULL FK -> `TDriverProceso`
- `cantidad` DECIMAL(18,2) NOT NULL DEFAULT 0
- `precioUnitario` DECIMAL(18,2) NOT NULL DEFAULT 0
- `costoTotal` DECIMAL(18,2) NOT NULL DEFAULT 0
- `moneda` VARCHAR(5) NOT NULL DEFAULT 'USD'
- Campos de auditoría: `fechaCreacion`, `creadoPor`, `fechaModificacion`, `modificadoPor`
- Restricción Única Compuesta: `UQ_TDriverEmpresa_Empresa_Driver (idProcesoAdopcionEmpresa, idDriverProceso)`

## 2. Dominio y Capas .NET 10

- **Dominio:** Nuevas entidades `TDriverProceso` y `TDriverEmpresa` en `Landscape.Tsi.Domain/Adoption/AdoptionEntities.cs`.
- **Persistencia EF Core:** Mapeo de tablas y relaciones con cascada controlada en `CatalogDbContext.cs`.
- **Servicios:**
  - `SaveProcessDriverAsync`: Inserta/actualiza en `ProcessDrivers` y genera registros faltantes en `CompanyDrivers` para cada subsidiaria en `AdoptionProcessCompanies`.
  - `DeleteProcessDriverAsync`: Elimina el template y sus filas por empresa en cascada, registrando auditoría.
  - `UpdateCompanyDriverValueAsync`: Modifica `cantidad`, `precioUnitario`, `moneda` y recalcula `costoTotal` con auditoría.
  - `BatchConveneCompaniesAsync`: Clona automáticamente todos los templates existentes al convocar nuevas empresas.
  - `GetEvaluationReportsAsync`: Prioriza `ProcessDrivers` y `CompanyDrivers` para columnas dinámicas y celdas de la matriz de volumetría.
- **Controlador:** Endpoints `SaveProcessDriver`, `DeleteProcessDriver`, y `UpdateCompanyDriverValue` en `AdoptionProcessController.cs`.
- **Vistas MVC:**
  - `Details.cshtml`: Sección 2 para plantilla de drivers corporativos con modal `#modal-save-process-driver`. Bloque de drivers de empresa en cada tarjeta de subsidiaria con modal `#modal-edit-company-driver` y guardado AJAX reactivo. Eliminación del botón y tabla per-tecnología de drivers obsoletos.
