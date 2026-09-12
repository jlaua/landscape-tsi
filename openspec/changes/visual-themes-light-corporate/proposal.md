# Propuesta: Sistema de Temas Visuales (Estilo Ligero y Estilo Corporativo Credicorp)

## Why

Landscape TSI requiere brindar una experiencia visual flexible y alineada tanto con el estándar general de interfaz limpia como con la identidad institucional de Grupo Credicorp. La incorporación de una opción seleccionable entre estilo "Ligero" (diseño actual) y estilo "Corporativo" (colores corporativos y logotipo inspirados en Credicorp) en la pantalla de inicio de sesión y en la configuración de la cuenta permite a los colaboradores adaptar la plataforma a su entorno de trabajo e identidad corporativa de manera consistente y accesible.

## What Changes

- **Motor de Tematización Dual (`light` vs `corporate`)**:
  - Implementación de tokens semánticos CSS Custom Properties en `site.css` que redefinen la paleta, elevación, contrastes y estilos de navegación según el tema activo.
  - Atributo declarativo `data-theme="light"` o `data-theme="corporate"` en el elemento raíz `<html>` o `<body>` de `_Layout.cshtml`.
  - Prevención de parpadeo visual (FOUC) mediante lectura temprana de la preferencia del usuario del lado del servidor (cookie) y script inline sincrónico.

- **Definición de Estilos**:
  - **Estilo Ligero (Light)**: Conserva la estética actual de la plataforma, basada en tonos neutros claros, acentos azules suaves de Material Design 3 e isotipo institucional "L".
  - **Estilo Corporativo (Credicorp)**: Aplica la identidad visual corporativa inspirada en Grupo Credicorp (`grupocredicorp.com`): azul marino profundo (`#002A86` / `#0A1B3A`), turquesa vibrante (`#2AD2C9`), verde cian (`#77E2AD`), acentos cálidos/naranja y tipografía refinada, incorporando el imagotipo/logo corporativo referencial estilizado en el encabezado y en el login.

- **Selector de Estilo en Inicio de Sesión (`/Account/Login`)**:
  - Incorporación de un control accesible (selector visual o botones segmentados) en la tarjeta de login para alternar entre "Ligero" y "Corporativo".
  - Actualización reactiva instantánea del estilo en la pantalla de login sin recargar la página y persistencia automática de la elección en la cookie de sesión.

- **Gestión de Estilo en Configuración de Cuenta**:
  - Incorporación de una opción o vista de configuración de cuenta/preferencias de usuario (accesible desde el menú del usuario en el navbar) con un switch/flag interactivo para alternar entre ambos estilos.
  - Al cambiar el flag, la preferencia se actualiza de forma inmediata en la interfaz y queda fijada para todas las solicitudes futuras de la cuenta.

## Capabilities

### New Capabilities
- `ui/visual-themes`: Gobierna la selección, persistencia (cookie segura y almacenamiento de perfil), renderizado sin parpadeo y aplicación de los estilos visuales "Ligero" y "Corporativo" (inspirado en Credicorp), integrados tanto en el flujo de inicio de sesión como en la configuración de cuenta de usuario.

### Modified Capabilities

## Impact

- **Capa Web MVC (`Landscape.Tsi.Web`)**:
  - `_Layout.cshtml`: Atributo `data-theme`, inyección del logo adecuado según el tema activo, enlace en la navegación de usuario hacia la configuración de cuenta y script anti-FOUC.
  - `Views/Account/Login.cshtml`: Selector interactivo de estilo en el formulario de login.
  - `Controllers/AccountController.cs`: Acciones para fijar y consultar la preferencia de tema, y vista/endpoint de configuración de cuenta.
  - `Views/Account/Preferences.cshtml`: Interfaz de configuración de preferencias de usuario con switch de estilo visual.
  - `wwwroot/css/site.css`: Definición de variables semánticas para `--md-primary`, navbar corporativo, fondos, bordes, tipografía y soporte de logos SVG ("Ligero" vs "Corporativo").
- **Persistencia y Base de Datos**:
  - Cero modificaciones DDL requeridas en SQL Server (`DDL_REQUIRED=NO`). El estado del tema se gestiona de forma segura mediante cookies HttpOnly/SameSite y preferencias de cliente.
- **Seguridad y Accesibilidad**:
  - Cumplimiento de contraste de color bajo WCAG 2.2 nivel AA tanto en estilo ligero como en corporativo.
