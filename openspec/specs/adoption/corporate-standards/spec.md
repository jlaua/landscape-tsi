# adoption/corporate-standards Specification

## Purpose
Administrar los estándares tecnológicos corporativos para cada Building Block con trazabilidad histórica completa, asegurando el registro de casos de uso de negocio y la vinculación con vendors y partners.

## Requirements

### Requirement: Trazabilidad Histórica de Estándares Tecnológicos Corporativos
El sistema debe mantener el historial inmutable de las tecnologías designadas como estándar corporativo (principal o alternativa homologada), evitando la sobreescritura de datos vigentes y archivando los estándares sustituidos.

#### Scenario: Designación de un nuevo estándar corporativo principal
- **WHEN** un Arquitecto de Seguridad designa una nueva tecnología como estándar principal de un Building Block que ya cuenta con un estándar vigente
- **THEN** el sistema actualiza el registro previo marcándolo como `HISTORICO_REEMPLAZADO` con fecha de término y motivo del cambio, inserta el nuevo registro como `ACTIVO_VIGENTE` e incrementa la trazabilidad de auditoría.

#### Scenario: Registro de alternativa tecnológica homologada
- **WHEN** se aprueba una tecnología adicional como alternativa homologada para el Building Block
- **THEN** el sistema la registra con rol de estándar `ALTERNATIVA` y estado `ACTIVO_VIGENTE` sin deshabilitar el estándar principal.

### Requirement: Cobertura de Casos de Uso en Estándares Corporativos
El sistema debe permitir vincular los casos de uso (`TCasosDeUso`) soportados por la tecnología estandarizada para garantizar que cumpla con los requerimientos funcionales del Building Block.

#### Scenario: Consulta de casos de uso asociados al estándar
- **WHEN** un usuario consulta el detalle de la tecnología estándar corporativa
- **THEN** el sistema presenta los casos de uso técnicos y de negocio que la tecnología homologada tiene cubiertos.

### Requirement: Información de Vendor y Contactos de Soporte / Partner
El estándar corporativo y las tecnologías vinculadas deben exponer la información del fabricante (`TVendor`), el contacto del vendor (`TContactoVendor`) y el contacto del partner canal (`TContactoPartner`).

#### Scenario: Visualización integral de vendor y canales
- **WHEN** se visualiza la ficha del estándar corporativo
- **THEN** el sistema despliega el nombre del fabricante, contacto directo de cuenta y datos de contacto del partner que suministra soporte de nivel 1 o licenciamiento.
