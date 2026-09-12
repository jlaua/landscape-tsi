# Diseño Técnico: Sistema de Temas Visuales (Ligero y Corporativo Credicorp)

## Context

Landscape TSI utiliza ASP.NET Core MVC (.NET 10) con una capa visual basada en tokens de Material Design 3 y Bootstrap 5 definidos en `site.css`. Actualmente, las variables `:root` (`--md-primary`, `--md-surface`, etc.) están fijadas en una paleta neutra clara única y no existe un mecanismo para alternar entre identidades visuales ni persistir la preferencia estética del usuario.

## Goals / Non-Goals

**Goals:**
- Proveer soporte completo para dos estilos visuales: **Estilo Ligero** (interfaz actual clara) y **Estilo Corporativo** (inspirado en la identidad institucional de Grupo Credicorp).
- Implementar la identidad visual de Grupo Credicorp: paleta con azul marino profundo (`#002A86` / `#0A1B3A`), turquesa vibrante (`#2AD2C9`), verde cian (`#77E2AD`), gris cálido/off-white y logotipo/imagotipo vectorial referencial estilizado.
- Habilitar un selector interactivo accesible en la pantalla de inicio de sesión (`/Account/Login`) que cambie en vivo el aspecto visual y guarde la elección.
- Proveer una vista/interfaz de configuración de preferencias de cuenta (`/Account/Preferences`) para usuarios autenticados con un flag/toggle switch para conmutar el tema.
- Garantizar renderizado del lado del servidor (SSR) libre de parpadeos (FOUC) mediante lectura de la cookie `landscape_theme`.
- Asegurar cumplimiento estricto de accesibilidad WCAG 2.2 nivel AA (contraste de color >= 4.5:1 para texto normal y foco visible).

**Non-Goals:**
- No implementar paletas arbitrarias o personalización infinita de colores (únicamente los dos estilos aprobados: Ligero y Corporativo).
- No ejecutar modificaciones DDL destructivas ni agregar tablas en base de datos; la preferencia se gestiona mediante cookies seguras y estado de sesión.

## Decisions

### 1. Tokens Semánticos de Tematización mediante CSS Custom Properties
- **Decisión**: Extender `site.css` encapsulando las variables bajo `:root, [data-theme="light"]` para el tema ligero y `[data-theme="corporate"]` para el tema corporativo.
- **Detalle de Tokens Corporativos**:
  - `--md-primary`: `#002A86` (Azul corporativo Credicorp)
  - `--md-accent`: `#2AD2C9` (Turquesa vibrante Credicorp)
  - `--md-accent-secondary`: `#77E2AD` (Verde cian suave)
  - `--md-accent-warm`: `#FF7800` (Naranja complementario BCP/Credicorp)
  - `--md-surface`: `#ffffff`
  - `--md-surface-container`: `#f4f6fa`
  - `--md-header-bg`: `#0A1B3A` / `#002A86` (Encabezado corporativo contrastado)
  - `--md-header-text`: `#ffffff`
- **Alternativas consideradas**: Archivos CSS independientes separados (`site-light.css` y `site-corporate.css`). Se descartó porque provocaría duplicidad de código y latencia de red adicional al alternar temas.

### 2. Logotipo Vectorial Adaptativo (SVG Inline / Parcial Razor)
- **Decisión**: Crear un componente parcial `_BrandLogo.cshtml` que renderice el isotipo tradicional "L" cuando el tema es ligero y el imagotipo corporativo estilizado referencial de Credicorp (con sus distintivas barras curvas fluidas turquesa/verde/cian y tipografía institucional) cuando el tema es corporativo.
- **Alternativas consideradas**: Imágenes PNG fijas. Se descartó porque pierden nitidez en pantallas Retina y requieren peticiones HTTP separadas.

### 3. Persistencia por Cookie `landscape_theme` con Lectura en SSR (Anti-FOUC)
- **Decisión**: Almacenar la preferencia de tema en una cookie `landscape_theme` (`SameSite=Lax; Path=/; MaxAge=365 días; HttpOnly=false`). En `_Layout.cshtml`, el servidor lee la cookie directamente desde `Context.Request.Cookies["landscape_theme"]` y emite `<html lang="es" data-theme="@theme">`.
- **Alternativas consideradas**: Almacenar únicamente en `localStorage`. Se descartó porque obligaría a esperar a que el navegador ejecute JavaScript para aplicar el tema, produciendo un destello blanco (FOUC) perceptible al navegar entre páginas.

### 4. Endpoints y Flujo de Conmutación en `AccountController`
- **Decisión**:
  - `POST /Account/SetTheme`: Endpoint que valida el valor contra lista blanca (`light`, `corporate`), fija la cookie y retorna respuesta JSON para llamadas AJAX o redirección segura para envíos estándar.
  - `GET /Account/Preferences`: Vista dedicada para usuarios autenticados que presenta su información básica y el flag toggle de estilo visual accesible (`aria-checked`, conmutación instantánea).
  - Enlace "Preferencias de cuenta" en el dropdown del navbar de usuario junto a "Cerrar sesión".

## Risks / Trade-offs

- **[Riesgo] Contraste en modo corporativo entre turquesa y blanco/fondo**:
  - *Mitigación*: Utilizar el turquesa `#2AD2C9` como acento gráfico y en elementos con suficiente contraste, asegurando que los textos interactivos principales empleen blanco `#ffffff` sobre azul `#002A86` o azul oscuro `#0A1B3A` sobre fondo claro, superando 4.5:1.
- **[Riesgo] Usuarios sin JavaScript o con cookies bloqueadas**:
  - *Mitigación*: El sistema degrada elegantemente al tema Ligero por defecto y los formularios de cambio de tema funcionan tanto vía fetch/AJAX como mediante submit estándar de formulario HTTP POST.
