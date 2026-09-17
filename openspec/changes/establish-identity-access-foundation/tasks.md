## 1. Confirmaciones previas de seguridad (OIDC corporativo)

- [ ] 1.1 [DEFERRED] Documentar el proveedor OIDC aprobado, emisor, sujeto, claims y URLs por ambiente; pospuesto para fase futura de integración federada.
- [ ] 1.2 [DEFERRED] Acordar aprovisionamiento previo frente a just-in-time, autoridad de altas y elevaciones; pospuesto para fase futura OIDC.
- [ ] 1.3 [DEFERRED] Aprobar vigencias y retención corporativa de usuarios y asignaciones externas; pospuesto para fase futura.
- [ ] 1.4 [DEFERRED] Cerrar la nomenclatura física definitiva de tablas IAM en producción; la estructura local de desarrollo está verificada.

## 2. Fundacion de la solucion y modulo

- [x] 2.1 Crear la solucion .NET 10, los proyectos iniciales de ASP.NET Core MVC y pruebas, y verificar `dotnet build --configuration Release`.
- [x] 2.2 Crear el limite modular de Identidad y Acceso con contratos publicos y dependencias internas controladas, y verificar mediante pruebas arquitectonicas que otros modulos no accedan a su persistencia directamente.
- [x] 2.3 Configurar opciones tipadas y validacion de inicio para OIDC, Identity, cookies, lockout y bootstrap, y verificar que la aplicacion falle de forma segura o omita solo el bootstrap ante configuracion incompleta sin mostrar secretos.
- [x] 2.4 Establecer User Secrets y variables de entorno sanitizadas, incluido el mapeo de `LANDSCAPE_TSI_BOOTSTRAP_ADMIN_PASSWORD`, y verificar que ninguna credencial o token aparezca en archivos rastreados ni en salida de pruebas.

## 3. Modelo de datos y migraciones no productivas

- [x] 3.1 Modelar `IamUsuario` compatible con ASP.NET Core Identity, `IamUsuarioLoginExterno`, `IamRol`, `IamPermiso`, `IamUsuarioRol`, `IamRolPermiso`, `IamUsuarioOrganizacion`, `IamAccesoEmergencia` e `IamEventoAuditoriaAutorizacion`, y verificar sus invariantes mediante pruebas unitarias.
- [x] 3.2 Configurar mapeos EF Core, claves, unicidad, vigencias, indices de FK y la referencia a `TEmpresaSubsidiaria`, y verificar el modelo generado sin modificar tablas funcionales existentes.
- [x] 3.3 Generar una migracion EF Core revisable que solo cree estructuras aprobadas, y verificar el script SQL resultante mediante revision automatizada para detectar operaciones destructivas o cambios no previstos.
- [ ] 3.4 [DEFERRED] Aplicar y revertir la migración exclusivamente en `db-landscape-tsi-dev-v2` o copia sanitizada; la conexión utiliza User Secrets (`ConnectionStrings:LandscapeTsiDb` y `DatabaseSafety:ExpectedDatabaseName=db-landscape-tsi-dev-v2`). Pospuesto para la fase futura de migración física IAM; producción `db-landscape-tsi` estrictamente bloqueada.
- [ ] 3.5 [DEFERRED] Implementar el sembrado idempotente de permisos para roles no administrativos. Los cuatro roles y permisos administrativos iniciales están sembrados; la matriz completa de roles no administrativos queda pospuesta para la fase futura.
- [x] 3.6 Mantener la ejecucion contra produccion fuera de la automatizacion del cambio, y verificar que ninguna tarea, prueba o pipeline contenga DDL, DML o migracion automatica hacia la base en vivo.

## 4. Autenticacion y sesiones

- [ ] 4.1 [DEFERRED] Integrar el desafío y retorno OIDC federado de ASP.NET Core; pospuesto para fase futura de integración corporativa. La autenticación local con ASP.NET Core Identity permanece 100% implementada y operativa.
- [x] 4.2 Integrar ASP.NET Core Identity para login local con hashing, politica configurable, lockout y proteccion de fuerza bruta, y verificar exito, credenciales invalidas, bloqueo y ausencia de texto plano.
- [x] 4.3 Implementar vinculacion unica por emisor-sujeto y usuario normalizado hacia una identidad interna comun, y verificar que correo o nombre visible no vinculen una identidad diferente ni existan roles paralelos.
- [x] 4.4 Implementar la pantalla dual Material Design 3 con acceso corporativo y formulario local, y verificar responsive, teclado, foco, mensajes genericos y WCAG 2.2 AA.
- [x] 4.5 Implementar cierre local y federado, expiracion y reevaluacion de cuentas suspendidas, y verificar que una sesion invalidada no ejecute acciones protegidas.
- [x] 4.6 Configurar cookies seguras, proteccion antifalsificacion y manejo seguro de errores, y verificar atributos de cookie y escenarios CSRF mediante pruebas de integracion.
- [x] 4.7 Verificar que contrasenas, hashes, secretos y tokens no se incluyan en logs, auditoria o mensajes, usando pruebas y exploracion automatizada de salidas.

## 5. Usuarios, roles y permisos

- [x] 5.1 Implementar casos de uso para alta, consulta, suspension y reactivacion de usuarios, y verificar estado, vigencia, denegacion predeterminada e historial mediante pruebas.
- [ ] 5.2 [DEFERRED] Implementar consulta y administración de matriz completa de roles y permisos no administrativos; pospuesto para fase futura IAM.
- [x] 5.3 Implementar asignacion y revocacion de roles con solicitante, aprobador, ejecutor, justificacion y vigencia, y verificar que el beneficiario no pueda aprobar su propia elevacion.
- [x] 5.4 Implementar invalidacion o reevaluacion de permisos efectivos despues de un cambio, y verificar que una sesion existente no conserve acceso revocado.
- [x] 5.5 Implementar bootstrap idempotente de `jean` y `administrador` desde User Secrets o `LANDSCAPE_TSI_BOOTSTRAP_ADMIN_PASSWORD`, y verificar creacion, rol `Administrador del Sistema`, repeticion sin duplicados, no sobrescritura de contrasena y proteccion de usuarios manuales.
- [x] 5.6 Restringir bootstrap a Development o entorno inicial expresamente autorizado, y verificar secreto ausente, produccion sin habilitacion y mensaje administrativo seguro sin contrasena predeterminada.

## 6. Alcance y autorizacion por politicas

- [x] 6.1 Implementar asignaciones por subsidiaria y alcance corporativo explicito, y verificar accesos permitidos y denegados entre al menos dos subsidiarias.
- [x] 6.2 Implementar el calculo de permisos efectivos combinando roles, vigencias y alcance, y verificar denegacion cuando falte cualquiera de las condiciones.
- [x] 6.3 Implementar politicas de propiedad, asignacion de caso y estado del flujo mediante contratos extensibles, y verificar decisiones positivas y negativas con recursos simulados.
- [x] 6.4 Implementar reglas de separacion para autoaprobacion, autovalidacion, excepcion propia y autoelevacion, y verificar que cada combinacion incompatible sea denegada y auditada.
- [x] 6.5 Implementar acceso `BREAK_GLASS` con incidente, justificacion, aprobador, alcance y vencimiento, y verificar expiracion automatica, alerta y ausencia de autoridad empresarial permanente.
- [x] 6.6 Aplicar autorizacion del lado servidor a todos los endpoints protegidos creados por este cambio, y verificar que solicitudes HTTP manipuladas no eludan controles de interfaz.

## 7. Auditoria de autorizacion

- [ ] 7.1 [DEFERRED] Implementar el registro append-only extendido para eventos federados OIDC. La administración de alcance corporativo/subsidiario, login local, lockout y decisiones privilegiadas ya registran auditoría inmutable (`OrganizationScopeAssigned`, etc.).
- [x] 7.2 Implementar auditoria de login local/OAuth, bootstrap, decisiones de alto impacto, denegaciones e intentos privilegiados, y verificar fecha, mecanismo, resultado y ausencia de contrasenas, hashes, secretos o tokens completos.
- [x] 7.3 Garantizar atomicidad entre mutaciones empresariales y sus eventos de auditoria cuando corresponda, y verificar rollback conjunto ante un fallo inducido.
- [x] 7.4 Implementar consulta acotada por `Audit.View` y `AUDIT_SCOPE`, y verificar denegacion sin filtracion para organizaciones no autorizadas.
- [x] 7.5 Impedir edicion y eliminacion de eventos desde la aplicacion, y verificar que no existan comandos, endpoints ni permisos ordinarios capaces de mutarlos.

## 8. Interfaz de administracion y acceso

- [x] 8.1 Crear experiencias de inicio, cierre y acceso denegado con Material Design 3, y verificar navegacion por teclado, foco visible, contraste y mensajes comprensibles bajo WCAG 2.2 AA.
- [x] 8.2 Crear pantallas adaptables de usuarios, roles, permisos, alcances y vigencias, y verificar su comportamiento en escritorio, tableta y movil.
- [x] 8.3 Mostrar acciones y navegacion segun capacidades efectivas sin usar la interfaz como frontera de seguridad, y verificar que cada control visible corresponda con la decision del servidor.
- [x] 8.4 Crear una consulta adaptable de auditoria con filtros autorizados, y verificar lectura mediante tecnologia de asistencia y ausencia de comunicacion basada solo en color.
- [ ] 8.5 [DEFERRED] Proporcionar confirmaciones reforzadas adicionales para elevaciones temporales federadas; pospuesto para fase futura.

## 9. Pruebas de seguridad y calidad

- [x] 9.1 Crear pruebas unitarias de permisos, vigencias, alcance y segregación, y verificar cobertura de requisitos y escenarios IAM locales implementados (`EffectiveAccessServiceTests`, `SeparationOfDutiesEvaluatorTests`).
- [x] 9.2 Crear pruebas de integración de autenticación local, lockout, expiración, sesiones y seguridad de credenciales (`LoginPageTests`, `LocalUserAdministrationTests`); OIDC federado queda pospuesto.
- [x] 9.3 Crear pruebas negativas de acceso horizontal entre subsidiarias y acceso vertical entre roles sin filtración de recursos no autorizados.
- [x] 9.4 Ejecutar pruebas de concurrencia y atomicidad sobre asignaciones y decisiones auditadas, verificando consistencia append-only (`AuditAtomicityTests`, `AuditRestoreSqlTests`).
- [x] 9.5 Ejecutar análisis de dependencias, secretos y código, verificando ausencia de credenciales en logs, salidas y repositorio (`SensitiveOutputTests`, `ProductionDatabaseSafetyTests`).
- [x] 9.6 Ejecutar pruebas automatizadas de accesibilidad y revisión de flujos críticos de autenticación y administración bajo WCAG 2.2 AA.
- [x] 9.7 Ejecutar `dotnet format`, `dotnet build --configuration Release` y `dotnet test --configuration Release`, y verificar que todos finalicen correctamente sin advertencias nuevas no justificadas.

## 10. Preparacion operativa sin despliegue productivo

- [ ] 10.1 [DEFERRED] Documentar matriz final de roles-permisos corporativa federada; pospuesto para fase futura OIDC.
- [x] 10.2 Documentar configuracion por ambiente, `dotnet user-secrets set "BootstrapAdmin:Password" "<secreto>"`, variable de entorno, rotacion, monitoreo y alertas sin valores reales, y verificar el procedimiento en un entorno no productivo.
- [ ] 10.3 [DEFERRED] Preparar scripts de migración y rollback para revisión operativa OIDC; pospuesto para fase futura.
- [ ] 10.4 [DEFERRED] Realizar revisión final de seguridad previa a despliegue productivo OIDC.
- [x] 10.5 Actualizar la utilidad `tools/Landscape.Tsi.PasswordReset` con la allowlist Development/Staging/Production, metadatos sanitizados, confirmacion reforzada y rechazo de Production sin base autorizada; verificar que no acepte bases arbitrarias ni exponga credenciales.
- [ ] 10.6 [DEFERRED] Verificación del reset excepcional en base autorizada productiva; producción permanece bloqueada sin base autorizada en la política.
