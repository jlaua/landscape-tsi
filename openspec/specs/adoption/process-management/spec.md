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

### Requirement: Diagnóstico AS-IS por Subsidiaria con Contratos y Modelo Operativo
Para cada empresa participante en la evaluación, el sistema debe permitir documentar si cuenta con una tecnología implementada (`TTecnologiaTSIimplementadaSubsidiaria`), su contrato vigente (`TContratoTecnologia`) con fecha fin de vencimiento, su modelo operativo (`TModeloDeOperacion`) y sus proveedores asociados (Vendor y Partner locales). Si una subsidiaria no cuenta con tecnología registrada para el Building Block, el sistema no debe inyectar tecnologías por defecto ni registros automáticos, debe reportar explícitamente el estado "Sin tecnología registrada" y establecer los metadatos de contrato, vendor, partner y operación en guiones ("—"). Los drivers de dimensionamiento se gestionan de forma desacoplada a nivel de proceso y empresa en `TDriverEmpresa`.

#### Scenario: Registro de tecnología implementada con contrato y modelo operativo
- **WHEN** se registra que una subsidiaria cuenta con tecnología implementada activa
- **THEN** el sistema almacena los datos de versión desplegada, proveedor local, contrato (número, fecha de fin y monto) y modelo de operación independiente de los drivers de dimensionamiento.

#### Scenario: Subsidiaria sin tecnología actual
- **WHEN** la empresa subsidiaria no cuenta con tecnología previa en este Building Block o no ha registrado aún una tecnología local en la evaluación
- **THEN** el sistema no inyecta tecnologías por defecto ni crea entidades automáticas en TTecnologiaTSIimplementadaSubsidiaria, reporta "Sin tecnología registrada", muestra guiones ("—") en contrato, vendor, partner y tipo de operación, y permite capturar volúmenes de drivers directamente en TDriverEmpresa.

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

### Requirement: Solicitudes y Procesos de Implementación
El sistema MUST permitir la creación y seguimiento de procesos con `tipoProceso = 'IMPLEMENTACION'`, asociando la Familia, Subfamilia, Building Block, la vigencia contractual proyectada (`fechaInicioVigenciaContrato`, `fechaFinVigenciaContrato`), la tecnología estándar decretada (o permitiendo definirla como "Por definir") y la convocatoria formal de empresas subsidiarias.

#### Scenario: Creación de solicitud de implementación
- **WHEN** un usuario autorizado crea un proceso de tipo implementación
- **THEN** el sistema registra el proceso con `tipoProceso = 'IMPLEMENTACION'`, asocia las fechas de inicio y fin de vigencia contractual, vincula la tecnología estándar decretada o la registra como nula ("Por definir"), y convoca a las empresas seleccionadas.

#### Scenario: Visualización del resumen de implementación
- **WHEN** el usuario visualiza el detalle o resumen de un proceso de implementación
- **THEN** el sistema presenta explícitamente el nombre de la tecnología estándar decretada o el texto informativo "Por definir" en caso de no haberse seleccionado aún una tecnología estándar.

### Requirement: Registro de Avance Técnico y Arquitectura por Subsidiaria
Para cada empresa subsidiaria convocada en un proceso de implementación, el sistema MUST permitir registrar el especialista técnico responsable (permitiendo seleccionar ya sea un contacto de la empresa subsidiaria o un contacto del partner adjudicado), la fecha de puesta en marcha (`fechaImplementacion`), la tecnología AS-IS implementada por la empresa, y la URL o archivo digital de la arquitectura implementada.

#### Scenario: Selección flexible de especialista técnico (Empresa o Partner)
- **WHEN** el usuario actualiza el avance técnico de una subsidiaria
- **THEN** el sistema permite seleccionar opcionalmente como especialista a un contacto de `TEmpresaSubsidiaria` (`idContactoEmpresaSubsidiaria`) o a un contacto de `TPartner` (`idContactoPartner`), validando que al menos uno de ellos esté asignado si se indica responsable técnico.

#### Scenario: Carga y descarga de arquitectura técnica
- **WHEN** el usuario adjunta un archivo de arquitectura técnica o ingresa una URL de documentación
- **THEN** el sistema almacena el binario y sus metadatos de tipo/nombre en `TProcesoAdopcionEmpresa` y ofrece un enlace seguro para su descarga o consulta.

#### Scenario: Semáforo visual de estado de alineamiento
- **WHEN** se visualiza la lista de empresas subsidiarias en el proceso de implementación
- **THEN** el sistema presenta un semáforo visual con colores normalizados:
  - Verde: Subsidiaria alineada al estándar TSI (`ALINEADO` o tecnología igual al estándar corporativo).
  - Amarillo: Subsidiaria en proceso de despliegue o migración (`EN_DESPLIEGUE`, `MIGRACION` u `HOMOLOGADO`).
  - Rojo: Subsidiaria no alineada (`NO_ALINEADO` o tecnología divergente sin plan de migración).

### Requirement: Reportería Ejecutiva de Implementación con Encabezados a 90 Grados
El módulo de reportería de implementaciones MUST disponer las cabeceras de drivers (Pestaña 3: Volumetría por Empresa) y de capacidades (Pestaña 4) en orientación vertical rotada a 90 grados, y MUST inicializar los valores de volumetría por defecto en cero cuando una empresa no haya capturado valores formales para sus drivers.

#### Scenario: Visualización de cabeceras en 90 grados en volumetría y capacidades
- **WHEN** el usuario consulta las pestañas de Volumetría por Empresa o Capacidades en la vista de reportes
- **THEN** el sistema renderiza los títulos de las columnas con orientación vertical (`writing-mode: vertical-lr; transform: rotate(180deg)`), optimizando la densidad de datos en pantalla.

#### Scenario: Volumetría con valor por defecto cero
- **WHEN** un proceso cuenta con drivers definidos en la plantilla corporativa pero una empresa convocada no ha ingresado consumos
- **THEN** el reporte de volumetría muestra un valor por defecto de 0 en lugar de datos residuales o inventados.
