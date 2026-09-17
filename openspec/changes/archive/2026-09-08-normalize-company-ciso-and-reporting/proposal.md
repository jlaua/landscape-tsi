## Why

`TEmpresaSubsidiaria.contactoCiso` duplica en texto una relación que ya está
modelada por `TCISO.idEmpresaSubsidiaria`, por lo que puede quedar desfasada y
no permite representar correctamente cero, uno o varios CISO por empresa. Se
necesita una prevalidación de datos y un reporte de gobierno que use la
relación real antes de proponer retirar la columna redundante.

## What Changes

- Analizar en solo lectura la cardinalidad Empresa/Subsidiaria–CISO y comparar
  `contactoCiso` con `TCISO.nombreCISO`.
- Inventariar todas las dependencias de `contactoCiso` en código, consultas,
  vistas, reportes y pruebas.
- Crear el reporte de Reportería “Empresas y CISO”, con vistas por empresa y
  por CISO, filtros, indicadores, paginación y navegación segura.
- Derivar los indicadores mediante `LEFT JOIN` a `TCISO`, mostrando también
  empresas sin CISO y detectando representantes ausentes o duplicados.
- Mantener `contactoCiso` durante esta fase; preparar una propuesta separada
  de migración, riesgos y rollback para su eventual retiro.
- Proponer, sin aplicarlo, un índice único filtrado para un único representante
  por empresa cuando la prevalidación confirme esa regla.

## Capabilities

### New Capabilities

- `organization/company-ciso-reporting`: prevalidación de la relación
  Empresa/Subsidiaria–CISO y reporte de gobierno basado en `TCISO`.

### Modified Capabilities

- Ninguna.

## Impact

- `TEmpresaSubsidiaria`, `TCISO` y sus relaciones existentes se consultan sin
  cambios de esquema ni datos.
- Se afectan los servicios de Reportería, ViewModels, vistas Razor, rutas y
  pruebas de navegación.
- La autorización y el alcance organizacional existentes deben aplicarse al
  reporte; los datos sensibles no se exponen.
- La eventual retirada de `contactoCiso` queda fuera de esta implementación y
  requiere aprobación posterior de DDL.
