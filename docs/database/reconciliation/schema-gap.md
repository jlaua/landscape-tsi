# Brecha de esquema: `db-landscape-tsi-reconcile` vs `db-landscape-tsi-dev`

## Alcance y método

Inventario obtenido el 2026-09-06 mediante consultas de solo lectura sobre
`sys.tables`, `sys.columns`, `sys.types`, `sys.indexes`,
`sys.index_columns`, `sys.foreign_keys`, `sys.foreign_key_columns`,
`sys.default_constraints` y `sys.check_constraints`.

No se ejecutaron DDL ni DML. La conexión, contraseñas y secretos no forman
parte de este documento.

La comparación adicional con `db-landscape-tsi` (origen del restore) mostró el
mismo inventario de 26 tablas funcionales, columnas, índices y claves que
`db-landscape-tsi-reconcile`. Por tanto, no se detectó divergencia de esquema
entre PROD/restaurada y RECONCILE en esta lectura.

## Tablas

| Clasificación | Resultado |
|---|---|
| IGUAL | 26 tablas presentes en ambas bases (incluye `dbo.sysdiagrams`) |
| SOLO RECONCILE | Ninguna |
| SOLO DEV | 13 tablas IAM/migraciones, listadas abajo |

Solo DEV:

`__EFMigrationsHistory`, `IamAccesoEmergencia`,
`IamEventoAuditoriaAutorizacion`, `IamEventoAutenticacion`, `IamPermiso`,
`IamRol`, `IamRolPermiso`, `IamUsuario`, `IamUsuarioClaim`,
`IamUsuarioLoginExterno`, `IamUsuarioOrganizacion`, `IamUsuarioRol`,
`IamUsuarioToken`.

## Columnas y propiedades

| Clasificación | Cantidad / detalle |
|---|---|
| IGUAL | 154 columnas compartidas: tipo SQL, longitud, precisión, escala, nullability, identity y default iguales |
| SOLO RECONCILE | `dbo.TCISO.LineaDeNegocio`; `dbo.TCISO.Representante` |
| SOLO DEV | 92 columnas pertenecientes exclusivamente a las 13 tablas IAM/migraciones |
| DIFERENTE | Ninguna columna compartida con propiedades diferentes |

Las columnas exclusivas de RECONCILE son funcionales y deben preservarse.
Las columnas exclusivas DEV son infraestructura IAM; no deben eliminarse al
construir la futura base canónica.

## PK, FK, índices, únicos y checks

| Elemento | RECONCILE | DEV | Clasificación / interpretación |
|---|---:|---:|---|
| Filas de FK | 24 | 35 | Las 24 FKs funcionales son IGUALES; DEV añade 11 FKs IAM |
| Filas de índice | 27 | 70 | Los índices funcionales son IGUALES; DEV añade índices IAM/migración |
| Unique constraints | 0 | 0 | IGUAL en el inventario consultado |
| Checks | 0 | 4 | SOLO DEV, todos IAM (`CK_IamAccesoEmergencia_Vigencia`, `CK_IamUsuario_Vigencia`, `CK_IamUsuarioOrganizacion_Alcance`, `CK_IamUsuarioRol_Vigencia`) |

Las 24 FKs funcionales son:

`FK_TBuildingBlock_TDominio`, `FK_TBuildingBlock_TEstadoFaseAdopcion`,
`FK_TBuildingBlockVsTTecnologiaTSI_TBuildingBlock`,
`FK_TBuildingBlockVsTTecnologiaTSI_TTecnologiaTSI`,
`FK_TCapacidadDeSeguridad_TBuildingBlock`,
`FK_TCapacidadDeSeguridad_TMEstadoCapacidad`,
`FK_TCasosDeUso_TTecnologiaTSI`, `FK_TCISO_TEmpresaSubsidiaria`,
`FK_TContactoEmpresaSubsidiaria_TEmpresaSubsidiaria`,
`FK_TContactoPartner_TVendor`, `FK_TContactoVendor_TVendor`,
`FK_TDriver_TTecnologiaTSIimplementadaSubsidiaria`,
`FK_TFuncionalidad_TCapacidadDeSeguridad`,
`FK_TFuncionalidad_TMEstadoFuncionalidad`,
`FK_TModeloDeOperacion_TModalidadLaboral`,
`FK_TModeloDeOperacion_TTecnologiaTSIimplementadaSubsidiaria`,
`FK_TModeloDeOperacion_TTipoOperacion`,
`FK_TRegulacionAplicable_TEmpresaSubsidiaria`,
`FK_TTecnologiaTSI_TMEstadoAdopcionTSI`,
`FK_TTecnologiaTSI_TMFamilia`, `FK_TTecnologiaTSI_TMPosturaRoadmap`,
`FK_TTecnologiaTSIimplementadaSubsidiaria_TEmpresaSubsidiaria`,
`FK_TTecnologiaTSIimplementadaSubsidiaria_TTecnologiaTSI`,
`FK_TVendor_TTecnologiaTSI`.

## Conclusión

La diferencia de esquema es aditiva: RECONCILE conserva el modelo funcional
de PROD y DEV aporta IAM, auditoría y migraciones EF. No se justifica alterar
ninguna de las tres bases en esta etapa.
