# Design: Desacoplamiento de Tecnología Ficticia y Normalización de "Sin tecnología registrada"

## Arquitectura de la Solución

### 1. Eliminación de Auto-Creación de Registros de Tecnología Implementada
- **Problema previo:** En `UpdateCompanyDriverVolumeAsync`, cuando `implTech is null`, el método consultaba `dbContext.Technologies.Select(t => t.Id).FirstOrDefaultAsync()`, obteniendo `Id = 1` (Imperva), e insertaba una entidad en `TTecnologiaTSIimplementadaSubsidiaria`.
- **Diseño corregido:** La entidad `TTecnologiaTSIimplementadaSubsidiaria` representa únicamente tecnologías efectivamente implementadas y declaradas por la subsidiaria. Con el desacoplamiento de drivers (`TDriverProceso` y `TDriverEmpresa`), la captura de volumetría no depende de la existencia de una tecnología previa.
- Si `implTech is null`, el servicio omite la inserción en la tabla legacy `TDriver` y persiste exclusivamente en `TDriverEmpresa`.

### 2. Contrato de Datos para Reportería y Diagnóstico AS-IS
- En `GetEvaluationReportsAsync`:
  - `primaryImpl`: se busca primero en el proceso actual y secundariamente en el catálogo base del Building Block.
  - Si `primaryImpl is null`:
    - `techNombre`: `"Sin tecnología registrada"`
    - `tipoContrato`: `"—"`
    - `vendor`: `"—"`
    - `partner`: `"—"`
    - `tipoOperacion`: `"—"`
- En las vistas Razor (`Reports.cshtml`):
  - Se evalúa si el nombre de tecnología es nulo o igual a `"Sin tecnología registrada"` / `"No configurada"`, mostrándose con la clase Bootstrap `.badge .bg-secondary-subtle .text-secondary .border`.

### 3. Migración y Limpieza de Datos
- Script de remediación ejecutado bajo transacción en `db-landscape-tsi-dev-v2` para limpiar IDs 200..216 y 221..224 de `TTecnologiaTSIimplementadaSubsidiaria` y sus dependencias huérfanas en `TDriver`.
