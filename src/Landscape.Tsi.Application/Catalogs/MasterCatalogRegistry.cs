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
        Define("familia", "Familia", "TMFamilia", "idFamilia", "familia", "Tecnología", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombreFamilia", "Familia"), Text("descripcion", "descripcionFamilia", "Descripción", true)], ["nombre", "descripcion"]),
        Define("casos-uso", "Casos de Uso", "TCasosDeUso", "idCasosDeUso", "casos-uso", "Tecnología", "nombre", CatalogEditorMode.Modal,
            [ForeignKey("tecnologia", "idTecnologiaTSI", "Tecnología TSI", "tecnologia-tsi"), Text("nombre", "casoDeUso", "Caso de uso"), Text("descripcion", "descripcionCasoDeUso", "Descripción", true)], ["nombre", "tecnologia", "descripcion"]),
        Define("empresa-subsidiaria", "Empresa / Subsidiaria", "TEmpresaSubsidiaria", "idEmpresaSubsidiaria", "empresa-subsidiaria", "Organización", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombreEmpresa", "Empresa / subsidiaria"), Text("alias2", "alias2", "Alias 2"), Text("agrupador", "alias3-agrupador", "Alias 3 / agrupador"), Text("pais", "Pais", "País"), Text("ciudad", "ciudad", "Ciudad"), Text("rubro", "Rubro", "Rubro"), Text("contactoCiso", "contactoCiso", "Contacto CISO", true)], ["nombre", "alias2", "pais", "ciudad"]),
        Define("ciso", "CISO", "TCISO", "idCiso", "ciso", "Organización", "nombre", CatalogEditorMode.Modal,
            [ForeignKey("empresa", "idEmpresaSubsidiaria", "Empresa / subsidiaria", "empresa-subsidiaria"), Text("nombre", "nombreCISO", "Nombre CISO"), Text("email", "email", "Correo electrónico"), Text("telefono", "telefono", "Teléfono"), Text("otro", "otro", "Otro", true)], ["nombre", "empresa", "email", "telefono"]),
        Define("postura-roadmap", "Postura Roadmap", "TMPosturaRoadmap", "idPosturaResumenRoadmap", "postura-roadmap", "Tecnología", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombrePosturaRoadmap", "Postura roadmap"), Text("descripcion", "descripcionPosturaRoadmap", "Descripción", true)], ["nombre", "descripcion"]),
        Define("estado-adopcion-tsi", "Estado de Adopción TSI", "TMEstadoAdopcionTSI", "idEstadoAdopcionTSI", "estado-adopcion-tsi", "Tecnología", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "nombreEstadoAdopcionTSI", "Estado de adopción TSI"), Text("descripcion", "descripcionEstadoAdopcionTSI", "Descripción", true)], ["nombre", "descripcion"]),
        Define("modalidad-laboral", "Modalidad Laboral", "TModalidadLaboral", "idModalidadLaboral", "modalidad-laboral", "Operación", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "TipoModalidadLaboral", "Modalidad laboral"), Text("descripcion", "descripcion", "Descripción", true)], ["nombre", "descripcion"]),
        Define("tipo-operacion", "Tipo de Operación", "TTipoOperacion", "idTipoModeloOperacion", "tipo-operacion", "Operación", "nombre", CatalogEditorMode.Modal,
            [Text("nombre", "TipoModeloDeOperacion", "Tipo de operación"), Text("descripcion", "Descripcion", "Descripción", true)], ["nombre", "descripcion"])
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
        new("building-block", "tecnologia-tsi", "Muchos a muchos", "TBuildingBlockVsTTecnologiaTSI")
    ];

    public static bool IsAdministrable(string code) => GetByCode(code) is not null;
    public static MasterCatalogDefinition? GetByCode(string code) => Catalogs.SingleOrDefault(catalog => string.Equals(catalog.Code, code, StringComparison.Ordinal));
    public static MasterCatalogDefinition? GetByRoute(string route) => Catalogs.SingleOrDefault(catalog => string.Equals(catalog.Route, route, StringComparison.Ordinal));

    private static MasterCatalogDefinition Define(string code, string name, string table, string primaryKey, string route, string group, string displayColumn, CatalogEditorMode editorMode, IReadOnlyList<CatalogColumnDefinition> columns, IReadOnlyList<string> listColumns) =>
        new(code, name, table, primaryKey, route, group, displayColumn, editorMode, columns, listColumns);
    private static CatalogColumnDefinition Text(string code, string physicalName, string label, bool multiline = false) => new(code, physicalName, label, CatalogFieldType.Text, true, multiline);
    private static CatalogColumnDefinition Date(string code, string physicalName, string label) => new(code, physicalName, label, CatalogFieldType.DateTime);
    private static CatalogColumnDefinition ForeignKey(string code, string physicalName, string label, string reference) => new(code, physicalName, label, CatalogFieldType.ForeignKey, true, false, reference);
}
