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
            [Text("nombreCorporativo", "nombreTecnologiaAlternativa1-Corporativo", "Nombre corporativo"), Text("nombreLocal", "nombreTecnologiaAlternativa2-Local", "Nombre local"), ForeignKey("familia", "idFamilia", "Familia", "familia"), Text("grupo", "grupoQpertenece", "Grupo al que pertenece"), ForeignKey("estadoAdopcion", "idEstadoAdopcionTSI", "Estado de adopción TSI", "estado-adopcion-tsi"), ForeignKey("posturaRoadmap", "idPosturaResumenRoadmap", "Postura roadmap", "postura-roadmap"), Text("licenciamiento", "modeloEsquemaLicenciamientoSubscripcion", "Modelo de licenciamiento", true), Text("entorno", "entornoImplementacion", "Entorno de implementación", true), Text("referencia", "linkReferencia-Fuente", "Referencia / fuente", true), Text("responsable", "responsableTecnologiaTSI", "Responsable"), Text("unidadResponsable", "unidadResponsableTecnologiaTSI", "Unidad responsable"), Text("categoriaAsIs", "categoriaAS-IS", "Categoría AS-IS"), Text("fuente", "flagFuente", "Fuente")], ["nombreCorporativo", "nombreLocal", "familia", "estadoAdopcion", "posturaRoadmap"]),
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
        Define("tipo-servicio", "Tipo de Servicio", "TTipoServicio", "idTipoServicio", "tipo-servicio", "Operación", "nombre", CatalogEditorMode.Modal,
            [Text("codigo", "codigo", "Código"), Text("nombre", "nombre", "Nombre"), Text("descripcion", "descripcion", "Descripción", true), Text("orden", "orden", "Orden"), Text("esActivo", "esActivo", "Activo")], ["codigo", "nombre", "orden", "esActivo"], CatalogEntityType.Master, "Catálogo maestro de tipos de servicio de tecnología (Implementación, Migración, Operación)."),
        Define("actividad-nivel-soporte", "Actividad por Nivel de Soporte", "TActividadNivelSoporte", "idActividadSoporte", "actividad-nivel-soporte", "Operación", "descripcionActividad", CatalogEditorMode.Modal,
            [Text("nivelSoporte", "nivelSoporte", "Nivel de soporte"), Text("descripcionActividad", "descripcionActividad", "Descripción de la actividad", true), Text("ordenVisual", "ordenVisual", "Orden visual"), Text("esActivo", "esActivo", "Activo")], ["nivelSoporte", "descripcionActividad", "ordenVisual", "esActivo"], CatalogEntityType.Master, "Catálogo maestro de actividades asignadas por nivel de soporte N1, N2 y N3."),
        Define("modelo-operacion", "Modelo de Operación", "TModeloDeOperacion", "idModeloOperacion", "modelo-operacion", "Operación", "tecnologiaImplementada", CatalogEditorMode.Modal,
            [ForeignKey("tecnologiaImplementada", "idTecnologiaTSIimplementadaSubsidiaria", "Tecnología implementada", "tecnologia-tsi-implementada"), ForeignKey("tipoOperacion", "idTipoModeloDeOperacion", "Tipo de operación", "tipo-operacion"), ForeignKey("modalidadLaboral", "idModalidadLaboral", "Modalidad laboral", "modalidad-laboral")], ["tecnologiaImplementada", "tipoOperacion", "modalidadLaboral"], CatalogEntityType.Master, "Contexto operativo y modalidad laboral de una tecnología implementada."),
        Define("contrato-tecnologia", "Contrato de Tecnología", "TContratoTecnologia", "idContratoTecnologia", "contrato-tecnologia", "Operación", "numeroContrato", CatalogEditorMode.Modal,
            [ForeignKey("tecnologiaImplementada", "idTecnologiaTSIimplementadaSubsidiaria", "Tecnología implementada", "tecnologia-tsi-implementada"), Text("numeroContrato", "numeroContrato", "Número de contrato"), Text("esAdenda", "esAdenda", "Es adenda"), Text("esPayg", "esPayg", "Es PAYG", true), ForeignKey("contratoPadre", "idContratoPadre", "Contrato padre", "contrato-tecnologia"), Date("fechaInicio", "fechaInicio", "Fecha de inicio"), Date("fechaFin", "fechaFin", "Fecha de fin"), Date("fechaAdjudicacion", "fechaAdjudicacion", "Fecha de adjudicación"), Text("rutaDocumentoContrato", "rutaDocumentoContrato", "Ruta de documento", true), Text("montoContratado", "montoContratado", "Monto contratado"), Text("moneda", "moneda", "Moneda"), Text("observaciones", "observaciones", "Observaciones", true)], ["numeroContrato", "tecnologiaImplementada", "fechaInicio", "fechaFin", "montoContratado", "moneda"], CatalogEntityType.Transactional, "Contratos y adendas asociados a tecnologías implementadas."),
        Define("proceso-adopcion-tsi", "Proceso de Adopción TSI", "TProcesoAdopcionTSI", "idProcesoAdopcionTSI", "proceso-adopcion-tsi", "Arquitectura de seguridad", "nombreProceso", CatalogEditorMode.Modal,
            [Text("codigoProceso", "codigoProceso", "Código de proceso"), Text("nombreProceso", "nombreProceso", "Nombre del proceso"), ForeignKey("buildingBlock", "idBuildingBlock", "Building Block", "building-block"), ForeignKey("estadoAdopcion", "idEstadoAdopcionTSI", "Estado de adopción", "estado-adopcion-tsi"), Text("objetivo", "objetivo", "Objetivo", true), Text("alcance", "alcance", "Alcance", true), Text("liderCorporativoTSI", "liderCorporativoTSI", "Líder corporativo TSI"), Date("fechaInicio", "fechaInicio", "Fecha de inicio"), Date("fechaEstimadaCierre", "fechaEstimadaCierre", "Fecha estimada de cierre")], ["codigoProceso", "nombreProceso", "buildingBlock", "estadoAdopcion", "liderCorporativoTSI"], CatalogEntityType.Transactional, "Procesos y evaluaciones de adopción tecnológica TSI a nivel corporativo."),
        Define("proceso-adopcion-empresa", "Empresa en Proceso de Adopción", "TProcesoAdopcionEmpresa", "idProcesoAdopcionEmpresa", "proceso-adopcion-empresa", "Organización", "justificacionNoAplica", CatalogEditorMode.Modal,
            [ForeignKey("proceso", "idProcesoAdopcionTSI", "Proceso de adopción", "proceso-adopcion-tsi"), ForeignKey("empresa", "idEmpresaSubsidiaria", "Empresa subsidiaria", "empresa-subsidiaria"), Text("aplica", "aplica", "Aplica"), Text("justificacionNoAplica", "justificacionNoAplica", "Justificación por No Participar del proceso", true), Date("fechaIncorporacion", "fechaIncorporacion", "Fecha de incorporación")], ["proceso", "empresa", "aplica", "fechaIncorporacion"], CatalogEntityType.Transactional, "Participación y estado de aplicación de subsidiarias en procesos de adopción."),
        Define("estandar-tecnologia-historico", "Histórico de Estándares", "TEstandarTecnologiaHistorico", "idEstandarTecnologia", "estandar-tecnologia-historico", "Arquitectura de seguridad", "rolEstandar", CatalogEditorMode.Modal,
            [ForeignKey("buildingBlock", "idBuildingBlock", "Building Block", "building-block"), ForeignKey("tecnologia", "idTecnologiaTSI", "Tecnología TSI", "tecnologia-tsi"), ForeignKey("proceso", "idProcesoAdopcionTSI", "Proceso de adopción", "proceso-adopcion-tsi"), Text("rolEstandar", "rolEstandar", "Rol de estándar"), Text("estadoVigencia", "estadoVigencia", "Estado de vigencia"), Date("fechaInicioVigencia", "fechaInicioVigencia", "Inicio de vigencia"), Date("fechaFinVigencia", "fechaFinVigencia", "Fin de vigencia"), Text("motivoCambio", "motivoCambio", "Motivo de cambio", true), Text("sustentoArquitectura", "sustentoArquitectura", "Sustento de arquitectura", true)], ["buildingBlock", "tecnologia", "rolEstandar", "estadoVigencia", "fechaInicioVigencia", "fechaFinVigencia"], CatalogEntityType.Transactional, "Registro histórico y trazabilidad de tecnologías designadas como estándar."),
        Define("tecnologia-tsi-implementada", "Tecnología TSI Implementada por Empresa", "TTecnologiaTSIimplementadaSubsidiaria", "idTecnologiaTSIimplementadaSubsidiaria", "tecnologia-tsi-implementada", "Tecnología", "versionDesplegada", CatalogEditorMode.Modal,
            [ForeignKey("empresa", "idEmpresaSubsidiaria", "Empresa subsidiaria", "empresa-subsidiaria"), ForeignKey("tecnologia", "idTecnologiaTSI", "Tecnología TSI", "tecnologia-tsi"), ForeignKey("buildingBlock", "idBuildingBlock", "Building Block", "building-block"), Text("versionDesplegada", "versionDesplegada", "Versión desplegada"), Text("esTecnologiaPrimaria", "esTecnologiaPrimaria", "Es tecnología primaria"), Text("esInstanciaCorporativa", "esInstanciaCorporativa", "Instancia corporativa")], ["empresa", "tecnologia", "buildingBlock", "versionDesplegada", "esInstanciaCorporativa"], CatalogEntityType.Transactional, "Implementación de tecnologías TSI en empresas subsidiarias."),
        Define("driver", "Driver Operativo", "TDriver", "idDriver", "driver", "Operación", "descripcionDriver", CatalogEditorMode.Modal,
            [ForeignKey("tecnologiaImplementada", "idTecnologiaTSIimplementadaSubsidiaria", "Tecnología implementada", "tecnologia-tsi-implementada"), Text("descripcionDriver", "descripcionDriver", "Descripción del driver"), Text("unidadMedida", "unidadMedida", "Unidad de medida"), Text("cantidad", "cantidad", "Cantidad"), Text("precioUnitario", "precioUnitario", "Precio unitario"), Text("moneda", "moneda", "Moneda")], ["tecnologiaImplementada", "descripcionDriver", "unidadMedida", "cantidad", "precioUnitario", "moneda"], CatalogEntityType.Transactional, "Drivers operativos y volumetría de tecnologías implementadas."),
        Define("servicio-tecnologia", "Servicio de Tecnología", "TServicioTecnologia", "idServicio", "servicio-tecnologia", "Operación", "nombreServicio", CatalogEditorMode.Modal,
            [Text("codigoServicio", "codigoServicio", "Código de servicio"), Text("nombreServicio", "nombreServicio", "Nombre del servicio"), Text("descripcion", "descripcion", "Descripción", true), ForeignKey("tipoServicio", "idTipoServicio", "Tipo de servicio", "tipo-servicio"), ForeignKey("tecnologia", "idTecnologiaTSI", "Tecnología TSI", "tecnologia-tsi"), ForeignKey("tecnologiaImplementada", "idTecnologiaTSIimplementadaSubsidiaria", "Tecnología implementada", "tecnologia-tsi-implementada"), ForeignKey("empresa", "idEmpresaSubsidiaria", "Empresa subsidiaria", "empresa-subsidiaria"), ForeignKey("proceso", "idProcesoAdopcionTSI", "Proceso de adopción", "proceso-adopcion-tsi"), ForeignKey("vendor", "idVendor", "Vendor", "vendor"), Text("proveedor", "nombreProveedorServicio", "Proveedor del servicio"), Text("estadoServicio", "estadoServicio", "Estado del servicio"), Text("costoTotalEstimado", "costoTotalEstimado", "Costo total estimado"), Text("moneda", "moneda", "Moneda")], ["codigoServicio", "nombreServicio", "tipoServicio", "tecnologia", "empresa", "estadoServicio", "costoTotalEstimado"], CatalogEntityType.Transactional, "Servicios técnicos asociados a tecnologías TSI (Implementación, Migración, Operación)."),
        Define("tarifario-proyecto-horas", "Tarifario de Proyecto (Horas)", "TTarifarioProyectoHoras", "idTarifarioProyecto", "tarifario-proyecto-horas", "Operación", "complejidad", CatalogEditorMode.Modal,
            [ForeignKey("servicio", "idServicio", "Servicio", "servicio-tecnologia"), Text("complejidad", "complejidad", "Complejidad"), Text("rangoHorasDesde", "rangoHorasDesde", "Rango horas desde"), Text("rangoHorasHasta", "rangoHorasHasta", "Rango horas hasta"), Text("tarifaHora", "tarifaHora", "Tarifa por hora"), Text("horasEstimadas", "horasEstimadas", "Horas estimadas"), Text("subtotal", "subtotal", "Subtotal"), Text("moneda", "moneda", "Moneda"), Text("observaciones", "observaciones", "Observaciones", true)], ["servicio", "complejidad", "tarifaHora", "horasEstimadas", "subtotal", "moneda"], CatalogEntityType.Transactional, "Tarifarios por complejidad y rango de horas de servicios de proyecto."),
        Define("tarifario-operacion", "Tarifario de Operación", "TTarifarioOperacion", "idTarifarioOperacion", "tarifario-operacion", "Operación", "nivelSoporte", CatalogEditorMode.Modal,
            [ForeignKey("servicio", "idServicio", "Servicio", "servicio-tecnologia"), Text("nivelSoporte", "nivelSoporte", "Nivel de soporte"), Text("modalidad", "modalidad", "Modalidad"), Text("detalleModalidad", "detalleModalidad", "Detalle de modalidad", true), Text("horasBaseMensual", "horasBaseMensual", "Horas base mensual"), Text("expertise", "expertise", "Expertise"), Text("locacion", "locacion", "Locación"), Text("tarifaHora", "tarifaHora", "Tarifa por hora"), Text("tarifaMensual", "tarifaMensual", "Tarifa mensual"), Text("cantidadMeses", "cantidadMeses", "Cantidad de meses"), Text("horasEstimadas", "horasEstimadas", "Horas estimadas"), Text("subtotal", "subtotal", "Subtotal"), Text("moneda", "moneda", "Moneda")], ["servicio", "nivelSoporte", "modalidad", "tarifaHora", "tarifaMensual", "subtotal", "moneda"], CatalogEntityType.Transactional, "Matriz tarifaria de operación y soporte continuo N1, N2 y N3."),
        Define("vendor", "Vendor", "TVendor", "idVendor", "vendor", "Organización", "nombreVendor", CatalogEditorMode.Modal,
            [Text("nombreVendor", "nombreVendor", "Nombre del vendor"), Text("descripcionVendor", "descripcionVendor", "Descripción del vendor", true), ForeignKey("tecnologia", "idTecnologiaTSI", "Tecnología TSI", "tecnologia-tsi")], ["nombreVendor", "descripcionVendor", "tecnologia"], CatalogEntityType.Transactional, "Proveedores o fabricantes relacionados con tecnología TSI."),
        Define("contacto-vendor", "Contacto de Vendor", "TContactoVendor", "idContactoVendor", "contacto-vendor", "Organización", "nombreContactoVendor", CatalogEditorMode.Modal,
            [ForeignKey("vendor", "idVendor", "Vendor", "vendor"), Text("rol", "ROL", "Rol / Cargo", true), Text("nombreContactoVendor", "nombreContactoVendor", "Nombre del contacto"), Text("email", "email", "Correo electrónico"), Text("telefono", "telefono", "Teléfono"), Text("otro", "otro", "Otro", true), Text("notas", "NOTAS", "Notas", true)], ["nombreContactoVendor", "vendor", "rol", "email", "telefono"], CatalogEntityType.Transactional, "Contactos comerciales o técnicos asociados a un vendor."),
        Define("contacto-partner", "Contacto de Partner", "TContactoPartner", "idContactoPartner", "contacto-partner", "Organización", "nombreContactoPartner", CatalogEditorMode.Modal,
            [ForeignKey("vendor", "idVendor", "Vendor", "vendor"), Text("rol", "ROL", "Rol / Cargo", true), Text("nombreContactoPartner", "nombreContactoPartner", "Nombre del partner"), Text("email", "email", "Correo electrónico"), Text("telefono", "telefono", "Teléfono"), Text("otro", "otro", "Otro", true), Text("notas", "NOTAS", "Notas", true)], ["nombreContactoPartner", "vendor", "rol", "email", "telefono"], CatalogEntityType.Transactional, "Contactos comerciales o técnicos asociados a un partner de tecnología.")
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
        new("building-block", "tecnologia-tsi", "Muchos a muchos", "TBuildingBlockVsTTecnologiaTSI", "TBuildingBlockVsTTecnologiaTSI"),
        new("building-block", "proceso-adopcion-tsi", "Uno a muchos", "FK_TProcesoAdopcionTSI_TBuildingBlock"),
        new("estado-adopcion-tsi", "proceso-adopcion-tsi", "Uno a muchos", "FK_TProcesoAdopcionTSI_TMEstadoAdopcionTSI"),
        new("proceso-adopcion-tsi", "proceso-adopcion-empresa", "Uno a muchos", "FK_TProcesoAdopcionEmpresa_TProcesoAdopcionTSI"),
        new("empresa-subsidiaria", "proceso-adopcion-empresa", "Uno a muchos", "FK_TProcesoAdopcionEmpresa_TEmpresaSubsidiaria"),
        new("building-block", "estandar-tecnologia-historico", "Uno a muchos", "FK_TEstandarTecnologiaHistorico_TBuildingBlock"),
        new("tecnologia-tsi", "estandar-tecnologia-historico", "Uno a muchos", "FK_TEstandarTecnologiaHistorico_TTecnologiaTSI"),
        new("proceso-adopcion-tsi", "estandar-tecnologia-historico", "Uno a muchos", "FK_TEstandarTecnologiaHistorico_TProcesoAdopcionTSI"),
        new("proceso-adopcion-tsi", "servicio-tecnologia", "Uno a muchos", "FK_TServicioTecnologia_TProcesoAdopcionTSI"),
        new("tecnologia-tsi", "tecnologia-tsi-implementada", "Uno a muchos", "FK_TTecnologiaTSIimplementadaSubsidiaria_TTecnologiaTSI"),
        new("empresa-subsidiaria", "tecnologia-tsi-implementada", "Uno a muchos", "FK_TTecnologiaTSIimplementadaSubsidiaria_TEmpresaSubsidiaria"),
        new("tecnologia-tsi-implementada", "contrato-tecnologia", "Uno a muchos", "FK_TContratoTecnologia_Implementada"),
        new("tecnologia-tsi-implementada", "driver", "Uno a muchos", "FK_TDriver_TTecnologiaTSIimplementadaSubsidiaria"),
        new("tecnologia-tsi-implementada", "modelo-operacion", "Uno a muchos", "FK_TModeloDeOperacion_TTecnologiaTSIimplementadaSubsidiaria"),
        new("tipo-servicio", "servicio-tecnologia", "Uno a muchos", "FK_TServicioTecnologia_Tipo"),
        new("servicio-tecnologia", "tarifario-proyecto-horas", "Uno a muchos", "FK_TTarifarioProyectoHoras_TServicioTecnologia"),
        new("servicio-tecnologia", "tarifario-operacion", "Uno a muchos", "FK_TTarifarioOperacion_TServicioTecnologia"),
        new("tecnologia-tsi", "vendor", "Uno a muchos", "FK_TVendor_TTecnologiaTSI"),
        new("vendor", "contacto-vendor", "Uno a muchos", "FK_TContactoVendor_TVendor"),
        new("vendor", "contacto-partner", "Uno a muchos", "FK_TContactoPartner_TVendor")
    ];

    public static readonly IReadOnlyList<CatalogEntityMetadata> EntityMetadata =
        Catalogs.Select(catalog => new CatalogEntityMetadata(catalog.Code, catalog.PhysicalTable, catalog.Name, catalog.EntityType, catalog.Group, catalog.NavigationRoute, catalog.IsAdministrable, catalog.Description ?? "Entidad administrable del catálogo Landscape TSI.", catalog.IsDeletable)).Concat(
        [
            Entity("regulacion", "TRegulacionAplicable", "Regulación Aplicable", CatalogEntityType.Transactional, "Organización", null, false, "Regulación aplicable a una empresa subsidiaria."),
            Entity("contacto-empresa", "TContactoEmpresaSubsidiaria", "Contacto de Empresa", CatalogEntityType.Transactional, "Organización", null, false, "Contacto asociado a una empresa subsidiaria."),
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
    private static readonly string[] CanonicalReportingCatalogCodes =
    [
        "dominio",
        "building-block",
        "capacidad-seguridad",
        "funcionalidad",
        "tecnologia-tsi",
        "familia"
    ];

    public static readonly IReadOnlyList<MasterCatalogDefinition> ReportingCatalogs =
        CanonicalReportingCatalogCodes
            .Select(GetByCode)
            .Where(catalog => catalog is not null && catalog.IncludeInReporting)
            .Cast<MasterCatalogDefinition>()
            .ToArray();

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