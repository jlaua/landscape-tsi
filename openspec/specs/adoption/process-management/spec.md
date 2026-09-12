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
