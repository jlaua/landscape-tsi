# Proceso de Adopción TSI, Estándares Corporativos Históricos y Contratos de Subsidiarias

## 1. Propósito y Contexto Funcional

Este documento especifica la evolución del modelo funcional, de dominio y de base de datos de **Landscape TSI** para representar con fidelidad la adopción corporativa de los *Building Blocks*, la fijación de estándares tecnológicos con histórico inmutable y la realidad operativa de las empresas subsidiarias.

### Jerarquía Conceptual Completa

```
Dominio
   └── Building Block <──> Familia
          ├── Capacidad de Seguridad (Agnóstico a tecnologías y empresas)
          │      └── Funcionalidad
          │
          └── Proceso de Adopción TSI
                 ├── Estándar Tecnológico Corporativo (Histórico Inmutable)
                 │      ├── Estándar Principal Vigente
                 │      ├── Estándares Alternativos (Homologados)
                 │      ├── Histórico Reemplazado (Fecha inicio, fin, motivo)
                 │      └── Casos de Uso Vinculados
                 │
                 └── Convocatoria a Empresas Subsidiarias
                        ├── Focal TSI Subsidiaria (TContactoEmpresaSubsidiaria)
                        ├── Declaración de Aplicabilidad (Aplica / No Aplica + Justificación)
                        ├── Estado de Alineamiento (ALINEADO | HOMOLOGADO | NO_ALINEADO | PENDIENTE | NO_APLICA)
                        └── Tecnologías Implementadas por Subsidiaria
                               ├── Vendor y Contactos
                               ├── Modelo de Operación y Modalidad Laboral
                               ├── Contratos y Adendas Jerárquicas (1:N)
                               └── Drivers de Costo (Unidad, Cantidad, Precio Unitario, Costo Total)
```

---

## 2. Modelo Físico SQL Server

Las nuevas tablas y extensiones fueron introducidas de forma aditiva y segura en la base de datos autorizada `db-landscape-tsi-dev-v2`:

### 2.1. Nuevas Tablas de Gobierno y Adopción

| Tabla | Clave Primaria | Propósito | Relaciones Principales |
|---|---|---|---|
| `dbo.TProcesoAdopcionTSI` | `idProcesoAdopcionTSI` (IDENTITY) | Ciclo de vida del proceso de adopción por Building Block | FK `idBuildingBlock` → `TBuildingBlock`, FK `idEstadoAdopcionTSI` → `TMEstadoAdopcionTSI` |
| `dbo.TProcesoAdopcionEmpresa` | `idProcesoAdopcionEmpresa` (IDENTITY) | Convocatoria formal de una empresa subsidiaria a un proceso | FK `idProcesoAdopcionTSI` → `TProcesoAdopcionTSI`, FK `idEmpresaSubsidiaria` → `TEmpresaSubsidiaria`, FK `idContactoEmpresaSubsidiaria` → `TContactoEmpresaSubsidiaria` |
| `dbo.TEstandarTecnologiaHistorico` | `idEstandarTecnologia` (IDENTITY) | Registro histórico e inmutable de tecnologías estándar corporativas | FK `idBuildingBlock` → `TBuildingBlock`, FK `idTecnologiaTSI` → `TTecnologiaTSI`, FK `idProcesoAdopcionTSI` → `TProcesoAdopcionTSI` |
| `dbo.TContratoTecnologia` | `idContratoTecnologia` (IDENTITY) | Contratos marco y adendas jerárquicas (1:N) | FK `idTecnologiaTSIimplementadaSubsidiaria` → `TTecnologiaTSIimplementadaSubsidiaria`, FK `idContratoPadre` (auto-referencial) |

### 2.2. Extensiones No Destructivas en Tablas Existentes

- `dbo.TBuildingBlock`: columna `idFamilia INT NULL` con FK a `dbo.TMFamilia(idFamilia)`.
- `dbo.TTecnologiaTSIimplementadaSubsidiaria`: columnas `idBuildingBlock INT NULL`, `idProcesoAdopcionEmpresa INT NULL`, `esTecnologiaPrimaria BIT NOT NULL DEFAULT 1`.
- `dbo.TDriver`: columnas `unidadMedida NVARCHAR(100) NULL`, `cantidad DECIMAL(18,2) NULL`, `precioUnitario DECIMAL(18,2) NULL`, `moneda NVARCHAR(10) NOT NULL DEFAULT 'USD'`.
- `dbo.TCasosDeUso`: columna `idEstandarTecnologia INT NULL` con FK a `dbo.TEstandarTecnologiaHistorico(idEstandarTecnologia)`.

---

## 3. Reglas de Negocio e Invariantes del Dominio

### 3.1. Transición Histórica de Estándares Corporativos
- Un Building Block o Proceso de Adopción sólo puede tener **un único estándar PRINCIPAL con estado `ACTIVO_VIGENTE`**.
- Al registrar o actualizar un nuevo estándar como `PRINCIPAL`, el estándar principal previo pasa **atómica e inmutablemente** a estado `HISTORICO_REEMPLAZADO`, registrando su `fechaFinVigencia = UTC_NOW` y el `motivoCambio`.
- Se permite la coexistencia de múltiples estándares con rol `ALTERNATIVA` (homologados) en estado `ACTIVO_VIGENTE`.
- Al definir cualquier estándar en `TEstandarTecnologiaHistorico`, el servicio sincroniza automáticamente la pareja `(idBuildingBlock, idTecnologiaTSI)` en la tabla puente preexistente `dbo.TBuildingBlockVsTTecnologiaTSI` para no romper los módulos de consulta legacy (`TechnologyMappingController`).

### 3.2. Contratos y Adendas Jerárquicas (1:N)
- Un contrato principal tiene `esAdenda = false` y `idContratoPadre = NULL`.
- Una adenda tiene `esAdenda = true` y **requiere obligatoriamente un `idContratoPadre` válido** que pertenezca a la misma tecnología implementada. El backend rechaza cualquier adenda sin padre válido.
- La eliminación de un contrato principal elimina en cascada sus adendas vinculadas.

### 3.3. Drivers de Costo
- Múltiples drivers por cada tecnología implementada.
- Cada driver proyecta su costo total mediante la fórmula `cantidad * precioUnitario`.
- El modelo expone el total agregado en la moneda pactada.

### 3.4. Evaluación Automática de Alineamiento
Para cada empresa subsidiaria convocada:
1. Si `aplica = false`: Estado = `NO_APLICA`.
2. Si `aplica = true` y no tiene tecnologías implementadas registradas: Estado = `PENDIENTE`.
3. Si alguna de sus tecnologías implementadas coincide con el Estándar `PRINCIPAL` vigente: Estado = `ALINEADO`.
4. Si no coincide con el principal pero coincide con un Estándar `ALTERNATIVA` vigente: Estado = `HOMOLOGADO`.
5. Si sus tecnologías implementadas no coinciden ni con el principal ni con alternativas: Estado = `NO_ALINEADO`.

---

## 4. Auditoría y Seguridad

- Todas las mutaciones de procesos, estándares, contratos, adendas, drivers y modelos operativos se registran en `dbo.TAuditOperation` mediante `IAuditTrailService`.
- Todas las acciones POST en `AdoptionProcessController` exigen token antiforgery (`@Html.AntiForgeryToken()`) y autorización server-side con la política `Permissions.CatalogEdit`.
- Las consultas de solo lectura exigen `Permissions.CatalogView`.
