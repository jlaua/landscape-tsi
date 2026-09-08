## 1. Prerrequisitos y base del proyecto

- [x] 1.1 Confirmar los códigos IAM de Administrador del Sistema y Arquitecto de Seguridad Corporativo, sus cuatro permisos y el alcance corporativo, y verificar la aprobación registrada antes de configurar asignaciones.
- [x] 1.2 Confirmar límites de longitud/formato y tratamiento de datos de CISO con el responsable funcional, y verificar que las reglas acordadas queden cubiertas por pruebas de validación.
- [x] 1.3 Crear la solución .NET 10, proyectos del monolito modular, `global.json` y `.editorconfig` conforme a AGENTS.md, y verificar con `dotnet restore` y `dotnet build --configuration Release`.
- [x] 1.4 Establecer una configuración de desarrollo/pruebas que no contenga secretos ni apunte a producción para escritura, y verificar mediante una prueba de configuración que las mutaciones fallan cerradas ante un destino marcado como producción.

## 2. Persistencia y compatibilidad SQL Server

- [x] 2.1 Crear mappings EF Core explícitos para las 16 tablas `dbo`, incluidos nombres históricos, PK `IDENTITY`, nulabilidad y FK, y verificarlos con pruebas de metadatos contra una base no productiva o copia sanitizada.
- [x] 2.2 Implementar consultas tipadas y paginadas para conteo, búsqueda, filtros y ordenamientos registrados, y verificar resultados y límites con pruebas de integración para los 16 catálogos.
- [x] 2.3 Implementar resolutores tipados para las seis familias de FK de formularios, y verificar que devuelvan etiquetas funcionales y rechacen IDs inexistentes o de otro catálogo.
- [x] 2.4 Implementar detección optimista de concurrencia basada en valores originales/token firmado sin cambiar el esquema, y verificar que dos ediciones paralelas no produzcan sobrescritura silenciosa.
- [x] 2.5 Generar, solo si la herramienta lo requiere, una migración revisable y verificar automáticamente que no contiene DDL ni DML para ninguna de las 16 tablas ni operaciones contra los cuatro catálogos autoritativos; no ejecutar la migración en producción.

## 3. Registro controlado y casos de uso

- [x] 3.1 Implementar el registro inmutable `CatalogoMaestroDefinition` con códigos funcionales, campos, modos, permisos, alcance y estrategias para los 16 catálogos, y verificar que el inventario coincide exactamente con las especificaciones.
- [x] 3.2 Implementar resolución de catálogo por código de lista blanca y orden/filtro por expresiones registradas, y verificar que nombres de tabla, columnas y payloads manipulados se rechazan sin generar SQL dinámico.
- [x] 3.3 Implementar listados y detalles que proyecten únicamente campos funcionales y relaciones legibles, y verificar que ninguna PK/FK técnica se presenta como columna o etiqueta de negocio.
- [x] 3.4 Implementar comandos tipados de creación y edición para los doce catálogos mutables, con allowlist de propiedades y obligatoriedad funcional, y verificar caminos válidos, overposting, PK suministrada y campos faltantes.
- [x] 3.5 Configurar `SoloLectura` para `TMEstadoAdopcionTSI`, `TEstadoFaseAdopcion`, `TMEstadoCapacidad` y `TMEstadoFuncionalidad`, y verificar que consultas funcionan y toda mutación se deniega en servidor.
- [x] 3.6 Configurar `SinRetiro` para los otros doce catálogos y ausencia total de ejecución `DELETE`, y verificar que ninguna ruta ni caso de uso pueda eliminar o desactivar registros.

## 4. Autorización, alcance y auditoría

- [x] 4.1 Integrar `Catalogos.Ver`, `Catalogos.Crear`, `Catalogos.Editar` y `Catalogos.Desactivar` con políticas del servidor, y verificar pruebas permitidas y denegadas sin depender de visibilidad de botones.
- [x] 4.2 Exigir alcance corporativo explícito para mutaciones y especialmente para `TEmpresaSubsidiaria`, y verificar pruebas negativas con un usuario limitado a una subsidiaria.
- [x] 4.3 Aplicar separación de funciones del fundamento IAM para impedir autoelevación de permisos o alcance, y verificar que el intento se deniega y audita.
- [x] 4.4 Integrar eventos append-only con actor, instante, catálogo, registro, operación, cambios permitidos, resultado y correlación, y verificar atomicidad entre edición exitosa y auditoría en una base no productiva.
- [x] 4.5 Implementar falla cerrada cuando autorización o auditoría obligatoria no estén disponibles, y verificar que ninguna mutación se confirme y que el mensaje no revele secretos.
- [x] 4.6 Aplicar minimización y enmascaramiento aprobados a email/teléfono de CISO y a errores de infraestructura, y verificar que logs y auditoría no contienen credenciales, tokens, cadenas de conexión ni datos personales innecesarios.

## 5. Interfaz ASP.NET Core MVC

- [x] 5.1 Implementar Administración de Tablas Maestras con breadcrumbs, búsqueda y agrupación por Taxonomía, Tecnología/Adopción y Organización/Operación, y verificar que solo muestra catálogos y capacidades autorizados.
- [x] 5.2 Implementar listado con descripción, conteo, búsqueda, filtros aplicables, paginación, estados de carga/vacío/error y acciones efectivas, y verificar conservación de filtros y página tras operaciones.
- [x] 5.3 Implementar diálogos para Familia, Postura Roadmap, Modalidad Laboral y Tipo de Operación, además de detalle de los cuatro autoritativos, y verificar foco, validaciones y cierre accesibles.
- [x] 5.4 Implementar panel lateral en escritorio y página móvil para Capacidad, Funcionalidad, Caso de Uso y CISO, y verificar reflujo, prevención de pérdida de cambios y controles FK funcionales.
- [x] 5.5 Implementar páginas por secciones para Dominio, Building Block, Tecnología TSI y Empresa/Subsidiaria, y verificar etiquetas, orden de lectura, validación y ausencia de IDs técnicos.
- [x] 5.6 Implementar el mensaje de retiro no disponible y el patrón de confirmación futura sin activar acciones destructivas, y verificar que Cancelar nunca envía una mutación.
- [x] 5.7 Implementar responsive con tabla en escritorio y cards/resumen en móvil, y verificar visualmente anchos de escritorio, tableta y móvil sin desplazamiento horizontal excesivo.

## 6. Calidad, accesibilidad y seguridad

- [x] 6.1 Añadir pruebas unitarias por definición de catálogo, validación, política, relación y estrategia de retiro, y verificar que `dotnet test --configuration Release` cubre caminos normales y fallos relevantes.
- [x] 6.2 Añadir pruebas de integración MVC y SQL no productivo para consulta, creación, edición, concurrencia, FK, overposting y catálogos autoritativos, y verificar aislamiento y limpieza del entorno de prueba.
- [x] 6.3 Añadir pruebas automatizadas de autenticación/autorización para usuario anónimo, permiso ausente, rol sin permiso, alcance incorrecto, manipulación HTTP y falla de auditoría, y verificar denegación predeterminada sin efectos parciales.
- [x] 6.4 Ejecutar revisión WCAG 2.2 AA con teclado, lector de pantalla, foco, contraste, nombres accesibles y errores, y verificar resultados sin incidencias críticas o altas abiertas.
- [x] 6.5 Ejecutar análisis de seguridad para inyección, SQL dinámico, CSRF, overposting, XSS, exposición de IDs/PII y secretos, y verificar que no queden hallazgos críticos o altos.
- [x] 6.6 Ejecutar `dotnet format`, `dotnet build --configuration Release` y `dotnet test --configuration Release`, y verificar compilación sin errores y suite completa aprobada.

## 7. Entrega controlada y base de datos

- [x] 7.1 Documentar configuración, permisos, operación, soporte y rollback del módulo sin credenciales, y verificar que las instrucciones no contienen pasos automáticos de migración o escritura sobre producción.
- [x] 7.2 Validar en un entorno no productivo la lectura de los 16 catálogos y las mutaciones autorizadas de los doce habilitados, y verificar auditoría, concurrencia e integridad antes de solicitar promoción.
- [x] 7.3 Revisar el paquete de despliegue y cualquier script/migración para demostrar ausencia de `INSERT`, `UPDATE`, `DELETE`, `ALTER`, `CREATE`, `DROP` o ejecución de procedimientos contra la base real, y conservar evidencia de la revisión.
- [x] 7.4 Solicitar autorización operativa independiente para desplegar únicamente la aplicación; verificar que el procedimiento mantiene deshabilitada toda ejecución automática de migraciones sobre producción.
- [x] 7.5 Realizar smoke tests de solo lectura después del despliegue y verificar selección, listados, detalle, permisos y salud sin modificar ningún registro de producción.
