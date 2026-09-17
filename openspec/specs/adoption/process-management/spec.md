# adoption/process-management Specification

## Purpose
Proveer la gobernanza del ciclo de vida de los procesos de adopción tecnológica (TProcesoAdopcionTSI) asociados a Building Blocks de seguridad, integrando la convocatoria de empresas subsidiarias y la designación de especialistas focales de contacto.

## Requirements

### Requirement: Apertura y Gobernanza del Proceso de Adopción TSI
El sistema debe permitir a los usuarios autorizados (Arquitecto de Seguridad o Administrador) crear y gestionar procesos de adopción tecnológica para un Building Block específico, controlando su avance a través del catálogo maestro `TMEstadoAdopcionTSI`.

#### Scenario: Creación exitosa de un proceso de adopción
- **WHEN** un Arquitecto de Seguridad registra un nuevo proceso de adopción especificando un código único, nombre, Building Block objetivo, estado de adopción inicial y fechas de compromiso
- **THEN** el sistema registra la entidad `TProcesoAdopcionTSI`, asocia el Building Block correspondiente y registra el evento en la auditoría inmutable.

#### Scenario: Rechazo de duplicidad de código de proceso
- **WHEN** se intenta registrar un proceso de adopción con un código de proceso que ya existe en el sistema
- **THEN** el sistema rechaza la operación informando la infracción de unicidad y no altera el estado de la base de datos.

### Requirement: Convocatoria de Empresas Subsidiarias y Asignación Focal
El proceso de adopción debe registrar la participación de las empresas subsidiarias del grupo corporativo, asignando obligatoriamente el especialista técnico focal que acompaña el proceso (`TContactoEmpresaSubsidiaria`).

#### Scenario: Registro de subsidiaria con focal en el proceso
- **WHEN** se añade una empresa subsidiaria al proceso de adopción junto a un contacto focal perteneciente a dicha empresa
- **THEN** el sistema crea el registro en `TProcesoAdopcionEmpresa` vinculado al proceso y al contacto focal.

#### Scenario: Registro de excepción por 'No Aplica'
- **WHEN** una subsidiaria declara que el Building Block no aplica a su modelo de negocio (`aplica = 0`) proporcionando una justificación arquitectónica
- **THEN** el sistema registra el estado de excepción, conserva la justificación y exime a la empresa de registrar tecnologías implementadas.

#### Scenario: Denegación de excepción sin justificación
- **WHEN** se intenta marcar una subsidiaria como no aplicable (`aplica = 0`) sin ingresar una justificación técnica
- **THEN** el sistema bloquea la operación y exige la justificación correspondiente.

### Requirement: Registro Integral de Solicitud de Evaluación y Vinculación Taxonómica
El sistema debe permitir a los usuarios con permisos de catálogo crear solicitudes de evaluación dentro del ciclo de adopción tecnológica (`TEstadoFaseAdopcion = EVALUACION`), seleccionando el Dominio (`TMDominio`) y el Building Block (`TBuildingBlock`) del catálogo maestro, e inspeccionando de forma reactiva las Capacidades de Seguridad (`TCapacidadDeSeguridad`) y Funcionalidades (`TFuncionalidad`) asociadas sin duplicar registros de base de datos.

#### Scenario: Registro exitoso de evaluación con vinculación taxonómica
- **WHEN** un Arquitecto de Seguridad registra una evaluación seleccionando Dominio y Building Block existentes
- **THEN** el sistema persiste la solicitud en `TProcesoAdopcionTSI` con código único, estado inicial de madurez tecnológica (`TMEstadoAdopcionTSI`) y enlaza en vivo sus capacidades y funcionalidades para reportes futuros.

#### Scenario: Actualización de metadatos en tablas maestras reflejada en vivo
- **WHEN** se actualiza la información de una capacidad o funcionalidad en su respectiva tabla maestra
- **THEN** la consulta de la evaluación refleja automáticamente los datos maestros actualizados sin requerir sincronizaciones estáticas.

### Requirement: Convocatoria Múltiple de Subsidiarias mediante Selección por Checkboxes
La interfaz de evaluación debe permitir seleccionar múltiples empresas subsidiarias (`TEmpresaSubsidiaria`) simultáneamente mediante casillas de verificación (checkboxes), registrando su participación en `TProcesoAdopcionEmpresa` y asignando el contacto técnico focal (`TContactoEmpresaSubsidiaria`) para cada empresa seleccionada.

#### Scenario: Convocatoria en lote de subsidiarias
- **WHEN** el usuario selecciona varias subsidiarias mediante checkboxes y especifica sus contactos focales
- **THEN** el sistema crea o actualiza en una única operación los registros de `TProcesoAdopcionEmpresa` correspondientes a la evaluación.

#### Scenario: Exclusión con justificación técnica
- **WHEN** una subsidiaria convocada se desmarca o declara como no aplicable
- **THEN** el sistema registra `aplica = 0` y exige una justificación técnica obligatoria.

### Requirement: Diagnóstico AS-IS por Subsidiaria con Contratos, Drivers y Modelo Operativo
Para cada empresa participante en la evaluación, el sistema debe permitir documentar si cuenta con una tecnología implementada (`TTecnologiaTSIimplementadaSubsidiaria`), su contrato vigente (`TContratoTecnologia`) con fecha fin de vencimiento, uno o múltiples drivers de dimensionamiento (`TDriver`), su modelo operativo (`TModeloDeOperacion`) y sus proveedores asociados (Vendor y Partner locales).

#### Scenario: Registro de tecnología implementada con contrato y drivers
- **WHEN** se registra que una subsidiaria cuenta con tecnología implementada activa
- **THEN** el sistema almacena los datos de versión desplegada, proveedor local, contrato (número, fecha de fin y monto) y al menos un driver de dimensionamiento con su unidad y valor.

#### Scenario: Subsidiaria sin tecnología actual
- **WHEN** se declara que la empresa subsidiaria no cuenta con tecnología previa en este Building Block
- **THEN** el sistema no exige contrato ni drivers y califica a la empresa para adopción inmediata del estándar corporativo.

### Requirement: Definición del Estándar Corporativo y Cronograma de Convergencia Contractual
El proceso de evaluación debe permitir fijar la tecnología estándar corporativo (`TTecnologiaTSI` / `TEstandarTecnologiaHistorico`), su contrato marco y proyectar la fecha de convergencia de cada subsidiaria en función del vencimiento del contrato local registrado en su diagnóstico AS-IS.

#### Scenario: Adjudicación de estándar corporativo y proyección de migración
- **WHEN** se define la tecnología estándar corporativa adjudicada para el proceso
- **THEN** el sistema genera el cronograma donde cada subsidiaria con contrato local vigente tiene como fecha de alineación el día posterior al vencimiento de su contrato actual.

### Requirement: Gestión del Ciclo de Vida y Baja Auditada de Evaluaciones
El sistema debe permitir actualizar evaluaciones en curso y dar de baja evaluaciones canceladas o desestimadas, registrando de forma inmutable el motivo, usuario ejecutor y fecha en la auditoría del sistema.

#### Scenario: Baja justificada de una evaluación
- **WHEN** un usuario autorizado ejecuta la acción de dar de baja una evaluación en curso ingresando el motivo
- **THEN** el sistema actualiza el estado del proceso, desactiva la vigencia y registra el evento inmutable en `IamEventoAuditoriaAutorizacion`.

