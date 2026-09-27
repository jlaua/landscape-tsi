# adoption/process-management Specification

## MODIFIED Requirements

### Requirement: Diagnóstico AS-IS por Subsidiaria con Contratos y Modelo Operativo
Para cada empresa participante en la evaluación, el sistema debe permitir documentar si cuenta con una tecnología implementada (`TTecnologiaTSIimplementadaSubsidiaria`), su contrato vigente (`TContratoTecnologia`) con fecha fin de vencimiento, su modelo operativo (`TModeloDeOperacion`) y sus proveedores asociados (Vendor y Partner locales). Si una subsidiaria no cuenta con tecnología registrada para el Building Block, el sistema no debe inyectar tecnologías por defecto ni registros automáticos, debe reportar explícitamente el estado "Sin tecnología registrada" y establecer los metadatos de contrato, vendor, partner y operación en guiones ("—"). Los drivers de dimensionamiento se gestionan de forma desacoplada a nivel de proceso y empresa en `TDriverEmpresa`.

#### Scenario: Registro de tecnología implementada con contrato y modelo operativo
- **WHEN** se registra que una subsidiaria cuenta con tecnología implementada activa
- **THEN** el sistema almacena los datos de versión desplegada, proveedor local, contrato (número, fecha de fin y monto) y modelo de operación independiente de los drivers de dimensionamiento.

#### Scenario: Subsidiaria sin tecnología actual
- **WHEN** la empresa subsidiaria no cuenta con tecnología previa en este Building Block o no ha registrado aún una tecnología local en la evaluación
- **THEN** el sistema no inyecta tecnologías por defecto ni crea entidades automáticas en TTecnologiaTSIimplementadaSubsidiaria, reporta "Sin tecnología registrada", muestra guiones ("—") en contrato, vendor, partner y tipo de operación, y permite capturar volúmenes de drivers directamente en TDriverEmpresa.
