# Tareas: Sistema de Temas Visuales (Ligero y Corporativo Credicorp)

## 1. Tokens Semánticos y Estilos CSS Adaptativos

- [x] 1.1 Definir tokens semánticos CSS Custom Properties en `src/Landscape.Tsi.Web/wwwroot/css/site.css` para los selectores `:root, [data-theme="light"]` y `[data-theme="corporate"]`, incorporando la paleta institucional inspirada en Grupo Credicorp (`#002A86`, `#0A1B3A`, `#2AD2C9`, `#77E2AD`, `#FF7800`).
- [x] 1.2 Diseñar estilos de navegación, tarjetas de contenido, botones y elevaciones de Material Design 3 específicos para el tema corporativo y tema ligero.
- [x] 1.3 Crear el componente parcial Razor `src/Landscape.Tsi.Web/Views/Shared/_BrandLogo.cshtml` con representación SVG vectorial dual: isotipo "L" para estilo Ligero e imagotipo referencial estilizado de Credicorp para estilo Corporativo.

## 2. Persistencia y Renderizado en Servidor (Anti-FOUC)

- [x] 2.1 Implementar métodos en `Landscape.Tsi.Web/Controllers/AccountController.cs` (`SetTheme` POST y `Preferences` GET) para validar el tema contra lista blanca (`light`, `corporate`) y persistir la cookie segura `landscape_theme`.
- [x] 2.2 Actualizar `src/Landscape.Tsi.Web/Views/Shared/_Layout.cshtml` para leer la cookie de tema del lado del servidor, inyectar el atributo `data-theme` en la etiqueta raíz `<html>` e incluir un script defensivo temprano anti-FOUC.

## 3. Selector en Login y Configuración de Cuenta

- [x] 3.1 Enriquecer la vista de acceso `src/Landscape.Tsi.Web/Views/Account/Login.cshtml` con un selector accesible de estilo visual ("Ligero" vs "Corporativo") con previsualización reactiva instantánea y sincronización de cookies.
- [x] 3.2 Crear la vista de configuración `src/Landscape.Tsi.Web/Views/Account/Preferences.cshtml` con un control interactivo tipo flag / toggle switch para alternar el estilo visual en cualquier momento.
- [x] 3.3 Integrar el acceso a "Preferencias de cuenta" en el menú de usuario autenticado del navbar en `src/Landscape.Tsi.Web/Views/Shared/_Layout.cshtml`.

## 4. Pruebas Automatizadas y Verificación de Calidad

- [x] 4.1 Implementar pruebas HTTP en `tests/Landscape.Tsi.Tests/Web/ThemeHttpTests.cs` verificando la lectura de cookies, la respuesta del endpoint `SetTheme`, el rechazo de valores no autorizados y el renderizado correcto del atributo `data-theme`.
- [x] 4.2 Implementar pruebas de accesibilidad en `tests/Landscape.Tsi.Tests/Web/ThemeAccessibilityTests.cs` validando contraste de color WCAG 2.2 AA y atributos ARIA de los controles de selección de tema.
- [x] 4.3 Ejecutar `dotnet test --configuration Release` asegurando que las 223 pruebas existentes más las nuevas pruebas pasen con 0 errores.
