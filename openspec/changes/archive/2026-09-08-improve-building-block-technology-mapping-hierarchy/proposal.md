## Why

La ruta `/Administration/TechnologyMapping` presenta actualmente a Tecnología TSI como entidad principal, aunque el modelo funcional requerido parte de cada Building Block y muestra debajo sus Tecnologías TSI relacionadas. La jerarquía debe corregirse sin reabrir el cambio de mapeo ya archivado ni perder el análisis complementario "Building Blocks por Familia".

## What Changes

- Convertir Building Block en la entidad principal del listado, búsqueda y paginación de la pantalla de mapeo.
- Mostrar Dominio, Fase de adopción, cantidad y detalle expandible de Tecnologías TSI como información dependiente de cada Building Block.
- Permitir búsqueda, filtros por Dominio, Fase, Familia y estado, además de ordenamiento y paginación server-side por Building Block.
- Ofrecer desde cada Building Block la administración explícita de Tecnologías asociadas y disponibles, con acciones separadas para asociar y desasociar.
- Agregar una vista secundaria de Tecnologías TSI sin asignar que permita asociarlas a un Building Block sin perder la vista principal.
- Reorganizar KPIs para incluir total de Building Blocks, con/sin Tecnología, total de Tecnologías, total de relaciones y cobertura calculada exclusivamente sobre Building Blocks.
- Mantener la sección "Building Blocks por Familia" en la parte inferior sin cambiar su lógica.
- Conservar autorización server-side, alcance organizacional, rutas generadas por backend, diseño responsive y WCAG 2.2 AA.
- No modificar el esquema ni los datos de SQL Server.

## Capabilities

### New Capabilities

Ninguna.

### Modified Capabilities

- `catalog/building-block-technology-mapping`: cambia la jerarquía principal a `Building Block -> Tecnologías TSI`, amplía consulta y administración, conserva tecnologías no asignadas y preserva el análisis por Familia.

## Impact

- Afecta consultas y modelos del mapeo, `TechnologyMappingController`, vistas Razor, estilos/scripts y pruebas web/de servicio.
- No introduce dependencias entre módulos, migrations, DDL ni cambios en las tablas existentes.
- Mantiene las policies de catálogo y el alcance organizacional existentes; no amplía permisos ni expone datos adicionales.
