## Purpose

Proveer un sistema de tematización visual seleccionable entre un estilo 'Ligero' y un estilo 'Corporativo' (inspirado en la identidad de Grupo Credicorp), con capacidad de conmutación en la pantalla de inicio de sesión y en la configuración de la cuenta de usuario, garantizando persistencia segura, renderizado sin parpadeo (anti-FOUC) y cumplimiento de accesibilidad WCAG 2.2 AA.

## ADDED Requirements

### Requirement: Tematización Visual Dual (Estilo Ligero y Estilo Corporativo Credicorp)
El sistema debe soportar dos temas visuales completos gobernados por el atributo `data-theme` en el elemento raíz HTML y tokens CSS semánticos: el estilo 'Ligero' (basado en la estética clara actual de Landscape TSI) y el estilo 'Corporativo' (inspirado en la paleta institucional de Grupo Credicorp: azul marino profundo, acentos turquesa/teal, verde cian y logotipo/imagotipo corporativo referencial).

#### Scenario: Visualización en estilo Ligero
- **WHEN** el sistema tiene activo el valor de tema `light`
- **THEN** la plataforma despliega la paleta neutra clara actual, el isotipo estándar de Landscape TSI y mantiene una relación de contraste mínima de 4.5:1 para texto normal.

#### Scenario: Visualización en estilo Corporativo Credicorp
- **WHEN** el sistema tiene activo el valor de tema `corporate`
- **THEN** la plataforma despliega la paleta corporativa inspirada en Grupo Credicorp (encabezado azul marino corporativo, acentos turquesa `#2AD2C9`, componentes refinados y el imagotipo corporativo referencial) cumpliendo con contraste accesible WCAG 2.2 AA.

### Requirement: Selección Reactiva de Estilo en Inicio de Sesión
La pantalla de inicio de sesión (`/Account/Login`) debe proporcionar un control de selección accesible (selector visual o botones segmentados con teclado y foco) que permita elegir entre el estilo 'Ligero' y el estilo 'Corporativo' antes de autenticarse.

#### Scenario: Conmutación visual inmediata en login
- **WHEN** un usuario no autenticado selecciona la opción 'Corporativo' en la pantalla de login
- **THEN** la interfaz del login adapta instantáneamente sus colores, logotipo y presentación sin recargar la página y almacena la preferencia en la cookie de navegación.

#### Scenario: Persistencia de estilo post-autenticación
- **WHEN** un usuario inicia sesión habiendo seleccionado previamente un estilo específico en el login
- **THEN** al ingresar al portal de Landscape TSI, la sesión completa del usuario se inicializa y presenta en el estilo visual seleccionado.

### Requirement: Flag de Conmutación de Estilo en Configuración de Cuenta
El usuario autenticado debe disponer de una sección accesible de configuración de cuenta / preferencias (accesible desde el menú de usuario en la barra superior) que incluya un control tipo flag / toggle switch para alternar entre el estilo 'Ligero' y 'Corporativo'.

#### Scenario: Cambio de tema desde la configuración de cuenta
- **WHEN** el usuario autenticado activa o conmuta el flag de tema en sus preferencias de cuenta
- **THEN** el sistema actualiza de inmediato el atributo de tema en el documento, actualiza la cookie de preferencia del usuario y emite una confirmación accesible.

#### Scenario: Navegación accesible del control de tema
- **WHEN** un usuario navega la configuración de tema utilizando exclusivamente teclado o lector de pantalla
- **THEN** el control recibe foco visible claramente distinguible, anuncia su estado actual (Ligero o Corporativo) mediante atributos `aria-checked` / `aria-pressed` y responde a teclas estándar (`Espacio` / `Enter`).

### Requirement: Prevención de Parpadeo Visual (Anti-FOUC) y Persistencia Robusta
El sistema debe determinar e inyectar el tema visual correspondiente del lado del servidor a partir de la cookie de preferencia en cada petición HTTP, complementado con un script sincrónico temprano para evitar destellos de contenido desestilizado (FOUC).

#### Scenario: Carga directa de página con tema corporativo
- **WHEN** un navegador realiza una solicitud HTTP con la cookie de tema establecida en `corporate`
- **THEN** el documento HTML inicial se sirve con `data-theme="corporate"` pre-renderizado desde el servidor, evitando parpadeos o saltos de color durante el renderizado.

#### Scenario: Ausencia de preferencia previa
- **WHEN** un usuario accede por primera vez sin cookies de tema previas
- **THEN** el sistema aplica de forma predeterminada el estilo 'Ligero' asegurando una experiencia estándar y funcional.
