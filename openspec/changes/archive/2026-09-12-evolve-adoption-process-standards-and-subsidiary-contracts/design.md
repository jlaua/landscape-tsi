# Diseño Técnico: Evolución del Proceso de Adopción TSI, Estándares Corporativos Históricos y Contratos de Subsidiarias

## Context

Landscape TSI opera como un monolito modular en ASP.NET Core (.NET 10) con Entity Framework Core sobre SQL Server (`db-landscape-tsi-dev-v2`). La jerarquía conceptual `Dominio -> Building Block -> Capacidad -> Funcionalidad` se encuentra implementada y agnóstica de marcas y subsidiarias.
Actualmente existen 26 tablas en el esquema. Para la tecnología estándar corporativa se cuenta con `TBuildingBlockVsTTecnologiaTSI` (asociación plana N:M), mientras que para las subsidiarias existe `TTecnologiaTSIimplementadaSubsidiaria` sin relación física a `TBuildingBlock`. Asimismo, `TDriver` y `TModeloDeOperacion` cuelgan directamente de `TTecnologiaTSIimplementadaSubsidiaria`, y no existe soporte de contratos con adendas (1:N) ni histórico de estándares corporativos.

Ver `proposal.md` para la motivación y objetivos de negocio.

## Goals / Non-Goals

**Goals:**
- Implementar la entidad de gobernanza `TProcesoAdopcionTSI`, vinculando Building Blocks y fases del catálogo autoritativo `TMEstadoAdopcionTSI`.
- Proveer soporte de histórico inmutable para estándares corporativos (`TEstandarTecnologiaHistorico`), registrando transiciones de `ACTIVO_VIGENTE` a `HISTORICO_REEMPLAZADO` con fecha de término y motivo del cambio.
- Mantener la relación con casos de uso (`TCasosDeUso`), fabricantes (`TVendor`), contacto del vendor (`TContactoVendor`) y partner canal (`TContactoPartner`).
- Mapear las subsidiarias convocadas al proceso (`TProcesoAdopcionEmpresa`) con su especialista técnico asignado (`TContactoEmpresaSubsidiaria`) y soporte de excepciones formales (`No Aplica`).
- Contextualizar `TTecnologiaTSIimplementadaSubsidiaria` vinculándola al Building Block y proceso sin alterar sus dependencias existentes (`TDriver` y `TModeloDeOperacion`).
- Incorporar la entidad de contratos y adendas `TContratoTecnologia` (1:N hacia tecnología implementada con autorreferencia para adendas).
- Extender `TDriver` para soportar unidad de medida, cantidad volumétrica, precio unitario y moneda, permitiendo múltiples drivers libres por tecnología.
- Vincular `TBuildingBlock` con `TMFamilia`.

**Non-Goals:**
- No se modificará el módulo de Identity/OIDC federado (continúa DEFERRED).
- No se aplicará DDL destructivo (no se eliminarán tablas ni columnas en producción o desarrollo).
- No se rediseñarán las 16 tablas de catálogos maestros administradas en `/Administration/MasterTables`.
- No se crearán migraciones automáticas sobre tablas excluidas en EF Core; se utilizarán scripts SQL gobernados y aprobados para `db-landscape-tsi-dev-v2`.

## Decisions

### 1. Reutilización Aditiva de `TTecnologiaTSIimplementadaSubsidiaria`
- **Decisión:** En lugar de crear una tabla paralela que obligue a migrar o reasignar las FKs de `TDriver` y `TModeloDeOperacion`, se extiende `TTecnologiaTSIimplementadaSubsidiaria` agregando columnas nullables (`idBuildingBlock`, `idProcesoAdopcionEmpresa`, `esTecnologiaPrimaria`).
- **Alternativa Descartada:** Crear una nueva tabla `TBuildingBlockEmpresaTecnologia` y migrar datos, lo cual habría roto las dependencias de `TDriver` y `TModeloDeOperacion` existentes.
- **Razón:** Preserva el 100% de la integridad de los datos actuales y simplifica la compatibilidad del modelo de operación.

### 2. Estándares Corporativos con Histórico Explícito
- **Decisión:** Crear `TEstandarTecnologiaHistorico` para registrar la asignación de tecnología corporativa con vigencias (`fechaInicioVigencia`, `fechaFinVigencia`), rol (`PRINCIPAL` vs `ALTERNATIVA`) y estado (`ACTIVO_VIGENTE`, `HISTORICO_REEMPLAZADO`). Al registrar un nuevo estándar principal, una transacción cierra el anterior fijando su fecha fin y crea el nuevo como vigente.
- **Alternativa Descartada:** Sobreescribir el registro existente o manejar solo una bandera booleana `esActivo` sin vigencias ni motivo.
- **Razón:** Requisito explícito del Product Owner para conservar la auditoría histórica de qué tecnología fue el estándar oficial en cada periodo temporal.

### 3. Modelo Jerárquico de Contratos y Adendas (1:N)
- **Decisión:** Implementar `TContratoTecnologia` asociada a `TTecnologiaTSIimplementadaSubsidiaria` con una clave autorreferencial `idContratoPadre` para adendas (`esAdenda = 1`).
- **Alternativa Descartada:** Crear dos tablas separadas (`TContrato` y `TAdenda`).
- **Razón:** Las adendas comparten casi todos los atributos de un contrato (fechas, montos, ruta del documento, adjudicación), por lo que una relación reflexiva es más limpia y permite $N$ adendas sobre un contrato principal.

### 4. Drivers Volumétricos Libres con Precio Unitario
- **Decisión:** Enriquecer `dbo.TDriver` con `unidadMedida`, `cantidad`, `precioUnitario` y `moneda`. El cálculo del costo total se expone a nivel de aplicación (`cantidad * precioUnitario`) y/o columna calculada no persistida.
- **Alternativa Descartada:** Crear una tabla nueva de métricas de drivers.
- **Razón:** `TDriver` ya existe en el modelo y está asociado a la tecnología implementada; dotarla de estas columnas satisface directamente la necesidad del usuario sin tablas redundantes.

### 5. Compatibilidad Retrospectiva con `TBuildingBlockVsTTecnologiaTSI`
- **Decisión:** La tabla puente existente `TBuildingBlockVsTTecnologiaTSI` se mantendrá sincronizada con los estándares corporativos vigentes (`ACTIVO_VIGENTE`) para que el servicio `BuildingBlockTechnologyMappingService` y el controlador `TechnologyMappingController` sigan funcionando sin cambios regresivos.

## Risks / Trade-offs

- **[Riesgo] Concurrencia al cambiar el estándar corporativo:** Dos administradores podrían intentar sustituir el estándar simultáneamente.
  → **Mitigación:** Aplicar control de concurrencia optimista y transacción en la capa de aplicación con aislamiento serializable/snapshot para el cierre del estándar anterior y alta del nuevo.
- **[Riesgo] Contratos huérfanos o adendas sin padre:** Intentar registrar una adenda apuntando a un contrato inexistente o de otra empresa.
  → **Mitigación:** Validación en servidor de que `idContratoPadre` exista y pertenezca a la misma `idTecnologiaTSIimplementadaSubsidiaria`.
- **[Riesgo] Registros existentes con Building Block nulo:** Registros previos en `TTecnologiaTSIimplementadaSubsidiaria` que no tienen `idBuildingBlock`.
  → **Mitigación:** La columna es nullable y la interfaz mostrará una advertencia indicando que requiere asignación de Building Block para ser visible en el Proceso de Adopción.

## Migration Plan

1. **Scripts DDL Aditivos en DEV-v2 (`db-landscape-tsi-dev-v2`):**
   - Validar `SELECT DB_NAME() = 'db-landscape-tsi-dev-v2'`.
   - Crear tablas `TProcesoAdopcionTSI`, `TProcesoAdopcionEmpresa`, `TEstandarTecnologiaHistorico`, `TContratoTecnologia`.
   - Alterar de forma no destructiva `TBuildingBlock`, `TTecnologiaTSIimplementadaSubsidiaria`, `TDriver` y `TCasosDeUso` agregando columnas con defaults / nulls.
2. **Entidades en Dominio y Mapeo en EF Core (`Landscape.Tsi.Infrastructure`):**
   - Configurar `CatalogDbContext` para mapear las nuevas entidades y propiedades.
3. **Casos de Uso y Servicios de Aplicación (`Landscape.Tsi.Application`):**
   - Implementar `IAdoptionProcessService` para gestionar el proceso, convocatoria de empresas, contratos, drivers y transición de estándares históricos.
4. **Capa Web y Vistas MVC (`Landscape.Tsi.Web`):**
   - Actualizar el detalle de Building Block incorporando la pestaña de Adopción TSI, vistas del Proceso, contratos y drivers.
5. **Rollback Strategy:**
   - Dado que los cambios son aditivos (columnas nullables y tablas nuevas), un rollback solo requiere desestimar las nuevas rutas y retirar las tablas creadas sin afectar los datos preexistentes.
