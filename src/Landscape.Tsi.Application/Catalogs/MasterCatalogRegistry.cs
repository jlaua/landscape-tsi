namespace Landscape.Tsi.Application.Catalogs;

public static class MasterCatalogRegistry
{
    public static readonly IReadOnlyList<MasterCatalogDefinition> Catalogs =
    [
        Define("dominio", "Dominio", "TMDominio", "iddominio", "dominio", "Arquitectura de seguridad", "dominio", CatalogEditorMode.Modal,
            [Text("dominio", "dominio", "Dominio"), Text("descripcion", "descripcionDominio", "Descripción del dominio", true), Text("referencias", "referencias", "Referencias", true), Text("dimensionCiber", "homologacionDimensionSegunCiber", "Homologación según CIBER", true), Text("dimensionLineamiento", "homologacionDimensionSegunLineamiento", "Homologación según lineamiento", true), Text("subDominioCvt", "subDominioCVT", "Subdominio CVT", true), Text("ejemplos", "Ejemplos", "Ejemplos", true)], ["dominio", "descripcion", "subDominioCvt"]),
        Define("building-block", "Building Block", "TBuildingBlock", "idBuildingBlock", "building-block", "Arquitectura de seguridad", "nombre", CatalogEditorMode.Modal,
            [ForeignKey("dominio", "idDominio", "Dominio", "dominio"), Text("nombre", "nombreBuildingBlock", "Building Block"), Text("definicion", "definicionBuildingBlock", "Definición", true), ForeignKey("faseAdopcion", "idEstadoFaseDeAdopcionBuildingBlock", "Fase de adopción", "fase-adopcion"), Text("rutaEntregable", "rutaDelEntregable", "Ruta del entregable", true), Text("pilarZt", "PilarZT", "Pilar ZT")], ["nombre", "dominio", "faseAdopcion"]),
        Define("capacidad-seguridad", "Capacidad de Seguridad", "TCapacidadDeSeguridad", "idCapacidad", "capacidad-seguridad", "Arquitectura de seguridad", "nombre", CatalogEditorMode.Modal,
            [ForeignKey("buildingBlock", "idBuildingBlock", "Building Block", "building-block"), Text("nombre", "nombreCapacidad", "Capacidad"), Text("descripcion", "descripcionCapacidad", "Descripción", true), ForeignKey("estado", "idEstadoCapacidad", "Estado de capacidad", "estado-capacidad")], ["nombre", "buildingBlock", "estado"]),
        Define("estado-capacidad", "Estado de Capacidad", "TMEstadoCapacidad", "idEstadoCapacidad", "estado-capacidad", "Arquitectura de seguridad", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombreEstadoCapacidad", "Estado de capacidad"), Text("descripcion", "descripcionEstadoCapacidad", "Descripción", true)], ["nombre", "descripcion"]),
        Define("funcionalidad", "Funcionalidad", "TFuncionalidad", "idFuncionalidad", "funcionalidad", "Arquitectura de seguridad", "nombre", CatalogEditorMode.Modal,
            [ForeignKey("capacidad", "idCapacidad", "Capacidad de seguridad", "capacidad-seguridad"), Text("nombre", "nombreFuncionalidad", "Funcionalidad"), Text("descripcion", "descripcionFuncionalidad", "Descripción", true), ForeignKey("estado", "idEstadoCoberturaFuncionalidad", "Estado de funcionalidad", "estado-funcionalidad")], ["nombre", "capacidad", "estado"]),
        Define("estado-funcionalidad", "Estado de Funcionalidad", "TMEstadoFuncionalidad", "idEstadoCoberturaFuncionalidad", "estado-funcionalidad", "Arquitectura de seguridad", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombreEstadoFuncionalidad", "Estado de funcionalidad"), Text("descripcion", "descripcionEstadoFuncionalidad", "Descripción", true)], ["nombre", "descripcion"]),
        Define("fase-adopcion", "Fase de Adopción", "TEstadoFaseAdopcion", "idEstadoFaseAdopcion", "fase-adopcion", "Arquitectura de seguridad", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombreFaseAdopcion", "Fase de adopción"), Text("descripcion", "descripcionFaseAdopcion", "Descripción", true)], ["nombre", "descripcion"]),
        Define("tecnologia-tsi", "Tecnología TSI", "TTecnologiaTSI", "idTecnologiaTSI", "tecnologia-tsi", "Tecnología", "nombreCorporativo", CatalogEditorMode.Page,
            [Text("nombreCorporativo", "nombreTecnologiaAlternativa1-Corporativo", "Nombre corporativo"), Text("nombreLocal", "nombreTecnologiaAlternativa2-Local", "Nombre local"), ForeignKey("familia", "idFamilia", "Familia", "familia"), Text("grupo", "grupoQpertenece", "Grupo al que pertenece"), ForeignKey("estadoAdopcion", "idEstadoAdopcionTSI", "Estado de adopción TSI", "estado-adopcion-tsi"), ForeignKey("posturaRoadmap", "idPosturaResumenRoadmap", "Postura roadmap", "postura-roadmap"), Date("fechaEvaluacion", "fechaCompromisoEvaluacionCorporativo", "Compromiso de evaluación corporativa"), Date("fechaFinContrato", "fechaFinDeContratoMasCercanaCorporativo", "Fin de contrato más cercano"), Date("fechaAdjudicacion", "fechaDeAdjudicacionCorporativo", "Fecha de adjudicación"), Text("licenciamiento", "modeloEsquemaLicenciamientoSubscripcion", "Modelo de licenciamiento", true), Text("entorno", "entornoImplementacion", "Entorno de implementación", true), Text("referencia", "linkReferencia-Fuente", "Referencia / fuente", true), Text("responsable", "responsableTecnologiaTSI", "Responsable"), Text("unidadResponsable", "unidadResponsableTecnologiaTSI", "Unidad responsable"), Text("categoriaAsIs", "categoriaAS-IS", "Categoría AS-IS"), Text("fuente", "flagFuente", "Fuente")], ["nombreCorporativo", "nombreLocal", "familia", "estadoAdopcion", "posturaRoadmap"]),
        Define("familia", "Familia", "TMFamilia", "idFamilia", "familia", "Arquitectura de seguridad", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombreFamilia", "Familia"), Text("descripcion", "descripcionFamilia", "Descripción", true)], ["nombre", "descripcion"]),
        Define("casos-uso", "Casos de Uso", "TCasosDeUso", "idCasosDeUso", "casos-uso", "Tecnología", "nombre", CatalogEditorMode.Modal,
            [ForeignKey("tecnologia", "idTecnologiaTSI", "Tecnología TSI", "tecnologia-tsi"), Text("nombre", "casoDeUso", "Caso de uso"), Text("descripcion", "descripcionCasoDeUso", "Descripción", true)], ["nombre", "tecnologia", "descripcion"]),
        Define("empresa-subsidiaria", "Empresa / Subsidiaria", "TEmpresaSubsidiaria", "idEmpresaSubsidiaria", "empresa-subsidiaria", "Organización", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombreEmpresa", "Empresa / subsidiaria"), Text("alias2", "alias2", "Alias 2"), Text("agrupador", "alias3-agrupador", "Alias 3 / agrupador"), Text("pais", "Pais", "País"), Text("ciudad", "ciudad", "Ciudad"), Text("rubro", "Rubro", "Rubro"), Text("contactoCiso", "contactoCiso", "Contacto CISO", true)], ["nombre", "alias2", "pais", "ciudad"]),
        Define("ciso", "CISO", "TCISO", "idCiso", "ciso", "Organización", "nombre", CatalogEditorMode.Modal,
            [ForeignKey("empresa", "idEmpresaSubsidiaria", "Empresa / subsidiaria", "empresa-subsidiaria"), Text("nombre", "nombreCISO", "Nombre CISO"), Text("email", "email", "Correo electrónico"), Text("telefono", "telefono", "Teléfono"), Text("otro", "otro", "Otro", true)], ["nombre", "empresa"]),
        Define("postura-roadmap", "Postura Roadmap", "TMPosturaRoadmap", "idPosturaResumenRoadmap", "postura-roadmap", "Arquitectura de seguridad", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombrePosturaRoadmap", "Postura roadmap"), Text("descripcion", "descripcionPosturaRoadmap", "Descripción", true)], ["nombre", "descripcion"]),
        Define("estado-adopcion-tsi", "Estado de Adopción TSI", "TMEstadoAdopcionTSI", "idEstadoAdopcionTSI", "estado-adopcion-tsi", "Arquitectura de seguridad", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombreEstadoAdopcionTSI", "Estado de adopción TSI"), Text("descripcion", "descripcionEstadoAdopcionTSI", "Descripción", true)], ["nombre", "descripcion"]),
        Define("modalidad-laboral", "Modalidad Laboral", "TModalidadLaboral", "idModalidadLaboral", "modalidad-laboral", "Operación", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "TipoModalidadLaboral", "Modalidad laboral"), Text("descripcion", "descripcion", "Descripción", true)], ["nombre", "descripcion"], CatalogEntityType.Master, "Catálogo de modalidades laborales aplicables al modelo de operación."),
        Define("tipo-operacion", "Tipo de Operación", "TTipoOperacion", "idTipoModeloOperacion", "tipo-operacion", "Operación", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "TipoModeloDeOperacion", "Tipo de operación"), Text("descripcion", "Descripcion", "Descripción", true)], ["nombre", "descripcion"], CatalogEntityType.Master, "Catálogo de tipos de operación."),
    ];

    public static readonly IReadOnlyList<CatalogRelationDefinition> Relations =
    [
        new("dominio", "building-block", "Uno a muchos", "FK_TBuildingBlock_TDominio"),
        new("fase-adopcion", "building-block", "Uno a muchos", "FK_TBuildingBlock_TEstadoFaseAdopcion"),
        new("building-block", "capacidad-seguridad", "Uno a muchos", "FK_TCapacidadDeSeguridad_TBuildingBlock"),
        new("estado-capacidad", "capacidad-seguridad", "Uno a muchos", "FK_TCapacidadDeSeguridad_TMEstadoCapacidad"),
        new("capacidad-seguridad", "funcionalidad", "Uno a muchos", "FK_TFuncionalidad_TCapacidadDeSeguridad"),
        new("estado-funcionalidad", "funcionalidad", "Uno a muchos", "FK_TFuncionalidad_TMEstadoFuncionalidad"),
        new("familia", "tecnologia-tsi", "Uno a muchos", "FK_TTecnologiaTSI_TMFamilia"),
        new("estado-adopcion-tsi", "tecnologia-tsi", "Uno a muchos", "FK_TTecnologiaTSI_TMEstadoAdopcionTSI"),
        new("postura-roadmap", "tecnologia-tsi", "Uno a muchos", "FK_TTecnologiaTSI_TMPosturaRoadmap"),
        new("tecnologia-tsi", "casos-uso", "Uno a muchos", "FK_TCasosDeUso_TTecnologiaTSI"),
        new("empresa-subsidiaria", "ciso", "Uno a muchos", "FK_TCISO_TEmpresaSubsidiaria"),
        new("building-block", "tecnologia-tsi", "Muchos a muchos", "TBuildingBlockVsTTecnologiaTSI", "TBuildingBlockVsTTecnologiaTSI")
    ];

    public static readonly IReadOnlyList<CatalogEntityMetadata> EntityMetadata =
        Catalogs.Select(catalog => new CatalogEntityMetadata(catalog.Code, catalog.PhysicalTable, catalog.Name, catalog.EntityType, catalog.Group, catalog.NavigationRoute, catalog.IsAdministrable, catalog.Description ?? "Entidad administrable del catálogo Landscape TSI.", catalog.Code is "dominio" or "building-block" or "capacidad-seguridad" or "funcionalidad")).Concat(
        [
            Entity("tecnologia-tsi-implementada", "TTecnologiaTSIimplementadaSubsidiaria", "Tecnología TSI implementada", CatalogEntityType.Transactional, "Tecnología", null, false, "Implementación de una tecnología TSI en una empresa subsidiaria."),
            Entity("vendor", "TVendor", "Vendor", CatalogEntityType.Transactional, "Organización", null, false, "Proveedor o fabricante relacionado con tecnología TSI."),
            Entity("regulacion", "TRegulacionAplicable", "Regulación Aplicable", CatalogEntityType.Transactional, "Organización", null, false, "Regulación aplicable a una empresa subsidiaria."),
            Entity("contacto-empresa", "TContactoEmpresaSubsidiaria", "Contacto de Empresa", CatalogEntityType.Transactional, "Organización", null, false, "Contacto asociado a una empresa subsidiaria."),
            Entity("contacto-vendor", "TContactoVendor", "Contacto de Vendor", CatalogEntityType.Transactional, "Organización", null, false, "Contacto asociado a un vendor."),
            Entity("contacto-partner", "TContactoPartner", "Contacto de Partner", CatalogEntityType.Transactional, "Organización", null, false, "Contacto asociado a un partner."),
            Entity("driver", "TDriver", "Driver", CatalogEntityType.Transactional, "Operación", null, false, "Driver operativo de una tecnología implementada."),
            Entity("modelo-operacion", "TModeloDeOperacion", "Modelo de Operación", CatalogEntityType.Transactional, "Operación", null, false, "Contexto operativo de una tecnología implementada."),
            Entity("bridge-building-technology", "TBuildingBlockVsTTecnologiaTSI", "Building Block ↔ Tecnología", CatalogEntityType.Transactional, "Tecnología", null, false, "Tabla puente de la relación muchos a muchos; actualmente sin PK/UNIQUE.")
        ]).ToArray();

    public static readonly IReadOnlyList<CatalogRelationshipMetadata> LogicalRelationships =
    [
        Relationship("dominio", "building-block", "1", "N", "FK_TBuildingBlock_TDominio"),
        Relationship("fase-adopcion", "building-block", "1", "N", "FK_TBuildingBlock_TEstadoFaseAdopcion"),
        Relationship("building-block", "capacidad-seguridad", "1", "N", "FK_TCapacidadDeSeguridad_TBuildingBlock"),
        Relationship("estado-capacidad", "capacidad-seguridad", "1", "N", "FK_TCapacidadDeSeguridad_TMEstadoCapacidad"),
        Relationship("capacidad-seguridad", "funcionalidad", "1", "N", "FK_TFuncionalidad_TCapacidadDeSeguridad"),
        Relationship("estado-funcionalidad", "funcionalidad", "1", "N", "FK_TFuncionalidad_TMEstadoFuncionalidad"),
        Relationship("familia", "tecnologia-tsi", "1", "N", "FK_TTecnologiaTSI_TMFamilia"),
        Relationship("estado-adopcion-tsi", "tecnologia-tsi", "1", "N", "FK_TTecnologiaTSI_TMEstadoAdopcionTSI"),
        Relationship("postura-roadmap", "tecnologia-tsi", "1", "N", "FK_TTecnologiaTSI_TMPosturaRoadmap"),
        Relationship("tecnologia-tsi", "casos-uso", "1", "N", "FK_TCasosDeUso_TTecnologiaTSI"),
        Relationship("empresa-subsidiaria", "ciso", "1", "N", "FK_TCISO_TEmpresaSubsidiaria"),
        Relationship("building-block", "bridge-building-technology", "1", "N", "FK_TBuildingBlockVsTTecnologiaTSI", "bridge-building-technology"),
        Relationship("bridge-building-technology", "tecnologia-tsi", "N", "1", "FK_TBuildingBlockVsTTecnologiaTSI", "bridge-building-technology"),
        Relationship("empresa-subsidiaria", "contacto-empresa", "1", "N", "FK_TContactoEmpresaSubsidiaria_TEmpresaSubsidiaria"),
        Relationship("empresa-subsidiaria", "regulacion", "1", "N", "FK_TRegulacionAplicable_TEmpresaSubsidiaria"),
        Relationship("tecnologia-tsi", "tecnologia-tsi-implementada", "1", "N", "FK_TTecnologiaTSIimplementadaSubsidiaria_TTecnologiaTSI"),
        Relationship("empresa-subsidiaria", "tecnologia-tsi-implementada", "1", "N", "FK_TTecnologiaTSIimplementadaSubsidiaria_TEmpresaSubsidiaria"),
        Relationship("tecnologia-tsi", "vendor", "1", "N", "FK_TVendor_TTecnologiaTSI"),
        Relationship("vendor", "contacto-vendor", "1", "N", "FK_TContactoVendor_TVendor"),
        Relationship("vendor", "contacto-partner", "1", "N", "FK_TContactoPartner_TVendor"),
        Relationship("tecnologia-tsi-implementada", "driver", "1", "N", "FK_TDriver_TTecnologiaTSIimplementadaSubsidiaria"),
        Relationship("modalidad-laboral", "modelo-operacion", "1", "N", "FK_TModeloDeOperacion_TModalidadLaboral"),
        Relationship("tecnologia-tsi-implementada", "modelo-operacion", "1", "N", "FK_TModeloDeOperacion_TTecnologiaTSIimplementadaSubsidiaria"),
        Relationship("tipo-operacion", "modelo-operacion", "1", "N", "FK_TModeloDeOperacion_TTipoOperacion")
    ];

    public static readonly IReadOnlyList<MasterCatalogDefinition> ReportingCatalogs =
        Catalogs.Where(catalog => catalog.IncludeInReporting).ToArray();

    public static readonly IReadOnlyList<CatalogRelationDefinition> ReportingRelations =
        Relations.Where(relation =>
            (GetByCode(relation.ParentCatalogCode)?.IncludeInReporting ?? false) &&
            (GetByCode(relation.ChildCatalogCode)?.IncludeInReporting ?? false)).ToArray();

    public static bool IsAdministrable(string code) => GetByCode(code) is not null;
    public static MasterCatalogDefinition? GetByCode(string code) => Catalogs.SingleOrDefault(catalog => string.Equals(catalog.Code, code, StringComparison.Ordinal));
    public static MasterCatalogDefinition? GetByRoute(string route) => Catalogs.SingleOrDefault(catalog => string.Equals(catalog.Route, route, StringComparison.Ordinal));

    private static MasterCatalogDefinition Define(string code, string name, string table, string primaryKey, string route, string group, string displayColumn, CatalogEditorMode editorMode, IReadOnlyList<CatalogColumnDefinition> columns, IReadOnlyList<string> listColumns, CatalogEntityType? entityType = null, string? description = null) =>
        new(code, name, table, primaryKey, route, group, displayColumn, editorMode, columns, listColumns, entityType, description);
    private static CatalogEntityMetadata Entity(string code, string table, string name, CatalogEntityType type, string group, string? route, bool administrable, string description) => new(code, table, name, type, group, route, administrable, description);
    private static CatalogRelationshipMetadata Relationship(string from, string to, string fromCardinality, string toCardinality, string foreignKey, string? bridge = null) => new(from, to, fromCardinality, toCardinality, foreignKey, bridge is null ? "ForeignKey" : "Bridge", bridge);
    private static CatalogColumnDefinition Text(string code, string physicalName, string label, bool multiline = false) => new(code, physicalName, label, CatalogFieldType.Text, true, multiline);
    private static CatalogColumnDefinition Date(string code, string physicalName, string label) => new(code, physicalName, label, CatalogFieldType.DateTime);
    private static CatalogColumnDefinition ForeignKey(string code, string physicalName, string label, string reference) => new(code, physicalName, label, CatalogFieldType.ForeignKey, true, false, reference);
}