## 1. Prevalidación de modelo y datos

- [x] 1.1 Consultar en `db-landscape-tsi-dev-v2` las FK reales de `TBuildingBlockVsTTecnologiaTSI` y verificar columnas, cardinalidad y tablas referenciadas; entregar reporte de solo lectura.
- [x] 1.2 Consultar índices, PK, UNIQUE y duplicados por `(idBuildingBlock,idTecnologiaTSI)` en los ambientes autorizados; no ejecutar DDL/DML y documentar impacto.
- [x] 1.3 Confirmar y reutilizar la relación única en `CatalogRelationshipMetadata`; agregar pruebas de que no se inventan relaciones ni rutas.

## 2. Application y autorización

- [x] 2.1 Crear DTOs y servicio de consulta con whitelist, proyección `AsNoTracking`, filtros, KPIs y paginación server-side; verificar consultas sin N+1.
- [x] 2.2 Crear comandos transaccionales idempotentes para asociar/desasociar pares; verificar que no modifiquen entidades principales.
- [x] 2.3 Definir o reutilizar permisos/policies para ver y administrar relaciones, validar alcance organizacional y manejar `DbUpdateException`; probar respuestas 403.
- [x] 2.4 Registrar auditoría de asociación y desasociación sin secretos; verificar actor, UTC, pares y resultado.

## 3. Interfaz y navegación

- [x] 3.1 Agregar la opción y ruta de Mapeo de Tecnologías TSI mediante metadata/backend; probar que las rutas de detalles no devuelvan 404.
- [x] 3.2 Implementar dashboard responsive con KPIs, filtros, badges Mapeada/Sin mapear, tabla y paginación; verificar accesibilidad WCAG 2.2 AA.
- [x] 3.3 Implementar selección múltiple y confirmación explícita de desasociación; verificar que el texto no confunda desasociar con eliminar entidades.
- [x] 3.4 Integrar acciones `+ Asociar Tecnología` y `Administrar relaciones` en detalles de Building Block y Tecnología TSI reutilizando componentes.

## 4. Integridad y esquema futuro

- [x] 4.1 Preparar validación de concurrencia y resultados idempotentes cuando no exista constraint de unicidad; cubrir carreras en pruebas.
- [x] 4.2 Proponer migration separada para PK compuesta o UNIQUE según el reporte de duplicados y compatibilidad EF; mostrar SQL y no aplicarla sin aprobación.
- [x] 4.3 Documentar permisos mínimos, rollback y deuda técnica de la tabla puente; verificar que no se alteren catalogos funcionales.

## 5. Pruebas y entrega

- [x] 5.1 Agregar pruebas de listado, filtros, KPIs, paginación, asociaciones, desasociaciones, duplicados, autorización y rutas.
- [x] 5.2 Ejecutar `dotnet build` y `dotnet test`; validar que no haya escrituras fuera del ambiente autorizado.
- [x] 5.3 Realizar validación visual local de Tecnología sin mapear, Tecnología mapeada y Building Block con varias Tecnologías; revisión visual aprobada antes de aplicar el DDL autorizado.
