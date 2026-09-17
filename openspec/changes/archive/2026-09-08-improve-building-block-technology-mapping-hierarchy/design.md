## Context

La implementación existente ya ofrece consultas, administración bidireccional, autorización y el reporte `Building Blocks por Familia`, pero el `Index` proyecta y pagina Tecnologías TSI como filas principales. Véanse `proposal.md` y el delta spec para el comportamiento requerido.

## Goals / Non-Goals

**Goals:**

- Reorientar la consulta y la presentación principal alrededor de Building Blocks con Dominio, Fase, conteo, filtros y ordenamiento sin consultas N+1.
- Reutilizar la ruta existente de administración de relaciones por Building Block.
- Separar Tecnologías asociadas y disponibles, y ofrecer una consulta secundaria de Tecnologías sin asignar.
- Preservar autorización, alcance organizacional, auditoría de cambios, reporte por Familia, responsive y WCAG 2.2 AA.

**Non-Goals:**

- Cambiar tablas, claves, índices, datos o migrations.
- Alterar los permisos atómicos o el significado de las relaciones N:M.
- Reimplementar o reabrir el cambio archivado de mapeo.

## Decisions

1. El servicio devolverá una página proyectada de Building Blocks con Dominio, Fase y `COUNT` de filas puente. La consulta raíz realizará filtros, ordenamiento y `OFFSET/FETCH`; una segunda consulta acotada por los identificadores de la página obtendrá el detalle expandible. Así el número de consultas no crece por fila.
2. Búsqueda y filtros operarán sobre Building Blocks. Dominio y Fase usarán las FK existentes; Familia se resolverá mediante la tabla puente y `TTecnologiaTSI`. Los valores de filtros provendrán de consultas de catálogo, nunca de IDs o etiquetas hardcodeados.
3. La acción primaria reutilizará `BuildingBlock/{id}/Relations` y las rutas de detalle seguirán generándose en backend. No se aceptarán nombres de tabla ni rutas arbitrarias desde HTTP.
4. La administración separará asociadas y disponibles. Las acciones unitarias POST recibirán únicamente identificadores validados, usarán antiforgery y reutilizarán la transacción/auditoría existente sobre la tabla puente.
5. La vista secundaria consultará Tecnologías mediante `NOT EXISTS` sobre la tabla puente, con paginación y asociación a Building Blocks permitidos; no cambiará cardinalidad ni normalizará relaciones múltiples.
6. `Building Blocks por Familia` permanecerá como sección independiente después del listado y reutilizará su lógica actual.
7. Las policies actuales `Catalog.View` y `Catalog.Edit`, junto con la evaluación contextual existente, seguirán protegiendo lectura y escritura. Este cambio no amplía alcance ni añade datos personales.

## Risks / Trade-offs

- [Agrupar tecnologías puede encarecer la consulta] → paginar primero identificadores de Building Block y cargar relaciones para esa página en una consulta acotada, verificando el número de comandos.
- [Cambiar nombres de parámetros puede romper enlaces guardados] → aceptar temporalmente parámetros compatibles cuando sea inocuo y cubrir rutas con pruebas.
- [La jerarquía móvil puede volverse difícil de escanear] → usar filas/tarjetas adaptables con etiquetas explícitas y pruebas del HTML accesible.
- [Ordenar por conteos puede aumentar el costo SQL] → calcular el agregado en servidor, limitar tamaños de página y revisar el plan sin crear índices en este cambio.

## Migration Plan

1. Cambiar contrato, consulta, controlador, vista y pruebas en una entrega atómica.
2. Ejecutar build, suite completa y validación visual local de estados con/sin tecnologías.
3. Desplegar sin migration ni DDL. Para rollback, revertir únicamente los archivos de aplicación de este cambio; la base permanece intacta.
