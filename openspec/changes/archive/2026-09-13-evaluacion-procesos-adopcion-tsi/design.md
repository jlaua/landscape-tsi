## Context

Landscape TSI cuenta con las tablas relacionales y entidades base para adopción (`TProcesoAdopcionTSI`, `TProcesoAdopcionEmpresa`, `TTecnologiaTSIimplementadaSubsidiaria`, `TContratoTecnologia`, `TDriver`, `TModeloDeOperacion`). Sin embargo, la experiencia de usuario y los flujos requerían evolucionar desde convocatorias individuales aisladas hacia un flujo unificado y guiado de **Evaluaciones** que permita seleccionar en lote empresas con casillas de verificación (checkboxes), diagnosticar su realidad AS-IS completa (contratos, fechas de fin, drivers y modelos operativos) y compararla contra el estándar corporativo propuesto.

## Goals / Non-Goals

**Goals:**
- Proporcionar acceso directo desde la página de Inicio y navegación jerárquica por fases en el Navbar (Exploraciones, Evaluaciones, Implementaciones).
- Implementar la bandeja de gestión de Evaluaciones con soporte para registro, edición en curso y baja auditada.
- Conectar la evaluación con Dominio y Building Block existentes, reflejando en tiempo real sus Capacidades y Funcionalidades maestras sin duplicar datos.
- Proveer selección masiva de subsidiarias (`TEmpresaSubsidiaria`) mediante checkboxes en el formulario, asociando un contacto técnico focal por empresa.
- Capturar el diagnóstico AS-IS por subsidiaria: existencia de tecnología actual, proveedor (Vendor/Partner), contrato vigente con fecha de vencimiento, drivers 1:N y modelo de operación.
- Generar la matriz de convergencia que proyecta la adopción corporativa al vencimiento de los contratos locales vigentes.

**Non-Goals:**
- Modificar el motor de contratos legales externos o almacenar archivos binarios de contratos en base de datos.
- Ejecutar migraciones DDL destructivas sobre la base de datos de producción existente.

## Decisions

### 1. Selección Múltiple con Checkboxes en lugar de Modal Individual
- **Decisión:** Presentar la lista completa de empresas subsidiarias de `TEmpresaSubsidiaria` con casillas de verificación (checkboxes) y selector de contacto focal en una sola pantalla.
- **Razón:** El usuario solicitó expresamente esta modalidad para agilizar la convocatoria masiva de las empresas del grupo en una sola acción, evitando ventanas emergentes repetitivas.
- **Alternativa descartada:** Diálogo modal emergente para convocar una empresa a la vez.

### 2. Separación de Estados: Fase de Proceso vs. Madurez Tecnológica
- **Decisión:**
  - La fase de la solicitud utiliza el catálogo maestro `dbo.TEstadoFaseAdopcion` (Fase 2: `EVALUACION`).
  - La madurez o nivel de la tecnología utiliza `dbo.TMEstadoAdopcionTSI` (`SIN EVALUACION`, `PRE-CALIFICADA`, `CALIFICADA`, `ESTANDARIZADA`, `IMPLEMENTADO`).
- **Razón:** Cumple con la regla arquitectónica de Landscape TSI y con los catálogos físicos autoritativos confirmados por el usuario.

### 3. Consulta en Vivo de Taxonomía sin Clonar Tablas
- **Decisión:** La relación entre la evaluación y las capacidades/funcionalidades se resuelve mediante la clave foránea a `TBuildingBlock` y consultas en vivo a `TCapacidadDeSeguridad` y `TFuncionalidad`.
- **Razón:** Garantiza que cualquier actualización o enriquecimiento futuro en las tablas maestras se propague automáticamente a las evaluaciones sin desfases.

### 4. Proveedores Unificados (Vendor y Partner)
- **Decisión:** Reutilizar la abstracción de Vendor, Partner y contactos para vincularlos a `TTecnologiaTSIimplementadaSubsidiaria` sin crear tablas clonadas.
- **Razón:** Reduce la complejidad del modelo relacional y facilita búsquedas y homologaciones globales entre proveedores locales y globales.

## Risks / Trade-offs

- **[Riesgo: Subsidiaria sin contacto previo registrado]** → *Mitigación:* La interfaz permite seleccionar contactos existentes o ingresar temporalmente los datos del focal técnico local, validando en servidor.
- **[Riesgo: Complejidad del formulario multi-paso]** → *Mitigación:* Se estructura mediante un asistente visual (Wizard) en 4 pasos con navegación fluida, guardado parcial y previsualización de la matriz de convergencia.
