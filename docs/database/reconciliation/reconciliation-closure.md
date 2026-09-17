# Cierre formal de reconciliación de ambientes

Fecha de cierre: 2026-09-06  
Resultado: **LANDSCAPE_TSI_ENVIRONMENTS_HOMOLOGATED_WITH_APPROVED_DIFFERENCES**

## Objetivo original

Construir una base canónica, reconciliar el esquema y la información funcional
de Landscape TSI, incorporar IAM mediante migraciones EF Core y promover de
forma controlada las diferencias aprobadas sin sobrescribir datos históricos
autoritativos de producción.

El proceso se ejecutó por fases y con protección explícita del destino. No se
realizaron escrituras accidentales en bases distintas del destino aprobado de
cada fase.

## Arquitectura final de ambientes

| Rol | Base | Estado |
|---|---|---|
| Golden Database | `db-landscape-tsi-reconcile` | Fuente canónica técnica y funcional |
| Desarrollo activo | `db-landscape-tsi-dev-v2` | Homologado con Golden |
| Producción activa | `db-landscape-tsi` | Homologado con Golden, con diferencias aprobadas |
| Desarrollo anterior | `db-landscape-tsi-dev` | Conservado como rollback histórico |

La migración `20260901215337_InitialIdentityAccess` está registrada en los tres
ambientes activos. Los conteos funcionales y la configuración IAM quedaron
homologados; las validaciones `EXCEPT` fueron cero en ambos sentidos para las
26 tablas funcionales.

## Golden Database

`db-landscape-tsi-reconcile` queda designada como Golden Database durante esta
etapa. Contiene el modelo funcional canónico, IAM y la trazabilidad temporal de
reconciliación.

`dbo.ReconciliacionTrazabilidad` es infraestructura del proceso y no forma
parte del modelo productivo.

## Diferencias aprobadas

1. `IamEventoAutenticacion` e `IamEventoAuditoriaAutorizacion` conservan la
   historia propia de cada ambiente.
2. `dbo.ReconciliacionTrazabilidad` está ausente en PROD por diseño.
3. `TBuildingBlockVsTTecnologiaTSI` continúa sin PK/UNIQUE como deuda técnica.

Estas diferencias no deben resolverse mediante copias manuales ni mediante
restores entre ambientes.

## Backups recomendados

- Mantener backup completo verificado antes de cada migración de esquema.
- Mantener backup diferencial y de log según el SLA de cada ambiente.
- Probar periódicamente la restauración en una ubicación aislada.
- Conservar el backup de `db-landscape-tsi-dev` como rollback hasta completar
  el periodo operativo acordado.
- Documentar fecha, LSN, retención y responsable de cada backup sin incluir
  secretos ni cadenas de conexión.

## Estrategia de rollback

- Cambios de esquema: rollback mediante una migración EF Core compensatoria;
  no editar migraciones aplicadas ni restaurar PROD desde DEV.
- Cambios de datos maestros: utilizar las interfaces administrativas y
  respaldos aprobados, con auditoría y revisión previa.
- Despliegues de aplicación: volver al artefacto anterior compatible con el
  esquema vigente.
- Un restore de PROD solo podrá ejecutarse como procedimiento de recuperación
  ante desastre, con aprobación operativa independiente y respaldo verificado;
  nunca como mecanismo normal de homologación.

## Política de evolución posterior

1. No se realizarán homologaciones manuales normales entre bases.
2. Los cambios de esquema se promoverán exclusivamente mediante EF Core
   Migrations versionadas.
3. Flujo obligatorio: **Código → DEV → Pruebas → PROD**.
4. Los cambios de master data utilizarán las interfaces administrativas de
   Landscape TSI cuando estén disponibles.
5. PROD nunca se actualizará mediante RESTORE desde DEV.
6. Las cuentas runtime conservarán permisos mínimos y separados de las cuentas
   administrativas o de migración.
7. Antes de cada promoción se validarán destino, backup, migraciones,
   constraints, FK huérfanas y resultado funcional.

## Recomendaciones operativas

- Mantener separados los secretos por ambiente y fuera del repositorio.
- Revisar periódicamente roles, permisos y cuentas break-glass.
- Monitorizar errores de autenticación y auditoría sin registrar hashes,
  tokens, contraseñas ni cadenas de conexión.
- Ejecutar `DBCC CHECKDB` según la ventana operativa aprobada.
- Generar un reporte de validación posterior a cada release.
- Mantener la documentación de Golden, DEV-v2 y PROD alineada con el código.

## Trabajo futuro

Registrar como deuda independiente:

`mapear-relacion-buildingblock-tecnologia`

El trabajo deberá evaluar, sin asumir una solución:

- PK compuesta o restricción UNIQUE;
- mapping EF Core;
- cardinalidad many-to-many;
- duplicados y datos existentes;
- impacto en consultas, migraciones y reportes.

No se modifica la tabla durante este cierre.

## Seguridad del cierre

Este cierre es exclusivamente documental. No ejecuta SQL, no realiza
migraciones y no modifica esquema ni datos en ninguna base.
