# Análisis del modelo del Catálogo Landscape TSI

**Estado:** borrador para validación funcional  
**Fuente:** `00 - definicion de landscape tsi/Modelado Catalogo Landscape TSI - Flujos de trabajo.drawio`  
**Vista de referencia:** [diagrama JPG](../00%20-%20definicion%20de%20landscape%20tsi/Modelado%20Catalogo%20Landscape%20TSI%20-%20Flujos%20de%20trabajo-MODELADO-CATALOGO.jpg)  
**Corrección incorporada:** el catálogo de estado de capacidad está identificado como `M:EstadoCapacidad`.

## 1. Propósito y alcance

El modelo busca soportar un catálogo corporativo de Tecnologías de Seguridad de la Información (TSI) y el seguimiento de su adopción en empresas subsidiarias. Integra cuatro perspectivas:

1. **Taxonomía de seguridad:** dominio, building block, capacidad y feature.
2. **Catálogo tecnológico:** tecnología TSI, alternativas, estado de adopción, postura de roadmap, casos de uso, fabricantes y contactos.
3. **Adopción por subsidiaria:** tecnología implementada, contratos, negociación, adjudicación, dimensionamiento y modelo operativo.
4. **Gobierno y consulta:** responsables organizacionales y dashboards de catálogo y evolución de adopción.

La fuente es un modelo conceptual en construcción. No define tipos de datos, obligatoriedad, unicidad, reglas de borrado ni todas las cardinalidades de forma inequívoca. Este documento distingue entre lo representado y las recomendaciones de diseño.

## 2. Lectura ejecutiva

La entidad central es **Tecnología TSI**. Cada tecnología se clasifica bajo un **Building Block**, que pertenece a un **Dominio**. En paralelo, el building block agrupa **Capacidades de Seguridad**, y estas se detallan mediante **Features**.

La adopción se registra mediante **Tecnología TSI Implementada Subsidiaria**, entidad asociativa que representa una implementación concreta en una empresa. Esta separación es acertada: permite mantener una definición corporativa única de la tecnología y registrar condiciones locales como fabricante, contrato, fechas y dimensionamiento.

El modelo cubre adecuadamente la intención funcional, pero antes de transformarlo en un esquema físico requiere resolver cinco asuntos principales:

- cardinalidades inconsistentes alrededor de subsidiarias e implementaciones;
- duplicidad entre campos de texto y entidades de referencia (vendor, contactos y modelo de operación);
- atributos multivaluados o alternativas embebidas en una sola entidad;
- catálogos maestros con nombres inconsistentes;
- ausencia de entidades explícitas para cobertura tecnología-capacidad/feature y para el historial de adopción.

## 3. Contextos funcionales y responsables

| Contexto | Responsable mostrado | Contenido principal |
|---|---|---|
| Arquitectura de Seguridad Corporativa | `ARQ. SEG. CORPORATIVO` | Dominio, building block, capacidad, feature y estados asociados. |
| Ingeniería de Seguridad Corporativa | `ING. SEG. CORPORATIVO` | Tecnología TSI, roadmap, adopción, casos de uso, vendors y familia. |
| Gobierno de Contratos | `SPOC - GOB. DE CONTRATOS` | Implementación por subsidiaria, contrato, negociación, adjudicación, drivers y modelo operativo. |
| Focal de subsidiaria | `SPOC - FOCAL` | Empresa, regulación, CISO y contactos. |
| Gobierno corporativo | `EQ. DE GOB. CORPORATIVO ???` | Consumidor o propietario propuesto de los dashboards; el responsable está pendiente de confirmación. |

Los límites representan responsabilidad funcional, no necesariamente separación en bases de datos o servicios.

## 4. Modelo conceptual

### 4.1. Cadena taxonómica

```text
Dominio 1 ── N Building Block 1 ── N Capacidad de Seguridad 1 ── N Feature
                         │
                         └── 1 ── N Tecnología TSI
```

Esta jerarquía permite navegar desde una dimensión general de seguridad hasta funcionalidades concretas y tecnologías relacionadas. Sin embargo, el modelo actual vincula una tecnología a un solo building block. Si una tecnología cubre varios building blocks, capacidades o features, será necesaria una relación muchos-a-muchos de cobertura.

### 4.2. Catálogo tecnológico

**Tecnología TSI** conserva los datos corporativos de una plataforma o producto: nombres alternativos corporativo/local, ola de evaluación, grupo, estado de adopción, postura de roadmap, fechas corporativas, licenciamiento, entorno, fuente y responsables.

Sus entidades relacionadas son:

- **Caso de Uso:** usos documentados para una tecnología.
- **Vendor:** proveedor o fabricante relacionado con la tecnología.
- **Contacto Vendor** y **Contacto Partner:** personas de contacto del proveedor.
- **Familia:** clasificación adicional cuya relación exacta no resulta inequívoca en el diagrama.
- **Estado de Adopción TSI** y **Postura Roadmap:** catálogos maestros.

### 4.3. Implementación local

**Tecnología TSI Implementada Subsidiaria** materializa la adopción de una tecnología corporativa por una empresa. Contiene la empresa, tecnología de catálogo, denominación implementada, fabricante/partner, contacto, modelo de dimensionamiento y fechas del ciclo contractual.

Se complementa con:

- **Driver de Dimensionamiento:** métrica, valor y frecuencia de cálculo de cada driver.
- **Modelo de Operación:** condiciones operativas, RACI, actividades, SLA/SLO, tiempos de atención, penalidades y comentarios.
- **Tipo de Operación** y **Modalidad Laboral:** catálogos del modelo operativo.

### 4.4. Subsidiarias y gobierno local

**Empresa Subsidiaria** registra nombre, alias, agrupador, país, rubro y focal. Se relaciona con:

- **Regulación Aplicable**;
- **CISO**;
- **Contacto Empresa Subsidiaria**;
- implementaciones de tecnologías TSI.

## 5. Diccionario preliminar de entidades

### 5.1. Taxonomía de seguridad

| Entidad | Propósito | Atributos representados |
|---|---|---|
| Dominio | Nivel superior de clasificación. | `idDominio`, `nombreDominio`, `descripcionDominio`, `referencias`, homologaciones, `subDominioCVT`, `ejemplos`. |
| Building Block | Agrupación arquitectónica dentro del dominio. | `idBuildingBlock`, `idDominio`, nombre, definición, estado de fase de adopción, ruta del entregable, pilar ZT. |
| Capacidad de Seguridad | Capacidad ofrecida o requerida. | `idCapacidad`, `idBuildingBlock`, nombre, descripción, estado. |
| Feature | Funcionalidad que concreta una capacidad. | `idFuncionalidad`, `idCapacidad`, descripción, estado de cobertura. Falta un nombre explícito. |
| Estado Fase Adopción | Catálogo del estado del building block. | Identificador, nombre y descripción. |
| Estado Capacidad | Catálogo del estado de la capacidad. | `M:EstadoCapacidad`; identificador, nombre y descripción. |
| Estado Funcionalidad | Catálogo de cobertura de features. | Identificador, nombre y descripción. |

### 5.2. Tecnología y proveedores

| Entidad | Propósito | Atributos representados |
|---|---|---|
| Tecnología TSI | Registro corporativo de una tecnología. | Identificador, building block, nombres alternativos, ola, grupo, adopción, roadmap, fechas, licenciamiento, entorno, fuente, responsables, categoría AS-IS y flag de fuente. |
| Caso de Uso | Uso funcional de la tecnología. | Identificador, tecnología, nombre/caso y descripción. |
| Vendor | Proveedor asociado a la tecnología. | Identificador, tecnología, nombre y descripción. |
| Contacto Vendor | Contacto del fabricante. | Identificador, vendor, nombre, correo, teléfono y otro. |
| Contacto Partner | Contacto de un partner del vendor. | Identificador, vendor, nombre, correo, teléfono y otro. |
| Familia | Clasificación de tecnología. | Identificador, nombre y descripción. |
| Estado Adopción TSI | Estado maestro de adopción. | Identificador, nombre y descripción. |
| Postura Roadmap | Posición resumida del roadmap. | Identificador, nombre y descripción. |

### 5.3. Subsidiaria, implementación y operación

| Entidad | Propósito | Atributos representados |
|---|---|---|
| Empresa Subsidiaria | Organización que adopta tecnologías. | Identificador, nombre, alias, agrupador, país, rubro y focal. |
| Tecnología TSI Implementada Subsidiaria | Instancia local de una tecnología del catálogo. | Identificadores de implementación, empresa y tecnología; fabricante, nombre implementado, contacto, dimensionamiento y fechas contractuales. |
| Driver Dimensionamiento | Variable usada para dimensionar una implementación. | Identificador, implementación, nombre, unidad, valor, frecuencia y descripción. |
| Modelo de Operación | Características operativas de la implementación. | Identificador, modalidad, licencias, actividades, RACI, SLA/SLO, tiempos, penalidades y comentarios. |
| Tipo Operación | Catálogo de tipo operativo. | Identificador y tipo. |
| Modalidad Laboral | Catálogo de modalidad de trabajo. | Identificador y tipo. |
| Regulación Aplicable | Norma aplicable a una subsidiaria. | Identificador, empresa, nombre y descripción de aplicabilidad. |
| CISO | Responsable de seguridad de una subsidiaria. | Identificador, empresa, nombre, correo, teléfono y otro. |
| Contacto Empresa Subsidiaria | Contacto o focal local. | Identificador, empresa, nombre, rol, correo, teléfono y otro. |

## 6. Relaciones y cardinalidades

### 6.1. Relaciones claras o respaldadas por claves dibujadas

| Principal | Dependiente | Cardinalidad esperada | Evidencia |
|---|---|---:|---|
| Dominio | Building Block | 1:N | `idDominio` en Building Block. |
| Building Block | Capacidad | 1:N | `idBuildingBlock` en Capacidad. |
| Capacidad | Feature | 1:N | `idCapacidad` en Feature. |
| Building Block | Tecnología TSI | 1:N | `idBuildingBlock` en Tecnología TSI. |
| Tecnología TSI | Caso de Uso | 1:N | `idTecnologiaTSI` en Caso de Uso. |
| Tecnología TSI | Implementación Subsidiaria | 1:0..N | `idTecnologiaTSI` en la implementación. |
| Empresa Subsidiaria | Implementación Subsidiaria | 1:0..N | `idEmpresaSubsidiaria` en la implementación. |
| Implementación Subsidiaria | Driver Dimensionamiento | 1:N | Identificador de implementación en Driver. |
| Empresa Subsidiaria | Regulación Aplicable | 1:N | `idEmpresaSubsidiaria` en Regulación. |
| Empresa Subsidiaria | CISO | 1:N según dibujo | `idEmpresaSubsidiaria` en CISO; validar vigencia temporal. |
| Empresa Subsidiaria | Contacto | 1:N | `idEmpresaSubsidiaria` en Contacto. |
| Vendor | Contacto Vendor | 1:N | `idVendor` en Contacto Vendor. |
| Vendor | Contacto Partner | 1:N | `idVendor` en Contacto Partner. |

### 6.2. Relaciones pendientes de confirmación

- El dibujo muestra **Implementación–Empresa** como `1:1`, pero las claves sugieren que una empresa puede tener muchas implementaciones y cada implementación pertenece a una empresa.
- La relación **Tecnología–Vendor** parece `1:N`. En la práctica puede ser `N:M`: una tecnología puede tener fabricante, distribuidor y partners, y un vendor puede participar en varias tecnologías.
- **Modelo de Operación** parece relacionado con la implementación, pero no incluye claramente la clave foránea de esta. Debe decidirse si la relación es `1:1`, `1:N` con vigencia o compartida entre implementaciones.
- Las relaciones de **Tipo de Operación** y **Modalidad Laboral** se dibujan como `1:1`; normalmente serían catálogos `1:N` hacia Modelo de Operación.
- La relación de **Familia** con Tecnología TSI no queda expresada de forma consistente: la tecnología debería incluir `idFamilia` o existir una tabla asociativa.
- El diagrama muestra `0:N` entre tecnología e implementación, pero debe formalizarse como una tecnología con cero o muchas implementaciones; cada implementación debe referir exactamente una tecnología del catálogo.

## 7. Reglas de negocio inferidas para validar

Estas reglas son propuestas derivadas del modelo, no definiciones aprobadas:

1. Una implementación no puede existir sin empresa subsidiaria y tecnología TSI de catálogo.
2. Una tecnología puede existir en el catálogo aunque todavía no esté implementada.
3. La fecha de inicio de negociación no debería ser posterior a la fecha de adjudicación; la adjudicación no debería ser posterior al fin de contrato, salvo renovaciones o ciclos separados.
4. Los estados de adopción, capacidad, feature y roadmap deben provenir de catálogos controlados y no de texto libre.
5. Los enlaces de fuente deben conservar trazabilidad, fecha de consulta y propietario del dato.
6. Un driver de dimensionamiento debe declarar unidad, valor y frecuencia compatibles.
7. Los contactos y CISO deberían admitir vigencia (`fechaInicio`, `fechaFin`) para conservar historia.
8. País, rubro, pilar ZT, entorno, modalidad de licenciamiento y unidad responsable son candidatos a catálogos maestros.

## 8. Hallazgos de calidad y riesgos

### Alta prioridad

- **Cobertura no modelada:** el dashboard solicita comparar capacidades y features, pero no existe una entidad que indique qué capacidad o feature cubre cada tecnología y con qué nivel.
- **Historial insuficiente:** los estados y fechas se sobrescriben; no se puede reconstruir la evolución de adopción que pide el segundo dashboard.
- **Cardinalidad de subsidiaria incorrecta:** el `1:1` dibujado impediría múltiples tecnologías por empresa.
- **Redundancia de proveedor:** la implementación almacena fabricante y contacto como texto mientras existen Vendor y contactos como entidades.
- **Alternativas embebidas:** los nombres “alternativa 1 corporativo” y “alternativa 2 local” no escalan y mezclan identidad, clasificación y alcance.

### Prioridad media

- Las entidades maestras usan el prefijo `M:` de forma irregular; la nueva revisión ya diferencia correctamente `M:EstadoFaseAdopcion` de `M:EstadoCapacidad`.
- Hay mezcla de idioma, espacios en nombres, abreviaturas y ortografía variable (`Vendor`, `Feature`, `Cantidqad`, `Subscripcion`, `Grupo q Pertenece`).
- No se especifican tipos, longitudes, campos obligatorios, claves únicas ni reglas de auditoría.
- `Contacto/Focal` en Empresa duplica la entidad Contacto Empresa Subsidiaria.
- `ModeloDimensionamiento` figura como atributo y además existen drivers, pero no una cabecera formal del modelo de dimensionamiento.
- Modelo de Operación combina identidad, configuración, métricas y acuerdos de servicio; podría requerir descomposición y vigencia.

### Seguridad y privacidad

- Correos, teléfonos y nombres de responsables son datos personales corporativos; requieren control de acceso y política de retención.
- Las fuentes, contratos, fechas de negociación y penalidades pueden ser información confidencial.
- La aplicación debería registrar quién crea, modifica y aprueba cada registro, sin guardar secretos o credenciales en la base ni en documentación.

## 9. Propuesta de normalización

### Cambios mínimos para un MVP

1. Normalizar nombres en singular y `PascalCase` para entidades; usar `camelCase` o `snake_case` de forma uniforme en persistencia.
2. Corregir la relación Empresa Subsidiaria `1:N` Implementación.
3. Agregar `idFamilia` a Tecnología TSI o una tabla `TecnologiaFamilia` si admite varias familias.
4. Agregar `idImplementacion` a Modelo de Operación y definir su unicidad/vigencia.
5. Sustituir fabricante y contacto de texto por referencias a una relación `TecnologiaProveedor` o `ImplementacionProveedor`, conservando un campo de observación cuando sea necesario.
6. Crear `TecnologiaCobertura` con tecnología, capacidad/feature, nivel, evidencia, fuente y vigencia.
7. Crear `HistorialAdopcionTecnologia` para empresa/tecnología, estado, fecha efectiva, comentario y responsable.
8. Añadir auditoría común: `creadoEn`, `creadoPor`, `actualizadoEn`, `actualizadoPor`, y opcionalmente `eliminadoEn`.

### Estructuras recomendadas

| Nueva estructura | Finalidad |
|---|---|
| `TecnologiaNombre` | Manejar nombres corporativos, locales, alias y alternativas con tipo, alcance y vigencia. |
| `TecnologiaCobertura` | Relacionar tecnología con capacidad o feature y sustentar el dashboard de cobertura. |
| `HistorialAdopcionTecnologia` | Registrar transiciones de estado y calcular evolución temporal. |
| `TecnologiaProveedor` | Resolver la relación N:M y distinguir fabricante, vendor, partner o distribuidor. |
| `ModeloDimensionamiento` | Cabecera versionada de drivers y supuestos por implementación. |
| `ContactoOrganizacion` | Unificar contactos, rol, tipo, organización y vigencia, si el alcance permite una abstracción común. |

## 10. Dashboards y métricas

### Catálogo de Tecnologías TSI

El modelo pretende habilitar:

- búsqueda de cobertura por dominio y capacidad;
- búsqueda de plataformas por estado de adopción y postura de roadmap;
- comparación de cobertura de capacidades frente a features.

Para que sea implementable se requiere definir el concepto de **cobertura**: binaria, porcentual, por nivel (`no cubre`, `parcial`, `total`) o basada en evidencia. También debe aclararse si la cobertura corresponde a la tecnología corporativa o a una implementación concreta.

### Evolución de adopción/implementación

Indicadores dibujados:

- porcentaje de adopción/implementación;
- cantidad de subsidiarias pendientes.

Definición sugerida, pendiente de aprobación:

```text
% adopción = subsidiarias elegibles con implementación en estado objetivo
             ------------------------------------------------------------ × 100
                         total de subsidiarias elegibles
```

Se deben definir el estado objetivo, la población elegible, la fecha de corte y el tratamiento de excepciones. Sin historial de estados, solo podrá obtenerse una fotografía actual, no una evolución.

## 11. Preguntas abiertas

1. ¿Quién es el propietario confirmado de cada dashboard en lugar de `EQ. DE GOB. CORPORATIVO ???`?
2. ¿Una tecnología puede cubrir múltiples building blocks, capacidades y features?
3. ¿“alternativa corporativa” y “alternativa local” son nombres, productos diferentes o niveles de preferencia?
4. ¿Una subsidiaria puede implementar más de una instancia o versión de la misma tecnología?
5. ¿Cuál es el catálogo oficial de estados y cuáles son sus transiciones permitidas?
6. ¿Qué significa `OLA Evaluación`, `Grupo q Pertenece`, `Categoría AS-IS` y `Flag Fuente`?
7. ¿Vendor, fabricante y partner son roles de una misma organización o conceptos separados?
8. ¿El modelo de operación es único por implementación, versionado en el tiempo o reutilizable?
9. ¿CISO debe ser uno vigente por subsidiaria o se necesita historial de titulares?
10. ¿Cómo se calcula exactamente cobertura, adopción e implementación?
11. ¿Qué representan `subDominioCVT`, las homologaciones y `Pilar ZT`, y cuáles son sus catálogos de referencia?
12. ¿Se conservarán renovaciones contractuales como registros separados o como fechas sobrescritas?

## 12. Criterios de preparación para diseño físico

El modelo estará listo para traducirse a SQL/EF Core cuando se hayan aprobado:

- glosario y nombres canónicos;
- claves primarias, foráneas, cardinalidades y obligatoriedad;
- catálogos maestros y transiciones de estado;
- definición de cobertura y adopción;
- reglas de vigencia e historial;
- tipos de datos, restricciones únicas e índices;
- clasificación de datos y permisos por rol;
- reglas de auditoría, retención y eliminación.

## 13. Conclusión

El diseño constituye una buena base conceptual: separa el catálogo corporativo de las implementaciones locales y contempla taxonomía, adopción, operación y gobierno. Su principal brecha no es la cantidad de entidades, sino la falta de relaciones e historial necesarios para responder de manera confiable las preguntas de cobertura y evolución. La siguiente iteración debería priorizar esas definiciones y corregir las cardinalidades antes de generar el modelo físico o iniciar la implementación .NET.
