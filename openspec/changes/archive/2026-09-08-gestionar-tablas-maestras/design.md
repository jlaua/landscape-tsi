## Context

Véase `proposal.md` para la motivación y `specs/catalogos-maestros/*` para los contratos observables. El repositorio sigue siendo una semilla sin solución ni código de aplicación; el diseño se integra conceptualmente con .NET 10, ASP.NET Core MVC, Entity Framework Core, SQL Server y el monolito modular aprobado.

La referencia visual `docs/ux/referencias/gestion-tablas-maestras.jpg` representa a Arquitecto de Seguridad Corporativo y Administrador administrando una selección cerrada de 16 tablas mediante Listar, Registrar, Actualizar y Borrar. Se conserva la selección y el ocultamiento de PK planteados; “Borrar” se sustituye por una política de retiro controlado porque la inspección real no encontró indicadores de activo/inactivo y sí encontró dependencias referenciales.

La inspección de producción se realizó exclusivamente con `sys.tables`, `sys.columns`, `sys.types`, `sys.indexes`, `sys.index_columns`, `sys.default_constraints`, `sys.check_constraints`, `sys.foreign_keys`, `sys.foreign_key_columns` y `sys.partitions`. No se ejecutaron DDL, DML ni procedimientos. Hallazgos comunes:

- los 16 objetos están en `dbo`;
- todas las PK son `int IDENTITY`, no anulables y el único índice de cada tabla;
- todas las columnas funcionales son anulables en SQL Server;
- no existen `DEFAULT`, `CHECK`, restricciones de unicidad funcional ni índices secundarios en estas tablas;
- todas las FK observadas están habilitadas, son confiables y usan `NO_ACTION` para actualización y eliminación;
- no existe columna de activo, vigencia o eliminación lógica en ninguno de los 16 objetos;
- la base existente no ofrece token de concurrencia (`rowversion`) en estos objetos.

### Inventario físico y dependencias

En la columna “Obligatorio en aplicación”, la regla se aplica a operaciones nuevas o editadas por este módulo; no altera la nulabilidad SQL ni invalida registros históricos incompletos.

| Catálogo / filas observadas | PK oculta | Obligatorio en aplicación | Opcionales existentes | FK salientes y dependencias entrantes | Modo inicial |
|---|---|---|---|---|---|
| Dominio / 9 | `iddominio` | `dominio` | `descripcionDominio`, `referencias`, `homologacionDimensionSegunCiber`, `homologacionDimensionSegunLineamiento`, `subDominioCVT`, `Ejemplos` | Referenciado por `TBuildingBlock.idDominio` | Pantalla completa; crear/editar; retiro bloqueado |
| Building Block / 147 | `idBuildingBlock` | `nombreBuildingBlock`, `idDominio` | `definicionBuildingBlock`, `idEstadoFaseDeAdopcionBuildingBlock`, `rutaDelEntregable`, `PilarZT` | FK a Dominio y Fase; referenciado por Capacidad y `TBuildingBlockVsTTecnologiaTSI` | Pantalla completa; crear/editar; retiro bloqueado |
| Capacidad de Seguridad / 175 | `idCapacidad` | `nombreCapacidad`, `idBuildingBlock` | `descripcionCapacidad`, `idEstadoCapacidad` | FK a Building Block y Estado; referenciada por Funcionalidad | Panel lateral; crear/editar; retiro bloqueado |
| Estado de Capacidad / 5 | `idEstadoCapacidad` | No aplica a mutación | `nombreEstadoCapacidad`, `descripcionEstadoCapacidad` son físicamente anulables | Referenciado por Capacidad | Diálogo de detalle; solo lectura autoritativa |
| Funcionalidad / 500 | `idFuncionalidad` | `nombreFuncionalidad`, `idCapacidad` | `descripcionFuncionalidad`, `idEstadoCoberturaFuncionalidad` | FK a Capacidad y Estado de Funcionalidad | Panel lateral; crear/editar; retiro bloqueado |
| Estado de Funcionalidad / 5 | `idEstadoCoberturaFuncionalidad` | No aplica a mutación | `nombreEstadoFuncionalidad`, `descripcionEstadoFuncionalidad` son físicamente anulables | Referenciado por Funcionalidad | Diálogo de detalle; solo lectura autoritativa |
| Fase de Adopción / 5 | `idEstadoFaseAdopcion` | No aplica a mutación | `nombreFaseAdopcion`, `descripcionFaseAdopcion` son físicamente anulables | Referenciada por Building Block | Diálogo de detalle; solo lectura autoritativa |
| Tecnología TSI / 1 | `idTecnologiaTSI` | `nombreTecnologiaAlternativa1-Corporativo` | nombre local, Familia, grupo, Estado de Adopción, Postura, tres fechas, licenciamiento, entorno, fuente, responsable, unidad, categoría y flag | FK a Familia, Estado y Postura; referenciada por BuildingBlock-vs-Tecnología, Casos de Uso, Implementación por Subsidiaria y Vendor | Pantalla completa por secciones; crear/editar; retiro bloqueado |
| Familia / 3 | `idFamilia` | `nombreFamilia` | `descripcionFamilia` | Referenciada por Tecnología TSI | Diálogo; crear/editar; retiro bloqueado |
| Casos de Uso / 1 | `idCasosDeUso` | `casoDeUso`, `idTecnologiaTSI` | `descripcionCasoDeUso` | FK a Tecnología TSI | Panel lateral; crear/editar; retiro bloqueado |
| Empresa / Subsidiaria / 24 | `idEmpresaSubsidiaria` | `nombreEmpresa` | `alias2`, `alias3-agrupador`, `Pais`, `ciudad`, `Rubro`, `contactoCiso` | Referenciada por CISO, Contacto Empresa, Regulación e Implementación por Subsidiaria | Pantalla completa; crear/editar con alcance corporativo; retiro bloqueado |
| CISO / 24 | `idCiso` | `nombreCISO`, `idEmpresaSubsidiaria` | `email`, `telefono`, `otro`, `LineaDeNegocio`, `Representante` | FK a Empresa/Subsidiaria | Panel lateral; crear/editar; retiro bloqueado |
| Postura Roadmap / 5 | `idPosturaResumenRoadmap` | `nombrePosturaRoadmap` | `descripcionPosturaRoadmap` | Referenciada por Tecnología TSI | Diálogo; crear/editar; retiro bloqueado |
| Estado de Adopción TSI / 5 | `idEstadoAdopcionTSI` | No aplica a mutación | `nombreEstadoAdopcionTSI`, `descripcionEstadoAdopcionTSI` son físicamente anulables | Referenciado por Tecnología TSI | Diálogo de detalle; solo lectura autoritativa |
| Modalidad Laboral / 3 | `idModalidadLaboral` | `TipoModalidadLaboral` | `descripcion` | Referenciada por `TModeloDeOperacion` | Diálogo; crear/editar; retiro bloqueado |
| Tipo de Operación / 2 | `idTipoModeloOperacion` | `TipoModeloDeOperacion` | `Descripcion` | Referenciado por `TModeloDeOperacion` | Diálogo; crear/editar; retiro bloqueado |

Los nombres de columnas con guiones, mayúsculas o diferencias históricas se preservan en el mapping físico y se traducen a etiquetas y propiedades funcionales. No se propone normalizarlos en SQL.

## Goals / Non-Goals

**Goals:**

- Definir un límite modular de Catálogos Maestros que no permita seleccionar objetos SQL arbitrarios.
- Reutilizar exactamente el esquema existente mediante mappings explícitos y modelos de comandos acotados.
- Diseñar consultas y formularios por metadatos controlados, sin convertir el módulo en un CRUD SQL genérico.
- Proteger las mutaciones mediante permisos, políticas, alcance corporativo, validación de relaciones y auditoría atómica.
- Hacer usable el módulo en escritorio, tableta y móvil con WCAG 2.2 AA.

**Non-Goals:**

- Ejecutar DDL, DML, procedimientos o migraciones en la base real.
- Agregar columnas de activo, `rowversion`, índices o restricciones en este cambio.
- Modificar los valores de los cuatro catálogos autoritativos.
- Corregir nombres físicos históricos por uniformidad estética.
- Administrar cualquier tabla no incluida explícitamente en la lista blanca.
- Implementar el fundamento de identidad y auditoría que pertenece a `establish-identity-access-foundation`.

## Decisions

### 1. Módulo explícito y registro cerrado

El módulo tendrá Presentación, Aplicación, Dominio e Infraestructura dentro del monolito. Un registro inmutable de definiciones asociará un código de ruta estable con nombre funcional, entidad/mapping aprobado, campos visibles/editables, búsquedas, orden, permisos, alcance, modo de formulario y estrategia de retiro.

```text
Navegador
   |
ASP.NET Core MVC -- política/permiso --> Identidad y Acceso
   |
Casos de uso de Catálogos Maestros -- auditoría --> Auditoría IAM
   |
Repositorio específico registrado --> mappings EF Core explícitos --> SQL Server dbo
```

El código recibido, por ejemplo `dominios`, solo busca una definición preconstruida. Ninguna definición contiene SQL aportado por el usuario y no se concatenan tablas, columnas, ordenamientos ni predicados procedentes de HTTP.

**Alternativa considerada:** un controlador CRUD genérico que reciba el nombre de tabla se descarta por inyección, overposting, exposición de esquema y dificultad para aplicar reglas por catálogo.

### 2. Definición declarativa con comportamiento tipado

`CatalogoMaestroDefinition` es un concepto de aplicación, no una nueva tabla. Describe presentación y capacidades; cada catálogo conserva modelos tipados de consulta/comando, validadores y mapping. Los campos de orden y filtro se resuelven por expresiones registradas, nunca por cadenas libres.

Información mínima:

- código funcional estable y nombre;
- descripción y grupo visual;
- tipo de consulta/comando registrado;
- permiso por operación;
- campos visibles, editables, requeridos y sensibles;
- resolutores de etiquetas para FK;
- modo diálogo, panel o página;
- estrategia `SoloLectura`, `SinRetiro` o una estrategia futura aprobada;
- alcance corporativo y reglas de auditoría.

**Alternativa considerada:** crear una tabla de configuración editable se pospone porque permitiría cambiar superficie de ataque y reglas de negocio sin revisión de código/OpenSpec.

### 3. Mappings fieles y sin propiedad migratoria sobre producción

EF Core mapeará `dbo`, nombres exactos, PK `IDENTITY`, nulabilidad y FK actuales. Las entidades de persistencia no expondrán setters indiscriminados a MVC. Los ViewModels y comandos separan nombres funcionales de columnas físicas.

No se agregan migraciones para estas tablas. Si el proyecto genera una migración incidental, una revisión automatizada y humana debe demostrar ausencia de DDL/DML sobre ellas. La cadena de conexión real solo se obtiene de configuración segura y nunca se registra.

**Alternativa considerada:** regenerar un esquema uniforme se descarta porque rompe compatibilidad y no aporta valor funcional suficiente.

### 4. Reglas funcionales encima de una base permisiva

Aunque todas las columnas de negocio permiten `NULL`, el módulo exigirá el campo de etiqueta principal y las relaciones padre necesarias para registros nuevos o modificados. Los registros históricos incompletos seguirán visibles y se marcarán como “Información incompleta”; no se modificarán automáticamente.

No se fuerza unicidad porque SQL Server no la garantiza y el dominio no ha aprobado reglas de comparación, acentos o mayúsculas. El formulario puede advertir coincidencias exactas, pero no bloqueará sin una decisión posterior.

**Alternativa considerada:** inferir que todo campo anulable es opcional se descarta por producir maestros sin identidad funcional; imponer restricciones físicas se descarta por riesgo sobre datos existentes.

### 5. Relaciones mediante servicios de catálogo

Cada formulario usa servicios tipados y valida el ID en el servidor:

| Formulario | Servicios de opciones | Validación del servidor |
|---|---|---|
| Building Block | Dominio; Fase de Adopción | existencia en `TMDominio` y `TEstadoFaseAdopcion` |
| Capacidad de Seguridad | Building Block; Estado de Capacidad | existencia en sus tablas y correspondencia de tipo |
| Funcionalidad | Capacidad; Estado de Funcionalidad | existencia en sus tablas y correspondencia de tipo |
| Tecnología TSI | Familia; Estado de Adopción TSI; Postura Roadmap | existencia en los tres catálogos; los estados provienen de SQL |
| Casos de Uso | Tecnología TSI | existencia de la tecnología seleccionada |
| CISO | Empresa/Subsidiaria | existencia de la empresa y alcance corporativo efectivo |
| Otros formularios | No tienen FK salientes | no se acepta ningún identificador técnico adicional |

Para hasta aproximadamente 100 opciones se usará selector con búsqueda local; para cardinalidad superior o crecimiento incierto, autocomplete paginado en servidor. La capa de presentación nunca hardcodea IDs ni etiquetas.

### 6. Autorización por permiso y política

| Operación | Permiso | Política adicional | Auditoría |
|---|---|---|---|
| Ver selección/listado/detalle | `Catalogos.Ver` | catálogo permitido; identidad activa; alcance de consulta | lecturas sensibles o denegadas según política IAM |
| Crear | `Catalogos.Crear` | catálogo mutable; alcance corporativo; validación; auditoría disponible | intento y resultado; valores nuevos permitidos |
| Editar | `Catalogos.Editar` | catálogo mutable; alcance corporativo; concurrencia; validación | valores anteriores/nuevos y resultado |
| Desactivar/retirar | `Catalogos.Desactivar` | estrategia aprobada; sin dependencia incompatible; justificación si se habilita | intento, dependencia, justificación y resultado |

Los dos roles solicitados reciben inicialmente estos permisos según la matriz IAM, no mediante comprobaciones de texto en controladores. El Arquitecto es dueño funcional y el Administrador es dueño técnico/operativo. “Administrador del Sistema” y “Arquitecto de Seguridad Corporativo” se mapean provisionalmente a los códigos canónicos Administrador y Arquitecto de Seguridad; los códigos definitivos se confirman antes de sembrar asignaciones.

Todos los catálogos son corporativos para mantenimiento. `TEmpresaSubsidiaria` no se limita a la subsidiaria del actor porque editar el catálogo organizacional cambia el límite de acceso: exige alcance corporativo explícito y no inferido del rol. No hay estado de workflow aplicable a un mantenimiento maestro; las restricciones relevantes son modo del catálogo, integridad, concurrencia y disponibilidad de auditoría.

### 7. Catálogos autoritativos separados del workflow

Se mantienen conceptos separados:

- Estado de Solicitud: fuera del alcance de este módulo y responsable del workflow administrativo.
- Estado de Adopción TSI: `TMEstadoAdopcionTSI`, madurez de la tecnología.
- Fase de Adopción: `TEstadoFaseAdopcion`, fase del ciclo asociada al Building Block.
- Estado de Capacidad: `TMEstadoCapacidad`.
- Cobertura de Funcionalidad: `TMEstadoFuncionalidad`.

Los cuatro últimos se consultan y se usan como opciones, pero este cambio no habilita su mutación. Esto preserva sus IDs y evita que una pantalla administrativa eluda el gobierno OpenSpec.

### 8. Retiro bloqueado por defecto

La estrategia inicial de los 16 catálogos es `SoloLectura` para los cuatro autoritativos y `SinRetiro` para los doce restantes. No se ejecuta `DELETE`. La acción solo aparecerá en el futuro si otra propuesta define columna de vigencia, semántica, tratamiento de referencias, informes, reversibilidad y autorización operativa.

**Alternativas consideradas:** capturar el error de FK después de intentar borrar se descarta porque es una mala experiencia y no protege tablas sin dependencias actuales; insertar una columna `Activo` ahora se descarta porque sería un cambio físico no validado.

### 9. Concurrencia compatible con el esquema actual

Como no existe `rowversion`, el comando de edición llevará una huella de los valores originales editables o un token firmado derivado de ellos. Antes de guardar, el servidor compara la versión observada con la actual. Una divergencia devuelve conflicto y exige recarga; no se hace “last write wins” silencioso.

**Alternativa considerada:** agregar `rowversion` sería más robusto, pero requiere DDL y evaluación de todos los consumidores; queda como recomendación futura.

### 10. Auditoría transaccional y falla cerrada

Las mutaciones consumirán el contrato append-only del fundamento IAM. El evento incluirá actor, catálogo funcional, PK protegida, operación, campos permitidos anteriores/nuevos, resultado y correlación. La persistencia y el evento exitoso comparten unidad de trabajo cuando la infraestructura lo permita. Si no puede garantizarse auditoría, no se confirma la mutación.

No se registran contraseñas, tokens, cadenas de conexión ni campos no incluidos en la operación. Los intentos fallidos conservan causa segura y correlación. Esto introduce una dependencia de orden: consulta puede entregarse después de autenticación/autorización; mutación no se habilita hasta disponer de auditoría confiable.

### 11. Estrategia visual por complejidad

- **Diálogo compacto:** Familia, Postura Roadmap, Modalidad Laboral y Tipo de Operación. Los cuatro catálogos autoritativos usan diálogo solo para detalle.
- **Panel lateral en escritorio / página en móvil:** Capacidad, Funcionalidad, Caso de Uso y CISO.
- **Página completa por secciones:** Dominio, Building Block, Tecnología TSI y Empresa/Subsidiaria.

La clasificación combina cantidad de campos, texto largo, relaciones y consecuencias. Tecnología TSI tiene 16 campos funcionales y tres FK; Dominio concentra siete campos descriptivos; Building Block mezcla dos FK y texto largo; Empresa tiene dependencias organizacionales sensibles.

### 12. Propuesta visual

Los wireframes expresan jerarquía y comportamiento, no un estilo final ni nuevos componentes.

#### 12.1. Pantalla principal de selección

```text
Landscape TSI / Administración / Tablas maestras

Administración de Tablas Maestras               [Buscar catálogo...]
Seleccione el catálogo autorizado que desea consultar o mantener.

┌ Taxonomía de seguridad ─────────────────────────────────────────┐
│ [Dominio] [Building Block] [Capacidad] [Funcionalidad]          │
│ [Estado capacidad · solo lectura] [Estado funcionalidad · S/L]  │
└──────────────────────────────────────────────────────────────────┘
┌ Tecnología y adopción ──────────────────────────────────────────┐
│ [Tecnología TSI] [Familia] [Casos de Uso] [Postura Roadmap]     │
│ [Fase adopción · S/L] [Estado adopción TSI · S/L]               │
└──────────────────────────────────────────────────────────────────┘
┌ Organización y operación ───────────────────────────────────────┐
│ [Empresa/Subsidiaria] [CISO] [Modalidad laboral] [Tipo operación]│
└──────────────────────────────────────────────────────────────────┘
```

Cada tarjeta muestra nombre, descripción breve, conteo y una etiqueta textual “Solo lectura” cuando corresponda. El menú lateral del escritorio puede duplicar los grupos para navegación persistente; móvil utiliza grupos desplegables y búsqueda.

#### 12.2. Listado de registros

```text
Tablas maestras / Taxonomía / Building Block

Building Block                           147 registros     [Nuevo]
Agrupaciones arquitectónicas por Dominio.
[Buscar por nombre...] [Dominio: Todos] [Fase: Todas] [Limpiar]

Nombre                 Dominio              Fase              Acciones
Protección Endpoint    Seguridad Endpoint   EVALUACION        [Ver] [Editar] [⋮]
Identidad Digital      Identidad y Acceso   ESTANDARIZACION   [Ver] [Editar] [⋮]

Mostrando 1–25 de 147                         [<] 1 2 3 ... [>]
```

No aparece `idBuildingBlock`. El menú no muestra Retirar mientras la estrategia sea `SinRetiro`.

#### 12.3. Creación

```text
┌ Nueva Familia ───────────────────────────────────┐
│ Nombre *       [                              ]  │
│ Descripción    [                              ]  │
│                [                              ]  │
│                           [Cancelar] [Guardar]   │
└──────────────────────────────────────────────────┘

Página: Nueva Tecnología TSI
[Identificación] [Clasificación] [Adopción] [Fechas] [Responsables y fuente]
Nombre corporativo * [.........................]
Familia              [Buscar familia ▾]
Estado de adopción   [Seleccionar desde SQL ▾]
Postura Roadmap      [Seleccionar ▾]
...                                      [Cancelar] [Guardar]
```

Los errores se muestran junto al campo y en un resumen accesible. Guardar queda protegido en servidor aunque el botón no se presente sin permiso.

#### 12.4. Edición

```text
Tablas maestras / Dominio / Seguridad de Endpoints / Editar

Editar Dominio
Nombre *                         [Seguridad de Endpoints]
Descripción                     [........................]
Referencias                     [........................]
Homologaciones                  [........................]
Subdominio CVT                  [........................]
Ejemplos                        [........................]

[Cancelar]                                      [Guardar cambios]
```

La PK no se muestra. Si la huella original no coincide, se presenta: “Este registro cambió desde que lo abrió. Recargue para revisar la versión actual”.

#### 12.5. Detalle

```text
Tecnología TSI / Plataforma X

Plataforma X                                  [Editar]
Clasificación        Familia: Endpoint
                     Estado adopción: CALIFICADA
                     Postura Roadmap: Mantener
Fechas               Compromiso evaluación: ...
Licenciamiento       ...
Responsables         ...
Referencia           [Abrir fuente]
Casos de uso         1 relacionado
```

El detalle agrupa valores funcionales. Las relaciones se presentan como enlaces autorizados, nunca como IDs.

#### 12.6. Confirmación de desactivación/eliminación

Estado inicial (acción no habilitada):

```text
Retiro no disponible
Este catálogo no dispone de una estrategia de desactivación aprobada.
El registro puede estar relacionado con información histórica o transaccional.
Se requiere un cambio OpenSpec antes de habilitar esta operación.       [Cerrar]
```

Patrón futuro, solo después de aprobación:

```text
¿Desactivar “<nombre funcional>”?
Ya no estará disponible para nuevas selecciones; las referencias históricas
se conservarán. Esta acción quedará auditada. [Cancelar] [Desactivar]
```

Nunca se usará el segundo diálogo como autorización implícita para borrado físico.

#### 12.7. Comportamiento responsive

```text
Desktop ≥ 1024 px       Tablet 600–1023 px        Móvil < 600 px
menú + tabla            menú plegable + tabla     navegación compacta
filtros en línea        filtros en panel          filtros en bottom sheet
panel lateral           panel/página              formulario a página completa
acciones por fila       menú por fila             cards + menú por registro
```

En móvil, una card muestra nombre, relación primaria, estado funcional y menú. Detalle y edición ocupan una sola columna; las acciones principales se mantienen visibles sin cubrir contenido. El orden DOM conserva la secuencia lógica en todos los tamaños.

## Risks / Trade-offs

- **[No existe desactivación lógica]** → mantener retiro bloqueado; proponer semántica y DDL en un cambio separado antes de habilitarlo.
- **[Todas las columnas funcionales permiten `NULL`]** → validar reglas mínimas en aplicación y no corregir automáticamente datos históricos.
- **[Sin unicidad funcional]** → advertir duplicados exactos sin prometer unicidad; definirla solo tras analizar datos y reglas de cotejamiento.
- **[Sin `rowversion`]** → usar comparación optimista de valores originales; evaluar token físico en un cambio futuro.
- **[FK sin índices secundarios observados]** → paginación y filtros pueden degradarse al crecer; medir con datos representativos antes de proponer índices.
- **[Auditoría IAM todavía propuesta]** → habilitar mutaciones solo después de que el contrato de auditoría esté implementado; fallar cerrado.
- **[Roles solicitados difieren en etiqueta]** → confirmar códigos estables en la matriz IAM; mantener permisos como unidad de decisión.
- **[Datos personales de CISO]** → limitar campos visibles/editables por permiso y alcance; evitar incluir email/teléfono en listados generales o auditoría completa.
- **[Conteos y búsquedas costosas en `nvarchar(max)`]** → paginar en servidor, limitar ordenamientos registrados y medir; no crear índices sin propuesta aprobada.
- **[Base real protegida]** → usar copia sanitizada o base de desarrollo para pruebas de escritura; configurar controles que impidan migrar o escribir a producción desde automatización.

## Migration Plan

1. Implementar primero el fundamento aprobado de autenticación, permisos, políticas, alcance y auditoría en un entorno no productivo.
2. Crear la solución y el módulo de Catálogos Maestros sin modificar la base real; mapear los 16 objetos existentes.
3. Validar mappings y consultas contra metadatos o una copia sanitizada, incluida compatibilidad con nombres físicos históricos.
4. Crear la lista blanca y habilitar inicialmente solo consultas en un entorno controlado.
5. Ejecutar pruebas unitarias, integración, autorización negativa, accesibilidad y responsive; las pruebas de mutación usarán una base efímera/no productiva.
6. Revisar cualquier migración generada y demostrar que no contiene DDL ni DML para estas tablas. No ejecutarla en producción.
7. Habilitar Crear/Editar por catálogo después de verificar permisos, alcance, concurrencia y auditoría atómica; mantener los cuatro autoritativos y todo retiro en solo lectura/bloqueado.
8. Promover la aplicación mediante autorización operativa independiente. Ningún paso de despliegue ejecuta migraciones contra la base real.

**Rollback:** deshabilitar el módulo o las capacidades de mutación mediante configuración controlada y conservar lectura si es segura. Como este cambio no posee migraciones ni cambia esquema/datos, el rollback de aplicación no requiere revertir objetos SQL. Los registros creados o editados por una operación productiva autorizada no se eliminan automáticamente; su corrección sigue un procedimiento empresarial auditado.

## Open Questions

- Confirmar los códigos internos definitivos que corresponden a las etiquetas “Administrador del Sistema” y “Arquitecto de Seguridad Corporativo” antes de asignar permisos.
- Confirmar límites máximos y reglas de formato funcionales para los campos actualmente `nvarchar(max)`, especialmente email, teléfono, URL y textos descriptivos.
- Definir la política de retención y enmascaramiento para datos de contacto de CISO en auditoría y reporting.
- Determinar, mediante un cambio posterior, si algún catálogo requiere vigencia temporal, desactivación reversible, unicidad o `rowversion`.
