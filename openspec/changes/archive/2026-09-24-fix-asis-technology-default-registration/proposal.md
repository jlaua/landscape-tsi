# Proposal: Corrección de Asignación por Defecto de Tecnología AS-IS y Estandarización de "Sin tecnología registrada"

## Justificación de Negocio y Arquitectura
Durante la evaluación de procesos de adopción tecnológica (ej. Proceso 269: ETP + Awareness: Proofpoint), los usuarios registraron la tecnología implementada únicamente para una subsidiaria específica (Banco de Crédito del Perú). Sin embargo, al guardar o sincronizar volúmenes de drivers para el resto de las empresas convocadas, el sistema asignaba automáticamente y por defecto la primera tecnología del catálogo (`Id = 1`, Imperva), generando diagnósticos AS-IS falsos, ensuciando la base de datos con registros no configurados y distorsionando los reportes ejecutivos.

El requerimiento exige:
1. Eliminar la inserción automática o por defecto de tecnologías implementadas (`TTecnologiaTSIimplementadaSubsidiaria`) al registrar o modificar drivers de volumetría.
2. Identificar y mostrar explícitamente el estado `"Sin tecnología registrada"` en todas las vistas de reportería y seguimiento cuando una empresa no cuente con una tecnología registrada para el Building Block evaluado.
3. Establecer valores nulos o guiones (`"—"`) en los metadatos contractuales, vendor, partner y tipo de operación para subsidiarias sin tecnología.
4. Limpiar en base de datos los registros automáticos ficticios generados previamente.

## Alcance del Cambio
1. **Backend (`AdoptionProcessService`):**
   - Refactor de `UpdateCompanyDriverVolumeAsync`: Se elimina el fallback que creaba registros en `TTecnologiaTSIimplementadaSubsidiaria` con `defaultTechId = 1`. Los drivers por empresa se persisten directamente en `TDriverEmpresa`.
   - Normalización en `GetEvaluationReportsAsync`: Si una subsidiaria no cuenta con tecnología registrada en el proceso ni en el catálogo del Building Block, se establece `techNombre = "Sin tecnología registrada"` y `tipoContrato = vendor = partner = tipoOperacion = "—"`.
2. **Capa Web y Presentación (`Reports.cshtml`):**
   - Actualización de las 5 pestañas de reportería (Alcance, Vencimiento de Contratos, Volumetría por Empresa, Capacidades por Empresa y Proyección de Costos AS-IS) para renderizar un distintivo visual estilizado `<span class="badge bg-secondary-subtle text-secondary border">Sin tecnología registrada</span>`.
3. **Saneamiento de Base de Datos:**
   - Eliminación controlada de los 20 registros ficticios de `TTecnologiaTSIimplementadaSubsidiaria` y sus 103 drivers legacy asociados en el proceso 269, preservando intacto el registro legítimo de Banco de Crédito del Perú (Proofpoint) y los 132 registros de `TDriverEmpresa`.
4. **Verificación y Pruebas Automatizadas:**
   - Adición de pruebas unitarias específicas y ejecución exitosa de 351/351 pruebas (100% aprobadas).
