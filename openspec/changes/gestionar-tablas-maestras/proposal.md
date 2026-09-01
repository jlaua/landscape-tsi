## Why

Landscape TSI necesita una administración Web gobernada para sus catálogos existentes, hoy accesibles solo como objetos SQL Server y sin una experiencia funcional consistente. El cambio habilita su consulta y mantenimiento controlado sin exponer claves técnicas, nombres físicos ni operaciones genéricas sobre tablas, preservando la base existente y sus relaciones.

## What Changes

- Incorporar el módulo de **Administración de Tablas Maestras** al monolito modular ASP.NET Core MVC, con un registro explícito y cerrado de los 16 catálogos autorizados.
- Proporcionar selección de catálogo, búsqueda, filtros aplicables, conteo, paginación, detalle, creación y edición mediante experiencias adaptadas a la complejidad real de cada objeto.
- Presentar las relaciones mediante valores funcionales obtenidos del servidor; las PK y FK técnicas se conservarán internamente y no se mostrarán como columnas de negocio.
- Autorizar cada operación en el servidor mediante `Catalogos.Ver`, `Catalogos.Crear`, `Catalogos.Editar` y `Catalogos.Desactivar`, asignables inicialmente a Administrador del Sistema y Arquitecto de Seguridad Corporativo conforme a las políticas de Landscape TSI.
- Registrar cada intento de mantenimiento con actor, instante, catálogo, registro, operación, cambios permitidos y resultado, sin secretos ni datos innecesarios.
- Bloquear el borrado físico inicial: ninguna de las 16 tablas posee un indicador activo/inactivo y varias tienen dependencias `NO_ACTION`; cualquier estrategia física o lógica futura requerirá aprobación independiente.
- Mantener `TMEstadoAdopcionTSI`, `TEstadoFaseAdopcion`, `TMEstadoCapacidad` y `TMEstadoFuncionalidad` como catálogos autoritativos de solo lectura en esta capacidad. Un cambio de sus valores requiere otro cambio OpenSpec aprobado y autorización operativa independiente.
- Aplicar Material Design 3, diseño adaptable para escritorio/tableta/móvil, breadcrumbs, estados de carga/vacío/error/éxito y WCAG 2.2 AA.
- No modificar el esquema ni los datos de SQL Server. Las migraciones podrán generarse posteriormente para revisión, pero nunca ejecutarse contra producción por este cambio.

## Capabilities

### New Capabilities

- `catalogos-maestros/administracion`: selección, consulta y mantenimiento gobernado de los catálogos incluidos en la lista blanca, respetando relaciones e integridad.
- `catalogos-maestros/seguridad-auditoria`: permisos, políticas, responsabilidades, alcance, protección contra selección arbitraria de tablas y auditoría de operaciones.
- `catalogos-maestros/experiencia-usuario`: comportamiento visual, accesible y adaptable de selección, listados, formularios, detalle y confirmaciones.

### Modified Capabilities

Ninguna. Este cambio consume los contratos propuestos por `establish-identity-access-foundation` sin modificar sus requisitos.

## Impact

- **Módulos:** nuevo límite funcional de Catálogos Maestros; dependencias hacia Identidad y Acceso para autorización/auditoría, y hacia Infraestructura para mappings explícitos de EF Core a `dbo`.
- **Aplicación:** controladores, casos de uso, modelos de presentación, validación del servidor y componentes MVC específicos por definición de catálogo; no existe todavía código de aplicación en el repositorio.
- **SQL Server:** reutiliza las 16 tablas, PK, FK y restricciones observadas. No propone DDL ni DML sobre producción ni rediseña objetos funcionales.
- **Seguridad:** denegación predeterminada, permisos atómicos, políticas contextuales, lista blanca, protección contra mass assignment y auditoría de acciones privilegiadas. El nombre de rol es una asignación inicial, no la decisión final de autorización.
- **Organización:** `TEmpresaSubsidiaria` es un catálogo corporativo; su mantenimiento requiere alcance corporativo explícito. Los demás catálogos son corporativos salvo que una definición aprobada indique lo contrario.
- **Dependencias de entrega:** las mutaciones quedan condicionadas a que autenticación, autorización por políticas y auditoría confiable del cambio `establish-identity-access-foundation` estén disponibles; en su ausencia, el sistema falla de forma cerrada.
- **Nomenclatura pendiente:** “Administrador del Sistema” y “Arquitecto de Seguridad Corporativo” se tratan como etiquetas funcionales de los roles canónicos Administrador y Arquitecto de Seguridad, respectivamente, hasta que la matriz de identidad confirme sus códigos estables.
