# Administracion de Catalogos Maestros Specification (Delta: Vendors y Tecnologias TSI)

## Purpose
Extender la administración de catálogos maestros de Vendors y Tecnologías TSI para soportar asociaciones bidireccionales dinámicas, búsqueda y registro rápido de tecnologías vinculadas desde el Vendor, y estandarizar la captura de versiones desplegadas mediante catálogo relacional.

## Requirements

### Requirement: Gestión de Tecnologías TSI Asociadas a Vendors
El catálogo de Vendors (`TMVendor`) MUST permitir visualizar, asociar, desasociar y crear tecnologías TSI (`TTecnologiaTSI`) vinculadas al proveedor mediante modales interactivos y pestañas dedicadas tanto desde el listado como desde el detalle del Vendor.

#### Scenario: Visualización de tecnologías asociadas y desasociación
- **WHEN** un usuario autorizado abre la gestión de tecnologías para un Vendor
- **THEN** el sistema lista las tecnologías actualmente asociadas (`idVendor = vendor.Id`) y ofrece una acción para desasociar la tecnología (estableciendo `idVendor = NULL` tras confirmación).

#### Scenario: Búsqueda y asociación de tecnología existente
- **WHEN** el usuario busca una tecnología TSI en la pestaña "Asociar Existente" del modal de Vendor
- **THEN** el sistema filtra las tecnologías del catálogo maestro en tiempo real y permite asociarla al Vendor en curso con un solo clic.

#### Scenario: Registro rápido de tecnología para el Vendor
- **WHEN** el usuario completa el formulario de alta rápida de tecnología TSI dentro del modal del Vendor
- **THEN** el sistema crea la nueva entidad `TTecnologiaTSI` asignándole automáticamente el `idVendor` del proveedor actual.

### Requirement: Estandarización de Relación de Vendor y Versión Desplegada en Catálogos
El catálogo de Tecnologías TSI (`TTecnologiaTSI`) MUST ofrecer selectores combo para Vendor (`TMVendor`) y Versión Desplegada (`TMVersionDesplegada`). Asimismo, el catálogo de Tecnologías Implementadas por Subsidiaria (`TTecnologiaTSIimplementadaSubsidiaria`) MUST presentar el campo "Versión Desplegada" como un desplegable relacional en lugar de entrada de texto libre.

#### Scenario: Selección de Vendor y Versión Desplegada en Tecnología TSI
- **WHEN** se crea o edita un registro en el catálogo de Tecnologías TSI
- **THEN** el sistema presenta combos dinámicos poblados con las opciones activas de `TMVendor` y `TMVersionDesplegada`.

#### Scenario: Selección de Versión Desplegada en Tecnología Implementada por Subsidiaria
- **WHEN** se administra una tecnología implementada en una subsidiaria
- **THEN** el sistema despliega el campo Versión Desplegada como selector vinculado a `TMVersionDesplegada`, persistiendo la clave foránea correspondiente.
