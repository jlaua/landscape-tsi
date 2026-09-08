# Revisión del paquete de despliegue de Tablas Maestras

- El despliegue de esta capacidad es únicamente de aplicación y vistas MVC.
- `dotnet build --configuration Release` y la suite de pruebas se ejecutan
  antes de promover.
- No se agregaron migrations para las 16 tablas; `CatalogDbContext` excluye
  esos objetos de migrations.
- Los secretos se obtienen de configuración segura/variables de entorno y no
  se incluyen en `appsettings` versionados.
- Los scripts SQL existentes que contienen DDL pertenecen a cambios aprobados
  distintos y no forman parte del paquete de Tablas Maestras ni se ejecutan
  automáticamente durante el arranque.
- No se autoriza despliegue a PROD desde este cambio. El rollback funcional es
  deshabilitar mutaciones y conservar lectura.

