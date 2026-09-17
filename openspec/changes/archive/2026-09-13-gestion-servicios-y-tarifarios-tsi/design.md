## Context

Landscape TSI gestiona actualmente los procesos de adopción tecnológica, estándares corporativos y contratos de licenciamiento/drivers de hardware (TContratoTecnologia, TDriver). No existían tablas para dimensionar servicios de mano de obra. Véase proposal.md para la justificación.

Las cuatro especificaciones e imágenes de negocio recibidas definen requerimientos diferenciados:
- Proyectos por horas (Implementación y Migración) con 4 niveles de complejidad.
- Servicios continuos de Operación con tres niveles de soporte (N1, N2, N3), catálogo de actividades detallado y matriz de tarifarios cruzada por modalidad (Pay-Per-Use, Mensual 8x5 con 176h, Mensual 24x7 con 720h), seniority (Junior, Senior) y locación (Remoto, Presencial).

## Goals / Non-Goals

**Goals:**
- Extender el esquema relacional de SQL Server con 5 tablas aditivas (TTipoServicio, TServicioTecnologia, TTarifarioProyectoHoras, TActividadNivelSoporte, TTarifarioOperacion).
- Permitir tanto la presupuestación de servicios corporativos en evaluaciones de adopción (/evaluaciones/{id}) como la gestión de servicios directos en tecnologías de subsidiarias.
- Calcular subtotales y costos totales en tiempo real en servidor y cliente sin desincronización aritmética.
- Proteger el acceso por alcance organizacional (subsidiaria) y registrar eventos de auditoría inmutables en mutaciones.

**Non-Goals:**
- No se implementa facturación electrónica ni integración ERP contable (SAP/Oracle).
- No se reemplazan los drivers de infraestructura (TDriver), los cuales coexisten para dimensionar servidores, licencias o nodos.
- No se altera la estructura de tablas existentes ni se ejecutan migraciones destructivas.

## Decisions

### 1. Separación de tablas de tarifario: TTarifarioProyectoHoras vs TTarifarioOperacion
- **Decisión:** Crear dos tablas hijas especializadas en lugar de una tabla genérica con múltiples columnas nulas.
- **Razón:** Implementación/Migración operan conceptualmente por rangos de horas y complejidad de proyecto, mientras que Operación es un modelo de capacidad y niveles de servicio (SLA/Modalidad/Horas mensuales/Locación/Expertise). La separación mantiene integridad referencial limpia, constraints no nulos y consultas EF Core fuertemente tipadas.
- **Alternativa descartada:** Una sola tabla TTarifario con 15 columnas opcionales y lógica condicional compleja en el discriminador.

### 2. Vinculación polivalente pero tipada en TServicioTecnologia
- **Decisión:** Incorporar pares de claves foráneas opcionales:
  - Ámbito Corporativo: (idTecnologiaTSI, idProcesoAdopcionTSI)
  - Ámbito Subsidiaria: (idTecnologiaTSIimplementadaSubsidiaria, idEmpresaSubsidiaria)
  Con un check constraint CK_TServicio_Target que valida que al menos un ámbito esté definido.
- **Razón:** Permite consultar eficientemente los servicios desde la ficha de evaluación del proceso de adopción, así como desde el inventario local de la empresa.

### 3. Catálogo Normalizado de Actividades TActividadNivelSoporte
- **Decisión:** Almacenar las actividades de N1 (8), N2 (12) y N3 (5) en una tabla de catálogo precargada mediante script.
- **Razón:** Facilita desplegar checkboxes o listas de verificación en la UI para definir el alcance del servicio operativo contratado y asegura consistencia con la especificación visual provista.

## Risks / Trade-offs

- **[Riesgo]** Desviaciones en el cálculo de horas mensuales en años bisiestos o meses con diferente cantidad de días laborables.
  - **Mitigación:** Se fijan las constantes estandarizadas de negocio solicitadas: promedio de 176 horas para modalidad 8x5 y 720 horas para 24x7.
- **[Riesgo]** Registro de servicios corporativos sin vendor adjudicado en etapas tempranas de exploración.
  - **Mitigación:** El campo idVendor se define como nullable en TServicioTecnologia, permitiendo ingresar un nombre provisional en 
ombreProveedorServicio.

## Migration Plan

1. Ejecutar scripts/database/evolve-service-schema.sql validando previamente DB_NAME() = 'db-landscape-tsi-dev-v2'.
2. Sembrar datos maestros de TTipoServicio y actividades en TActividadNivelSoporte.
3. Validar esquema mediante pruebas de integración automatizadas (AdoptionProcessSchemaIntegrationTests).
