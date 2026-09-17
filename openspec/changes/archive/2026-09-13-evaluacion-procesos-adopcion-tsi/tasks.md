## 1. Interfaz de Usuario y Navegación

- [x] 1.1 Incorporar botón principal "Procesos de Adopción TSI" en `src/Landscape.Tsi.Web/Views/Home/Index.cshtml` a la misma altura del botón "Explorar vista de entidades".
- [x] 1.2 Actualizar la barra superior en `src/Landscape.Tsi.Web/Views/Shared/_Layout.cshtml` para presentar el menú desplegable "Procesos de Adopción TSI" con subopciones "Exploraciones", "Evaluaciones" (activo) e "Implementaciones".

## 2. Modelos, Contratos y Lógica de Negocio

- [x] 2.1 Crear los ViewModels en `src/Landscape.Tsi.Web/Models/EvaluationViewModels.cs` para listado, filtros, creación multi-paso con selección por checkboxes de subsidiarias, diagnóstico AS-IS y baja justificada.
- [x] 2.2 Extender la interfaz `src/Landscape.Tsi.Application/Adoption/IAdoptionProcessService.cs` con métodos para gestión de evaluaciones, convocatoria en lote de subsidiarias y diagnóstico AS-IS.
- [x] 2.3 Implementar en `src/Landscape.Tsi.Infrastructure/Adoption/AdoptionProcessService.cs` la lógica de consulta en vivo de capacidades sin duplicar tablas maestras, convocatoria múltiple con checkboxes, persistencia de contratos, drivers 1:N y baja auditada.

## 3. Vistas y Controlador de Evaluaciones

- [x] 3.1 Implementar acciones en `src/Landscape.Tsi.Web/Controllers/AdoptionProcessController.cs` para listar evaluaciones, crear con wizard, editar en curso, dar de baja y guardar diagnósticos de subsidiarias.
- [x] 3.2 Crear la vista Razor `src/Landscape.Tsi.Web/Views/AdoptionProcess/Evaluations.cshtml` con tabla de evaluaciones, filtros por dominio y estados maestros (`TEstadoFaseAdopcion` y `TMEstadoAdopcionTSI`).
- [x] 3.3 Crear la vista Razor `src/Landscape.Tsi.Web/Views/AdoptionProcess/CreateEvaluation.cshtml` con el wizard de 4 pasos (Dominio/BB con capacidades en vivo, selección masiva de subsidiarias con checkboxes, diagnóstico AS-IS con contrato/drivers y estándar corporativo).
- [x] 3.4 Crear la vista Razor `src/Landscape.Tsi.Web/Views/AdoptionProcess/EditEvaluation.cshtml` para actualizar evaluaciones en curso y consultar la matriz de convergencia.

## 4. Pruebas y Verificación

- [x] 4.1 Implementar pruebas unitarias y de integración en `tests/Landscape.Tsi.Tests/Adoption/AdoptionEvaluationTests.cs` validando el ciclo completo de evaluación, convocatoria masiva por checkboxes y diagnóstico AS-IS.
- [x] 4.2 Ejecutar `dotnet test --configuration Release` asegurando que todas las pruebas de la solución superen con 0 errores.
- [x] 4.3 Ejecutar `dotnet format --verify-no-changes` y verificar la integridad del código.
