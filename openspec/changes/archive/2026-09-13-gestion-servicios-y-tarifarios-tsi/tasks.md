## 1. Base de Datos y Esquema DDL Seguro

- [x] 1.1 Crear scripts/database/evolve-service-schema.sql con verificación de DB_NAME() = 'db-landscape-tsi-dev-v2', creando las tablas dbo.TTipoServicio, dbo.TServicioTecnologia, dbo.TTarifarioProyectoHoras, dbo.TActividadNivelSoporte y dbo.TTarifarioOperacion, índices, FKs y semillas maestras.
- [x] 1.2 Implementar y ejecutar prueba de integración en 	ests/Landscape.Tsi.Tests/Catalogs/ServiceSchemaIntegrationTests.cs verificando la aplicación segura y la existencia de las 5 tablas y columnas.

## 2. Capa de Dominio y Persistencia EF Core

- [x] 2.1 Crear entidades fuertemente tipadas en src/Landscape.Tsi.Domain/Adoption/ServiceEntities.cs con propiedades, restricciones y constantes de negocio.
- [x] 2.2 Configurar el mapeo en CatalogDbContext en src/Landscape.Tsi.Infrastructure/Persistence/CatalogDbContext.cs con configuración Fluent API para claves foráneas y cascadas controladas.

## 3. Lógica de Aplicación y Cálculo de Tarifarios

- [x] 3.1 Definir contratos e interfaces DTOs en src/Landscape.Tsi.Application/Adoption/ (IServiceManagementService, modelos de entrada y salida para proyectos por horas y operaciones N1-N3).
- [x] 3.2 Implementar ServiceManagementService en src/Landscape.Tsi.Infrastructure/Adoption/ServiceManagementService.cs con cálculo automático de subtotales, totales y registro en auditoría.
- [x] 3.3 Escribir pruebas unitarias en 	ests/Landscape.Tsi.Tests/Adoption/ServiceManagementTests.cs cubriendo cálculos de tarifas de implementación, migración y soporte N1-N3 en distintas modalidades.

## 4. Interfaz de Usuario y Vistas Razor

- [x] 4.1 Extender AdoptionProcessController (o controlador dedicado de servicios) con acciones para consultar, agregar y modificar servicios y líneas de tarifario.
- [x] 4.2 Incorporar pestaña o bloque de "Servicios y Tarifarios" en src/Landscape.Tsi.Web/Views/AdoptionProcess/Details.cshtml permitiendo ver servicios corporativos y por subsidiaria, con modales interactivos para registrar proyectos por horas y matrices operativas.

## 5. Verificación Integral y Calidad

- [x] 5.1 Ejecutar dotnet build --configuration Release asegurando 0 errores y 0 advertencias.
- [x] 5.2 Ejecutar dotnet test --configuration Release garantizando 100% de pruebas pasando.
- [x] 5.3 Ejecutar dotnet format para adherencia de estilo y documentar walkthrough.
