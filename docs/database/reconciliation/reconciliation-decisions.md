# Informe de decisiones de reconciliación

**Estado:** análisis de solo lectura; no se ejecutó ningún cambio.

**Fuentes:** `db-landscape-tsi-reconcile` (modelo funcional restaurado),
`db-landscape-tsi` (origen del restore) y `db-landscape-tsi-dev` (IAM y
desarrollo). La comparación se realizó el 2026-09-06 con PK, columnas y FK
reales. No se muestran secretos, hashes, tokens ni cadenas de conexión.

## 1. TCISO

### Diferencia de esquema

`RECONCILE` tiene dos columnas que no existen en DEV:

| Columna | RECONCILE | DEV | Decisión requerida |
|---|---|---|---|
| `LineaDeNegocio` | existe | no existe | preservar; no eliminar |
| `Representante` | existe | no existe | preservar; no eliminar |

Las demás columnas compartidas tienen el mismo tipo, nulabilidad y propiedades.

### Diferencia de datos

| PK | Campo | RECONCILE | DEV | Tipo |
|---:|---|---|---|---|
| 9 | `otro` | Pacifico Seguros y Pensiones | Pacifico Seguros | mismo PK, valor diferente |
| 10 | `nombreCISO` | Kasandra Jaramillo | Joan Caceres | mismo PK, valor diferente |
| 10 | `email` | kassandrajaramillo@prima.com.pe | jocaceres@pacifico.com.pe | mismo PK, valor diferente |
| 18 | `nombreCISO` | Sebastian Veliz | Alvaro Alberto Arce Chocano | mismo PK, valor diferente |
| 18 | `email` | sebastian.veliz@tenpo.cl | alvaro.arce@krealo.pe | mismo PK, valor diferente |
| 19 | `nombreCISO` | Kelly Correa Murillo | Alvaro Alberto Arce Chocano | mismo PK, valor diferente |
| 19 | `email` | kelly.correa@monokera.com | alvaro.arce@krealo.pe | mismo PK, valor diferente |
| 20 | `nombreCISO` | Julian Camilo Sanchez Peña | — | mismo PK; revisar valor DEV antes de promover |
| 20 | `email` | juliansanchez@tyba.com.co | — | mismo PK; revisar valor DEV antes de promover |
| 22 | `otro` | Pacifico Prestaciones - Clínicas  | Pacifico Clínicas | mismo PK, valor diferente |
| 23 | `otro` | Pacifico Prestaciones - Laboratorios | Pacifico Laboratorios | mismo PK, valor diferente |

Además, los 23 PK comunes tienen valor en `LineaDeNegocio` y/o
`Representante` únicamente en RECONCILE. Esos valores deben conservarse.

### Registros por PK

- Solo RECONCILE: PK `24`.
- Solo DEV: ninguno.
- PK comunes: `1..23`.
- Total: RECONCILE **24**, DEV **23**.

**Recomendación:** conservar todas las filas y columnas de RECONCILE; resolver
manualmente los conflictos de identidad/contacto antes de cualquier copia.

## 2. TBuildingBlock

Hay 147 filas en RECONCILE y una en DEV. El único PK común es `1`; no hay
registros solo DEV.

| PK | Campo | RECONCILE | DEV | Tipo de diferencia |
|---:|---|---|---|---|
| 1 | `definicionBuildingBlock` | `Defensa de aplicaciones a nivel de Web application firewall y Api Security. WAAP - Web Application and API Protection: Las plataformas WAAP sirven para proteger los nuevos servicios públicos, ya que combinan un amplio alcance y controles de seguridad específicos para aplicaciones web y API con facilidad de implementación a escala.` | El mismo texto seguido de `actualmente solo alcance BCP` | mismo PK, descripción diferente |

`idDominio`, `nombreBuildingBlock`, `idEstadoFaseDeAdopcionBuildingBlock`,
`rutaDelEntregable` y `PilarZT` no presentaron diferencias para el PK común.

**Recomendación:** conservar la descripción RECONCILE salvo que el
responsable funcional confirme que la frase BCP de DEV es una corrección.

## 3. TFuncionalidad

| Clasificación | Cantidad |
|---|---:|
| Total RECONCILE | 500 |
| Total DEV | 8 |
| Solo RECONCILE | 500 (PK 2267–2766) |
| Solo DEV | 8 (PK 1–8) |
| Mismo PK con valores diferentes | 0 |
| PK compartidas con valores iguales | 0 |

### Solo DEV

Todos dependen de la capacidad `Web Application Firewall` y tienen los
estados indicados:

| PK | Nombre funcional | Capacidad | Estado | Origen |
|---:|---|---|---|---|
| 1 | Web Application Firewall | Web Application Firewall | CUBIERTO | DEV |
| 2 | WAF - Attack Detection | Web Application Firewall | CUBIERTO | DEV |
| 3 | WAF - Real-time Monitoring | Web Application Firewall | CUBIERTO | DEV |
| 4 | WAF - Rule-Based Policies | Web Application Firewall | CUBIERTO | DEV |
| 5 | WAF - Logging and Reporting | Web Application Firewall | CUBIERTO | DEV |
| 6 | WAF- Rate Limiting | Web Application Firewall | CUBIERTO | DEV |
| 7 | WAF - SSL/TLS Inspection | Web Application Firewall | CUBIERTO | DEV |
| 8 | Integraciones con terceros | Web Application Firewall | PARCIALMENTE | DEV |

### Solo RECONCILE

Los 500 registros tienen PK consecutivos `2267`–`2766`. Sus nombres y
relaciones reales fueron verificados mediante `idCapacidad` y
`idEstadoCoberturaFuncionalidad`; no existe ningún PK común con DEV. Se
consideran datos funcionales de RECONCILE/PROD y no deben reemplazarse por el
subconjunto DEV.

**Recomendación:** no promover los 8 registros DEV automáticamente. Son un
subconjunto funcional plausible, pero también podrían ser una carga parcial de
desarrollo; requieren validación del propietario funcional y decisión sobre
colisiones semánticas (no de PK).

## 4. TBuildingBlockVsTTecnologiaTSI

Validación en ambas bases:

| Propiedad | Resultado |
|---|---|
| PK declarada | ninguna |
| UNIQUE constraint | ninguna |
| Índices | ninguno específico |
| FK | `idBuildingBlock` → `TBuildingBlock.idBuildingBlock`; `idTecnologiaTSI` → `TTecnologiaTSI.idTecnologiaTSI` |
| Filas RECONCILE | 1 |
| Filas DEV | 1 |
| Pares distintos | 1 en cada base |
| Duplicados del par | 0 |
| Nulos en FKs | 0 |

La estructura es una relación muchos-a-muchos (tabla puente) y la combinación
`(idBuildingBlock, idTecnologiaTSI)` es el identificador natural candidato.
No obstante, no se debe crear PK compuesta ni UNIQUE constraint sin una
decisión de diseño y una verificación de todas las cargas existentes.

**Propuesta pendiente:** añadir una restricción única sobre el par, o una PK
compuesta si el modelo EF la requiere. No implementar en esta etapa.

## 5. Datos exclusivos DEV en tablas funcionales

La única tabla funcional compartida con registros exclusivos DEV es
`TFuncionalidad` (los 8 registros listados arriba). El resto de tablas
funcionales no presentó filas exclusivas DEV; sus diferencias son filas
exclusivas RECONCILE o valores comunes ya iguales.

Clasificación preliminar de los 8 registros DEV: **indeterminado / posible
dato funcional legítimo**. No hay evidencia suficiente para calificarlos como
prueba, y no se promoverán automáticamente.

## DECISIONES REQUERIDAS DEL USUARIO

| # | Tabla | PK | Conflicto | RECONCILE | DEV | Recomendación |
|---:|---|---|---|---|---|---|
| 1 | TCISO | 9,10,18,19,20,22,23 | contactos/nombres/`otro` diferentes | conservar datos funcionales y columnas exclusivas | revisar antes de copiar | decisión del responsable de datos por PK/campo |
| 2 | TCISO | 1–23 | `LineaDeNegocio`, `Representante` solo en RECONCILE | preservar | ausentes | no eliminar ni reconstruir |
| 3 | TCISO | 24 | fila solo RECONCILE | conservar | no existe | aprobar conservación |
| 4 | TBuildingBlock | 1 | definición con sufijo BCP en DEV | texto completo RECONCILE | texto BCP DEV | elegir versión funcional |
| 5 | TFuncionalidad | 2267–2766 | 500 filas solo RECONCILE | conservar | no existen | aprobar como conjunto canónico |
| 6 | TFuncionalidad | 1–8 | 8 filas solo DEV | no existen | subconjunto WAF | confirmar si son datos legítimos; no copiar aún |
| 7 | TBuildingBlockVsTTecnologiaTSI | par de FKs | ausencia de PK/UNIQUE | 1 par, sin duplicados | 1 par, sin duplicados | aprobar PK compuesta o UNIQUE, sin aplicar aún |

## Decisiones recibidas

El usuario aprobó las siguientes decisiones de gobierno de datos:

1. `TCISO`: gana RECONCILE/PROD; no se copiarán los valores conflictivos de
   DEV.
2. `TCISO.LineaDeNegocio` y `TCISO.Representante`: se conservarán ambas.
3. `TCISO` PK 24: se conservará.
4. `TBuildingBlock` PK 1: se conservará el texto de RECONCILE sin el sufijo
   `actualmente solo alcance BCP`.
5. Las 500 funcionalidades de RECONCILE se conservarán.
6. Las funcionalidades DEV `2,3,4,5,6,8` quedan aprobadas para promoción con
   PK nuevas y trazabilidad; DEV PK 1 y 7 permanecen pendientes y no se
   copiarán.
7. Se propone PK compuesta `(idBuildingBlock, idTecnologiaTSI)` para la tabla
   puente, pendiente de validación final y de una decisión de esquema.
8. Las seis funcionalidades DEV consistentes (`2,3,4,5,6,8`) quedan
   aprobadas para promoción futura, con PK nuevas generadas por RECONCILE y
   registro de trazabilidad. No usar `IDENTITY_INSERT`.
9. DEV PK 1 y DEV PK 7 permanecen en revisión manual y no se promoverán.

La inspección del código confirmó que `TBuildingBlockVsTTecnologiaTSI` no está
representada actualmente en `IdentityDbContext`: no existe `DbSet`, entidad ni
configuración `HasKey`/`IsUnique`. Por ello la decisión PRIMARY KEY compuesta
frente a `UNIQUE` queda pendiente de diseñar y validar en EF Core.

### Validación semántica de las 8 funcionalidades DEV

La comparación de nombres normalizados no encontró coincidencias exactas con
RECONCILE. Se observaron únicamente estas posibles coincidencias parciales:

| DEV PK | Nombre DEV | Posible registro RECONCILE | Evaluación |
|---:|---|---|---|
| 1 | Web Application Firewall | PK 2409: `WAF / Web Application Firewall / Next Generation Firewall / API Protection` | posible solapamiento; requiere revisión funcional |
| 7 | WAF - SSL/TLS Inspection | PK 2730: `TLS` | posible solapamiento; requiere revisión funcional |

Las otras seis no presentaron coincidencia textual ni parcial. Esta validación
no sustituye la revisión del propietario funcional: no se realizó ninguna
fusión ni promoción.

### Estado actualizado

Con las decisiones anteriores, quedan pendientes únicamente:

- confirmar si DEV PK 1 y 7 son conceptos ya cubiertos por RECONCILE;
- decidir si se formaliza la unicidad del par de FKs como PK compuesta o como
  `UNIQUE` constraint;
- aprobar posteriormente cualquier script de aplicación.

Las decisiones del usuario indican que RECONCILE/PROD es la fuente funcional
canónica. Por ello no se generarán INSERT para los casos DEV 1 y 7 hasta cerrar
la revisión semántica.

**Resultado:** el informe queda detenido aquí. No se ejecutó ningún cambio en
`db-landscape-tsi`, `db-landscape-tsi-dev` ni `db-landscape-tsi-reconcile`.
