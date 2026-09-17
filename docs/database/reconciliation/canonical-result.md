# Resultado FASE D — Golden Database candidata

Fecha de validación: 2026-09-06. Todas las comprobaciones de Codex fueron de
solo lectura sobre `db-landscape-tsi-reconcile`. No se ejecutaron DML, DDL,
migraciones ni DBCC.

## Migraciones y IAM

- Migración aplicada: `20260901215337_InitialIdentityAccess` (`10.0.0`).
- Las 12 tablas IAM existen.
- Conteos RECONCILE = DEV:

| Tabla | RECONCILE | DEV |
|---|---:|---:|
| IamPermiso | 15 | 15 |
| IamRol | 4 | 4 |
| IamUsuario | 2 | 2 |
| IamRolPermiso | 18 | 18 |
| IamUsuarioRol | 2 | 2 |
| IamEventoAutenticacion | 60 | 60 |
| IamEventoAuditoriaAutorizacion | 5 | 5 |
| IamAccesoEmergencia | 0 | 0 |
| IamUsuarioClaim | 0 | 0 |
| IamUsuarioLoginExterno | 0 | 0 |
| IamUsuarioOrganizacion | 0 | 0 |
| IamUsuarioToken | 0 | 0 |

`jean` y `administrador` están activos y ambos tienen el rol
`SYSTEM_ADMINISTRATOR`. No se expusieron hashes, stamps ni tokens.

## Datos funcionales

| Tabla | Filas |
|---|---:|
| sysdiagrams | 1 |
| TBuildingBlock | 147 |
| TBuildingBlockVsTTecnologiaTSI | 1 |
| TCapacidadDeSeguridad | 175 |
| TCasosDeUso | 1 |
| TCISO | 24 |
| TContactoEmpresaSubsidiaria | 2 |
| TContactoPartner | 1 |
| TContactoVendor | 2 |
| TDriver | 6 |
| TEmpresaSubsidiaria | 24 |
| TEstadoFaseAdopcion | 5 |
| TFuncionalidad | 506 |
| TMDominio | 9 |
| TMEstadoAdopcionTSI | 5 |
| TMEstadoCapacidad | 5 |
| TMEstadoFuncionalidad | 5 |
| TMFamilia | 3 |
| TModalidadLaboral | 3 |
| TModeloDeOperacion | 1 |
| TMPosturaRoadmap | 5 |
| TRegulacionAplicable | 2 |
| TTecnologiaTSI | 1 |
| TTecnologiaTSIimplementadaSubsidiaria | 1 |
| TTipoOperacion | 2 |
| TVendor | 2 |

## Funcionalidades promovidas

| DEV PK | RECONCILE PK | Nombre | Capacidad | Estado | Trazabilidad |
|---:|---:|---|---|---|---|
| 2 | 3002 | WAF - Attack Detection | Web Application Firewall | CUBIERTO | PROMOTE |
| 3 | 3003 | WAF - Real-time Monitoring | Web Application Firewall | CUBIERTO | PROMOTE |
| 4 | 3004 | WAF - Rule-Based Policies | Web Application Firewall | CUBIERTO | PROMOTE |
| 5 | 3005 | WAF - Logging and Reporting | Web Application Firewall | CUBIERTO | PROMOTE |
| 6 | 3006 | WAF- Rate Limiting | Web Application Firewall | CUBIERTO | PROMOTE |
| 8 | 3007 | Integraciones con terceros | Web Application Firewall | PARCIALMENTE | PROMOTE |

## Integridad estructural

- PK detectadas: 39.
- FK detectadas: 35.
- Índices detectados: 61.
- Check constraints detectados: 4.
- Huérfanos FK: 0 en todas las relaciones funcionales e IAM verificadas.
- `TBuildingBlockVsTTecnologiaTSI` continúa sin PK/UNIQUE; queda registrada
  como deuda técnica independiente y no fue modificada.

## DBCC pendiente

Debe ejecutarse administrativamente como `sa`:

- `DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS`;
- `DBCC CHECKDB`.

Codex no ejecutó DBCC ni puede certificar su resultado.

## Conclusión

`db-landscape-tsi-reconcile` **puede considerarse GOLDEN DATABASE CANDIDATE
CONDICIONAL**: migración IAM, datos IAM, funcionalidades, FK, PK, índices y
conteos son consistentes; la certificación final queda bloqueada únicamente
hasta recibir el resultado administrativo satisfactorio de ambos DBCC y la
deuda técnica de `TBuildingBlockVsTTecnologiaTSI` permanece fuera de alcance.
