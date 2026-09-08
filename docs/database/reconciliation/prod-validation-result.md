# FASE F4 — Resultado de validación integral de PROD

Fecha: 2026-09-06  
Base validada: `db-landscape-tsi`  
Fuente comparativa: `db-landscape-tsi-reconcile`

La fase fue exclusivamente de lectura. No se ejecutaron escrituras, DDL,
migraciones ni restores.

## Conclusión

**PROD_VALIDATED_WITH_APPROVED_DIFFERENCES**

Las diferencias restantes son las aprobadas por el proceso:

1. auditoría y eventos específicos por ambiente: PROD inicia con cero eventos;
2. `dbo.ReconciliacionTrazabilidad` permanece fuera de PROD;
3. `TBuildingBlockVsTTecnologiaTSI` continúa sin PK/UNIQUE como deuda técnica.

## IAM y migración

- `20260901215337_InitialIdentityAccess`: registrada.
- Las 12 tablas IAM están presentes.
- `IamPermiso`: 15.
- `IamRol`: 4.
- `IamUsuario`: 2.
- `IamRolPermiso`: 18.
- `IamUsuarioRol`: 2.
- `jean`: activo, rol `SYSTEM_ADMINISTRATOR`.
- `administrador`: activo, rol `SYSTEM_ADMINISTRATOR`.
- FK de roles/permisos huérfanas: 0.
- FK de usuarios/roles huérfanas: 0.

No se documentan hashes, sellos de seguridad, tokens ni otros valores
confidenciales.

## Auditoría por ambiente

| Tabla | Filas PROD |
|---|---:|
| `IamEventoAutenticacion` | 0 |
| `IamEventoAuditoriaAutorizacion` | 0 |

Estos ceros son correctos para un PROD recién habilitado y no constituyen una
diferencia funcional que deba sincronizarse desde Golden.

## Comparación de datos funcionales

Se compararon 26 tablas mediante ambos sentidos de `EXCEPT`:

```text
Golden EXCEPT PROD = 0 filas en todas las tablas
PROD EXCEPT Golden = 0 filas en todas las tablas
```

Incluye `TFuncionalidad` con 506 filas, `TCISO`, `TBuildingBlock`,
`TCapacidadDeSeguridad`, `TEmpresaSubsidiaria`, estados maestros,
`TBuildingBlockVsTTecnologiaTSI` y `sysdiagrams`.

Todas las tablas quedaron clasificadas como `IDENTICO` para datos funcionales.

## TFuncionalidad

- Total PROD: 506.
- PK 3002: presente.
- PK 3003: presente.
- PK 3004: presente.
- PK 3005: presente.
- PK 3006: presente.
- PK 3007: presente.
- FK funcionales huérfanas: 0.

## Comparación de esquema

La comparación excluye explícitamente `dbo.ReconciliacionTrazabilidad`.

| Elemento | Golden | PROD | Solo Golden | Solo PROD | Estado |
|---|---:|---:|---:|---:|---|
| Tablas | 39 | 39 | 0 | 0 | IDENTICO |
| Columnas | 248 | 248 | 0 | 0 | IDENTICO |
| PK | 38 | 38 | 0 | 0 | IDENTICO |
| FK | 35 | 35 | 0 | 0 | IDENTICO |
| Índices | 60 | 60 | 0 | 0 | IDENTICO |
| Defaults | 0 | 0 | 0 | 0 | IDENTICO |
| Checks | 4 | 4 | 0 | 0 | IDENTICO |

La igualdad de columnas incluye tipo, longitud, precisión, escala, nulabilidad
e indicador `IDENTITY`.

## Integridad

- FK huérfanas en todas las relaciones: 0.
- PK duplicadas: 0; las restricciones PK están presentes y válidas.
- Referencias IAM faltantes: 0.
- Referencias funcionales faltantes: 0.

## Diferencias aprobadas y deuda técnica

| Diferencia | Tratamiento |
|---|---|
| Eventos de autenticación/autorización | Específicos de ambiente; no copiar desde Golden |
| `ReconciliacionTrazabilidad` | Ausente en PROD por diseño |
| `TBuildingBlockVsTTecnologiaTSI` sin PK/UNIQUE | Registrar como deuda; no modificar durante F4 |

FASE G no fue ejecutada.
