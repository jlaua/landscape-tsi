## Context

La base de produccion contiene 25 tablas funcionales en `dbo`, pero no posee un modelo de usuarios de aplicacion, roles funcionales, permisos, alcance organizacional, politicas de seguridad ni auditoria. Los tres usuarios SQL observados son identidades tecnicas y no deben convertirse en actores del negocio. Vease `proposal.md` para la motivacion y `specs/identity/*` para los contratos de comportamiento.

El fundamento atraviesa todos los modulos del monolito, debe integrarse con `TEmpresaSubsidiaria` sin redisenarla y debe permitir incorporar despues el workflow de adopcion. Ningun paso de este cambio autoriza DDL, DML o migraciones sobre produccion.

## Goals / Non-Goals

**Goals:**

- Establecer un limite modular de Identidad y Acceso con contratos consumibles por los demas modulos.
- Separar autenticacion federada, perfiles locales, permisos, alcance y evaluacion de politicas.
- Aplicar denegacion predeterminada, minimo privilegio y segregacion de funciones.
- Proponer persistencia compatible con SQL Server y con las tablas existentes.
- Generar auditoria inmutable para decisiones y cambios de acceso.
- Permitir interfaces Material Design 3, adaptables y WCAG 2.2 AA.

**Non-Goals:**

- Almacenar o validar contrasenas locales.
- Elegir de forma irreversible un proveedor corporativo especifico.
- Implementar el workflow completo de adopcion en este cambio.
- Redisenar las tablas actuales de catalogo, tecnologia u organizacion.
- Ejecutar migraciones o cambios sobre produccion.
- Reemplazar la seguridad tecnica propia de SQL Server.

## Decisions

### 1. Autenticacion federada OIDC y perfil local minimo

ASP.NET Core delegara la autenticacion a un proveedor OpenID Connect configurable. La identidad local se vinculara mediante la pareja estable emisor-sujeto; correo y nombre seran atributos descriptivos, no claves de identidad. El perfil local determinara habilitacion, roles y alcances.

**Razon:** evita administrar secretos de usuario y desacopla la autorizacion empresarial del proveedor elegido.

**Alternativas consideradas:** ASP.NET Core Identity con contrasenas locales se descarta por aumentar superficie de riesgo; usar solamente claims del proveedor se descarta porque no representa asignaciones por subsidiaria, vigencias ni historia local con suficiente control.

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
| `IamRol` | Paquete funcional de permisos | N:M con Usuario y Permiso |
| `IamPermiso` | Capacidad atomica estable | Codigo unico |
| `IamUsuarioRol` | Asignacion de rol con vigencia | Usuario N:M Rol; actor/aprobacion |
| `IamRolPermiso` | Contenido de cada rol | Rol N:M Permiso; par unico |
| `IamUsuarioOrganizacion` | Alcance por subsidiaria | Usuario N:M `TEmpresaSubsidiaria`; vigencia |
| `IamAccesoEmergencia` | Elevacion temporal controlada | Usuario, alcance, incidente, aprobador y vencimiento |
| `IamEventoAuditoriaAutorizacion` | Evento append-only | Actor, permiso, recurso, organizacion, decision y correlacion |

Restricciones previstas: claves primarias, indices en todas las FK, unicidad para emisor-sujeto y codigo de permiso, pares unicos en tablas puente, vigencias coherentes y FK hacia `TEmpresaSubsidiaria`. No se propone cambiar columnas actuales. Las referencias a actores desde futuras solicitudes se definiran en el cambio del workflow, no aqui.

**Alternativa considerada:** usar tablas generales actuales se descarta porque ninguna representa estos conceptos y sobrecargarlas dañaria su significado.

### 7. Auditoria append-only separada de logs tecnicos

Las decisiones relevantes generaran un evento empresarial estructurado dentro de la misma unidad de trabajo cuando acompanen una mutacion. Denegaciones y lecturas sensibles se enviaran de forma confiable sin incluir tokens ni secretos. No existira permiso de aplicacion para editar eventos.

**Razon:** los logs de aplicacion no ofrecen por si solos semantica, retencion ni integridad empresarial.

### 8. Experiencia por capacidades efectivas

La navegacion, paneles, formularios, tablas, tarjetas y dialogos se compondran segun permisos efectivos. Una accion oculta no sustituira la autorizacion del servidor. La administracion mostrara rol, permiso, alcance, vigencia, solicitante y aprobador de forma textual y adaptable.

**Razon:** reduce errores operativos y cumple WCAG 2.2 AA sin comunicar estados solo mediante color.

## Risks / Trade-offs

- **[Claims corporativos insuficientes o inestables]** -> validar emisor, sujeto y claims contractuales en un entorno no productivo antes de integrar; mantener mapeo configurable.
- **[Bloqueo inicial por ausencia de administradores]** -> definir un procedimiento de bootstrap de una sola vez, auditable y revocable, separado de la operacion normal.
- **[Autoelevacion administrativa]** -> exigir aprobador distinto, cuenta administrativa separada y acceso de emergencia con caducidad.
- **[Consultas sin filtro organizacional]** -> obligar a usar contratos de consulta acotados y pruebas negativas entre subsidiarias; no depender solo de filtros visuales.
- **[Permisos excesivamente granulares]** -> mantener nomenclatura estable y catalogo gobernado; agrupar en roles sin perder atomicidad.
- **[Auditoria dentro de la misma base]** -> proteger acceso y considerar exportacion inmutable externa como evolucion posterior; la primera fase garantiza append-only desde la aplicacion.
- **[Migracion sobre una base permisiva]** -> crear estructuras aisladas y validar en copia o entorno controlado; no modificar produccion desde el flujo de desarrollo.

## Migration Plan

1. Validar proveedor OIDC, emisor, sujeto, claims y procedimiento de bootstrap fuera de produccion.
2. Aprobar por OpenSpec el modelo fisico definitivo, nombres, restricciones, indices, retencion y rollback.
3. Generar una migracion EF Core revisable que solo cree las nuevas estructuras y FK hacia `TEmpresaSubsidiaria`.
4. Probar la migracion y su reversa sobre una copia sanitizada o entorno no productivo.
5. Sembrar permisos atomicos y los cuatro roles iniciales mediante un proceso idempotente y auditable.
6. Crear el primer Administrador mediante el procedimiento de bootstrap aprobado y deshabilitar ese mecanismo.
7. Habilitar autenticacion y autorizacion por etapas, comenzando en modo de observacion para comparar decisiones esperadas.
8. Activar denegacion obligatoria despues de validar asignaciones, alcances y paneles.
9. Ejecutar en produccion solo mediante autorizacion operativa independiente, respaldo verificado y ventana aprobada.

**Rollback:** deshabilitar la integracion OIDC/politicas mediante configuracion controlada, restaurar el modo anterior de acceso solo si fue aprobado para contingencia y revertir las nuevas estructuras unicamente si no contienen auditoria o asignaciones que deban conservarse. Nunca ejecutar automaticamente rollback destructivo en produccion.

## Open Questions

- Proveedor OIDC corporativo, nombres definitivos de claims y politica de aprovisionamiento previo frente a just-in-time controlado.
- Periodos de vigencia y retencion corporativos para usuarios, asignaciones y auditoria.
- Autoridad concreta que aprueba altas y elevaciones del primer Administrador y de los Puntos de Gobierno.
- Destino externo futuro para conservar auditoria de alta garantia fuera de la base transaccional.
