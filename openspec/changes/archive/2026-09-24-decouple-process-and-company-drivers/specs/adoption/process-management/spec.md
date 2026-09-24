# adoption/process-management Specification

## MODIFIED Requirements

### Requirement: Diagnóstico AS-IS por Subsidiaria con Contratos y Modelo Operativo
Para cada empresa participante en la evaluación, el sistema debe permitir documentar si cuenta con una tecnología implementada (`TTecnologiaTSIimplementadaSubsidiaria`), su contrato vigente (`TContratoTecnologia`) con fecha fin de vencimiento, su modelo operativo (`TModeloDeOperacion`) y sus proveedores asociados (Vendor y Partner locales). Los drivers de dimensionamiento se gestionan de forma desacoplada a nivel de proceso y empresa.

#### Scenario: Registro de tecnología implementada con contrato y modelo operativo
- **WHEN** se registra que una subsidiaria cuenta con tecnología implementada activa
- **THEN** el sistema almacena los datos de versión desplegada, proveedor local, contrato (número, fecha de fin y monto) y modelo de operación independiente de los drivers de dimensionamiento.

#### Scenario: Subsidiaria sin tecnología actual
- **WHEN** se declara que la empresa subsidiaria no cuenta con tecnología previa en este Building Block
- **THEN** el sistema no exige contrato local y califica a la empresa para adopción inmediata del estándar corporativo.

## ADDED Requirements

### Requirement: Plantilla Corporativa de Drivers a Nivel de Proceso
El sistema MUST permitir definir a nivel de cada solicitud de adopción (`TProcesoAdopcionTSI`) la plantilla de drivers de dimensionamiento y proyección (`TDriverProceso`), especificando su descripción y unidad de medida, con homologación automática para todas las empresas participantes.

#### Scenario: Creación de driver en el proceso con auto-clonado a subsidiarias
- **WHEN** un usuario con permisos de edición registra un nuevo driver en el proceso de adopción
- **THEN** el sistema persiste la plantilla en `TDriverProceso` y genera automáticamente un registro en `TDriverEmpresa` para cada empresa subsidiaria convocada en la evaluación con cantidad y precio inicial en cero.

#### Scenario: Eliminación en cascada de driver del proceso
- **WHEN** un usuario autorizado elimina un driver de la plantilla del proceso
- **THEN** el sistema elimina en cascada los registros de consumo asociados en todas las subsidiarias y audita la acción.

### Requirement: Consumo y Dimensionamiento de Drivers por Subsidiaria
El sistema MUST capturar y actualizar de manera independiente por cada subsidiaria convocada (`TProcesoAdopcionEmpresa`) los valores de consumo de los drivers del proceso (`TDriverEmpresa`), registrando cantidad, precio unitario, moneda y recalculando el costo total proyectado.

#### Scenario: Actualización de valores de driver por empresa
- **WHEN** se modifica la cantidad o precio unitario de un driver para una subsidiaria específica
- **THEN** el sistema actualiza el registro en `TDriverEmpresa`, recalcula el costo total (`cantidad * precioUnitario`) y preserva la moneda seleccionada.

#### Scenario: Convocatoria de nueva empresa con asignación automática de drivers
- **WHEN** se convoca una nueva subsidiaria a una evaluación que ya cuenta con drivers de proceso definidos
- **THEN** el sistema clona automáticamente la plantilla de drivers existente creando sus registros correspondientes en `TDriverEmpresa`.
