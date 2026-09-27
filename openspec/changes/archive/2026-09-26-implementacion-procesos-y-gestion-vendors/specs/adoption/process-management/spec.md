# Adoption Process Management Specification (Delta: Procesos de Implementación)

## Purpose
Extender la gestión de procesos de adopción tecnológica para soportar formalmente los procesos de tipo "IMPLEMENTACION", permitiendo dar seguimiento a la puesta en marcha efectiva del estándar corporativo por empresa subsidiaria, su avance técnico, arquitectura implementada y reportería ejecutiva con métricas y cabeceras normalizadas.

## Requirements

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
