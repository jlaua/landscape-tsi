## Context

El módulo actual ya dispone de `/reporteria`, Chart.js, `IReportingService`, un registro controlado de catálogos y endpoints de lectura para totales, detalle y relaciones. El vertical slice debe reutilizar esa base sin modificar el esquema ni habilitar escritura. La motivación y el alcance funcional están en `proposal.md`; los contratos observables están en `specs/reporting/landscape-explorer/spec.md`.

## Goals / Non-Goals

**Goals:**

- Validar antes de habilitar relaciones que `db-landscape-tsi-dev` confirme PK, FK, columnas y cardinalidad mediante `sys.foreign_keys`, `sys.foreign_key_columns`, `sys.tables` y `sys.columns`.
- Implementar la primera ruta `Dominio -> Building Block` con detalle, KPI directo, gráfico relacionado y grilla filtrada server-side.
- Mantener nombres funcionales y códigos de whitelist separados de tablas y columnas físicas.
- Proteger todos los endpoints con `CatalogView` y mantenerlos de solo lectura.
- Controlar el estado de selección, solicitudes concurrentes e instancias Chart.js.
- Dejar preparada la extensión posterior a Capacidades, Funcionalidades y la rama tecnológica sin activar esos KPIs en esta fase.

**Non-Goals:**

- No implementar todavía Capacidades de Seguridad, Funcionalidades ni Tecnologías TSI como KPIs del registro seleccionado.
- No presentar una relación directa entre Funcionalidad y Tecnología TSI.
- No implementar la tabla puente tecnológica hasta confirmar su estructura real.
- No ejecutar migraciones, DDL, DML, `database update` ni escrituras sobre ninguna base.
- No implementar exportación, comparación, favoritos ni filtros organizacionales nuevos en este vertical slice.

## Decisions

### Metadatos y whitelist

Se conservará `MasterCatalogRegistry` como fuente controlada de códigos funcionales, columnas de presentación y relaciones conocidas. Antes de implementar el vertical slice se ejecutará una inspección de solo lectura contra `db-landscape-tsi-dev` y se documentará el resultado. El código recibido por HTTP se resolverá contra la whitelist; los identificadores SQL se tomarán únicamente de definiciones verificadas.

La relación inicial se habilitará solo si la inspección confirma:

```text
dbo.TMDominio.iddominio
  <- dbo.TBuildingBlock.idDominio
```

La cadena posterior se conservará como metadato para fases futuras:

```text
Dominio -> Building Block -> Capacidad de Seguridad -> Funcionalidad
Building Block <-> Tecnología TSI -> Casos de Uso
```

### Capa de lectura

Se ampliará la capa de aplicación con DTOs específicos de reportería y un servicio de lectura. Las consultas deberán proyectar solo columnas funcionales, usar operaciones asíncronas, `AsNoTracking()` cuando la consulta se ejecute sobre entidades EF, `CountAsync()` y agrupaciones para conteos. Cuando un catálogo dinámico no tenga una entidad EF adecuada, se usará una consulta controlada con identificadores de whitelist y parámetros para valores, sin interpolar entradas HTTP.

La grilla relacionada recibirá `parentId`, búsqueda, columna de orden permitida, dirección, página y tamaño; el servidor filtrará por la FK y devolverá solo la página solicitada.

### Contrato HTTP

Se mantendrán rutas funcionales bajo `/reporteria/api/catalogos`:

```text
GET /reporteria/api/catalogos
GET /reporteria/api/catalogos/dominio/detalle
GET /reporteria/api/catalogos/dominio/registros/{id}/kpis
GET /reporteria/api/catalogos/dominio/registros/{id}/relaciones/building-block
```

Los endpoints devolverán DTOs JSON sin nombres físicos innecesarios. Los errores distinguirán catálogo inválido, registro inexistente, ausencia de relación y error de lectura.

### Estado de interfaz

El frontend mantendrá un estado explícito con catálogo, registro, relación, filtros, página, orden y un identificador de solicitud. Cada respuesta comprobará que corresponde al estado vigente antes de pintar la UI. El estado inicial ocultará la grilla contextual; seleccionar `Dominio` la mostrará y seleccionarlo en la grilla activará el KPI y el gráfico relacionado.

Los gráficos se gestionarán por identificador. Si solo cambian datos se usará `chart.update()`; si cambia el contexto o canvas se llamará `chart.destroy()` antes de crear otra instancia. La alternativa tabular seguirá disponible para accesibilidad.

### KPIs

En esta fase solo se implementará:

```text
Building Blocks = COUNT(TBuildingBlock.idBuildingBlock)
                  filtrado por idDominio
```

No se calcularán capacidades, funcionalidades ni tecnologías TSI. Las fases futuras usarán `COUNT(DISTINCT ...)` sobre relaciones verificadas para evitar duplicados.

### Seguridad y alcance

El controlador continuará protegido con `Permissions.CatalogView`. No se agregarán acciones de escritura. Si el modelo de datos contiene alcance por organización o subsidiaria aplicable a los catálogos, el servicio de reportería deberá aplicar el mismo filtro antes de contar o paginar.

## Risks / Trade-offs

- **[Metadatos SQL diferentes del registro actual]** -> detener la activación de la relación, corregir la definición controlada y no inferir relaciones por nombres.
- **[Catálogos dinámicos sin entidades EF completas]** -> usar consultas read-only controladas con whitelist y documentar la excepción a `AsNoTracking`.
- **[Conteos costosos en tablas grandes]** -> usar agregaciones server-side, índices existentes y paginación; medir antes de habilitar ramas transitivas.
- **[Respuestas AJAX fuera de orden]** -> cancelar solicitudes cuando sea posible y validar un request/version token antes de actualizar el estado.
- **[Instancias Chart.js acumuladas]** -> mantener un registro por canvas y destruirlo al cambiar de contexto.
- **[Diferencias desktop/móvil]** -> presentar relaciones como secciones apilables y mantener tabla accesible equivalente.
- **[Cambio accidental de alcance organizacional]** -> reutilizar los filtros de autorización existentes y cubrirlos con pruebas de servidor.

## Migration Plan

1. Ejecutar y registrar únicamente consultas de metadatos contra `db-landscape-tsi-dev`.
2. Implementar contratos, servicio read-only, endpoints y UI del vertical slice.
3. Compilar y ejecutar todas las pruebas.
4. Iniciar localmente y revisar visualmente `/reporteria`.
5. Si la revisión falla, retirar solo la UI/servicio del vertical slice; no hay cambios de esquema que revertir.
6. Mantener las ramas de Capacidad, Funcionalidad y Tecnología TSI deshabilitadas hasta un cambio OpenSpec posterior.

## Validación de metadatos realizada

Las consultas de solo lectura se ejecutaron contra `db-landscape-tsi-dev` mediante `sys.foreign_keys`, `sys.foreign_key_columns`, `sys.tables`, `sys.columns`, `sys.indexes` y `sys.index_columns`.

Relaciones confirmadas:

```text
TBuildingBlock.idDominio -> TMDominio.iddominio
TCapacidadDeSeguridad.idBuildingBlock -> TBuildingBlock.idBuildingBlock
TFuncionalidad.idCapacidad -> TCapacidadDeSeguridad.idCapacidad
TBuildingBlockVsTTecnologiaTSI.idBuildingBlock -> TBuildingBlock.idBuildingBlock
TBuildingBlockVsTTecnologiaTSI.idTecnologiaTSI -> TTecnologiaTSI.idTecnologiaTSI
TCasosDeUso.idTecnologiaTSI -> TTecnologiaTSI.idTecnologiaTSI
```

PK confirmadas:

```text
TMDominio: iddominio
TBuildingBlock: idBuildingBlock
TCapacidadDeSeguridad: idCapacidad
TFuncionalidad: idFuncionalidad
TTecnologiaTSI: idTecnologiaTSI
TCasosDeUso: idCasosDeUso
```

`TBuildingBlockVsTTecnologiaTSI` no tiene PK ni índice único declarado. En la muestra actual contiene una fila, un par distinto y no presenta nulos ni duplicados. Se tratará como tabla puente muchos-a-muchos y se usarán conteos `DISTINCT` cuando se habilite en una fase posterior; no se utilizará en el vertical slice actual.
