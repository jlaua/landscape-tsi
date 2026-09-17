# Propuesta futura: normalización Empresa/CISO

Este documento no autoriza ni ejecuta cambios de esquema.

## Dependencias que deben desaparecer antes de retirar `contactoCiso`

- `MasterCatalogRegistry` lo declara como campo de Empresa/Subsidiaria.
- El modelo genérico de catálogos lo expone en consultas y formularios.
- La documentación de catálogos lo describe como dato editable.
- La prevalidación lo conserva como dato legado de comparación.

La relación autoritativa futura seguirá siendo
`TCISO.idEmpresaSubsidiaria -> TEmpresaSubsidiaria.idEmpresaSubsidiaria`.

## Migration futura propuesta

1. Confirmar que ningún C# / Razor / EF / SQL / reporte / test lea o escriba
   `contactoCiso`.
2. Conservar el reporte de diferencias como evidencia y exportar el backup
   aprobado de la columna.
3. En una migration separada y revisada, retirar únicamente la columna
   `contactoCiso`.
4. Evaluar por separado un índice único filtrado sobre
   `TCISO(idEmpresaSubsidiaria)` para `Representante = 1`.

## Impacto y rollback

El impacto sería la pérdida de la copia textual legado y cualquier formulario
que aún la exponga. El rollback requiere restaurar la columna desde el backup
aprobado, volver a desplegar el modelo de catálogo compatible y verificar que
la relación `TCISO` siga intacta. No se propone backfill automático.

La migration, el índice, el backfill y el `DROP COLUMN` requieren aprobación
explícita posterior.
