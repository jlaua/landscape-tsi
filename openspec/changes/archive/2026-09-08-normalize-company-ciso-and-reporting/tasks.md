## 1. Prevalidación y dependencias

- [x] 1.1 Ejecutar consultas SELECT contra `TEmpresaSubsidiaria` y `TCISO` para producir totales, buckets de cardinalidad, representantes duplicados/ausentes y verificar los resultados en un reporte de análisis sin modificar datos.
- [x] 1.2 Buscar `contactoCiso` en entidades, EF, SQL, servicios, controladores, Razor, ViewModels, JavaScript, tests y objetos SQL, y entregar un inventario verificable de dependencias.
- [x] 1.3 Analizar duplicados de `Representante = 1` y redactar la recomendación de índice único filtrado, su impacto y rollback; verificar que no se ejecute DDL.

## 2. Modelo de consulta y seguridad

- [x] 2.1 Definir el contrato paginado del reporte con vistas por empresa y por CISO, campos funcionales y estados de representante; verificar que no exponga IDs técnicos como columnas principales.
- [x] 2.2 Implementar la consulta server-side con LEFT JOIN, proyección, filtros opcionales parametrizados, AsNoTracking y paginación; verificar ausencia de N+1 mediante pruebas/SQL generado.
- [x] 2.3 Aplicar autorización de Reportería y alcance organizacional a filas y KPIs; verificar acceso denegado fuera del alcance.
- [x] 2.4 Resolver las rutas de Empresa/Subsidiaria y CISO mediante metadata/backend; verificar que los endpoints administrables respondan 200/302/403 y nunca 404.

## 3. Interfaz Empresas y CISO

- [x] 3.1 Agregar la entrada de navegación y ruta `/reporteria/empresas-ciso` conforme a la convención existente; verificar navegación autenticada.
- [x] 3.2 Renderizar filtros, selector de vista, KPIs, tabla semántica, estados de datos inconsistentes y estado vacío con el patrón común de grillas; verificar responsive y accesibilidad.
- [x] 3.3 Implementar navegación Ver Empresa/Ver CISO y conservar filtros/paginación en query string; verificar rutas desde ambas vistas.

## 4. Pruebas

- [x] 4.1 Cubrir empresas con cero, uno, varios CISO, representantes ausentes y múltiples representantes.
- [x] 4.2 Cubrir búsqueda, filtros, LEFT JOIN, vistas, paginación, KPIs y valores sin coincidencias con HTTP 200.
- [x] 4.3 Cubrir autorización organizacional, navegación no-404 y ausencia de consultas N+1.
- [x] 4.4 Ejecutar `dotnet build` y `dotnet test`; verificar que no haya migrations ni escrituras SQL.

## 5. Propuesta posterior de normalización

- [x] 5.1 Documentar los cambios de código necesarios para dejar de leer `contactoCiso` y la migration futura de retirada, incluyendo backup, riesgos y rollback.
- [x] 5.2 Detenerse para aprobación explícita antes de crear/aplicar cualquier migration, índice filtrado, DROP COLUMN o backfill.
