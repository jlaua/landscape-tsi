## Purpose

Gestionar el inventario de tecnologías TSI implementadas por las empresas subsidiarias para cada Building Block, incorporando la administración de contratos con adendas (1:N), dimensionamiento volumétrico con drivers de costeo y esquemas operativos.

## ADDED Requirements

### Requirement: Registro Contextualizado de Tecnología Implementada en Subsidiaria
El sistema debe permitir registrar qué tecnologías TSI tiene implementadas una subsidiaria para un Building Block determinado en el contexto de un proceso de adopción, identificando la tecnología primaria y la versión desplegada.

#### Scenario: Registro exitoso de tecnología local para un Building Block
- **WHEN** el CISO o SPOC de una subsidiaria registra la tecnología utilizada por su empresa para un Building Block dentro del proceso de adopción
- **THEN** el sistema registra la implementación en `TTecnologiaTSIimplementadaSubsidiaria` asociando el Building Block y el proceso de la subsidiaria.

### Requirement: Gestión de Contratos y Adendas por Tecnología Implementada (1:N)
Cada tecnología implementada por una subsidiaria debe permitir registrar uno o varios contratos, así como sus correspondientes adendas jerárquicas, capturando fechas, montos y la ruta del archivo contractual.

#### Scenario: Registro de contrato original
- **WHEN** se ingresa el contrato principal de una tecnología implementada con número de contrato, fecha de inicio, fecha fin, fecha de adjudicación, monto y ruta de archivo
- **THEN** el sistema guarda el registro en `TContratoTecnologia` con `esAdenda = 0` y `idContratoPadre = NULL`.

#### Scenario: Registro de adenda vinculada a contrato existente
- **WHEN** se registra una adenda para una tecnología implementada seleccionando un contrato padre existente
- **THEN** el sistema valida que el contrato padre pertenezca a la misma tecnología implementada y registra la adenda con `esAdenda = 1` vinculada a su contrato padre.

#### Scenario: Rechazo de adenda sin contrato padre válido
- **WHEN** se intenta registrar una adenda sin especificar un contrato principal válido
- **THEN** el sistema rechaza la operación informando la falta del contrato de referencia.

### Requirement: Dimensionamiento y Costeo mediante Múltiples Drivers (1:N)
El sistema debe permitir registrar libremente múltiples drivers operativos (`TDriver`) por cada tecnología implementada, definiendo concepto, unidad de medida, cantidad y precio unitario.

#### Scenario: Registro de driver con métricas y precio unitario
- **WHEN** el usuario agrega un driver especificando descripción, unidad de medida (ej. 'Usuarios', 'Nodos'), cantidad volumétrica y precio unitario
- **THEN** el sistema almacena el driver en `TDriver` y calcula el costo total proyectado (`cantidad * precioUnitario`).

### Requirement: Asociación de Modelo de Operación
Cada tecnología implementada en una subsidiaria debe vincularse a un modelo de operación (`TModeloDeOperacion`), preservando su tipo de operación (`TTipoOperacion`) y su modalidad laboral (`TModalidadLaboral`).

#### Scenario: Consulta de modelo operativo de la implementación
- **WHEN** se visualiza la ficha técnica de la tecnología en la subsidiaria
- **THEN** el sistema muestra el tipo de operación (interno/tercerizado) y la modalidad laboral (turno/presencial/remoto) configurados.
