# Reconciliación de Tablas Maestras

## Estado verificado

El Product Owner aprobó para esta fase los permisos existentes de catálogos,
auditoría y administración, manteniendo alcance corporativo activo para toda
mutación. Identity/OIDC permanece diferido y no se crean permisos nuevos.
También confirmó que email y teléfono de CISO son PII y que sus límites
técnicos son los tipos y longitudes existentes en `TCISO`; esta fase no agrega
validaciones físicas, migraciones ni DDL.

La implementación actual conserva una lista blanca de 16 catálogos, rutas MVC
nombradas, proyección de campos funcionales, paginación server-side,
resolutores de FK y auditoría transaccional para crear/editar. Las PK técnicas
no se presentan en la interfaz y el catálogo se resuelve únicamente por código
registrado; no se acepta el nombre de una tabla desde HTTP.

Los cuatro catálogos autoritativos (`estado-adopcion-tsi`, `fase-adopcion`,
`estado-capacidad` y `estado-funcionalidad`) son explícitamente de solo lectura
en servidor y en la UI. Los demás catálogos permanecen sin retiro: no se ofrece
ni se ejecuta una ruta DELETE salvo los flujos heredados de análisis de impacto
aprobados para las entidades con soporte existente.

## Seguridad y datos

La autenticación y autorización usan el IAM local existente y las políticas
`Catalogos.Ver`, `Catalogos.Crear`, `Catalogos.Editar` y
`Catalogos.Eliminar`. Las operaciones de edición registran actor, catálogo,
registro, cambios y correlación en la auditoría append-only. No se ejecutaron
migrations, DDL, backfill ni DML en esta reconciliación; la única consulta DEV
de validación confirmó `db-landscape-tsi-dev-v2`.

## Evidencia de integración

La ejecución externa aprobada contra `db-landscape-tsi-dev-v2` validó las ocho
pruebas dinámicas del mapeo Building Block/Tecnología y la prueba SQL de
delete/restore, para un total focalizado de 9/9. La suite completa obtuvo
139/139 antes de la ampliación MVC de esta reconciliación.

La concurrencia MVC se verifica con dos clientes HTTP independientes que leen
el mismo formulario y conservan cookies y tokens antiforgery propios. La
primera escritura se confirma y la segunda, basada en la versión obsoleta, se
rechaza antes de invocar la mutación. También se verifican token CSRF ausente,
permiso de edición ausente y alcance corporativo ausente, todos sin mutación.

La cobertura MVC parametrizada adicional obtuvo 19/19 y recorre listado,
detalle y formularios de creación/edición de los doce catálogos mutables, junto
con lectura y denegación de rutas de mutación para los cuatro autoritativos.
Usa `WebApplicationFactory`, autenticación de prueba y dobles de servicios, por
lo que no abre SQL. Combinada con los validadores tipados de los doce catálogos,
la concurrencia MVC y las 9/9 pruebas SQL DEV dinámicas aceptadas, cubre consulta,
FK, overposting, aislamiento, limpieza y persistencia real sin datos seed fijos.

## Evidencia visual y WCAG

El navegador autenticado validó las listas, detalles y formularios a
1440 x 900 y 390 x 844. No se detectó overflow global ni recorte de controles
en Master Tables, Dominio, Building Block, Capacidad, Funcionalidad, Caso de
Uso y CISO. Auditoría presentó inicialmente overflow móvil por textos largos;
se corrigió la contención de ancho y se repitió la medición a 390 px con ancho
de documento igual al viewport.

La revisión WCAG comprobó Tab y Shift+Tab, foco visible, labels y nombres
accesibles, encabezados de tabla, alertas de validación, diálogos, cierre con
Escape y prevención de pérdida de cambios. Se corrigió el contraste del enlace
de salto, que heredaba azul sobre azul; la nueva combinación usa texto blanco
sobre el color primario y la repetición automatizada no detectó fallos de
contraste en las vistas representativas.

## Gaps cerrados y verificación final

Se ejecutó y validó la verificación completa del cambio:
- `dotnet format --verify-no-changes` ejecutado correctamente (código de salida 0).
- `dotnet build --configuration Release` ejecutado con 0 errores y 0 advertencias.
- Suite de pruebas completa: 164 pruebas ejecutadas, 164 superadas (0 errores, 0 omitidas).
- Pruebas dinámicas de integración SQL ejecutadas contra `db-landscape-tsi-dev-v2`: 9/9 superadas (8 de `BuildingBlockTechnologyMappingServiceTests` y 1 de `AuditRestoreSqlTests`) utilizando prefijo `ITEST_<GUID>` con limpieza garantizada y validación estricta de base de datos.
- Las 40 tareas de `gestionar-tablas-maestras` se encuentran 100% completadas y verificadas.

## Concurrencia y PII

Las ediciones MVC incluyen un token SHA-256 lógico derivado de los valores
observados; si el registro cambió, el servidor rechaza la versión antigua y
solicita recarga. No existe `rowversion` y no se agregó una columna.

En el listado CISO solo se muestran nombre y empresa. Email y teléfono quedan
fuera de la grilla y se redactan como `[REDACTED]` tanto en la presentación de
detalle como en la auditoría de catálogo. El editor ya no precarga esos valores:
dejarlos vacíos conserva los datos existentes y escribir un reemplazo explícito
los actualiza. No se registran contraseñas, tokens, hashes ni cadenas de conexión.

La repetición runtime posterior al reinicio confirmó el listado CISO sin datos
de contacto, el detalle con email y teléfono redactados, y el editor sin
precargar esos valores. La vista de Auditoría y un detalle de operación no
contuvieron `PasswordHash`, `SecurityStamp`, tokens, passwords, secretos,
emails sin redactar ni patrones de cadena de conexión. No se observaron errores
visibles ni overflow horizontal en estas vistas.

## Operación y rollback

La autorización operativa independiente cubre exclusivamente el despliegue de
la aplicación en Landscape TSI DEV con `db-landscape-tsi-dev-v2`, condicionada
a build, pruebas, validación strict, PII runtime y CRUD DEV aprobados. No
autoriza producción ni ejecución automática de migraciones.

El smoke DEV read-only posterior al despliegue recorrió Inicio, Administración
de Tablas Maestras, Mapeo de Tecnologías, Reportería, Auditoría y detalles de
Building Block, Capacidad y CISO. Todas las rutas conservaron autenticación,
respondieron sin 404/500 inesperados, no mostraron secretos y no presentaron
overflow horizontal.

La consulta requiere `Catalogos.Ver`; crear y editar requieren además
`Catalogos.Crear` o `Catalogos.Editar`, alcance corporativo activo y auditoría
disponible. Los cuatro catálogos autoritativos son solo lectura. El retiro no
está habilitado para catálogos sin estrategia aprobada. Ante un error de
auditoría, la transacción se revierte y se muestra un mensaje funcional con el
correlation id disponible en logs. El rollback operativo consiste en
deshabilitar las capacidades de mutación de la aplicación; no requiere ni
ejecuta migrations.
