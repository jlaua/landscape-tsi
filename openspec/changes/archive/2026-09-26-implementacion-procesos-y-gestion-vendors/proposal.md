# Propuesta: Módulo de Procesos de Implementación y Gestión Integral de Vendors y Tecnologías TSI

## 1. Contexto y Justificación

El gobierno de adopción tecnológica corporativa en Landscape TSI contemplaba inicialmente la fase de "Evaluación" tecnológica. Sin embargo, una vez decretada la tecnología estándar, las organizaciones subsidiarias requieren un seguimiento específico del proceso de "Implementación", con registro de contratos vigentes, fechas efectivas de puesta en marcha, especialistas técnicos designados (tanto de la subsidiaria como del partner adjudicado), diagramas/URLs de arquitectura y semáforo visual de alineamiento al estándar corporativo.

Asimismo, la administración de tablas maestras requería una gestión integral de relaciones para Vendors y Tecnologías TSI: vinculación 1:N entre Vendors y Tecnologías TSI mediante modales con búsqueda interactiva, desasociación y registro rápido de tecnologías, así como la estandarización del atributo "Versión Desplegada" a través de un catálogo maestro relacional (`TMVersionDesplegada`) tanto en la definición de la tecnología como en la tecnología implementada por subsidiaria.

## 2. Alcance de los Cambios

### Módulo de Procesos de Implementación (`tipoProceso = 'IMPLEMENTACION'`)
1. **Flujo de Creación y Consulta**:
   - Creación de solicitudes de implementación filtrando por Familia, Subfamilia y Building Block.
   - Captura de vigencia del contrato corporativo (fecha inicio y fecha fin).
   - Selección de Tecnología Estándar decretada en la evaluación o directamente desde la tabla de estándares TSI. Si no existe decretada, se presenta el estado visual "Por definir".
   - Convocatoria de empresas subsidiarias participantes.
2. **Actualización de Avance Técnico por Subsidiaria**:
   - Modal interactivo para capturar la fecha efectiva de implementación (`fechaImplementacion`).
   - Selección de Especialista Técnico a Cargo con soporte dual: contacto de la empresa subsidiaria (`TContactoEmpresaSubsidiaria`) o contacto del partner adjudicado (`TContactoPartner`).
   - Selección de Tecnología AS-IS implementada por la empresa (estándar TSI o solución propia).
   - Semáforo visual del estado de adopción:
     - **Verde**: Alineado al estándar TSI (`ALINEADO` o tecnología estándar).
     - **Amarillo**: En despliegue o migración (`EN_DESPLIEGUE`, `MIGRACION`, `HOMOLOGADO`).
     - **Rojo**: No alineado (`NO_ALINEADO`).
   - Carga de archivo de arquitectura técnica o registro de URL de documentación arquitectural (`urlArquitectura`).
3. **Reportería & Seguimiento de Implementaciones**:
   - Pestaña 3 (Volumetría por Empresa): Cabeceras de drivers rotadas a 90 grados verticales (formato acorde con reportes ejecutivos). Inicialización por defecto en 0 para empresas sin valores capturados formalmente.
   - Pestaña 4 (Capacidades): Cabeceras de capacidades rotadas a 90 grados verticales.

### Gestión de Vendors y Tecnologías TSI en Catálogos Maestros
1. **Modal Interactivo de Tecnologías Asociadas a Vendor**:
   - Visualización de tecnologías asociadas al Vendor con opción de desasociación instantánea.
   - Pestaña de Búsqueda y Asociación de tecnologías TSI existentes en el catálogo maestro.
   - Pestaña de Creación Rápida de nueva tecnología TSI vinculada automáticamente al Vendor.
2. **Estandarización de Versión Desplegada y Vendor en Catálogos**:
   - En `TTecnologiaTSI`: Nuevas relaciones `idVendor` y `idVersionDesplegada` representadas mediante selectores desplegables dinámicos en el catálogo maestro.
   - En `TTecnologiaTSIimplementadaSubsidiaria`: Conversión del campo "Versión Desplegada" de texto libre a combo relacional mapeado a `TMVersionDesplegada`.
   - Scripts de migración SQL aditivos y scripts de ejecución PowerShell con backfill histórico.

## 3. Impacto y Compatibilidad

- Se conservó la compatibilidad hacia atrás con los procesos de evaluación existentes (`tipoProceso = 'EVALUACION'`).
- Los endpoints AJAX cuentan con tokens antiforgery y políticas de autorización de catálogos (`Permissions.CatalogEdit`).
- No se eliminaron columnas preexistentes; se añadieron columnas anulables y relaciones no destructivas en SQL Server.
