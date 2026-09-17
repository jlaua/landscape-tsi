// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.querySelectorAll("form[data-confirm-message]").forEach((form) => {
    form.addEventListener("submit", (event) => {
        if (!window.confirm(form.dataset.confirmMessage)) event.preventDefault();
    });
});

document.querySelectorAll("form[data-loading-form]").forEach((form) => {
    form.addEventListener("submit", () => {
        const status = form.querySelector(".loading-status");
        if (status) status.hidden = false;
        const submit = form.querySelector("button[type='submit']");
        if (submit) {
            submit.disabled = true;
            submit.setAttribute("aria-disabled", "true");
            if (submit.dataset.loadingText) submit.textContent = submit.dataset.loadingText;
        }
    });
});

document.querySelectorAll("form[data-protect-unsaved='true']").forEach((form) => {
    let dirty = false;
    form.addEventListener("input", () => { dirty = true; });
    form.addEventListener("change", () => { dirty = true; });
    form.addEventListener("submit", (event) => {
        if (!event.defaultPrevented) dirty = false;
    });

    const modal = form.closest(".modal");
    modal?.addEventListener("hide.bs.modal", (event) => {
        if (dirty && !window.confirm("Hay cambios sin guardar. ¿Desea descartarlos?")) {
            event.preventDefault();
        } else {
            dirty = false;
        }
    });

    window.addEventListener("beforeunload", (event) => {
        if (!dirty) return;
        event.preventDefault();
        event.returnValue = "";
    });
});

const initializeCatalogGraph = () => {
    const graphHost = document.querySelector("[data-catalog-graph]");
    if (!graphHost || !window.cytoscape) return;
    const entities = JSON.parse(document.querySelector("[data-map-entities]").textContent);
    const relationships = JSON.parse(document.querySelector("[data-map-relationships]").textContent);
    const requiredEntityProperties = ["id", "logicalName", "physicalTableName", "badge"];
    const status = document.querySelector("[data-map-status]");
    const invalidEntity = entities.find((entity) => requiredEntityProperties.some((property) => !entity[property]));
    if (invalidEntity) {
        const missing = requiredEntityProperties.find((property) => !invalidEntity[property]);
        const diagnostic = `Metadata inválida para entidad ${invalidEntity.id || "desconocida"}: falta ${missing}`;
        if (document.documentElement.dataset.environment === "Development") console.error(diagnostic);
        if (status) status.textContent = "No fue posible cargar el modelo del catálogo.";
        return;
    }
    const entityIds = new Set(entities.map((entity) => entity.id));
    const invalidRelationship = relationships.find((relationship) => !relationship.id || !entityIds.has(relationship.source) || !entityIds.has(relationship.target) || !relationship.sourceCardinality || !relationship.targetCardinality);
    if (invalidRelationship) {
        const diagnostic = `Metadata inválida para relación ${invalidRelationship.id || "desconocida"}: origen, destino o cardinalidad faltante`;
        if (document.documentElement.dataset.environment === "Development") console.error(diagnostic);
        if (status) status.textContent = "No fue posible cargar las relaciones del catálogo.";
        return;
    }
    const preferenceKey = "landscape-tsi-catalog-map";
    const saved = JSON.parse(window.localStorage.getItem(preferenceKey) || "{}");
    const state = { view: saved.view === "physical" ? "physical" : "logical", cardinality: saved.cardinality !== false, bridges: saved.bridges !== false, physicalSecondary: saved.physicalSecondary === true };
    const entityById = new Map(entities.map((entity) => [entity.id, entity]));
    const bridgeCode = "TBuildingBlockVsTTecnologiaTSI";
    const entityLabel = (entity) => `[${entity.badge}] ${state.view === "physical" ? entity.physicalTableName : entity.logicalName}${state.view === "logical" && state.physicalSecondary ? `\n${entity.physicalTableName}` : ""}`;
    const elements = () => {
        const visibleEntities = state.bridges ? entities : entities.filter((entity) => entity.id !== bridgeCode);
        const nodes = visibleEntities.map((entity) => ({ data: { id: entity.id, label: entityLabel(entity), logicalName: entity.logicalName, physicalTableName: entity.physicalTableName, badge: entity.badge, entityType: entity.entityType, group: entity.group, route: entity.route || "", description: entity.description, administrable: entity.isAdministrable } }));
        const edges = [];
        relationships.forEach((relation) => {
            if (!state.bridges && relation.bridgeEntity) {
                if (relation.source === "TBuildingBlock" && relation.target === bridgeCode) edges.push({ data: { id: "conceptual-building-technology", source: "TBuildingBlock", target: "TTecnologiaTSI", label: "TBuildingBlockVsTTecnologiaTSI", sourceCardinality: "N", targetCardinality: "M", relationType: "Bridge" } });
                return;
            }
            edges.push({ data: { id: relation.id, source: relation.source, target: relation.target, label: relation.foreignKeyName, sourceCardinality: relation.sourceCardinality, targetCardinality: relation.targetCardinality, relationType: relation.relationshipType } });
        });
        return [...nodes, ...edges];
    };
    const layoutOptions = { name: "breadthfirst", directed: true, roots: ["TMDominio", "TEstadoFaseAdopcion", "TMFamilia", "TEmpresaSubsidiaria", "TModalidadLaboral", "TTipoOperacion"], animate: false, padding: 40, spacingFactor: 1.35 };
    const cy = window.cytoscape({ container: graphHost, elements: elements(), layout: layoutOptions, minZoom: .35, maxZoom: 2.4, wheelSensitivity: .18, style: [
        { selector: "node", style: { "background-color": "#e8eefc", "border-color": "#5878b8", "border-width": 2, "shape": "round-rectangle", "label": "data(label)", "color": "#172d55", "font-size": 13, "font-weight": 700, "text-wrap": "wrap", "text-max-width": 170, "text-valign": "center", "text-halign": "center", "width": 190, "height": 64, "padding": 8, "overlay-opacity": 0 } },
        { selector: "node[badge = 'M']", style: { "background-color": "#e7f2ee", "border-color": "#287454", "color": "#123c2c" } },
        { selector: "node[badge = 'T']", style: { "background-color": "#e8eefc", "border-color": "#5878b8" } },
        { selector: "node:selected", style: { "border-color": "#9b4d00", "border-width": 4, "shadow-blur": 20, "shadow-color": "#f2b880", "shadow-opacity": .8 } },
        { selector: "node.dimmed", style: { "opacity": .22 } },
        { selector: "edge", style: { "curve-style": "bezier", "line-color": "#6a7892", "target-arrow-color": "#6a7892", "target-arrow-shape": "triangle", "width": 2, "label": "data(label)", "source-label": "data(sourceCardinality)", "target-label": "data(targetCardinality)", "font-size": 9, "color": "#3f4b62", "text-background-color": "#fff", "text-background-opacity": .9, "text-background-padding": 3, "text-rotation": "autorotate" } },
        { selector: "edge.hide-cardinality", style: { "source-label": "", "target-label": "" } },
        { selector: "edge.dimmed", style: { "opacity": .12 } },
        { selector: "edge[relationType = 'Bridge']", style: { "line-style": "dashed", "line-color": "#8e5ab7", "target-arrow-color": "#8e5ab7" } }
    ] });
    const save = () => window.localStorage.setItem(preferenceKey, JSON.stringify(state));
    const inspector = document.querySelector("[data-map-inspector]");
    const updateInspector = (entity) => {
        if (!entity) { inspector.hidden = true; return; }
        inspector.hidden = false;
        inspector.querySelector("[data-map-inspector-title]").textContent = entity.logicalName;
        inspector.querySelector("[data-map-inspector-description]").textContent = entity.description;
        inspector.querySelector("[data-map-inspector-physical]").textContent = entity.physicalTableName;
        inspector.querySelector("[data-map-inspector-type]").textContent = `${entity.badge} · ${entity.entityType}`;
        inspector.querySelector("[data-map-inspector-group]").textContent = entity.group;
        const route = inspector.querySelector("[data-map-inspector-route]");
        route.hidden = !entity.route;
        if (entity.route) route.href = entity.route;
    };
    const selectNode = (node) => {
        cy.elements().removeClass("dimmed");
        if (!node) { cy.elements().unselect(); updateInspector(null); return; }
        cy.elements().addClass("dimmed");
        node.removeClass("dimmed").select();
        node.connectedEdges().removeClass("dimmed");
        node.connectedEdges().connectedNodes().removeClass("dimmed");
        updateInspector(entityById.get(node.id()));
    };
    const syncLabels = () => {
        cy.nodes().forEach((node) => node.data("label", entityLabel(entityById.get(node.id()))));
        save();
        if (status) status.textContent = `Vista ${state.view === "logical" ? "lógica" : "física"} seleccionada.`;
    };
    const rebuildGraph = () => {
        const selected = cy.$("node:selected").id();
        cy.elements().remove();
        cy.add(elements());
        cy.layout(layoutOptions).run();
        cy.edges().toggleClass("hide-cardinality", !state.cardinality);
        if (selected && cy.$id(selected).length) selectNode(cy.$id(selected));
        save();
    };
    document.querySelectorAll("[data-map-view]").forEach((button) => button.addEventListener("click", () => {
        state.view = button.dataset.mapView;
        document.querySelectorAll("[data-map-view]").forEach((item) => { const selected = item === button; item.classList.toggle("is-selected", selected); item.setAttribute("aria-pressed", selected); });
        syncLabels();
    }));
    const cardinalityControl = document.querySelector("[data-map-cardinality]");
    const bridgeControl = document.querySelector("[data-map-bridges]");
    const physicalControl = document.querySelector("[data-map-physical-secondary]");
    cardinalityControl.checked = state.cardinality;
    bridgeControl.checked = state.bridges;
    physicalControl.checked = state.physicalSecondary;
    cardinalityControl.addEventListener("change", (event) => { state.cardinality = event.target.checked; cy.edges().toggleClass("hide-cardinality", !state.cardinality); save(); });
    bridgeControl.addEventListener("change", (event) => { state.bridges = event.target.checked; rebuildGraph(); });
    physicalControl.addEventListener("change", (event) => { state.physicalSecondary = event.target.checked; syncLabels(); });
    document.querySelectorAll("[data-map-action]").forEach((button) => button.addEventListener("click", () => {
        const action = button.dataset.mapAction;
        if (action === "fit") cy.fit(undefined, 40);
        if (action === "zoom-in") cy.zoom({ level: Math.min(2.4, cy.zoom() + .15), renderedPosition: { x: graphHost.clientWidth / 2, y: graphHost.clientHeight / 2 } });
        if (action === "zoom-out") cy.zoom({ level: Math.max(.35, cy.zoom() - .15), renderedPosition: { x: graphHost.clientWidth / 2, y: graphHost.clientHeight / 2 } });
        document.querySelector("[data-map-zoom-value]").textContent = `${Math.round(cy.zoom() * 100)} %`;
    }));
    cy.on("tap", "node", (event) => selectNode(event.target));
    cy.on("dbltap", "node", (event) => { const entity = entityById.get(event.target.id()); if (entity?.route) window.location.assign(entity.route); });
    cy.on("tap", (event) => { if (event.target === cy) selectNode(null); });
    cy.on("mouseover", "node", (event) => { graphHost.title = `${event.target.data("logicalName")} · ${event.target.data("physicalTableName")} — ${event.target.data("entityType")}`; });
    const entityList = document.querySelector("[data-map-entity-list]");
    entities.forEach((entity) => {
        const item = document.createElement("li");
        const label = entity.route ? document.createElement("a") : document.createElement("button");
        label.textContent = `[${entity.badge}] ${entity.logicalName} · ${entity.physicalTableName}`;
        label.className = "map-accessible-link";
        label.setAttribute("aria-label", `${entity.badge === "M" ? "Tabla maestra" : "Tabla transaccional"}: ${entity.logicalName}`);
        if (entity.route) label.href = entity.route;
        else { label.type = "button"; label.addEventListener("click", () => selectNode(cy.$id(entity.id))); }
        item.appendChild(label);
        entityList.appendChild(item);
    });
    const relationList = document.querySelector("[data-map-relation-list]");
    relationships.forEach((relation) => {
        const item = document.createElement("li");
        const from = entityById.get(relation.source);
        const to = entityById.get(relation.target);
        item.textContent = `${from.logicalName} (${relation.sourceCardinality}) → (${relation.targetCardinality}) ${to.logicalName} · ${relation.foreignKeyName}`;
        relationList.appendChild(item);
    });
    syncLabels();
    cy.edges().toggleClass("hide-cardinality", !state.cardinality);
};

initializeCatalogGraph();
