# adoption/service-management Specification

## Purpose
Gobierna la administración, parametrización y dimensionamiento económico de Servicios de TI en Landscape TSI (Implementación, Migración y Operación) junto con sus respectivos tarifarios de mano de obra (driver por rangos de horas y matrices operativas N1-N3) tanto a nivel corporativo como por subsidiaria.

## Requirements

### Requirement: Registro Unificado de Servicios de TI
El sistema MUST permitir la creación, consulta, edición y baja de servicios tecnológicos clasificados en Implementación, Migración y Operación. Un servicio MAY estar vinculado directamente a una tecnología implementada en una subsidiaria (idTecnologiaTSIimplementadaSubsidiaria e idEmpresaSubsidiaria) o a una tecnología corporativa evaluada o adjudicada (idTecnologiaTSI e idProcesoAdopcionTSI).

#### Scenario: Registro de servicio para tecnología implementada en subsidiaria
- **GIVEN** un usuario autenticado con permiso Permissions.AdoptionManage y alcance sobre una empresa subsidiaria
- **WHEN** registra un nuevo servicio seleccionando la subsidiaria, su tecnología implementada, el tipo de servicio y un código único
- **THEN** el sistema persiste la cabecera del servicio en dbo.TServicioTecnologia con estado inicial EVALUACION o COTIZADO y registra la traza de auditoría.

#### Scenario: Registro de servicio corporativo adjudicado en proceso de adopción
- **GIVEN** una evaluación de adopción TSI con una tecnología corporativa evaluada o adjudicada
- **WHEN** el líder corporativo registra un servicio corporativo enlazado a idProcesoAdopcionTSI e idTecnologiaTSI
- **THEN** el sistema registra el servicio como de alcance corporativo, asociando opcionalmente el proveedor (TVendor).

### Requirement: Tarifario por Rango de Horas para Implementación y Migración
Para servicios de tipo Implementación o Migración, el sistema MUST soportar un tarifario estructurado por complejidad y rango de horas (TTarifarioProyectoHoras), cubriendo: Baja (Por Hora), Proyecto menor (1 a 100 horas), Proyecto intermedio (101 a 200 horas) y Proyecto mayor (Mayor a 201 horas). El sistema MUST calcular determinísticamente el subtotal de cada rango (horasEstimadas * tarifaHora) y actualizar el costoTotalEstimado del servicio.

#### Scenario: Cálculo de costos por horas en proyecto de migración
- **GIVEN** un servicio de migración registrado
- **WHEN** el usuario ingresa una complejidad de Proyecto menor (1 a 100 horas) con 60 horas estimadas a una tarifa de 70 USD/hora
- **THEN** el sistema calcula el subtotal de 4,200 USD y actualiza el costo acumulado total del servicio.

### Requirement: Catálogo y Matriz Tarifaria de Operación N1, N2 y N3
Para servicios de tipo Operación, el sistema MUST proveer el catálogo de responsabilidades estandarizadas para Soporte Nivel 1, Soporte Nivel 2 y Soporte Nivel 3, y soportar una matriz de tarifario (TTarifarioOperacion) que combine:
1. Nivel de Soporte (N1, N2, N3)
2. Modalidad (Pay-Per-Use, Mensual 8x5 con 176h promedio, Mensual 24x7 con 720h promedio)
3. Expertise (Junior, Senior)
4. Locación (Remoto, Presencial)

#### Scenario: Cotización de soporte operativo mensual 24x7 senior remoto
- **GIVEN** un servicio de operación para una plataforma de ciberseguridad
- **WHEN** se agrega un ítem de tarifario con nivel N2, modalidad Mensual 24x7 (720 hrs), expertise Senior, locación Remoto, tarifa mensual de 5,000 USD y 12 meses de duración
- **THEN** el sistema calcula un subtotal de 60,000 USD y lo suma al costo total estimado del servicio operativo.

### Requirement: Control de Acceso, Segregación y Ámbito Organizacional
Las operaciones de consulta y mutación sobre servicios y tarifarios MUST validar permisos granulares y ámbito organizacional. Un usuario con alcance exclusivo sobre una subsidiaria solo MUST poder visualizar y gestionar servicios correspondientes a su empresa asignada, mientras que los roles corporativos (Permissions.AdoptionManage) pueden gestionar servicios tanto corporativos como de todas las subsidiarias.

#### Scenario: Intento de modificación fuera del alcance de la subsidiaria
- **GIVEN** un usuario asignado exclusivamente a la subsidiaria 'BCP Bolivia'
- **WHEN** intenta modificar un servicio o tarifario perteneciente a 'BCP Perú' o a nivel corporativo
- **THEN** el sistema deniega la operación con un error de autorización 403 Forbidden y registra el evento en IamEventoAuditoriaAutorizacion.
