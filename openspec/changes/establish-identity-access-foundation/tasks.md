## 1. Confirmaciones previas de seguridad

- [ ] 1.1 Documentar el proveedor OIDC aprobado, emisor, sujeto, claims y URLs por ambiente, y verificar el contrato con una autenticacion controlada fuera de produccion sin registrar tokens.
- [ ] 1.2 Acordar aprovisionamiento previo frente a just-in-time, autoridad de altas y elevaciones, y verificar que la decision tenga responsables y criterios de aceptacion registrados.
- [ ] 1.3 Aprobar vigencias y retencion de usuarios, asignaciones, accesos de emergencia y auditoria, y verificar que cada tipo de dato tenga una regla definida.
- [ ] 1.4 Cerrar la nomenclatura fisica de las nuevas tablas y el procedimiento de bootstrap, y verificar que una revision independiente confirme compatibilidad con la base existente.

## 2. Fundacion de la solucion y modulo

- [ ] 2.1 Crear la solucion .NET 10, los proyectos iniciales de ASP.NET Core MVC y pruebas, y verificar `dotnet build --configuration Release`.
- [ ] 2.2 Crear el limite modular de Identidad y Acceso con contratos publicos y dependencias internas controladas, y verificar mediante pruebas arquitectonicas que otros modulos no accedan a su persistencia directamente.
- [ ] 2.3 Configurar opciones tipadas y validacion de inicio para OIDC, cookies y seguridad, y verificar que la aplicacion falle de forma segura ante configuracion incompleta sin mostrar secretos.
- [ ] 2.4 Establecer gestion local de secretos y plantillas sanitizadas, y verificar que ninguna credencial o token aparezca en archivos rastreados ni en salida de pruebas.

## 3. Modelo de datos y migraciones no productivas

- [ ] 3.1 Modelar `IamUsuario`, `IamRol`, `IamPermiso`, `IamUsuarioRol`, `IamRolPermiso`, `IamUsuarioOrganizacion`, `IamAccesoEmergencia` e `IamEventoAuditoriaAutorizacion`, y verificar sus invariantes mediante pruebas unitarias.
- [ ] 3.2 Configurar mapeos EF Core, claves, unicidad, vigencias, indices de FK y la referencia a `TEmpresaSubsidiaria`, y verificar el modelo generado sin modificar tablas funcionales existentes.
- [ ] 3.3 Generar una migracion EF Core revisable que solo cree estructuras aprobadas, y verificar el script SQL resultante mediante revision automatizada para detectar operaciones destructivas o cambios no previstos.
- [ ] 3.4 Aplicar y revertir la migracion exclusivamente en una base de prueba desechable o copia sanitizada, y verificar que ambos procesos terminan correctamente y preservan las tablas existentes.
- [ ] 3.5 Implementar el sembrado idempotente de permisos y de los cuatro roles iniciales, y verificar que ejecuciones repetidas no creen duplicados ni cambien asignaciones de usuarios.
- [ ] 3.6 Mantener la ejecucion contra produccion fuera de la automatizacion del cambio, y verificar que ninguna tarea, prueba o pipeline contenga DDL, DML o migracion automatica hacia la base en vivo.

## 4. Autenticacion y sesiones

- [ ] 4.1 Integrar el desafio y retorno OIDC de ASP.NET Core, y verificar inicio satisfactorio, rechazo del proveedor, callback invalido y proteccion de correlacion.
- [ ] 4.2 Implementar vinculacion unica por emisor-sujeto y control de usuario activo, y verificar que correo o nombre no puedan vincular una identidad diferente.
- [ ] 4.3 Implementar cierre local y federado, expiracion y reevaluacion de cuentas suspendidas, y verificar que una sesion invalidada no ejecute acciones protegidas.
- [ ] 4.4 Configurar cookies seguras, proteccion antifalsificacion y manejo seguro de errores, y verificar atributos de cookie y escenarios CSRF mediante pruebas de integracion.
- [ ] 4.5 Verificar que contrasenas y tokens no se persistan ni se incluyan en logs, auditoria o mensajes, usando pruebas y exploracion automatizada de salidas.

## 5. Usuarios, roles y permisos

- [ ] 5.1 Implementar casos de uso para alta, consulta, suspension y reactivacion de usuarios, y verificar estado, vigencia, denegacion predeterminada e historial mediante pruebas.
- [ ] 5.2 Implementar consulta y administracion controlada de roles y permisos, y verificar que cada rol inicial contenga exactamente los permisos aprobados por la matriz.
- [ ] 5.3 Implementar asignacion y revocacion de roles con solicitante, aprobador, ejecutor, justificacion y vigencia, y verificar que el beneficiario no pueda aprobar su propia elevacion.
- [ ] 5.4 Implementar invalidacion o reevaluacion de permisos efectivos despues de un cambio, y verificar que una sesion existente no conserve acceso revocado.
- [ ] 5.5 Implementar el bootstrap de un solo uso para el primer Administrador conforme a la decision aprobada, y verificar que quede auditado y no pueda reutilizarse.

## 6. Alcance y autorizacion por politicas

- [ ] 6.1 Implementar asignaciones por subsidiaria y alcance corporativo explicito, y verificar accesos permitidos y denegados entre al menos dos subsidiarias.
- [ ] 6.2 Implementar el calculo de permisos efectivos combinando roles, vigencias y alcance, y verificar denegacion cuando falte cualquiera de las condiciones.
- [ ] 6.3 Implementar politicas de propiedad, asignacion de caso y estado del flujo mediante contratos extensibles, y verificar decisiones positivas y negativas con recursos simulados.
- [ ] 6.4 Implementar reglas de separacion para autoaprobacion, autovalidacion, excepcion propia y autoelevacion, y verificar que cada combinacion incompatible sea denegada y auditada.
- [ ] 6.5 Implementar acceso `BREAK_GLASS` con incidente, justificacion, aprobador, alcance y vencimiento, y verificar expiracion automatica, alerta y ausencia de autoridad empresarial permanente.
- [ ] 6.6 Aplicar autorizacion del lado servidor a todos los endpoints protegidos creados por este cambio, y verificar que solicitudes HTTP manipuladas no eludan controles de interfaz.

## 7. Auditoria de autorizacion

- [ ] 7.1 Implementar el registro append-only de cambios de usuario, rol, permiso, alcance y emergencia, y verificar actor, beneficiario, antes/despues, justificacion, aprobacion y correlacion.
- [ ] 7.2 Implementar auditoria de decisiones de alto impacto, denegaciones por segregacion e intentos privilegiados, y verificar que no se almacenen secretos ni tokens.
- [ ] 7.3 Garantizar atomicidad entre mutaciones empresariales y sus eventos de auditoria cuando corresponda, y verificar rollback conjunto ante un fallo inducido.
- [ ] 7.4 Implementar consulta acotada por `Audit.View` y `AUDIT_SCOPE`, y verificar denegacion sin filtracion para organizaciones no autorizadas.
- [ ] 7.5 Impedir edicion y eliminacion de eventos desde la aplicacion, y verificar que no existan comandos, endpoints ni permisos ordinarios capaces de mutarlos.

## 8. Interfaz de administracion y acceso

- [ ] 8.1 Crear experiencias de inicio, cierre y acceso denegado con Material Design 3, y verificar navegacion por teclado, foco visible, contraste y mensajes comprensibles bajo WCAG 2.2 AA.
- [ ] 8.2 Crear pantallas adaptables de usuarios, roles, permisos, alcances y vigencias, y verificar su comportamiento en escritorio, tableta y movil.
- [ ] 8.3 Mostrar acciones y navegacion segun capacidades efectivas sin usar la interfaz como frontera de seguridad, y verificar que cada control visible corresponda con la decision del servidor.
- [ ] 8.4 Crear una consulta adaptable de auditoria con filtros autorizados, y verificar lectura mediante tecnologia de asistencia y ausencia de comunicacion basada solo en color.
- [ ] 8.5 Proporcionar confirmaciones reforzadas para suspension, elevacion, revocacion y acceso de emergencia, y verificar que soliciten justificacion y expongan claramente el impacto.

## 9. Pruebas de seguridad y calidad

- [ ] 9.1 Crear pruebas unitarias de permisos, vigencias, alcance y segregacion, y verificar cobertura de cada requisito y escenario de `specs/identity`.
- [ ] 9.2 Crear pruebas de integracion de autenticacion y autorizacion con un proveedor OIDC simulado, y verificar sesiones validas, expiradas, manipuladas, usuarios inactivos y fallos del proveedor.
- [ ] 9.3 Crear pruebas negativas de acceso horizontal entre subsidiarias y acceso vertical entre roles, y verificar que no se revele existencia ni contenido de recursos denegados.
- [ ] 9.4 Ejecutar pruebas de concurrencia sobre asignaciones y decisiones auditadas, y verificar ausencia de duplicados, elevaciones perdidas o auditoria inconsistente.
- [ ] 9.5 Ejecutar analisis de dependencias, secretos y codigo, y verificar que no existan vulnerabilidades criticas conocidas ni credenciales incorporadas.
- [ ] 9.6 Ejecutar pruebas automatizadas de accesibilidad y revision manual de flujos criticos, y verificar conformidad WCAG 2.2 AA documentada.
- [ ] 9.7 Ejecutar `dotnet format`, `dotnet build --configuration Release` y `dotnet test --configuration Release`, y verificar que todos finalicen correctamente sin advertencias nuevas no justificadas.

## 10. Preparacion operativa sin despliegue productivo

- [ ] 10.1 Documentar matriz final de roles-permisos, procedimiento de altas, revocacion, emergencia y recuperacion, y verificar revision por Seguridad y Gobierno.
- [ ] 10.2 Documentar configuracion por ambiente, rotacion de secretos, monitoreo y alertas sin valores sensibles, y verificar el procedimiento en un entorno no productivo.
- [ ] 10.3 Preparar scripts de migracion y rollback para revision operativa sin ejecutarlos en produccion, y verificar correspondencia exacta con la migracion aprobada.
- [ ] 10.4 Realizar una revision final de seguridad, privacidad, segregacion, auditoria, accesibilidad y compatibilidad con la base existente, y verificar que los hallazgos bloqueantes esten cerrados antes de solicitar despliegue.
