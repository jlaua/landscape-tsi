# Landscape TSI AI Integration and MCP Specification

## Purpose
Exponer herramientas estructuradas y endpoints de integración para asistentes de Inteligencia Artificial (ChatGPT, Claude, Gemini, Antigravity) y servidores de protocolo MCP (Model Context Protocol), permitiendo la interoperabilidad segura, consulta de catálogos maestros, análisis de procesos de adopción y asistencia técnica automatizada sobre la arquitectura corporativa TSI.

## Requirements

### Requirement: Exposición de Herramientas de Consulta y Catálogos MCP
El sistema MUST proporcionar herramientas de consulta segura bajo el protocolo MCP y endpoints REST OpenAPI para buscar y consultar detalles de los catálogos principales del sistema (dominio, building-block, funcionalidad, familia, tecnología TSI, empresa subsidiaria y driver).

#### Scenario: Búsqueda federada de catálogos
- **WHEN** un agente o cliente MCP invoca la herramienta `search_catalogs` indicando el catálogo y un término de búsqueda
- **THEN** el sistema retorna los elementos coincidentes activos respetando la estructura normalizada de datos.

#### Scenario: Consulta detallada de entidad por identificador
- **WHEN** un asistente solicita los detalles de un elemento mediante `get_catalog_detail`
- **THEN** el sistema devuelve la ficha completa de la entidad, incluyendo sus relaciones funcionales y metadatos asociados.

### Requirement: Consulta de Contactos Anonimizados y Procesos de Adopción
El sistema MUST permitir a los asistentes de IA auditar y consultar las solicitudes y procesos de adopción tecnológica y listar contactos operativos de subsidiarias y partners protegiendo datos sensibles.

#### Scenario: Listado de procesos de adopción tecnológica
- **WHEN** el asistente ejecuta la herramienta `list_adoption_requests` filtrando por tipo de proceso o estado
- **THEN** el sistema retorna la lista resumida con métricas de avance, fechas de vigencia y estado corporativo.

#### Scenario: Listado de contactos operativos
- **WHEN** el agente consulta la herramienta `list_contacts` por empresa subsidiaria o partner
- **THEN** el sistema proporciona los contactos técnicos y de negocio autorizados con información de rol y canal de comunicación.

### Requirement: Mutación Controlada e Interpretación OCR
El sistema MUST ofrecer capacidades para registrar o actualizar entidades bajo reglas de negocio estrictas (`mutate_entity`) e interpretar formularios o fichas técnicas digitalizadas (`interpret_ocr_data`).

#### Scenario: Interpretación de datos OCR para fichas técnicas
- **WHEN** se envía un payload de texto o contenido de formulario extraído por OCR a la herramienta `interpret_ocr_data`
- **THEN** el sistema estructura e identifica campos clave como tecnología, vigencia contractual, especialista y subsidiaria vinculada.
