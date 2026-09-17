## 1. Consulta principal y contrato

- [x] 1.1 Reconciliar los DTOs y el contrato de mapeo para que Building Block sea la raíz y verificar con pruebas que cada fila contiene únicamente sus Tecnologías TSI relacionadas.
- [x] 1.2 Extender la proyección raíz con Dominio, Fase y cantidad de Tecnologías, verificando Building Blocks con cero, una y múltiples relaciones sin N+1.
- [x] 1.3 Implementar filtros server-side por búsqueda prioritaria de Building Block, Dominio, Fase, Familia, estado y solo pendientes; verificar cada filtro y sus combinaciones.
- [x] 1.4 Implementar ordenamiento server-side por Building Block, Dominio, Fase, cantidad y Estado, con Building Block ascendente predeterminado; verificar ascendente/descendente y valores inválidos.
- [x] 1.5 Implementar paginación por Building Block con total y navegación Anterior/Siguiente; verificar límites, página inexistente y estabilidad del orden.
- [x] 1.6 Ampliar KPIs con total de relaciones y verificar cobertura basada exclusivamente en Building Blocks, sin cargar relaciones en memoria.

## 2. Web y experiencia

- [x] 2.1 Adaptar controlador y ViewModels para Dominio, Fase, conteo, filtros, ordenamiento y navegación generada por backend; verificar rutas válidas y 404.
- [x] 2.2 Completar la grilla `Building Block | Dominio | Fase | Tecnologías TSI | Estado | Acciones`, sin IDs técnicos, usando chips `Mapeado/Pendiente`; verificar responsive, teclado y WCAG 2.2 AA.
- [x] 2.3 Implementar detalle expandible accesible con Tecnologías, Familia, estado aplicable, Ver, Desasociar y `+ Asociar tecnología`; verificar estados de cero, una y múltiples Tecnologías.
- [x] 2.4 Rediseñar `Administrar Tecnologías TSI` mostrando contexto del Building Block, asociadas primero y disponibles después, con acciones unitarias y confirmación de desasociación.
- [x] 2.5 Implementar asociación/desasociación idempotente que modifique solo la tabla puente; verificar duplicados, relaciones múltiples y auditoría existente.
- [x] 2.6 Conservar `Building Blocks por Familia` debajo del listado principal con gráfico, indicadores y detalle; verificar el orden del HTML y la interacción existente.
- [x] 2.7 Verificar que `Catalog.View`, `Catalog.Edit` y el alcance organizacional se evalúan server-side y que usuarios sin permiso reciben denegación sin acceso a datos.

## 3. Tecnologías sin asignar

- [x] 3.1 Implementar la vista secundaria `Tecnologías sin asignar` mediante `NOT EXISTS`, con búsqueda, Familia y paginación server-side; verificar Tecnología asociada y no asociada.
- [x] 3.2 Permitir asociar una Tecnología sin asignar a un Building Block autorizado sin alterar `TTecnologiaTSI`; verificar que desaparece de la vista y no se duplica.
- [x] 3.3 Mantener `Por Building Block` como vista predeterminada y navegación accesible entre vistas; verificar autorización y alcance en ambas.

## 4. Pruebas y entrega

- [x] 4.1 Completar pruebas no duplicadas para cero/una/múltiples Tecnologías, asociación, desasociación, duplicados, filtros, búsqueda, paginación, ordenamiento, navegación, autorización, KPIs, cobertura y vista sin asignar.
- [x] 4.2 Ejecutar `dotnet build --configuration Release`, `dotnet test --configuration Release` y `openspec validate --all --strict`; corregir cualquier fallo.
- [x] 4.3 Validar runtime y visualmente escritorio/móvil para la grilla, detalle expandible, administración y vista sin asignar, confirmando que `Building Blocks por Familia` permanece al final.
