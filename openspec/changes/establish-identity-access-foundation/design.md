## Context

La base de produccion contiene 25 tablas funcionales en `dbo`, pero no posee un modelo de usuarios de aplicacion, roles funcionales, permisos, alcance organizacional, politicas de seguridad ni auditoria. Los tres usuarios SQL observados son identidades tecnicas y no deben convertirse en actores del negocio. Vease `proposal.md` para la motivacion y `specs/identity/*` para los contratos de comportamiento.

El fundamento atraviesa todos los modulos del monolito, debe integrarse con `TEmpresaSubsidiaria` sin redisenarla y debe permitir incorporar despues el workflow de adopcion. Ningun paso de este cambio autoriza DDL, DML o migraciones sobre produccion.

## Goals / Non-Goals

**Goals:**

- Establecer un limite modular de Identidad y Acceso con contratos consumibles por los demas modulos.
- Unificar autenticacion corporativa y local, perfiles internos, permisos, alcance y evaluacion de politicas.
- Aplicar denegacion predeterminada, minimo privilegio y segregacion de funciones.
- Proponer persistencia compatible con SQL Server y con las tablas existentes.
- Generar auditoria inmutable para decisiones y cambios de acceso.
- Permitir interfaces Material Design 3, adaptables y WCAG 2.2 AA.

**Non-Goals:**

- Almacenar contrasenas, secretos de bootstrap o tokens en texto claro.
- Elegir de forma irreversible un proveedor corporativo especifico.
- Implementar el workflow completo de adopcion en este cambio.
- Redisenar las tablas actuales de catalogo, tecnologia u organizacion.
- Ejecutar migraciones o cambios sobre produccion.
- Reemplazar la seguridad tecnica propia de SQL Server.

## Decisions

### 1. Autenticacion dual con identidad interna comun

ASP.NET Core admitira OAuth/OpenID Connect corporativo y credenciales locales mediante ASP.NET Core Identity. Ambos mecanismos resolveran un unico `IamUsuario`; la pareja estable emisor-sujeto identificara el login externo y el nombre normalizado identificara la cuenta local. Correo y nombre visible no vincularan identidades por si solos. El perfil interno determinara habilitacion, roles y alcances.

ASP.NET Core Identity sera responsable de hashing, security stamp, bloqueo, contador de fallos y validacion de contrasenas. No se usara su catalogo de roles como fuente paralela: `IamRol`, `IamPermiso`, `IamUsuarioRol` y las politicas de Landscape TSI seguiran siendo la unica autoridad empresarial.

```text
OAuth/OIDC corporativo ----\
                            > IamUsuario -> IamUsuarioRol -> IamRol -> IamPermiso
ASP.NET Core Identity -----/
```

**Razon:** mantiene la experiencia corporativa y proporciona bootstrap/respaldo controlado sin duplicar autorizacion.

**Alternativas consideradas:** OIDC exclusivo no cubre el arranque solicitado; roles de ASP.NET Core Identity en paralelo se descartan por divergencia; hashing propio se descarta porque aumenta el riesgo criptografico.

### 2. Modulo de Identidad y Acceso en el monolito modular

El modulo sera propietario de autenticacion integrada, usuarios, roles, permisos, asignaciones organizacionales, decisiones de politica y auditoria de autorizacion. Expondra contratos de consulta/evaluacion; otros modulos no consultaran directamente sus tablas.

**Razon:** centraliza reglas transversales sin convertirlas en dependencias circulares. Identidad puede referenciar el identificador de `TEmpresaSubsidiaria`, mientras Organizacion no depende de las tablas internas de Identidad.

**Alternativa considerada:** duplicar verificaciones por controlador o modulo se descarta por inconsistencia y riesgo de omision.

### 3. Roles como paquetes y permisos como unidad de decision

Los roles iniciales se sembraran como datos controlados, pero las politicas exigiran identificadores atomicos como `Technology.View`, `Adoption.Submit` o `Governance.Approve`. La asignacion de rol tendra vigencia y no concedera alcance organizacional implicitamente.

**Razon:** permite evolucionar la matriz sin codificar toda autorizacion alrededor de cuatro nombres de rol.

**Alternativas consideradas:** comprobaciones exclusivas de rol se descartan; permisos individuales directos a usuarios se reservaran para excepciones futuras porque complican revision y revocacion.

### 4. Motor de politicas contextual con denegacion predeterminada

La evaluacion seguira este orden: identidad activa, permiso, alcance organizacional, propiedad/asignacion, estado del recurso, segregacion de funciones y requisitos adicionales. Toda condicion debe cumplirse. Las interfaces usaran la misma consulta de capacidades para presentar acciones, pero el servidor reevaluara cada comando.

**Razon:** evita que una autorizacion nominal ignore subsidiaria, estado o conflicto de interes.

**Alternativa considerada:** seguridad solo en la interfaz se descarta porque el cliente no es una frontera confiable.

### 5. Separacion de funciones basada en historial del caso

Las reglas no dependeran solo de combinaciones globales de rol. Cuando exista una solicitud de adopcion, se evaluaran actores historicos: creador, remitente, evaluadores, cargador de evidencia, recomendador de excepcion y aprobador. Administrador y roles empresariales deberan utilizar cuentas o contextos separados.

**Razon:** una persona puede cumplir mas de una funcion organizacional sin estar autorizada para ejercer ambos lados de una misma decision.

### 6. Modelo de datos propuesto y aislado

Se proponen tablas nuevas con prefijo de modulo o esquema logico coherente, sujeto a convencion fisica aprobada:

| Entidad propuesta | Finalidad | Relaciones principales |
|---|---|---|
| `IamUsuario` | Perfil local vinculado a emisor/sujeto | Identidad externa unica; estado y vigencia |
| `IamUsuarioLoginExterno` | Vinculo OAuth/OIDC | Usuario, proveedor, emisor y sujeto unicos |
| `IamRol` | Paquete funcional de permisos | N:M con Usuario y Permiso |
| `IamPermiso` | Capacidad atomica estable | Codigo unico |
| `IamUsuarioRol` | Asignacion de rol con vigencia | Usuario N:M Rol; actor/aprobacion |
| `IamRolPermiso` | Contenido de cada rol | Rol N:M Permiso; par unico |
| `IamUsuarioOrganizacion` | Alcance por subsidiaria | Usuario N:M `TEmpresaSubsidiaria`; vigencia |
| `IamAccesoEmergencia` | Elevacion temporal controlada | Usuario, alcance, incidente, aprobador y vencimiento |
| `IamEventoAuditoriaAutorizacion` | Evento append-only | Actor, permiso, recurso, organizacion, decision y correlacion |

`IamUsuario` incorporara los campos compatibles con ASP.NET Core Identity necesarios para usuario normalizado, hash, stamps, lockout y contador de fallos. La credencial sera opcional para identidades exclusivamente corporativas. Restricciones previstas: claves primarias, indices en todas las FK, unicidad para usuario normalizado, emisor-sujeto y codigo de permiso, pares unicos en tablas puente, vigencias coherentes y FK hacia `TEmpresaSubsidiaria`. No se propone cambiar columnas funcionales actuales. Las referencias a actores desde futuras solicitudes se definiran en el cambio del workflow, no aqui.

**Alternativa considerada:** usar tablas generales actuales se descarta porque ninguna representa estos conceptos y sobrecargarlas dañaria su significado.

### 7. Auditoria append-only separada de logs tecnicos

Las decisiones relevantes generaran un evento empresarial estructurado dentro de la misma unidad de trabajo cuando acompanen una mutacion. Denegaciones y lecturas sensibles se enviaran de forma confiable sin incluir tokens ni secretos. No existira permiso de aplicacion para editar eventos.

**Razon:** los logs de aplicacion no ofrecen por si solos semantica, retencion ni integridad empresarial.

### 8. Experiencia por capacidades efectivas

La navegacion, paneles, formularios, tablas, tarjetas y dialogos se compondran segun permisos efectivos. Una accion oculta no sustituira la autorizacion del servidor. La administracion mostrara rol, permiso, alcance, vigencia, solicitante y aprobador de forma textual y adaptable.

**Razon:** reduce errores operativos y cumple WCAG 2.2 AA sin comunicar estados solo mediante color.

### 9. Bootstrap administrativo idempotente y condicionado

Un inicializador controlado buscara `jean` y `administrador` por nombre normalizado. Solo se habilitara mediante configuracion explicita en Development o un entorno inicial autorizado y obtendra la contrasena desde `BootstrapAdmin:Password`, enlazable por User Secrets o `LANDSCAPE_TSI_BOOTSTRAP_ADMIN_PASSWORD`. No existira valor predeterminado.

Flujo:

```text
Inicio
  -> ambiente/configuracion autorizados?
     no -> omitir y auditar condicion segura
     si -> secreto presente?
        no -> omitir solo bootstrap y registrar instruccion administrativa
        si -> por cada usuario
           inexistente -> crear con Identity -> asignar rol -> auditar
           creado antes por bootstrap -> no cambiar contrasena; asegurar rol idempotente
           existente manual -> no cambiar ni elevar; advertir para revision
```

La marca de procedencia del bootstrap sera metadato interno auditable, no una inferencia basada solo en el nombre. La asignacion `Administrador del Sistema` concede los permisos aprobados de administracion de catalogos, usuarios, roles, consulta de auditoria y otras funciones administrativas; toda accion sigue su politica del servidor.

**Razon:** evita contrasenas conocidas, duplicados y elevaciones silenciosas de cuentas preexistentes.

**Alternativas consideradas:** seed EF con hash fijo, scripts SQL versionados y contrasena generada automaticamente se descartan porque exponen o vuelven irrecuperable el secreto.

### 10. Pantalla y auditoria de autenticacion

La pagina de acceso separara visual y semanticamente `Continuar con cuenta corporativa` del formulario Usuario/Contraseña/Ingresar. Usara Material Design 3, una columna en movil, orden de foco logico, etiquetas persistentes, resumen de errores accesible y mensajes que no permitan enumerar cuentas.

Los eventos registraran login exitoso/fallido, mecanismo `Local` u `OAuth`, identificador seguro, instante, resultado y correlacion. No incluiran contraseña, hash, secreto, cookie ni token completo. El logout invalida la sesion local y solicita cierre federado cuando aplique.

## Risks / Trade-offs

- **[Claims corporativos insuficientes o inestables]** -> validar emisor, sujeto y claims contractuales en un entorno no productivo antes de integrar; mantener mapeo configurable.
- **[Secreto bootstrap ausente o expuesto]** -> no generar valores predeterminados; omitir solo el bootstrap, usar User Secrets/variable de entorno y rotar cualquier valor divulgado.
- **[Cuenta local amplifica superficie de ataque]** -> lockout, politicas configurables, rate limiting, mensaje generico, auditoria y habilitacion controlada.
- **[Coincidencia con usuario manual]** -> no cambiar contrasena ni rol; exigir revision administrativa.
- **[Autoelevacion administrativa]** -> exigir aprobador distinto, cuenta administrativa separada y acceso de emergencia con caducidad.
- **[Consultas sin filtro organizacional]** -> obligar a usar contratos de consulta acotados y pruebas negativas entre subsidiarias; no depender solo de filtros visuales.
- **[Permisos excesivamente granulares]** -> mantener nomenclatura estable y catalogo gobernado; agrupar en roles sin perder atomicidad.
- **[Auditoria dentro de la misma base]** -> proteger acceso y considerar exportacion inmutable externa como evolucion posterior; la primera fase garantiza append-only desde la aplicacion.
- **[Migracion sobre una base permisiva]** -> crear estructuras aisladas y validar en copia o entorno controlado; no modificar produccion desde el flujo de desarrollo.

## Migration Plan

1. Validar proveedor OIDC, emisor, sujeto, claims, politica local y procedimiento de bootstrap fuera de produccion.
2. Aprobar por OpenSpec el modelo fisico definitivo, nombres, restricciones, indices, retencion y rollback.
3. Generar una migracion EF Core revisable que solo cree las nuevas estructuras Identity/IAM y FK hacia `TEmpresaSubsidiaria`.
4. Probar la migracion y su reversa exclusivamente sobre `db-landscape-tsi-dev`, una copia sanitizada o una base desechable; nunca sobre `db-landscape-tsi`.
5. Sembrar permisos atomicos y los cuatro roles iniciales mediante un proceso idempotente y auditable.
6. Configurar un secreto nuevo fuera de Git y ejecutar el bootstrap de `jean` y `administrador` solo en el entorno autorizado; verificar idempotencia y despues deshabilitarlo cuando corresponda.
7. Habilitar autenticacion y autorizacion por etapas, comenzando en modo de observacion para comparar decisiones esperadas.
8. Activar denegacion obligatoria despues de validar asignaciones, alcances y paneles.
9. Ejecutar en produccion solo mediante autorizacion operativa independiente, respaldo verificado y ventana aprobada.

**Rollback:** deshabilitar por separado login local, bootstrap o integracion OIDC mediante configuracion controlada, conservar el modelo comun de autorizacion y revertir estructuras solo en desarrollo si no contienen auditoria o asignaciones que deban conservarse. Nunca ejecutar automaticamente rollback destructivo en produccion.

## Open Questions

- Proveedor OIDC corporativo, nombres definitivos de claims y politica de aprovisionamiento previo frente a just-in-time controlado.
- Periodos de vigencia y retencion corporativos para usuarios, asignaciones y auditoria.
- Autoridad concreta que aprueba altas y elevaciones del primer Administrador y de los Puntos de Gobierno.
- Destino externo futuro para conservar auditoria de alta garantia fuera de la base transaccional.
