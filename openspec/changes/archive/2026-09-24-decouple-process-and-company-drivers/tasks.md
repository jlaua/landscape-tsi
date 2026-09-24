# Tasks: Desacoplamiento de Drivers Tecnológicos

- [x] 1. Crear tablas `dbo.TDriverProceso` y `dbo.TDriverEmpresa` en SQL Server con llaves foráneas, índices y restricción de unicidad compuesta.
- [x] 2. Ejecutar script de migración idempotente: 33 plantillas migradas a `TDriverProceso` y 725 registros generados/migrados en `TDriverEmpresa`.
- [x] 3. Modelar entidades `TDriverProceso` y `TDriverEmpresa` en `Landscape.Tsi.Domain`.
- [x] 4. Configurar mappings EF Core y DbSets en `CatalogDbContext`.
- [x] 5. Definir DTOs y comandos en `Landscape.Tsi.Application` (`ProcessDriverTemplateDto`, `CompanyDriverValueDto`, etc.).
- [x] 6. Implementar métodos de servicio en `AdoptionProcessService`: `SaveProcessDriverAsync`, `DeleteProcessDriverAsync`, `UpdateCompanyDriverValueAsync` y auto-clonado en `BatchConveneCompaniesAsync`.
- [x] 7. Crear endpoints en `AdoptionProcessController`: `POST ProcessDriver/Save`, `POST ProcessDriver/Delete`, `POST driver-value`.
- [x] 8. Actualizar vista `Details.cshtml`: nueva Sección 2 de drivers del proceso, bloque de consumo por subsidiaria y modales reactivos con recálculo dinámico.
- [x] 9. Eliminar botón y tabla per-tecnología de drivers en tarjetas de tecnología implementada para evitar ambigüedades.
- [x] 10. Actualizar `CatalogDbContextMappingTests` y `AdoptionProcessHttpTests` para cubrir las nuevas entidades y servicios.
- [x] 11. Ejecutar compilación y suite completa de pruebas: 347/347 pruebas superadas al 100%.
