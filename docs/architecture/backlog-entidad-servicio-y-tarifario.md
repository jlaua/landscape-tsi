# [BACKLOG] Entidad Servicio y Tarifario por Rango de Horas

## 1. Contexto y Justificación de Negocio

En los procesos de adopción y evaluación tecnológica TSI (`/evaluaciones/{id}`), el diagnóstico integral y dimensionamiento económico de una tecnología implementada en una subsidiaria requiere proyectar no solo el costo de licenciamiento y hardware (`TContratoTecnologia` y `TDriver`), sino también el **costo de servicios profesionales de mano de obra**.

Este requerimiento queda documentado en el **Backlog** para su análisis, diseño de datos e implementación en una iteración posterior.

---

## 2. Requerimientos Funcionales

### 2.1. Tipos de Servicio
Cada subsidiaria, para una tecnología implementada o en proceso de adopción, podrá registrar servicios clasificados en:

1. **Implementación**:
   - Actividades de despliegue inicial, instalación, parametrización, integración técnica y pruebas de aceptación operativa.
2. **Migración**:
   - Actividades de transición técnica, migración de políticas, reglas, identidades, datos y coexistencia desde la solución AS-IS saliente hacia la tecnología corporativa estándar.
3. **Operación**:
   - Servicios recurrentes de administración técnica, monitoreo, soporte de segundo/tercer nivel y mantenimiento evolutivo.

### 2.2. Tarifario por Servicio (Tipo Driver de Mano de Obra)
- Cada tipo de servicio contará con un **tarifario estructurado por rango de horas** o perfiles de esfuerzo.
- El tarifario permitirá calcular el costo proyectado de la mano de obra en función de la cantidad de horas requeridas y la tarifa horaria correspondiente al rango:
  $$\text{Costo Total del Servicio} = \sum (\text{Horas Estimadas en Rango} \times \text{Tarifa Horaria del Rango})$$
- Moneda configurable (USD / Moneda Local) con trazabilidad al proveedor de servicios (Vendor o Partner local adjudicado).

---

## 3. Propuesta de Arquitectura y Modelo de Datos (Para Revisión Futura)

```
[TTecnologiaTSIimplementadaSubsidiaria]
       │ 1
       │
       ▼ N
[TServicioTecnologia] (Transaccional)
   ├── idTipoServicio ──> [TTipoServicio] (Implementación | Migración | Operación)
   ├── idVendor / idPartner (Proveedor del Servicio)
   ├── HorasTotalesEstimadas
   ├── Moneda
   └── CostoTotalServicio
           │ 1
           ▼ N
   [TTarifarioServicioRango] (Estructura de Rango de Horas)
      ├── RangoHorasDesde (ej. 1)
      ├── RangoHorasHasta (ej. 100)
      ├── TarifaHora
      └── Observaciones / Perfil
```

---

## 4. Estado

- **Estado:** `REGISTRADO_EN_BACKLOG`
- **Prioridad:** Media / Diferido a siguiente fase de costos y estimaciones.
- **Próximos Pasos:**
  - Validar si el tarifario es corporativo o específico por subsidiaria/proveedor.
  - Diseñar el flujo de entrada de datos en la UI de la ficha detallada (`Details.cshtml`) a la altura de `Contratos` y `Drivers de Costo`.
