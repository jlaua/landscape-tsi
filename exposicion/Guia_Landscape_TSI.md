# Landscape TSI: experiencia de desarrollo con IA

Material de apoyo para Jean y los participantes. Preparado el 12 de septiembre de 2026.

## 1. Cómo presentar la experiencia

La lámina PowerPoint contiene una síntesis para exponer. Esta guía amplía los conceptos y propone un laboratorio reproducible. Tiempo sugerido: 5 minutos para la lámina y 20–30 minutos para la demostración, con herramientas previamente instaladas.

**Mensaje central:** el desarrollador sigue siendo responsable de las reglas del negocio, del modelo de datos y de comprobar el resultado. La IA ayuda a elaborar propuestas y a ejecutar cambios.

### Qué pertenece a la experiencia y qué es una propuesta

| Elemento | Base disponible para la exposición |
|---|---|
| Landscape TSI | Aplicación web .NET con SQL Server para gestionar la adopción de tecnologías de seguridad en varias empresas y con distintos roles. |
| Entorno y asistentes | El historial compartido recoge Visual Studio, consultas sobre VS Code, Codex, Cursor y el trabajo posterior con Antigravity. |
| ChatGPT como apoyo | Se utilizó la conversación para concretar funcionalidades y estructurar instrucciones destinadas al agente de código. |
| Continuidad | Se mencionaron archivos Markdown con instrucciones y actividades pendientes para retomar el proyecto con otro asistente. |
| Modelo funcional | Se planteó asociar empresas a cada Building Block y registrar las tecnologías que cada empresa utiliza. Su implementación completa no se acredita aquí. |
| Desarrollo y datos | El historial recoge separación entre desarrollo y producción, autenticación local y trabajo sobre catálogos y vistas operativas. No se ha auditado el repositorio actual. |
| npm y GitHub | Se incluyen como guía reproducible. No se dispone del registro exacto de instalaciones, versiones, commits o integración que Jean realizó. |
| OpenSpec y SDD | Propuesta para formalizar el proceso. La información disponible no confirma que OpenSpec ya esté instalado o aplicado en Landscape TSI. |
| Productividad y calidad | No hay mediciones comparables de horas ahorradas o defectos evitados. No presentar porcentajes inventados. |

## 2. Conceptos que conviene diferenciar

**Vibe Coding:** trabajar describiendo una intención en lenguaje natural, probar el resultado generado por IA y pedir ajustes. En su acepción estricta implica delegar gran parte de la comprensión y revisión del código. En una aplicación con permisos y datos corporativos, la experiencia se describe con más precisión como desarrollo asistido por IA cuando existe revisión técnica. La conversación sirve para explorar, pero las reglas importantes deben quedar documentadas.

**SDD, Specification Driven Development:** desarrollo guiado por especificaciones. Primero se expresa el comportamiento esperado con reglas y criterios verificables. Esas especificaciones orientan el diseño, las tareas y la implementación. Al probar se comprueba si el resultado satisface lo acordado. Si cambia el requisito, se actualiza también su especificación. SDD es un enfoque que puede aplicarse con diferentes herramientas. [GitHub Spec Kit](https://github.com/github/spec-kit).

**OpenSpec:** framework abierto que organiza especificaciones y cambios dentro del repositorio. El agente de IA produce o modifica código usando esa estructura. [OpenSpec](https://github.com/Fission-AI/OpenSpec).

**Un framework SDD** aporta convenciones, documentos y un flujo de trabajo. En esta conversación, la palabra framework se refiere a la organización del desarrollo con IA. ASP.NET Core cumple otro papel: es el framework de ejecución de la aplicación web.

## 3. Comparativo de frameworks y entornos

La clasificación distingue licencia y función. Un proyecto mantenido por una empresa puede ser abierto. Instalar una herramienta abierta no elimina los posibles cargos del proveedor de IA. El ajuste al caso Landscape TSI es una valoración para este taller, no un benchmark.

| Opción | Naturaleza y función | Cómo estructura el trabajo | Encaje en Landscape TSI | Consideración |
|---|---|---|---|---|
| OpenSpec | Framework abierto, MIT | Cambios con propuesta, especificaciones, diseño y tareas | Incorporar funcionalidades sobre la aplicación existente | Exige mantener las especificaciones al día |
| GitHub Spec Kit | Toolkit abierto, MIT, impulsado por GitHub | Principios del proyecto, especificación, plan, tareas e implementación | Estandarizar el inicio y desarrollo de funcionalidades | Requiere adoptar sus convenciones y preparación |
| BMAD Method | Método y herramientas abiertos | Roles especializados y flujos para distintas actividades del desarrollo | Ampliar el trabajo de análisis, arquitectura y entrega | Mayor alcance metodológico que una lista de tareas |
| Kiro | Producto propietario con soporte SDD | Requisitos, diseño y tareas integrados en el entorno | Taller con experiencia integrada para trabajar con specs | Disponibilidad y uso dependen del producto y del plan |
| Cursor | Editor comercial con agente | El agente trabaja sobre el repositorio y su contexto | Edición y ejecución asistidas, con reglas del proyecto | Necesita una disciplina de especificación acordada |
| Google Antigravity | Plataforma propietaria de desarrollo con agentes | Planes y artefactos para comunicar y revisar el trabajo | Continuar tareas con contexto y revisión de resultados | Sus artefactos deben conciliarse con el repositorio |
| Codex | Agente de programación, con CLI abierta y servicios de OpenAI | Lee archivos, modifica código y ejecuta comprobaciones | Implementar las tareas de Landscape TSI | La apertura del cliente y el acceso al servicio son asuntos distintos |

Fuentes por opción: [OpenSpec](https://github.com/Fission-AI/OpenSpec), [Spec Kit](https://github.com/github/spec-kit), [BMAD](https://github.com/bmad-code-org/BMAD-METHOD), [Kiro Specs](https://kiro.dev/docs/specs/), [Cursor Agent](https://cursor.com/docs/agent/overview), [tutorial oficial de Antigravity](https://codelabs.developers.google.com/getting-started-google-antigravity) y [Codex CLI](https://developers.openai.com/codex/cli/).

**Elección propuesta:** probar OpenSpec con una sola funcionalidad de Landscape TSI, conservando el entorno de desarrollo. Permite evaluar el valor de la especificación sin cambiar simultáneamente editor, base de datos y proceso.

## 4. Qué organiza OpenSpec

| Archivo o directorio | Contenido | Ejemplo para el taller |
|---|---|---|
| `openspec/specs/` | Comportamiento vigente documentado | Reglas actuales de catálogos y autorización |
| `openspec/changes/<cambio>/proposal.md` | Motivo y alcance | Asociar empresas a un Building Block |
| `openspec/changes/<cambio>/specs/` | Requisitos añadidos, modificados o eliminados | Impedir asociaciones duplicadas |
| `openspec/changes/<cambio>/design.md` | Decisiones técnicas | Relación transaccional y validación en servidor |
| `openspec/changes/<cambio>/tasks.md` | Trabajo que debe ejecutarse | Datos, servicio, formulario y pruebas |
| `openspec/changes/archive/` | Cambios archivados | Historial del cambio terminado |

La propuesta explica el problema. La especificación describe el resultado observable. El diseño elige una solución y las tareas la convierten en trabajo concreto. Es posible revisar documentos anteriores al descubrir nueva información. Al completar un cambio, se sincronizan sus especificaciones con las vigentes y se archiva el trabajo. [Primeros pasos](https://github.com/Fission-AI/OpenSpec/blob/main/docs/getting-started.md) y [conceptos](https://github.com/Fission-AI/OpenSpec/blob/main/docs/concepts.md).

En SDD, marcar una tarea como completada requiere evidencia: por ejemplo, que se rechace una asociación duplicada. La existencia de `tasks.md` por sí sola no demuestra calidad ni cumplimiento.

## 5. Preparación del equipo

Estas son recomendaciones prácticas para el laboratorio, no las especificaciones verificadas del equipo de Jean ni mínimos oficiales de todos los fabricantes.

| Componente | Preparación propuesta |
|---|---|
| Sistema | Windows 11, PowerShell y permisos para instalar las herramientas |
| Memoria | 16 GB como punto de partida práctico. 32 GB si se ejecutan SQL Server, IDE y otros servicios simultáneamente |
| Almacenamiento | SSD y margen para SDK, paquetes y una copia de la base. Reservar aproximadamente 30–50 GB para un laboratorio pequeño, ajustando por el tamaño real de los datos |
| .NET | SDK compatible con los `.csproj` y `global.json`. No cambiar la versión del proyecto durante el ejercicio |
| IDE | Visual Studio con la carga de desarrollo web ASP.NET, o VS Code con soporte C# y terminal |
| SQL | Instancia de desarrollo accesible y SSMS. Usuario propio para la aplicación y datos de demostración |
| Node.js y npm | Versión LTS soportada que cumpla los requisitos de los CLI. OpenSpec documenta Node.js 20.19.0 o superior |
| Git y GitHub | Git instalado y una cuenta con acceso al repositorio utilizado en el taller |
| IA | Cuenta habilitada para Codex u otro agente. Comprobar acceso antes de la exposición |
| GPU | El flujo propuesto usa modelos remotos y no requiere ejecutar un modelo local |

El requisito de Node corresponde a [OpenSpec](https://github.com/Fission-AI/OpenSpec). Los paquetes de la aplicación .NET se administran con NuGet. npm se usa aquí para herramientas auxiliares y, si el proyecto los incluye, paquetes web.

### Paso 1. Verificar herramientas

Ejecutar en PowerShell:

```powershell
dotnet --info
node --version
npm --version
git --version
```

Anotar versiones y comprobar el SDK requerido por el proyecto antes de restaurar paquetes.

### Paso 2. Preparar el repositorio

Para un participante que todavía no tenga copia local, sustituir la URL de ejemplo:

```powershell
git clone https://github.com/ORGANIZACION/REPOSITORIO.git
cd REPOSITORIO
git status
git switch -c taller/asignacion-empresas-bb
```

Si ya tiene el repositorio, abrir su carpeta y revisar `git status` antes de crear la rama. No volver a clonar encima de una copia existente.

Revisar `.gitignore` antes del primer commit. Código, especificaciones y scripts reproducibles pertenecen al control de versiones. Las credenciales y las copias privadas de la base de datos deben quedar fuera. La rama permite revisar el cambio mediante un pull request. [Flujo básico de GitHub](https://docs.github.com/en/get-started/using-github/hello-world).

### Paso 3. Instalar las herramientas auxiliares

```powershell
npm install -g @openai/codex
npm install -g @fission-ai/openspec@latest
codex --version
openspec --version
```

Abrir Codex desde la carpeta del proyecto y completar el inicio de sesión según el método disponible en la cuenta. Registrar las versiones instaladas para repetir el taller. El CLI admite trabajar con archivos locales y ejecutar comandos. Consultar la [instalación oficial de Codex](https://developers.openai.com/codex/cli/) y la [instalación de OpenSpec](https://github.com/Fission-AI/OpenSpec).

Usar ChatGPT para preparar el prompt y usar Codex para modificar el repositorio son actividades distintas. Este ejercicio no requiere integrar la API de OpenAI dentro de Landscape TSI.

### Paso 4. Levantar la aplicación sin cambios funcionales

Identificar el proyecto web y la solución reales. En una solución con varios proyectos, proporcionar la ruta correspondiente al comando:

```powershell
dotnet restore
dotnet build
```

Configurar la conexión a la base de desarrollo y ejecutar el proyecto web desde Visual Studio. Verificar que el inicio de sesión y una consulta de catálogo funcionen antes de introducir la nueva funcionalidad.

Guardar secretos de desarrollo mediante User Secrets o el mecanismo local acordado. User Secrets mantiene los valores fuera del repositorio, pero no los cifra. En el despliegue usar la configuración segura del entorno. La captura original de conexión no forma parte de los entregables porque contiene credenciales. [Secretos de desarrollo en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0).

### Paso 5. Dejar instrucciones duraderas

Propuesta de contenido para `AGENTS.md`:

```markdown
# Instrucciones para Landscape TSI

## Contexto
Aplicación .NET y SQL Server para adopción de tecnologías de seguridad.
Inspeccionar la solución y el esquema existente antes de proponer cambios.

## Reglas
- Trabajar en el ambiente de desarrollo.
- Mantener autorización en servidor y segregación por empresa.
- Los catálogos maestros corresponden al Administrador del Sistema
  y al Arquitecto de Seguridad Corporativo.
- Las vistas operativas se actualizan mediante sus tablas fuente.
- No incluir credenciales en código, prompts o documentos.
- Identificar impactos de datos antes de sustituir tablas existentes.

## Entrega
Explicar cambios, pruebas ejecutadas, resultados y pendientes.
Actualizar la documentación del cambio.
```

Codex reconoce `AGENTS.md` para instrucciones del proyecto. El nombre `agent.md` mencionado en el historial no garantiza la misma detección automática. Al cambiar de asistente, comprobar sus convenciones y pedirle que lea los archivos relevantes. [Documentación de AGENTS.md](https://developers.openai.com/codex/guides/agents-md).

## 6. Ejercicio: empresas que utilizan un Building Block

**Problema del taller:** registrar qué empresas cuentan con un Building Block y qué tecnologías utiliza cada empresa. Es una propuesta basada en la funcionalidad discutida, pendiente de contrastar con el esquema real.

Un Building Block puede tener muchas asociaciones de empresa. Una empresa puede participar en varios Building Blocks. Por tanto, la relación global es muchos a muchos, representada mediante una entidad intermedia. Cada asociación puede tener registros de tecnologías implementadas.

La tecnología corporativa estándar y la tecnología realmente utilizada por una empresa deben conservar significados diferenciados. No se debe eliminar `TBuildingBlockVsTecnologiaTSI` sin analizar si representa la relación con el estándar corporativo y qué dependencias existen.

### Criterios de aceptación propuestos

| ID | Situación | Resultado esperado |
|---|---|---|
| AC-01 | Usuario autorizado asocia una empresa válida | Se registra la relación empresa–Building Block |
| AC-02 | Repite la misma asociación | El sistema rechaza el duplicado y explica el motivo |
| AC-03 | Registra una tecnología para una asociación | La tecnología queda vinculada a esa empresa y ese Building Block |
| AC-04 | Usuario sin permiso intenta modificar mediante una petición directa | El servidor deniega la operación |
| AC-05 | Usuario intenta operar sobre otra empresa fuera de su alcance | La autorización impide la operación |
| AC-06 | Se proponen cambios de tablas con registros existentes | Existe un plan de migración verificable y de recuperación |

El permiso concreto para registrar adopciones debe acordarse en una matriz de roles. La responsabilidad de mantener catálogos no concede automáticamente permiso sobre todas las transacciones.

### Paso 6. Usar ChatGPT para preparar el encargo

Prompt propuesto, listo para adaptar:

```text
Ayúdame a convertir esta necesidad de Landscape TSI en una especificación
que pueda entregar a Codex.

CONTEXTO
Aplicación .NET con SQL Server. Hay Building Blocks, empresas subsidiarias,
tecnologías corporativas estándar y tecnologías implementadas por empresa.

OBJETIVO
Registrar las empresas que usan cada Building Block y, después, las
tecnologías que utiliza cada empresa en esa asociación.

RESTRICCIONES
No inventes columnas, claves ni permisos que no te haya entregado.
Marca los supuestos y pide el esquema que falte.
Analiza el propósito actual de TBuildingBlockVsTecnologiaTSI antes de
proponer reemplazarla. Considera TTecnologiaTSIImplementadaSubsidiaria.
Mantén separadas tecnología estándar y tecnología implementada.

ENTREGA
1. Alcance y preguntas pendientes.
2. Reglas de negocio y criterios de aceptación.
3. Alternativas del modelo de datos y sus impactos.
4. Prompt para Codex que le pida contrastar la propuesta con el repositorio.
```

ChatGPT recibe el contexto que se le entrega. Codex puede contrastarlo con el repositorio al que tenga acceso. Una respuesta bien redactada debe verificarse contra los archivos y los datos reales.

### Paso 7. Iniciar OpenSpec en el repositorio

En la terminal, desde la raíz del proyecto:

```powershell
openspec init
```

Seleccionar el agente que se vaya a usar. Leer la forma de invocación que imprime el instalador. La documentación utiliza nombres canónicos como `/opsx:propose`; en Codex la forma puede ser `$openspec-propose`. Son instrucciones para el chat del agente, no comandos PowerShell. [Referencia de comandos](https://github.com/Fission-AI/OpenSpec/blob/main/docs/commands.md).

Secuencia propuesta dentro del agente, utilizando los nombres que haya generado la instalación:

1. Explorar la solución, reglas existentes y relaciones de datos.
2. Proponer el cambio `asociar-empresas-building-block` con los criterios anteriores.
3. Revisar propuesta, especificación, diseño y tareas antes de implementar.
4. Aplicar las tareas acordadas sobre desarrollo.
5. Comprobar comportamiento y actualizar especificaciones.
6. Archivar cuando la evidencia confirme la finalización.

En el flujo canónico se emplean `/opsx:explore`, `/opsx:propose`, `/opsx:apply` y `/opsx:archive`. Algunas funciones, como `/opsx:verify`, requieren habilitar el perfil ampliado. Comprobar la instalación antes de usarlas. [Comandos y perfiles](https://github.com/Fission-AI/OpenSpec/blob/main/docs/commands.md).

### Paso 8. Pedir implementación con alcance concreto

```text
Lee AGENTS.md y los documentos del cambio de OpenSpec.
Contrasta nombres de tablas, relaciones y autorización con el código actual.
Implementa las tareas acordadas en desarrollo, manteniendo el alcance.

Comprueba duplicados, permisos en servidor y aislamiento entre empresas.
Si cambia el esquema, prepara la migración y el procedimiento de reversión.
No ejecutes cambios sobre producción.

Entrega los archivos modificados, pruebas ejecutadas y resultados reales.
Si algo no pudo verificarse, identifícalo como pendiente.
Actualiza las tareas y especificaciones para que otro agente pueda continuar.
```

### Paso 9. Revisar y publicar el cambio en GitHub

Ejecutar las pruebas existentes de la solución, usando su ruta si es necesario:

```powershell
dotnet build
dotnet test
git diff
git status
```

Probar también el formulario, la validación de duplicados y las peticiones sin permiso. Las pruebas que escriban en SQL deben apuntar a una base exclusiva de desarrollo o pruebas.

Seleccionar explícitamente los archivos revisados para `git add`, crear el commit y subir la rama. Los nombres son ejemplos adaptables:

```powershell
git commit -m "Agrega asociaciones de empresas a Building Blocks"
git push -u origin taller/asignacion-empresas-bb
```

Abrir un pull request en GitHub con alcance, cambios de datos y evidencia de pruebas. La integración con GitHub puede ser simplemente Git y pull requests; no implica que exista CI/CD automático. Desplegar en un entorno de prueba y comprobarlo antes de promover el cambio según el procedimiento del proyecto.

## 7. Guion breve para exponer

“Landscape TSI nace de una necesidad concreta: gestionar la adopción de tecnologías de seguridad en diferentes empresas, con roles y datos relacionados. Mi trabajo consistió en definir esa necesidad y convertirla en instrucciones que un agente pudiera ejecutar.

Usé ChatGPT para aclarar funcionalidades y estructurar prompts. El trabajo con asistentes de código permitió iterar sobre la aplicación. Cada propuesta necesitaba contrastarse con el modelo de datos, los permisos y el funcionamiento esperado.

Vibe Coding describe la interacción mediante lenguaje natural. SDD añade especificaciones verificables. OpenSpec organiza esas especificaciones y las tareas dentro del repositorio. Lo presento como una propuesta para formalizar el proceso de Landscape TSI.

En el ejercicio, una petición general como ‘asocia empresas a un Building Block’ se convierte en reglas: evitar duplicados, controlar permisos y distinguir el estándar corporativo de la implementación de cada empresa. El agente implementa y nosotros comprobamos esas reglas.

El aprendizaje es que saber qué pedir y cómo comprobarlo sigue siendo trabajo de ingeniería. El código generado es el inicio de la revisión.”

## 8. Referencias proporcionadas por Jean

- [Kiro vs Google Antigravity: ¿Cuál IDE de IA conviene más?](https://www.youtube.com/watch?v=0gd_w98aAHo). Se identificó título y descripción pública.
- [El FIN del Vibe Coding: Kiro vs Spec Kit vs OpenSpec para SDD](https://www.youtube.com/watch?v=XJiIhQ_FkdA). Se identificó título y descripción pública.
- [Tercer video, desde el segundo 44](https://www.youtube.com/watch?v=LbH0aMnb_Wk&t=44s). No se pudo verificar su contenido.

No se obtuvieron transcripciones de estos videos. Se conservan como referencias complementarias, sin atribuirles instrucciones o conclusiones no verificadas. Las instrucciones técnicas de esta guía se apoyan en las fuentes oficiales enlazadas en cada sección.
