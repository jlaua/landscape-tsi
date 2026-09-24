# Proposal: Desacoplamiento de Drivers Tecnológicos a Nivel de Proceso y Subsidiaria

## Justificación de Negocio y Arquitectura
En la gobernanza de adopción de tecnologías de seguridad de la información (TSI), los drivers de dimensionamiento (ej. Licencias de Usuario E5, Agentes de Endpoint, TB de Log, Dispositivos protegidos) no pertenecen a una tecnología particular implementada localmente, sino al **alcance y requerimiento de la evaluación a nivel corporativo**.

Anteriormente, los drivers estaban vinculados a `TTecnologiaTSIimplementadaSubsidiaria` (modelo 1:N por tecnología local), lo que generaba:
1. Ambigüedad cuando una subsidiaria aún no tenía tecnología implementada previa pero sí requería proyectar su volumen.
2. Desincronización y divergencia de nombres y unidades de medida entre empresas en la misma solicitud.
3. Creación accidental de drivers duplicados al registrar volúmenes desde diferentes vistas o matrices de reportería.

## Alcance del Cambio
1. **Definición de Plantilla de Drivers a Nivel Proceso (`TDriverProceso`):** Cada proceso de adopción (`TProcesoAdopcionTSI`) define el catálogo de drivers homologados con su descripción y unidad de medida.
2. **Replicación Automática por Empresa (`TDriverEmpresa`):** Al crear un driver en el proceso, o al convocar nuevas empresas, el driver se clona automáticamente para cada empresa participante (`TProcesoAdopcionEmpresa`), permitiendo registrar individualmente `Cantidad`, `PrecioUnitario`, `CostoTotal` y `Moneda`.
3. **Migración de Datos Idempotente:** Migración exitosa de 33 plantillas de drivers y 725 registros de empresas en SQL Server (`db-landscape-tsi-dev-v2`) preservando valores históricos y cálculos.
4. **Capa Web y Reportería:** Refactor de `Details.cshtml` con nueva sección de drivers del proceso y bloque de consumo por subsidiaria, eliminando la ambigüedad en tarjetas de tecnologías y garantizando la continuidad de la matriz de volumetría en `Reports.cshtml`.
5. **Verificación:** 347/347 pruebas automatizadas superadas al 100%.
