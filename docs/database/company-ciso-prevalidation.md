# Prevalidación Empresa/Subsidiaria–CISO

La relación autoritativa es `TCISO.idEmpresaSubsidiaria` →
`TEmpresaSubsidiaria.idEmpresaSubsidiaria`. La columna
`TEmpresaSubsidiaria.contactoCiso` se conserva únicamente como dato legado de
comparación durante esta fase.

La consulta reproducible está en
`scripts/database/company-ciso-prevalidation.sql`. Está protegida para aceptar
únicamente `db-landscape-tsi-dev-v2` y contiene exclusivamente `SELECT`.

## Dependencias encontradas

La referencia funcional activa a `contactoCiso` se encuentra en:

- `src/Landscape.Tsi.Application/Catalogs/MasterCatalogRegistry.cs`, donde se
  expone como columna editable/listable del catálogo Empresa/Subsidiaria.
- El modelo dinámico de catálogos y sus vistas Razor, que consumen las columnas
  declaradas por `MasterCatalogRegistry`.
- La documentación del modelo que describe contactos redundantes.

No se debe retirar la columna hasta eliminar estas lecturas y actualizar el
contrato de catálogo en un cambio DDL independiente.

## Regla futura de representante

La consulta enumera empresas con más de un `Representante = 1` y empresas sin
representante. Solo si el resultado confirma la regla de unicidad se podrá
crear un índice único filtrado sobre `TCISO(idEmpresaSubsidiaria)` con filtro
`Representante = 1`.

No se crea índice, no se modifica la columna y no se actualizan datos en esta
fase.

## Resultado DEV (solo lectura)

Ejecutado sobre `db-landscape-tsi-dev-v2` el 2026-09-08:

- 24 empresas/subsidiarias y 24 CISO.
- 0 empresas sin CISO; 24 con un CISO; 0 con varios CISO.
- 16 empresas sin `Representante = 1`.
- 0 empresas con representantes duplicados.
- 6 diferencias o ausencias de coincidencia entre `contactoCiso` y
  `TCISO.nombreCISO`.

La consulta usa `TEmpresaSubsidiaria.nombreEmpresa`, que es el nombre físico
real del esquema DEV. No se ejecutaron DML, DDL ni migraciones.

## Validación HTTP y visual

La ruta `/reporteria/empresas-ciso` respondió `302` para solicitudes anónimas y
`200` para `jean` con alcance corporativo. Las vistas `allCiso`, búsqueda,
paginación y filtros respondieron `200`. Los detalles válidos de Empresa y
CISO respondieron `200`; IDs inexistentes respondieron `404` controlado. Una
identidad autenticada sin `Catalogos.Ver` respondió `403` en pruebas HTTP
automatizadas.

La validación visual con Chromium se ejecutó a 1440 px y 390 px. En ambos
viewports no hubo overflow horizontal ni superposición; se observaron 77
controles enfocables y 9 etiquetas de formulario. La navegación, KPIs,
filtros, tabla y paginación conservaron el diseño responsive.
