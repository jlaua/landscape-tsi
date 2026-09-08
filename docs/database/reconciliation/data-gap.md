# Brecha de datos funcional: RECONCILE vs DEV

Comparación de solo lectura realizada el 2026-09-06. Se excluyeron tablas
`Iam*`, `__EFMigrationsHistory` y la tabla técnica `sysdiagrams`. La identidad
de cada fila se determinó con la PK real; no se resolvieron conflictos.

| Tabla | PK | RECONCILE | DEV | Solo RECONCILE | Solo DEV | Mismo PK/valores iguales | Mismo PK/valores diferentes |
|---|---|---:|---:|---:|---:|---:|---:|
| TBuildingBlock | idBuildingBlock | 147 | 1 | 146 | 0 | 0 | 1 |
| TBuildingBlockVsTTecnologiaTSI | (sin PK) | — | — | — | — | — | — |
| TCapacidadDeSeguridad | idCapacidad | 175 | 9 | 166 | 0 | 9 | 0 |
| TCasosDeUso | idCasosDeUso | 1 | 1 | 0 | 0 | 1 | 0 |
| TCISO | idCiso | 24 | 23 | 1 | 0 | 0 | 23 |
| TContactoEmpresaSubsidiaria | idContactoEmpresaSubsidiaria | 2 | 2 | 0 | 0 | 2 | 0 |
| TContactoPartner | idContactoPartner | 1 | 1 | 0 | 0 | 1 | 0 |
| TContactoVendor | idContactoVendor | 2 | 2 | 0 | 0 | 2 | 0 |
| TDriver | idDriver | 6 | 6 | 0 | 0 | 6 | 0 |
| TEmpresaSubsidiaria | idEmpresaSubsidiaria | 24 | 23 | 1 | 0 | 23 | 0 |
| TEstadoFaseAdopcion | idEstadoFaseAdopcion | 5 | 4 | 1 | 0 | 4 | 0 |
| TFuncionalidad | idFuncionalidad | 500 | 8 | 500 | 8 | 0 | 0 |
| TMDominio | iddominio | 9 | 9 | 0 | 0 | 9 | 0 |
| TMEstadoAdopcionTSI | idEstadoAdopcionTSI | 5 | 5 | 0 | 0 | 5 | 0 |
| TMEstadoCapacidad | idEstadoCapacidad | 5 | 4 | 1 | 0 | 4 | 0 |
| TMEstadoFuncionalidad | idEstadoCoberturaFuncionalidad | 5 | 4 | 1 | 0 | 4 | 0 |
| TMFamilia | idFamilia | 3 | 3 | 0 | 0 | 3 | 0 |
| TModalidadLaboral | idModalidadLaboral | 3 | 3 | 0 | 0 | 3 | 0 |
| TModeloDeOperacion | idModeloOperacion | 1 | 1 | 0 | 0 | 1 | 0 |
| TMPosturaRoadmap | idPosturaResumenRoadmap | 5 | 5 | 0 | 0 | 5 | 0 |
| TRegulacionAplicable | idRegulacion | 2 | 2 | 0 | 0 | 2 | 0 |
| TTecnologiaTSI | idTecnologiaTSI | 1 | 1 | 0 | 0 | 1 | 0 |
| TTecnologiaTSIimplementadaSubsidiaria | idTecnologiaTSIimplementadaSubsidiaria | 1 | 1 | 0 | 0 | 1 | 0 |
| TTipoOperacion | idTipoModeloOperacion | 2 | 2 | 0 | 0 | 2 | 0 |
| TVendor | idVendor | 2 | 2 | 0 | 0 | 2 | 0 |

Notas:

- `TFuncionalidad` presenta 500 filas exclusivas en RECONCILE y 8 exclusivas
  en DEV; al no compartir PK no se clasifican como conflicto de valores.
- `TCISO` tiene 23 PK comunes con valores diferentes y conserva además las
  columnas funcionales `LineaDeNegocio` y `Representante` solo en RECONCILE.
- La tabla puente `TBuildingBlockVsTTecnologiaTSI` no tiene PK ni unique
  constraint; debe reconciliarse por el par real de FKs, nunca por una PK
  inventada.
- No se imprimieron hashes, contraseñas, tokens ni valores de IAM.
