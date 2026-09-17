# Plan de promoción controlada de `TFuncionalidad`

**Estado:** las seis candidatas están aprobadas conceptualmente para promoción;
solo lectura. No se generaron ni ejecutaron INSERT.

## 1. Casos pendientes de revisión

### DEV PK 1 — `Web Application Firewall`

| Aspecto | DEV PK 1 | RECONCILE PK 2409 |
|---|---|---|
| Nombre | Web Application Firewall | WAF / Web Application Firewall / Next Generation Firewall / API Protection |
| Descripción | Diferencia un WAF tradicional mediante análisis de comportamiento e IA; protege aplicaciones frente a amenazas web | Capacidades de WAF/web application y API protection (WAAP) en runtime |
| Capacidad padre | ID DEV 1: Web Application Firewall | ID RECONCILE 48: CWPP: Cloud Workload Protection Platform |
| Building Block | ID 1: WAAP | ID 23: Cloud Native Application Protection Platform (CNAPP) |
| Estado | CUBIERTO | PARCIALMENTE |
| Propósito | Detección/bloqueo adaptativo de amenazas web | Cobertura amplia WAF, API y plataforma cloud |
| Alcance | Aplicación web y comportamiento | WAF/API en runtime dentro de CNAPP |
| Términos relevantes | WAF, IA, análisis de comportamiento, ataques conocidos | WAF, API protection, WAAP, runtime, CNAPP |

**Recomendación:** `REVISIÓN MANUAL` (posiblemente `COMPLEMENTARIO`). Los
padres funcionales no coinciden y el alcance de RECONCILE es más amplio; no se
puede afirmar duplicidad solo por el término WAF.

### DEV PK 7 — `WAF - SSL/TLS Inspection`

| Aspecto | DEV PK 7 | RECONCILE PK 2730 |
|---|---|---|
| Nombre | WAF - SSL/TLS Inspection | TLS |
| Descripción | Inspección SSL/TLS para identificar y bloquear amenazas cifradas | TLS |
| Capacidad padre | ID DEV 1: Web Application Firewall | ID RECONCILE 158: Pendiente definir Capacidades del Building Block |
| Building Block | ID 1: WAAP | ID 40: Cifrado en transito |
| Estado | CUBIERTO | NO DEFINIDO |
| Propósito | Inspección específica de tráfico cifrado asociado a WAF | Capacidad general de TLS/cifrado en tránsito |
| Alcance | Subfunción de inspección y bloqueo | Concepto general de cifrado en tránsito |
| Términos relevantes | WAF, SSL/TLS, inspection, amenazas cifradas | TLS, cifrado en tránsito |

**Recomendación:** `ESPECIALIZACIÓN`, sujeta a revisión manual del propietario
funcional. DEV describe una capacidad específica que puede estar contenida en
el concepto TLS de RECONCILE, pero los padres y estados son distintos.

Ninguno de estos dos registros se promoverá todavía.

## 2. Mapeo explícito de las otras seis candidatas

El mapeo se realizó por nombre funcional normalizado, nunca por igualdad de FK
numérica.

| Funcionalidad DEV | Capacidad DEV | PK DEV | Capacidad RECONCILE | PK RECONCILE | Estado DEV | Estado RECONCILE |
|---|---|---:|---|---:|---|---|
| WAF - Attack Detection | Web Application Firewall | 1 | Web Application Firewall | 1 | CUBIERTO (1) | CUBIERTO (1) |
| WAF - Real-time Monitoring | Web Application Firewall | 1 | Web Application Firewall | 1 | CUBIERTO (1) | CUBIERTO (1) |
| WAF - Rule-Based Policies | Web Application Firewall | 1 | Web Application Firewall | 1 | CUBIERTO (1) | CUBIERTO (1) |
| WAF - Logging and Reporting | Web Application Firewall | 1 | Web Application Firewall | 1 | CUBIERTO (1) | CUBIERTO (1) |
| WAF- Rate Limiting | Web Application Firewall | 1 | Web Application Firewall | 1 | CUBIERTO (1) | CUBIERTO (1) |
| Integraciones con terceros | Web Application Firewall | 1 | Web Application Firewall | 1 | PARCIALMENTE (2) | PARCIALMENTE (2) |

Resultado: las seis capacidades y los seis estados tienen correspondencia
funcional y los PK de las tablas padre son iguales. Esto habilita una futura
promoción condicionada, pero no constituye aprobación de escritura.

## 3. Estrategia de PK IDENTITY

RECONCILE contiene actualmente PK `2267..2766` (500 filas) y no utiliza los
PK DEV `1..8`; no existe colisión actual. Aun así, las dos alternativas son:

### A. Conservar PK DEV con `IDENTITY_INSERT`

- preserva la trazabilidad directa DEV→RECONCILE;
- exige transacción, `IDENTITY_INSERT ON/OFF` y control de concurrencia;
- acopla el canónico a una numeración histórica de desarrollo;
- debe documentar cada PK y validar futuras migraciones EF.

### B. Insertar con PK generado por RECONCILE

- mantiene la semántica de IDENTITY canónica;
- evita depender de `IDENTITY_INSERT`;
- requiere una tabla de mapeo auditada `PK DEV -> PK RECONCILE`;
- cambia la referencia técnica, pero no el significado funcional.

**Decisión recibida:** opción **B**, generar nuevos PK canónicos en RECONCILE y
conservar un mapeo de trazabilidad. No se ejecuta todavía.

La trazabilidad deberá conservar, como mínimo: `Tabla`, `PKDev`,
`PKReconcile`, fecha UTC, origen (`db-landscape-tsi-dev`), actor/proceso y
resultado. No se usará `IDENTITY_INSERT`.

## 4. Mapeo EF Core de la tabla puente

La revisión del código actual muestra que `IdentityDbContext` solo tiene
entidades funcionales explícitas para `TmDominio`; no existe una entidad ni un
`DbSet` para `TBuildingBlockVsTTecnologiaTSI`, ni una configuración
`HasKey(...)`, `HasIndex(...).IsUnique()` o `UsingEntity(...)` para esa tabla.

Por tanto, actualmente EF Core **no está mapeando** la tabla puente. No es
posible confirmar todavía que una PK compuesta sea compatible con el modelo
actual. Primero debe diseñarse la entidad/configuración EF y comprobar sus
relaciones y migraciones, sin aplicar cambios a la base.

## 5. Tabla puente

Se confirma conceptualmente la unicidad de
`(idBuildingBlock,idTecnologiaTSI)`: no hay duplicados, nulos ni índices
actuales en RECONCILE o DEV. La recomendación es una PK compuesta si el modelo
EF Core actual puede mapearla sin romper relaciones. Antes de implementarla
se debe revisar el modelo EF, migraciones y consumidores de la tabla. No se
ejecutará `ALTER`, migración ni creación de índice en esta fase.

## 6. Próximas decisiones requeridas

1. Resolver manualmente DEV PK 1 y 7 (`REVISIÓN MANUAL` / confirmar
   `COMPLEMENTARIO` y `ESPECIALIZACIÓN`).
2. Ejecutar, después de una aprobación específica de escritura, la promoción
   de los seis registros mapeados con PK nuevas y trazabilidad.
3. Diseñar y validar el mapeo EF Core de la tabla puente.
4. Decidir posteriormente entre PK compuesta y `UNIQUE`; no aplicar todavía.
