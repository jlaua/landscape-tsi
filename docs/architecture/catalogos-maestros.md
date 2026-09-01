# Catálogos maestros autoritativos de Landscape TSI

## 1. Propósito

Este documento registra los catálogos maestros existentes en SQL Server que
forman parte oficial del dominio Landscape TSI. La base de datos existente es
la fuente de verdad de sus identificadores y valores.

La aplicación debe consumir estos catálogos mediante servicios o consultas de
la capa de aplicación. Las vistas ASP.NET Core MVC pueden presentarlos, pero no
deben definirlos, duplicarlos ni hardcodear sus identificadores o etiquetas.

## 2. Correspondencia conceptual y física

| Concepto de dominio | Tabla SQL autoritativa | Clave primaria | Tabla que la referencia | Clave foránea |
|---|---|---|---|---|
| Estado de Adopción TSI | `dbo.TMEstadoAdopcionTSI` | `idEstadoAdopcionTSI` | `dbo.TTecnologiaTSI` | `idEstadoAdopcionTSI` |
| Fase de Adopción | `dbo.TEstadoFaseAdopcion` | `idEstadoFaseAdopcion` | `dbo.TBuildingBlock` | `idEstadoFaseDeAdopcionBuildingBlock` |
| Estado de Capacidad | `dbo.TMEstadoCapacidad` | `idEstadoCapacidad` | `dbo.TCapacidadDeSeguridad` | `idEstadoCapacidad` |
| Cobertura de Funcionalidad | `dbo.TMEstadoFuncionalidad` | `idEstadoCoberturaFuncionalidad` | `dbo.TFuncionalidad` | `idEstadoCoberturaFuncionalidad` |

Las cuatro claves primarias son `int IDENTITY`. Las claves foráneas observadas
están habilitadas y son confiables para SQL Server. Actualmente permiten
`NULL` y tienen acciones referenciales `NO_ACTION` para actualización y
eliminación.

## 3. Valores vigentes

### 3.1. Estado de Adopción TSI

Representa el nivel o madurez de adopción de una tecnología TSI. No representa
el estado administrativo de una solicitud.

| ID | Valor |
|---:|---|
| 1 | `SIN EVALUACION` |
| 2 | `PRE-CALIFICADA` |
| 3 | `CALIFICADA` |
| 4 | `ESTANDARIZADA` |
| 5 | `IMPLEMENTADO` |

### 3.2. Fase de Adopción

Representa la fase dentro del ciclo de adopción tecnológica.

| ID | Valor |
|---:|---|
| 1 | `EXPLORACION` |
| 2 | `EVALUACION` |
| 3 | `ESTANDARIZACION` |
| 4 | `IMPLEMENTACION` |
| 5 | `NO DEFINIDO` |

### 3.3. Estado de Capacidad

| ID | Valor |
|---:|---|
| 1 | `CUBIERTO` |
| 2 | `PARCIALMENTE` |
| 3 | `NO CUBIERTO` |
| 4 | `NO APLICA` |
| 5 | `NO DEFINIDO` |

### 3.4. Cobertura de Funcionalidad

| ID | Valor |
|---:|---|
| 1 | `CUBIERTO` |
| 2 | `PARCIALMENTE` |
| 3 | `NO CUBIERTO` |
| 4 | `NO APLICA` |
| 5 | `NO DEFINIDO` |

Las descripciones de los registros estaban vacías al realizar la inspección de
solo lectura. La aplicación no debe depender de que esas descripciones tengan
contenido.

## 4. Separación semántica obligatoria

Los catálogos anteriores tienen significados distintos y no son
intercambiables:

- **Estado de Solicitud:** controla el workflow administrativo, las
  transiciones y las acciones disponibles sobre una Solicitud de Adopción. Su
  catálogo y máquina de estados aún deben definirse mediante OpenSpec.
- **Estado de Adopción TSI:** representa la madurez o situación de adopción de
  una tecnología TSI.
- **Fase de Adopción:** representa la fase del ciclo tecnológico asociada al
  Building Block.
- **Estado de Capacidad:** expresa el nivel de cobertura de una capacidad de
  seguridad.
- **Cobertura de Funcionalidad:** expresa la cobertura de una funcionalidad o
  feature.

Estados de solicitud como `Borrador`, `Enviada`, `En evaluación`, `Observada`,
`En revisión de gobierno`, `Aprobada`, `Rechazada` o `Cancelada` no deben
incorporarse a ninguno de los cuatro catálogos existentes.

## 5. Contrato de consumo de aplicación

- SQL Server es la fuente de verdad de los identificadores y valores.
- La capa de aplicación debe exponer servicios o consultas de catálogo.
- La capa de presentación debe solicitar las opciones al servidor y usar el
  identificador como valor y el nombre recuperado como etiqueta.
- El servidor debe verificar que todo identificador recibido existe y es
  válido para el concepto correspondiente.
- Ocultar o filtrar opciones en la interfaz no sustituye la validación del
  servidor.
- La lógica no debe asumir que siempre existirán exactamente cinco registros.
- Los modelos de Entity Framework Core deben mapear las tablas existentes sin
  asumir propiedad migratoria sobre sus datos.
- Los identificadores existentes deben preservarse mientras formen parte de
  relaciones persistidas.

## 6. Protección frente a migraciones

Las migraciones y procesos automáticos de aplicación no deben:

- eliminar los catálogos o sus registros;
- renombrar tablas, columnas o valores;
- reemplazarlos por catálogos equivalentes;
- recrearlos bajo otro módulo o esquema;
- insertar o sembrar copias de sus valores;
- actualizar o eliminar automáticamente registros existentes;
- ejecutar reseeding de sus columnas `IDENTITY`.

Toda migración debe revisarse para confirmar que no contiene DDL ni DML contra
estos catálogos. Cualquier cambio futuro requiere un cambio OpenSpec aprobado y
una autorización operativa independiente antes de actuar sobre producción.

## 7. Gobierno de cambios futuros

La incorporación de un valor, el cambio de su significado o el retiro de un
valor existente debe:

1. documentarse mediante OpenSpec;
2. identificar las relaciones y comportamientos afectados;
3. evaluar compatibilidad con los identificadores existentes;
4. definir el impacto en formularios, validaciones, informes y auditoría;
5. contar con autorización operativa independiente para producción.

Un valor no debe eliminarse mientras esté referenciado. Si se requiere dejar
de utilizarlo, debe definirse explícitamente una estrategia de vigencia o
inactivación antes de proponer cualquier cambio físico.

## 8. Observaciones de integridad actuales

- Las cuatro FK permiten `NULL`. Por ello, la ausencia de clasificación puede
  expresarse mediante `NULL` o mediante `NO DEFINIDO`; la regla de negocio debe
  aclararse antes de implementar validaciones más estrictas.
- Los nombres no tienen una restricción funcional de unicidad observada.
- Las columnas de nombre y descripción son `nvarchar(max)`, aunque los valores
  actuales son cortos.
- Estas observaciones describen el estado actual y no autorizan modificar el
  esquema ni los datos.

