# FASE F — Plan de homologación controlada de PROD

Fecha del análisis: 2026-09-06  
Fuente canónica técnica: `db-landscape-tsi-reconcile`  
Destino analizado: `db-landscape-tsi`

Este documento contiene únicamente resultados de consultas de lectura. No se
ejecutaron `INSERT`, `UPDATE`, `DELETE`, DDL, migraciones ni `RESTORE`.

## Resumen ejecutivo

- Las 25 tablas funcionales compartidas tienen el mismo contenido: ambos
  sentidos de `EXCEPT` devuelven cero filas.
- `TFuncionalidad` es la única excepción de datos: Golden tiene 506 filas y
  PROD 500. Las seis filas adicionales son las PK canónicas 3002–3007.
- PROD no contiene infraestructura IAM. Golden contiene las 12 tablas IAM,
  `__EFMigrationsHistory` y `ReconciliacionTrazabilidad`.
- No se detectaron equivalentes exactos por `nombreFuncionalidad` en PROD para
  las seis funcionalidades canónicas. Esto no sustituye una revisión
  semántica manual.
- No se propone sobrescribir ningún dato funcional histórico de PROD.

## Comparación de esquema

| Elemento | Golden | PROD | Solo Golden | Solo PROD | Clasificación |
|---|---:|---:|---:|---:|---|
| Tablas de usuario | 40 | 26 | 14 | 0 | `CREATE_SCHEMA` para IAM e historial; trazabilidad no se promueve automáticamente |
| Columnas | 258 | 156 | 102 | 0 | `CREATE_SCHEMA` asociada a las tablas faltantes |
| PK | 39 | 25 | 14 | 0 | Crear las PK de las tablas faltantes |
| FK | 35 | 24 | 11 | 0 | Crear las FK IAM, respetando padres existentes |
| Índices | 61 | 26 | 35 | 0 | Crear índices definidos por la migración IAM |
| Defaults | 0 | 0 | 0 | 0 | `SIN_CAMBIOS` |
| Checks | 4 | 0 | 4 | 0 | Crear los checks IAM definidos por la migración |
| Migraciones EF | 1 | 0 | 1 | 0 | Aplicar `20260901215337_InitialIdentityAccess` mediante migración EF |

Las 14 tablas exclusivas de Golden son:

- `__EFMigrationsHistory`;
- `IamAccesoEmergencia`;
- `IamEventoAuditoriaAutorizacion`;
- `IamEventoAutenticacion`;
- `IamPermiso`;
- `IamRol`;
- `IamRolPermiso`;
- `IamUsuario`;
- `IamUsuarioClaim`;
- `IamUsuarioLoginExterno`;
- `IamUsuarioOrganizacion`;
- `IamUsuarioRol`;
- `IamUsuarioToken`;
- `ReconciliacionTrazabilidad`.

`ReconciliacionTrazabilidad` es infraestructura del proceso de reconciliación,
no una tabla funcional ni parte de la migración IAM; su promoción a PROD queda
fuera de esta fase salvo aprobación específica.

## Orden real de dependencias para una ejecución futura

El orden se deriva de las FK observadas en `sys.foreign_keys` y
`sys.foreign_key_columns`:

1. Aplicar la migración EF IAM y crear `__EFMigrationsHistory`.
2. Confirmar que `TEmpresaSubsidiaria` ya existe en PROD.
3. Insertar `IamPermiso`, `IamRol` e `IamUsuario`.
4. Insertar `IamRolPermiso` y `IamUsuarioRol`.
5. Insertar `IamAccesoEmergencia` e `IamUsuarioOrganizacion` (dependen también
   de `TEmpresaSubsidiaria`).
6. Insertar `IamUsuarioClaim`, `IamUsuarioLoginExterno` e `IamUsuarioToken`.
7. Insertar `IamEventoAutenticacion` y
   `IamEventoAuditoriaAutorizacion`.
8. Promover las seis filas de `TFuncionalidad`, después de validar sus FK a
   `TCapacidadDeSeguridad` y `TMEstadoFuncionalidad`.
9. Ejecutar validación de FK, constraints y conteos.

Las tablas funcionales existentes no requieren creación. La relación
`TBuildingBlockVsTTecnologiaTSI` continúa sin PK/UNIQUE en ambas bases y no se
modifica en FASE F.

## Comparación de datos funcionales

| Orden | Objeto | PROD | GOLDEN | Diferencia | Acción propuesta | Riesgo |
|---:|---|---:|---:|---|---|---|
| 1 | TMDominio | 9 | 9 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 2 | TEstadoFaseAdopcion | 5 | 5 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 3 | TMEstadoCapacidad | 5 | 5 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 4 | TMEstadoFuncionalidad | 5 | 5 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 5 | TMFamilia | 3 | 3 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 6 | TMEstadoAdopcionTSI | 5 | 5 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 7 | TMPosturaRoadmap | 5 | 5 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 8 | TModalidadLaboral | 3 | 3 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 9 | TTipoOperacion | 2 | 2 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 10 | TBuildingBlock | 147 | 147 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 11 | TEmpresaSubsidiaria | 24 | 24 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 12 | TTecnologiaTSI | 1 | 1 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 13 | TCapacidadDeSeguridad | 175 | 175 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 14 | TCISO | 24 | 24 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 15 | TCasosDeUso | 1 | 1 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 16 | TVendor | 2 | 2 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 17 | TRegulacionAplicable | 2 | 2 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 18 | TTecnologiaTSIimplementadaSubsidiaria | 1 | 1 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 19 | TContactoEmpresaSubsidiaria | 2 | 2 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 20 | TContactoPartner | 1 | 1 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 21 | TContactoVendor | 2 | 2 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 22 | TDriver | 6 | 6 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 23 | TModeloDeOperacion | 1 | 1 | EXCEPT=0/0 | `SIN_CAMBIOS` | Bajo |
| 24 | TBuildingBlockVsTTecnologiaTSI | 1 | 1 | EXCEPT=0/0 | `SIN_CAMBIOS`; conservar deuda técnica | Medio |
| 25 | TFuncionalidad | 500 | 506 | 6 solo Golden, 0 solo PROD | `INSERT_FALTANTES` tras aprobación | Alto |

No se encontraron registros exclusivos de PROD ni conflictos de contenido en
las tablas funcionales compartidas. `TCISO`, `TBuildingBlock`,
`TCapacidadDeSeguridad`, `TEmpresaSubsidiaria` y los estados maestros no deben
ser sobrescritos.

## IAM

Golden contiene los siguientes conteos, mientras PROD no posee todavía las
tablas IAM:

| Tabla | PROD | GOLDEN | Acción propuesta |
|---|---:|---:|---|
| IamPermiso | 0 / tabla inexistente | 15 | `CREATE_SCHEMA` + `INSERT_FALTANTES` |
| IamRol | 0 / tabla inexistente | 4 | `CREATE_SCHEMA` + `INSERT_FALTANTES` |
| IamUsuario | 0 / tabla inexistente | 2 | `CREATE_SCHEMA` + `INSERT_FALTANTES` |
| IamRolPermiso | 0 / tabla inexistente | 18 | Insertar después de roles y permisos |
| IamUsuarioRol | 0 / tabla inexistente | 2 | Insertar después de usuarios y roles |
| IamEventoAutenticacion | 0 / tabla inexistente | 60 | Insertar después del esquema |
| IamEventoAuditoriaAutorizacion | 0 / tabla inexistente | 5 | Insertar después del esquema |
| IamAccesoEmergencia | 0 / tabla inexistente | 0 | Crear tabla; sin filas actuales |
| IamUsuarioClaim | 0 / tabla inexistente | 0 | Crear tabla; sin filas actuales |
| IamUsuarioLoginExterno | 0 / tabla inexistente | 0 | Crear tabla; sin filas actuales |
| IamUsuarioOrganizacion | 0 / tabla inexistente | 0 | Crear tabla; sin filas actuales |
| IamUsuarioToken | 0 / tabla inexistente | 0 | Crear tabla; sin filas actuales |

La futura copia debe preservar los identificadores y relaciones de usuarios,
roles y permisos, incluyendo `jean` y `administrador`, sin exponer hashes,
sellos de seguridad, tokens ni otros valores sensibles.

## TFuncionalidad

Golden tiene rango de IDs `2267–3007` y 506 filas; PROD tiene rango
`2267–2766` y 500 filas. Las seis filas exclusivas de Golden son:

| PK Golden | Nombre funcional | FK capacidad | FK estado | Equivalente exacto por nombre en PROD |
|---:|---|---:|---:|---|
| 3002 | WAF - Attack Detection | 1 | 1 | No |
| 3003 | WAF - Real-time Monitoring | 1 | 1 | No |
| 3004 | WAF - Rule-Based Policies | 1 | 1 | No |
| 3005 | WAF - Logging and Reporting | 1 | 1 | No |
| 3006 | WAF- Rate Limiting | 1 | 1 | No |
| 3007 | Integraciones con terceros | 1 | 2 | No |

La FK de capacidad `1` y los estados `1`/`2` existen en las tablas padre de
PROD y Golden. Aun así, antes de insertar debe validarse la semántica completa
(descripción, capacidad y estado) y aprobarse una estrategia de identidad:

- preservar las PK 3002–3007 con `IDENTITY_INSERT`, o
- generar nuevas PK en PROD y registrar una trazabilidad canónica.

No se debe usar una coincidencia por nombre como única prueba de no duplicidad.

## Tabla de objetos y acciones

| Objeto | PROD | GOLDEN | Diferencia | Acción propuesta | Riesgo |
|---|---|---|---|---|---|
| Migración `InitialIdentityAccess` | No registrada | Registrada | Falta historial y esquema IAM | `CREATE_SCHEMA` mediante EF | Alto |
| Tablas IAM | No existen | 12 | Delta completo | `CREATE_SCHEMA` + datos IAM | Alto |
| `__EFMigrationsHistory` | No existe | 1 fila | Falta historial | Registrar mediante migración EF | Alto |
| `ReconciliacionTrazabilidad` | No existe | 6 | Infraestructura de reconciliación | `VALIDACION`; no promover automáticamente | Medio |
| 25 tablas funcionales compartidas | Existen | Existen | Sin diferencias de datos | `SIN_CAMBIOS` | Bajo |
| `TFuncionalidad` | 500 | 506 | Seis filas Golden | `INSERT_FALTANTES` pendiente | Alto |
| `TBuildingBlockVsTTecnologiaTSI` | 1, sin PK/UNIQUE | 1, sin PK/UNIQUE | Deuda técnica existente | `VALIDACION`; no alterar | Medio |

## Bloqueos y aprobaciones requeridas

1. Aprobar la aplicación de la migración IAM sobre PROD.
2. Aprobar la copia de IAM desde Golden preservando GUID, IDs, hashes y
   relaciones sin mostrarlos.
3. Decidir la estrategia de PK para las seis funcionalidades 3002–3007.
4. Confirmar revisión semántica de las seis funcionalidades y ausencia de
   duplicados funcionales históricos.
5. Mantener fuera de PROD `ReconciliacionTrazabilidad`, salvo aprobación
   explícita.
6. Mantener sin cambios `TBuildingBlockVsTTecnologiaTSI`.

Hasta resolver estos puntos, FASE F permanece en estado de `VALIDACION` y no
debe ejecutar escrituras.
