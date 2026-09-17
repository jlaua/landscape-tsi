# FASE G — Certificación final de ambientes

Fecha: 2026-09-06  
Golden: `db-landscape-tsi-reconcile`  
Desarrollo homologado: `db-landscape-tsi-dev-v2`  
Producción: `db-landscape-tsi`

La certificación fue ejecutada exclusivamente con consultas de lectura. No se
realizaron escrituras, cambios de esquema, migraciones ni restores.

## Conclusión

**LANDSCAPE_TSI_ENVIRONMENTS_HOMOLOGATED_WITH_APPROVED_DIFFERENCES**

Esta conclusión queda aprobada como cierre formal de la reconciliación. Las 26
tablas funcionales presentan `EXCEPT = 0` en ambos sentidos entre Golden y
DEV-v2, y entre Golden y PROD. Las diferencias restantes son ambientales o la
deuda técnica explícitamente aceptada; no constituyen un bloqueo de operación.

Las diferencias aceptadas son únicamente:

- eventos de auditoría propios de cada ambiente;
- `dbo.ReconciliacionTrazabilidad` presente en Golden/DEV-v2 y ausente en PROD;
- deuda técnica de `TBuildingBlockVsTTecnologiaTSI` sin PK/UNIQUE.

## Matriz funcional completa

Las columnas `GoldenDEV` y `GoldenPROD` muestran los conteos de ambos sentidos
de `EXCEPT` (`Golden EXCEPT ambiente / ambiente EXCEPT Golden`).

| Tabla | Golden | DEV-v2 | PROD | GoldenDEV | GoldenPROD | Estado |
|---|---:|---:|---:|---:|---:|---|
| sysdiagrams | 1 | 1 | 1 | 0/0 | 0/0 | IDENTICO |
| TBuildingBlock | 147 | 147 | 147 | 0/0 | 0/0 | IDENTICO |
| TBuildingBlockVsTTecnologiaTSI | 1 | 1 | 1 | 0/0 | 0/0 | IDENTICO |
| TCapacidadDeSeguridad | 175 | 175 | 175 | 0/0 | 0/0 | IDENTICO |
| TCasosDeUso | 1 | 1 | 1 | 0/0 | 0/0 | IDENTICO |
| TCISO | 24 | 24 | 24 | 0/0 | 0/0 | IDENTICO |
| TContactoEmpresaSubsidiaria | 2 | 2 | 2 | 0/0 | 0/0 | IDENTICO |
| TContactoPartner | 1 | 1 | 1 | 0/0 | 0/0 | IDENTICO |
| TContactoVendor | 2 | 2 | 2 | 0/0 | 0/0 | IDENTICO |
| TDriver | 6 | 6 | 6 | 0/0 | 0/0 | IDENTICO |
| TEmpresaSubsidiaria | 24 | 24 | 24 | 0/0 | 0/0 | IDENTICO |
| TEstadoFaseAdopcion | 5 | 5 | 5 | 0/0 | 0/0 | IDENTICO |
| TFuncionalidad | 506 | 506 | 506 | 0/0 | 0/0 | IDENTICO |
| TMDominio | 9 | 9 | 9 | 0/0 | 0/0 | IDENTICO |
| TMEstadoAdopcionTSI | 5 | 5 | 5 | 0/0 | 0/0 | IDENTICO |
| TMEstadoCapacidad | 5 | 5 | 5 | 0/0 | 0/0 | IDENTICO |
| TMEstadoFuncionalidad | 5 | 5 | 5 | 0/0 | 0/0 | IDENTICO |
| TMFamilia | 3 | 3 | 3 | 0/0 | 0/0 | IDENTICO |
| TModalidadLaboral | 3 | 3 | 3 | 0/0 | 0/0 | IDENTICO |
| TModeloDeOperacion | 1 | 1 | 1 | 0/0 | 0/0 | IDENTICO |
| TMPosturaRoadmap | 5 | 5 | 5 | 0/0 | 0/0 | IDENTICO |
| TRegulacionAplicable | 2 | 2 | 2 | 0/0 | 0/0 | IDENTICO |
| TTecnologiaTSI | 1 | 1 | 1 | 0/0 | 0/0 | IDENTICO |
| TTecnologiaTSIimplementadaSubsidiaria | 1 | 1 | 1 | 0/0 | 0/0 | IDENTICO |
| TTipoOperacion | 2 | 2 | 2 | 0/0 | 0/0 | IDENTICO |
| TVendor | 2 | 2 | 2 | 0/0 | 0/0 | IDENTICO |

## Conteos críticos

| Objeto | Golden | DEV-v2 | PROD |
|---|---:|---:|---:|
| TBuildingBlock | 147 | 147 | 147 |
| TCapacidadDeSeguridad | 175 | 175 | 175 |
| TFuncionalidad | 506 | 506 | 506 |
| IamPermiso | 15 | 15 | 15 |
| IamRol | 4 | 4 | 4 |
| IamUsuario | 2 | 2 | 2 |
| IamRolPermiso | 18 | 18 | 18 |
| IamUsuarioRol | 2 | 2 | 2 |

## IAM

En los tres ambientes se confirmó:

- `jean` activo;
- `administrador` activo;
- ambos asociados a `SYSTEM_ADMINISTRATOR`;
- FK IAM huérfanas: 0.

No se muestran hashes, sellos de seguridad, tokens ni identificadores
confidenciales.

## Auditoría por ambiente

| Ambiente | IamEventoAutenticacion | IamEventoAuditoriaAutorizacion | Estado |
|---|---:|---:|---|
| Golden | 60 | 5 | DIFERENCIA_APROBADA_POR_AMBIENTE |
| DEV-v2 | 60 | 5 | DIFERENCIA_APROBADA_POR_AMBIENTE |
| PROD | 0 | 0 | DIFERENCIA_APROBADA_POR_AMBIENTE |

Los eventos no se comparan como datos funcionales homologables.

## Esquema

Excluyendo `dbo.ReconciliacionTrazabilidad` de la exigencia de existencia en
PROD, los tres ambientes presentan:

| Elemento | Golden | DEV-v2 | PROD | Estado |
|---|---:|---:|---:|---|
| Tablas | 39 | 39 | 39 | IDENTICO |
| Columnas (tipo, longitud, precisión, escala, nullable, IDENTITY) | 248 | 248 | 248 | IDENTICO |
| PK | 38 | 38 | 38 | IDENTICO |
| FK | 35 | 35 | 35 | IDENTICO |
| Índices | 60 | 60 | 60 | IDENTICO |
| Defaults | 0 | 0 | 0 | IDENTICO |
| Checks | 4 | 4 | 4 | IDENTICO |

Presencia de la infraestructura temporal:

| Ambiente | ReconciliacionTrazabilidad |
|---|---|
| Golden | Presente |
| DEV-v2 | Presente |
| PROD | Ausente por diseño |

## Migraciones EF

`20260901215337_InitialIdentityAccess` está registrada en Golden, DEV-v2 y
PROD.

## Integridad lógica

| Validación | Golden | DEV-v2 | PROD |
|---|---:|---:|---:|
| FK huérfanas | 0 | 0 | 0 |
| PK duplicadas | 0 | 0 | 0 |
| Referencias IAM faltantes | 0 | 0 | 0 |

## Deuda técnica

`TBuildingBlockVsTTecnologiaTSI` continúa sin PK/UNIQUE en los tres ambientes.
No fue modificada durante la certificación.

Deuda recomendada:

`mapear-relacion-buildingblock-tecnologia`

Debe resolverse posteriormente validando primero el modelo EF Core y escogiendo
entre PK compuesta o restricción UNIQUE.

## Política posterior al cierre

- No se realizarán homologaciones manuales normales entre bases.
- Los cambios de esquema se promoverán mediante migraciones EF Core.
- El flujo oficial será: **Código → DEV → Pruebas → PROD**.
- Los cambios de datos maestros utilizarán las interfaces administrativas de
  Landscape TSI cuando estén disponibles.
- PROD nunca se actualizará mediante `RESTORE` desde DEV.
- Las cuentas runtime mantendrán el principio de mínimo privilegio.
