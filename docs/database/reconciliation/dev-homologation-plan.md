# Plan de homologación funcional RECONCILE → DEV

**Estado:** prevalidación completa de solo lectura. No se ejecutaron
`INSERT`, `UPDATE`, `DELETE`, `ALTER`, `CREATE`, `DROP`, migraciones ni
restores. El único destino futuro autorizado sería `db-landscape-tsi-dev`;
`db-landscape-tsi` queda fuera de alcance.

## DAG real y orden de sincronización

El DAG se obtuvo desde `sys.foreign_keys`, `sys.foreign_key_columns`,
`sys.tables` y `sys.columns`. Para una futura sincronización, los padres deben
procesarse antes que las hijas:

1. **Raíces:** `TMDominio`, `TEstadoFaseAdopcion`, `TMEstadoCapacidad`,
   `TMEstadoFuncionalidad`, `TMFamilia`, `TMEstadoAdopcionTSI`,
   `TMPosturaRoadmap`, `TModalidadLaboral`, `TTipoOperacion`,
   `TEmpresaSubsidiaria`, `TVendor`.
2. **Dependientes directos:** `TBuildingBlock`, `TTecnologiaTSI`.
3. **Dependientes de segundo nivel:** `TCapacidadDeSeguridad`, `TCISO`,
   `TCasosDeUso`, `TTecnologiaTSIimplementadaSubsidiaria`,
   `TRegulacionAplicable`, `TContactoEmpresaSubsidiaria`,
   `TContactoPartner`, `TContactoVendor`, `TDriver`, `TModeloDeOperacion`.
4. **Dependientes finales:** `TFuncionalidad` y
   `TBuildingBlockVsTTecnologiaTSI`.

Relaciones reales principales: `TBuildingBlock` → Dominio/Fase;
`TCapacidadDeSeguridad` → BuildingBlock/Estado; `TFuncionalidad` →
Capacidad/Estado; `TTecnologiaTSI` → Familia/Adopción/Postura;
`TCasosDeUso` → Tecnología; y la tabla puente → BuildingBlock/Tecnología.

## Comparación de las 26 tablas inventariadas

`SoloR` y `SoloD` son registros exclusivos según la PK real. `Diff` es mismo
PK con contenido diferente.

| Orden | Tabla | RECONCILE | DEV | SoloR | SoloD | Mismo PK/datos | Diff | IDENTITY | Acción |
|---:|---|---:|---:|---:|---:|---:|---:|---|---|
| 1 | sysdiagrams | 1 | 1 | 0 | 0 | 1 | 0 | `diagram_id` | SIN_CAMBIOS |
| 2 | TMDominio | 9 | 9 | 0 | 0 | 9 | 0 | — | SIN_CAMBIOS |
| 3 | TEstadoFaseAdopcion | 5 | 5 | 0 | 0 | 5 | 0 | `idEstadoFaseAdopcion` | SIN_CAMBIOS |
| 4 | TMEstadoCapacidad | 5 | 4 | 1 | 0 | 4 | 0 | `idEstadoCapacidad` | INSERT_FALTANTES |
| 5 | TMEstadoFuncionalidad | 5 | 4 | 1 | 0 | 4 | 0 | `idEstadoCoberturaFuncionalidad` | INSERT_FALTANTES |
| 6 | TMFamilia | 3 | 3 | 0 | 0 | 3 | 0 | `idFamilia` | SIN_CAMBIOS |
| 7 | TMEstadoAdopcionTSI | 5 | 5 | 0 | 0 | 5 | 0 | `idEstadoAdopcionTSI` | SIN_CAMBIOS |
| 8 | TMPosturaRoadmap | 5 | 5 | 0 | 0 | 5 | 0 | `idPosturaResumenRoadmap` | SIN_CAMBIOS |
| 9 | TModalidadLaboral | 3 | 3 | 0 | 0 | 3 | 0 | `idModalidadLaboral` | SIN_CAMBIOS |
| 10 | TTipoOperacion | 2 | 2 | 0 | 0 | 2 | 0 | `idTipoModeloOperacion` | SIN_CAMBIOS |
| 11 | TEmpresaSubsidiaria | 24 | 23 | 1 | 0 | 23 | 0 | `idEmpresaSubsidiaria` | INSERT_FALTANTES |
| 12 | TVendor | 2 | 2 | 0 | 0 | 2 | 0 | `idVendor` | SIN_CAMBIOS |
| 13 | TBuildingBlock | 147 | 147 | 0 | 0 | 147 | 0 | `idBuildingBlock` | SIN_CAMBIOS |
| 14 | TTecnologiaTSI | 1 | 1 | 0 | 0 | 1 | 0 | `idTecnologiaTSI` | SIN_CAMBIOS |
| 15 | TCapacidadDeSeguridad | 175 | 9 | 166 | 0 | 9 | 0 | `idCapacidad` | INSERT_FALTANTES; REQUIERE_PADRE_PRIMERO |
| 16 | TCISO | 24 | 23 | 1 | 0 | 0 | 23 | `idCiso` | INSERT_Y_UPDATE; CONFLICTO_MANUAL |
| 17 | TCasosDeUso | 1 | 1 | 0 | 0 | 1 | 0 | `idCasosDeUso` | SIN_CAMBIOS |
| 18 | TTecnologiaTSIimplementadaSubsidiaria | 1 | 1 | 0 | 0 | 1 | 0 | `idTecnologiaTSIimplementadaSubsidiaria` | SIN_CAMBIOS |
| 19 | TRegulacionAplicable | 2 | 2 | 0 | 0 | 2 | 0 | `idRegulacion` | SIN_CAMBIOS |
| 20 | TContactoEmpresaSubsidiaria | 2 | 2 | 0 | 0 | 2 | 0 | `idContactoEmpresaSubsidiaria` | SIN_CAMBIOS |
| 21 | TContactoPartner | 1 | 1 | 0 | 0 | 1 | 0 | — | SIN_CAMBIOS |
| 22 | TContactoVendor | 2 | 2 | 0 | 0 | 2 | 0 | `idContactoVendor` | SIN_CAMBIOS |
| 23 | TDriver | 6 | 6 | 0 | 0 | 6 | 0 | `idDriver` | SIN_CAMBIOS |
| 24 | TModeloDeOperacion | 1 | 1 | 0 | 0 | 1 | 0 | `idModeloOperacion` | SIN_CAMBIOS |
| 25 | TFuncionalidad | 506 | 8 | 506 | 8 | 0 | 0 | `idFuncionalidad` | INSERT_FALTANTES; CONFLICTO_MANUAL |
| 26 | TBuildingBlockVsTTecnologiaTSI | 1 | 1 | — | — | — | — | — | SIN_CAMBIOS; DEUDA TÉCNICA |

La tabla puente no tiene PK, por lo que su contenido se compara por el par de
FK y no puede clasificarse por PK.

## Dependencias FK por tabla

| Tabla hija | Columna | Tabla padre | Columna padre |
|---|---|---|---|
| TBuildingBlock | `idEstadoFaseDeAdopcionBuildingBlock` | TEstadoFaseAdopcion | `idEstadoFaseAdopcion` |
| TBuildingBlock | `idDominio` | TMDominio | `iddominio` |
| TBuildingBlockVsTTecnologiaTSI | `idBuildingBlock` | TBuildingBlock | `idBuildingBlock` |
| TBuildingBlockVsTTecnologiaTSI | `idTecnologiaTSI` | TTecnologiaTSI | `idTecnologiaTSI` |
| TCapacidadDeSeguridad | `idBuildingBlock` | TBuildingBlock | `idBuildingBlock` |
| TCapacidadDeSeguridad | `idEstadoCapacidad` | TMEstadoCapacidad | `idEstadoCapacidad` |
| TCasosDeUso | `idTecnologiaTSI` | TTecnologiaTSI | `idTecnologiaTSI` |
| TCISO | `idEmpresaSubsidiaria` | TEmpresaSubsidiaria | `idEmpresaSubsidiaria` |
| TContactoEmpresaSubsidiaria | `idEmpresaSubsidiaria` | TEmpresaSubsidiaria | `idEmpresaSubsidiaria` |
| TContactoPartner | `idVendor` | TVendor | `idVendor` |
| TContactoVendor | `idVendor` | TVendor | `idVendor` |
| TDriver | `idTecnologiaTSIimplementadaSubsidiaria` | TTecnologiaTSIimplementadaSubsidiaria | `idTecnologiaTSIimplementadaSubsidiaria` |
| TFuncionalidad | `idCapacidad` | TCapacidadDeSeguridad | `idCapacidad` |
| TFuncionalidad | `idEstadoCoberturaFuncionalidad` | TMEstadoFuncionalidad | `idEstadoCoberturaFuncionalidad` |
| TModeloDeOperacion | `idModalidadLaboral` | TModalidadLaboral | `idModalidadLaboral` |
| TModeloDeOperacion | `idTecnologiaTSIimplementadaSubsidiaria` | TTecnologiaTSIimplementadaSubsidiaria | `idTecnologiaTSIimplementadaSubsidiaria` |
| TModeloDeOperacion | `idTipoModeloOperacion` | TTipoOperacion | `idTipoModeloOperacion` |
| TRegulacionAplicable | `idEmpresaSubsidiaria` | TEmpresaSubsidiaria | `idEmpresaSubsidiaria` |
| TTecnologiaTSI | `idEstadoAdopcionTSI` | TMEstadoAdopcionTSI | `idEstadoAdopcionTSI` |
| TTecnologiaTSI | `idFamilia` | TMFamilia | `idFamilia` |
| TTecnologiaTSI | `idPosturaResumenRoadmap` | TMPosturaRoadmap | `idPosturaResumenRoadmap` |
| TTecnologiaTSIimplementadaSubsidiaria | `idEmpresaSubsidiaria` | TEmpresaSubsidiaria | `idEmpresaSubsidiaria` |
| TTecnologiaTSIimplementadaSubsidiaria | `idTecnologiaTSI` | TTecnologiaTSI | `idTecnologiaTSI` |
| TVendor | `idTecnologiaTSI` | TTecnologiaTSI | `idTecnologiaTSI` |

## Registros exclusivos DEV

| Tabla | PK | Nombre funcional | Acción recomendada |
|---|---:|---|---|
| TFuncionalidad | 1 | Web Application Firewall | CONFLICTO_MANUAL; no eliminar ni sobrescribir automáticamente |
| TFuncionalidad | 2 | WAF - Attack Detection | candidato a reemplazo por la versión canónica promovida |
| TFuncionalidad | 3 | WAF - Real-time Monitoring | candidato a reemplazo por la versión canónica promovida |
| TFuncionalidad | 4 | WAF - Rule-Based Policies | candidato a reemplazo por la versión canónica promovida |
| TFuncionalidad | 5 | WAF - Logging and Reporting | candidato a reemplazo por la versión canónica promovida |
| TFuncionalidad | 6 | WAF- Rate Limiting | candidato a reemplazo por la versión canónica promovida |
| TFuncionalidad | 7 | WAF - SSL/TLS Inspection | CONFLICTO_MANUAL; no eliminar ni sobrescribir automáticamente |
| TFuncionalidad | 8 | Integraciones con terceros | candidato a reemplazo por la versión canónica promovida |

Los registros DEV 2–6 y 8 tienen equivalentes canónicos con PK RECONCILE
3002–3007. Para lograr igualdad exacta en DEV será necesaria una decisión
explícita sobre los registros DEV 1 y 7; FASE E no ejecutará DELETE.

## IAM

IAM está homologado en conteos entre RECONCILE y DEV: 15 permisos, 4 roles,
2 usuarios, 18 relaciones rol-permiso, 2 relaciones usuario-rol, 60 eventos de
autenticación y 5 eventos de autorización. No se propone resincronización IAM.

## Estrategia futura, pendiente de aprobación

- Aplicar padres antes que hijos siguiendo el DAG.
- Para PK comunes con diferencias, actualizar DEV con valores RECONCILE dentro
  de transacciones por bloque.
- Insertar faltantes respetando FK; usar `IDENTITY_INSERT` solo cuando sea
  imprescindible para preservar referencias.
- No deshabilitar FK ni ejecutar DELETE automático.
- Validar cada tabla con `RECONCILE EXCEPT DEV` y `DEV EXCEPT RECONCILE`.
- Detenerse ante cualquier conflicto manual o FK faltante.

## Estado

Esta ejecución fue únicamente una prevalidación. No se modificó DEV ni PROD.
La homologación queda detenida hasta que se decidan los registros exclusivos
DEV y se apruebe un plan de escrituras por bloques.
