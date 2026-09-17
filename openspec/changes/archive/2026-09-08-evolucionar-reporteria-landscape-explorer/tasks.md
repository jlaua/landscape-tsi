## 1. Validación de metadatos y alcance

- [x] 1.1 Ejecutar consultas de solo lectura en `db-landscape-tsi-dev` sobre `sys.foreign_keys`, `sys.foreign_key_columns`, `sys.tables` y `sys.columns` para confirmar las cinco relaciones requeridas y documentar PK, FK, columnas y cardinalidad; verificar que no se ejecuten DDL/DML.
- [x] 1.2 Confirmar específicamente la estructura y cardinalidad de `TBuildingBlockVsTTecnologiaTSI`, y bloquear su uso en esta fase si faltan PK/FK verificables; comprobar el resultado contra los artefactos de diseño.
- [x] 1.3 Alinear `MasterCatalogRegistry` y la whitelist de reportería con los metadatos confirmados sin introducir nombres físicos en las rutas públicas; verificar con pruebas de códigos válidos e inválidos.

## 2. Contratos y capa de lectura

- [x] 2.1 Definir DTOs para KPIs generales, detalle paginado, selección y relación `Dominio -> Building Block`; verificar que no expongan PK técnicas como columnas funcionales.
- [x] 2.2 Implementar consultas asíncronas read-only para detalle de Dominios, conteo de Building Blocks y Building Blocks relacionados, con proyección, parámetros, paginación server-side y `AsNoTracking` cuando aplique; verificar ausencia de N+1 mediante pruebas del servicio.
- [x] 2.3 Implementar validación server-side de catálogo, relación, columna de ordenamiento y alcance organizacional; verificar rechazo de entradas fuera de whitelist.

## 3. API protegida

- [x] 3.1 Exponer endpoints funcionales de catálogo, detalle, KPI y Building Blocks relacionados bajo `/reporteria/api`, manteniendo `CatalogView` y solo lectura; verificar autorización para usuario permitido y denegado.
- [x] 3.2 Mapear estados de error para catálogo inválido, dominio inexistente, dominio sin hijos y fallo de consulta sin revelar SQL ni secretos; verificar respuestas HTTP y DTOs JSON.

## 4. Dashboard y estado de interacción

- [x] 4.1 Ajustar el estado inicial para ocultar la tabla redundante y mostrar «Seleccione una barra para explorar la información.» con alternativa accesible; verificar render inicial.
- [x] 4.2 Implementar selección de `Dominio`, indicador «Analizando: Dominio», grilla funcional con búsqueda, ordenamiento, paginación, selección de fila y total; verificar flujo de selección y estados Loading/Empty/Error.
- [x] 4.3 Implementar breadcrumb `Reportería > Dominio > {NombreDominio}` y navegación de retorno sin recarga completa; verificar conservación y limpieza del estado.
- [x] 4.4 Implementar KPIs generales únicamente para catálogos disponibles, registros totales y relaciones disponibles; verificar que no se muestre «Catálogos activos» sin semántica uniforme.

## 5. Chart.js y relación directa

- [x] 5.1 Implementar `Building Blocks por Dominio` con tooltips, selección visual, alternativa tabular, responsive y estados de carga/vacío/error; verificar valores contra el endpoint.
- [x] 5.2 Gestionar instancias Chart.js con `chart.update()` para cambios de datos y `chart.destroy()` al reemplazar contexto; verificar que no existan múltiples instancias sobre el mismo canvas.
- [x] 5.3 Al seleccionar un Dominio, cargar server-side el KPI directo y la grilla `Building Blocks relacionados con {NombreDominio}`; verificar dominios con y sin hijos.
- [x] 5.4 Ignorar respuestas AJAX obsoletas y cancelar solicitudes cuando sea posible; verificar que una selección nueva no sea sobrescrita por una respuesta antigua.

## 6. Responsive, accesibilidad y pruebas

- [x] 6.1 Aplicar Material Design 3, densidad compacta, foco visible, navegación por teclado y layout desktop/tablet/móvil; verificar con revisión visual y escenarios de teclado.
- [x] 6.2 Agregar pruebas de carga inicial, selección Dominio, detalle, búsqueda, paginación, ordenamiento permitido, código inválido, usuario no autorizado, dominio inexistente, dominio sin Building Blocks, dominio con Building Blocks, KPI y relación padre/hijo.
- [x] 6.3 Compilar la solución y ejecutar todas las pruebas en Release; verificar cero errores y registrar el resultado.
- [x] 6.4 Iniciar localmente en Development, validar `/reporteria` y detenerse para revisión visual; verificar que no se ejecuten migraciones ni escrituras.
