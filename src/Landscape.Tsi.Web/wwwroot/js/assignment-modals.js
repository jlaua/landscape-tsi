(() => {
    // Helper: Keyboard trap and focus management for WCAG 2.2 AA compliance
    let lastActiveElement = null;

    function setupAccessibility(modalEl) {
        if (!modalEl) return;

        modalEl.addEventListener("show.bs.modal", (e) => {
            lastActiveElement = e.relatedTarget || document.activeElement;
        });

        modalEl.addEventListener("shown.bs.modal", () => {
            const focusable = modalEl.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
            if (focusable.length) {
                focusable[0].focus();
            }
        });

        modalEl.addEventListener("hidden.bs.modal", () => {
            if (lastActiveElement && typeof lastActiveElement.focus === "function") {
                lastActiveElement.focus();
            }
        });

        modalEl.addEventListener("keydown", (e) => {
            if (e.key === "Tab") {
                const focusable = Array.from(modalEl.querySelectorAll('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'));
                if (!focusable.length) return;
                const first = focusable[0];
                const last = focusable[focusable.length - 1];
                if (e.shiftKey && document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                } else if (!e.shiftKey && document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            }
        });
    }

    // Helper: Escapes HTML to prevent XSS
    function escapeHtml(str) {
        if (!str) return "";
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // ==========================================
    // 1. Associate Capability Modal
    // ==========================================
    const assocCapModal = document.getElementById("associate-capability-modal");
    if (assocCapModal) {
        setupAccessibility(assocCapModal);
        const searchInput = document.getElementById("cap-candidate-search");
        const tbody = document.getElementById("cap-candidates-tbody");
        const impactPanel = document.getElementById("associate-cap-impact-panel");
        const impactDetails = document.getElementById("associate-cap-impact-details");
        const submitBtn = document.getElementById("btn-submit-assoc-cap");
        const assocForm = document.getElementById("associate-capability-form");
        const reassignAssocForm = document.getElementById("reassign-from-assoc-form");
        const capIdInput = document.getElementById("associate-cap-id");
        const tokenInput = document.getElementById("associate-cap-token");
        const reassignCapId = document.getElementById("reassign-assoc-cap-id");
        const reassignSourceBbId = document.getElementById("reassign-assoc-source-bb-id");
        const reassignToken = document.getElementById("reassign-assoc-token");
        const reassignJustification = document.getElementById("reassign-assoc-justification");
        const assocJustification = document.getElementById("associate-cap-justification");

        let candidates = [];
        let selectedCandidate = null;
        let isReassignMode = false;

        const actionUrl = assocForm ? assocForm.getAttribute("action") : "";
        const bbIdMatch = actionUrl.match(/building-block\/(\d+)/i);
        const currentBbId = bbIdMatch ? parseInt(bbIdMatch[1], 10) : 0;

        async function loadCandidates() {
            tbody.innerHTML = '<tr><td colspan="3" class="text-center py-3" role="status">Cargando capacidades…</td></tr>';
            submitBtn.disabled = true;
            impactPanel.hidden = true;
            try {
                const response = await fetch(`/Administration/MasterTables/building-block/${currentBbId}/capability-candidates`, {
                    headers: { Accept: "application/json" }
                });
                if (!response.ok) throw new Error("Error al obtener capacidades");
                candidates = await response.json();
                renderCandidates(candidates);
            } catch (err) {
                tbody.innerHTML = '<tr><td colspan="3" class="text-center text-danger py-3" role="alert">No fue posible cargar las capacidades candidatas.</td></tr>';
            }
        }

        function renderCandidates(items) {
            tbody.innerHTML = "";
            if (!items.length) {
                tbody.innerHTML = '<tr><td colspan="3" class="text-center text-muted py-3">No se encontraron capacidades que coincidan con la búsqueda.</td></tr>';
                return;
            }

            items.forEach((c) => {
                const tr = document.createElement("tr");
                let badge = "";
                let actionBtn = "";

                if (c.status === 0 || c.currentBuildingBlockId === null) {
                    badge = '<span class="badge bg-secondary">Sin asignar</span>';
                    actionBtn = `<button type="button" class="btn btn-sm btn-outline-primary btn-select-cap" data-id="${c.id}" data-mode="assign">Seleccionar</button>`;
                } else if (c.status === 1 || c.currentBuildingBlockId === currentBbId) {
                    badge = '<span class="badge bg-success">Ya asignada</span>';
                    actionBtn = '<button type="button" class="btn btn-sm btn-outline-secondary" disabled>Asignada</button>';
                } else {
                    const bbName = escapeHtml(c.currentBuildingBlockName || "Otro");
                    badge = `<span class="badge bg-warning text-dark">Asignada a: ${bbName}</span>`;
                    actionBtn = `<button type="button" class="btn btn-sm btn-outline-warning text-dark btn-select-cap" data-id="${c.id}" data-mode="reassign" data-source-bb="${c.currentBuildingBlockId}">Reasignar</button>`;
                }

                tr.innerHTML = `
                    <td><strong>${escapeHtml(c.name)}</strong></td>
                    <td>${badge}</td>
                    <td>${actionBtn}</td>
                `;
                tbody.appendChild(tr);
            });

            tbody.querySelectorAll(".btn-select-cap").forEach((btn) => {
                btn.addEventListener("click", () => handleCandidateSelect(btn));
            });
        }

        async function handleCandidateSelect(btn) {
            const capId = parseInt(btn.dataset.id, 10);
            const mode = btn.dataset.mode;
            selectedCandidate = candidates.find((c) => c.id === capId);
            if (!selectedCandidate) return;

            tbody.querySelectorAll("tr").forEach((row) => row.classList.remove("table-primary"));
            btn.closest("tr")?.classList.add("table-primary");

            if (mode === "assign") {
                isReassignMode = false;
                impactPanel.hidden = true;
                submitBtn.textContent = "Asociar Capacidad";
                submitBtn.disabled = false;

                capIdInput.value = selectedCandidate.id;
                try {
                    const tokenResp = await fetch(`/Administration/MasterTables/building-block/${currentBbId}/capability-reassign-impact?capabilityId=${capId}&targetBuildingBlockId=${currentBbId}`);
                    if (tokenResp.ok) {
                        const impact = await tokenResp.json();
                        tokenInput.value = impact.concurrencyToken || "";
                    }
                } catch {
                    tokenInput.value = "";
                }
            } else {
                isReassignMode = true;
                submitBtn.textContent = "Confirmar Reasignación";
                submitBtn.disabled = true;
                impactDetails.innerHTML = '<p class="text-muted" role="status">Calculando impacto de reasignación…</p>';
                impactPanel.hidden = false;

                try {
                    const response = await fetch(`/Administration/MasterTables/building-block/${currentBbId}/capability-reassign-impact?capabilityId=${capId}&targetBuildingBlockId=${currentBbId}`, {
                        headers: { Accept: "application/json" }
                    });
                    if (!response.ok) throw new Error("Error al consultar impacto");
                    const impact = await response.json();

                    let warningsHtml = "";
                    if (impact.warnings && impact.warnings.length) {
                        warningsHtml = '<ul class="mb-2 text-danger">' + impact.warnings.map((w) => `<li>${escapeHtml(w)}</li>`).join("") + '</ul>';
                    }

                    impactDetails.innerHTML = `
                        ${warningsHtml}
                        <p class="mb-1"><strong>Capacidad:</strong> ${escapeHtml(impact.capabilityName)}</p>
                        <p class="mb-1"><strong>Origen:</strong> ${escapeHtml(impact.sourceBuildingBlockName || "Sin Asignar")} &rarr; <strong>Destino:</strong> ${escapeHtml(impact.targetBuildingBlockName || "Actual")}</p>
                        <p class="mb-1"><strong>Funcionalidades hijas vinculadas:</strong> ${impact.childFunctionalitiesCount}</p>
                        <p class="mb-0"><strong>Tecnologías TSI en Building Block origen:</strong> ${impact.sourceRelatedTechnologiesCount}</p>
                    `;

                    reassignCapId.value = impact.capabilityId;
                    reassignSourceBbId.value = impact.sourceBuildingBlockId || "";
                    reassignToken.value = impact.concurrencyToken;
                    submitBtn.disabled = false;
                } catch (err) {
                    impactDetails.innerHTML = '<p class="text-danger" role="alert">No fue posible calcular el impacto de la reasignación.</p>';
                }
            }
        }

        searchInput?.addEventListener("input", (e) => {
            const query = e.target.value.trim().toLowerCase();
            const filtered = candidates.filter((c) => c.name.toLowerCase().includes(query));
            renderCandidates(filtered);
        });

        submitBtn?.addEventListener("click", () => {
            if (isReassignMode) {
                if (reassignJustification && assocJustification) {
                    reassignJustification.value = assocJustification.value;
                }
                reassignAssocForm.submit();
            } else {
                assocForm.submit();
            }
        });

        assocCapModal.addEventListener("show.bs.modal", () => {
            if (searchInput) searchInput.value = "";
            loadCandidates();
        });
    }

    // ==========================================
    // 2. Reassign Capability Modal
    // ==========================================
    const reassignCapModal = document.getElementById("reassign-capability-modal");
    if (reassignCapModal) {
        setupAccessibility(reassignCapModal);
        const displayName = document.getElementById("reassign-cap-display-name");
        const targetBbSelect = document.getElementById("reassign-cap-target-bb");
        const impactPanel = document.getElementById("reassign-cap-impact-panel");
        const impactContent = document.getElementById("reassign-cap-impact-content");
        const confirmBtn = document.getElementById("btn-confirm-reassign-cap");
        const form = document.getElementById("reassign-capability-form");
        const modalCapId = document.getElementById("reassign-modal-cap-id");
        const modalTargetBbId = document.getElementById("reassign-modal-target-bb-id");
        const modalToken = document.getElementById("reassign-modal-token");

        let activeCapId = 0;
        let activeCapName = "";
        let currentBuildingBlockId = 0;

        document.querySelectorAll(".btn-reassign-cap").forEach((btn) => {
            btn.addEventListener("click", () => {
                activeCapId = parseInt(btn.dataset.capId, 10);
                activeCapName = btn.dataset.capName || "";
                if (displayName) displayName.textContent = activeCapName;
                if (modalCapId) modalCapId.value = activeCapId;
            });
        });

        async function loadBuildingBlockOptions() {
            targetBbSelect.innerHTML = '<option value="">Cargando opciones…</option>';
            confirmBtn.disabled = true;
            impactPanel.hidden = true;

            const actionUrl = form ? form.getAttribute("action") : "";
            const bbIdMatch = actionUrl.match(/building-block\/(\d+)/i);
            currentBuildingBlockId = bbIdMatch ? parseInt(bbIdMatch[1], 10) : 0;

            try {
                const response = await fetch("/Administration/MasterTables/building-block-options", {
                    headers: { Accept: "application/json" }
                });
                if (!response.ok) throw new Error("Error al obtener opciones");
                const options = await response.json();
                targetBbSelect.innerHTML = '<option value="">-- Seleccione el Building Block de destino --</option>';
                options.forEach((opt) => {
                    if (opt.id !== currentBuildingBlockId) {
                        const el = document.createElement("option");
                        el.value = opt.id;
                        el.textContent = opt.name;
                        targetBbSelect.appendChild(el);
                    }
                });
            } catch {
                targetBbSelect.innerHTML = '<option value="">Error al cargar opciones</option>';
            }
        }

        targetBbSelect?.addEventListener("change", async () => {
            const targetBbId = parseInt(targetBbSelect.value, 10);
            if (!targetBbId) {
                confirmBtn.disabled = true;
                impactPanel.hidden = true;
                return;
            }

            confirmBtn.disabled = true;
            impactPanel.hidden = false;
            impactContent.innerHTML = '<p class="text-muted" role="status">Calculando impacto…</p>';

            try {
                const response = await fetch(`/Administration/MasterTables/building-block/${currentBuildingBlockId}/capability-reassign-impact?capabilityId=${activeCapId}&targetBuildingBlockId=${targetBbId}`, {
                    headers: { Accept: "application/json" }
                });
                if (!response.ok) throw new Error("Error al consultar impacto");
                const impact = await response.json();

                let warningsHtml = "";
                if (impact.warnings && impact.warnings.length) {
                    warningsHtml = '<ul class="mb-2 text-danger">' + impact.warnings.map((w) => `<li>${escapeHtml(w)}</li>`).join("") + '</ul>';
                }

                impactContent.innerHTML = `
                    ${warningsHtml}
                    <p class="mb-1"><strong>Capacidad:</strong> ${escapeHtml(impact.capabilityName)}</p>
                    <p class="mb-1"><strong>Destino:</strong> ${escapeHtml(impact.targetBuildingBlockName)}</p>
                    <p class="mb-1"><strong>Funcionalidades hijas vinculadas:</strong> ${impact.childFunctionalitiesCount}</p>
                    <p class="mb-0"><strong>Tecnologías TSI en Building Block origen:</strong> ${impact.sourceRelatedTechnologiesCount}</p>
                `;

                modalTargetBbId.value = targetBbId;
                modalToken.value = impact.concurrencyToken;
                confirmBtn.disabled = false;
            } catch {
                impactContent.innerHTML = '<p class="text-danger" role="alert">No fue posible calcular el impacto de la reasignación.</p>';
            }
        });

        confirmBtn?.addEventListener("click", () => {
            form.submit();
        });

        reassignCapModal.addEventListener("show.bs.modal", () => {
            loadBuildingBlockOptions();
        });
    }

    // ==========================================
    // 3. Associate Functionality Modal
    // ==========================================
    const assocFuncModal = document.getElementById("associate-functionality-modal");
    if (assocFuncModal) {
        setupAccessibility(assocFuncModal);
        const targetCapSelect = document.getElementById("associate-func-target-cap");
        const searchInput = document.getElementById("func-candidate-search");
        const tbody = document.getElementById("func-candidates-tbody");
        const impactPanel = document.getElementById("associate-func-impact-panel");
        const impactDetails = document.getElementById("associate-func-impact-details");
        const submitBtn = document.getElementById("btn-submit-assoc-func");
        const assocForm = document.getElementById("associate-functionality-form");
        const reassignAssocForm = document.getElementById("reassign-func-from-assoc-form");
        const funcIdInput = document.getElementById("associate-func-id");
        const funcTargetCapIdInput = document.getElementById("associate-func-target-cap-id");
        const funcTokenInput = document.getElementById("associate-func-token");
        const reassignFuncId = document.getElementById("reassign-func-assoc-id");
        const reassignFuncSourceCapId = document.getElementById("reassign-func-assoc-source-cap-id");
        const reassignFuncTargetCapId = document.getElementById("reassign-func-assoc-target-cap-id");
        const reassignFuncToken = document.getElementById("reassign-func-assoc-token");
        const reassignFuncJustification = document.getElementById("reassign-func-assoc-justification");
        const assocFuncJustification = document.getElementById("associate-func-justification");

        let candidates = [];
        let selectedCandidate = null;
        let isReassignMode = false;

        const actionUrl = assocForm ? assocForm.getAttribute("action") : "";
        const bbIdMatch = actionUrl.match(/building-block\/(\d+)/i);
        const currentBbId = bbIdMatch ? parseInt(bbIdMatch[1], 10) : 0;

        async function loadCandidates() {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center py-3" role="status">Cargando funcionalidades…</td></tr>';
            submitBtn.disabled = true;
            impactPanel.hidden = true;
            try {
                const response = await fetch(`/Administration/MasterTables/building-block/${currentBbId}/functionality-candidates`, {
                    headers: { Accept: "application/json" }
                });
                if (!response.ok) throw new Error("Error al obtener funcionalidades");
                candidates = await response.json();
                renderCandidates(candidates);
            } catch {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-danger py-3" role="alert">No fue posible cargar las funcionalidades candidatas.</td></tr>';
            }
        }

        function renderCandidates(items) {
            tbody.innerHTML = "";
            if (!items.length) {
                tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">No se encontraron funcionalidades que coincidan.</td></tr>';
                return;
            }

            items.forEach((f) => {
                const tr = document.createElement("tr");
                let badge = "";
                let actionBtn = "";

                if (f.status === 0 || f.currentCapabilityId === null) {
                    badge = '<span class="badge bg-secondary">Sin asignar</span>';
                    actionBtn = `<button type="button" class="btn btn-sm btn-outline-primary btn-select-func" data-id="${f.id}" data-mode="assign">Seleccionar</button>`;
                } else {
                    const capName = escapeHtml(f.currentCapabilityName || "Otra capacidad");
                    badge = `<span class="badge bg-warning text-dark">Asignada a: ${capName}</span>`;
                    actionBtn = `<button type="button" class="btn btn-sm btn-outline-warning text-dark btn-select-func" data-id="${f.id}" data-mode="reassign" data-source-cap="${f.currentCapabilityId}">Reasignar</button>`;
                }

                tr.innerHTML = `
                    <td><strong>${escapeHtml(f.name)}</strong></td>
                    <td>${escapeHtml(f.currentCapabilityName || "—")}</td>
                    <td>${badge}</td>
                    <td>${actionBtn}</td>
                `;
                tbody.appendChild(tr);
            });

            tbody.querySelectorAll(".btn-select-func").forEach((btn) => {
                btn.addEventListener("click", () => handleCandidateSelect(btn));
            });
        }

        async function handleCandidateSelect(btn) {
            const funcId = parseInt(btn.dataset.id, 10);
            const mode = btn.dataset.mode;
            const targetCapId = parseInt(targetCapSelect.value, 10);

            if (!targetCapId) {
                alert("Por favor seleccione primero la Capacidad de Seguridad de destino.");
                targetCapSelect.focus();
                return;
            }

            selectedCandidate = candidates.find((f) => f.id === funcId);
            if (!selectedCandidate) return;

            tbody.querySelectorAll("tr").forEach((row) => row.classList.remove("table-primary"));
            btn.closest("tr")?.classList.add("table-primary");

            if (mode === "assign") {
                isReassignMode = false;
                impactPanel.hidden = true;
                submitBtn.textContent = "Asociar Funcionalidad";
                submitBtn.disabled = false;

                funcIdInput.value = selectedCandidate.id;
                funcTargetCapIdInput.value = targetCapId;

                try {
                    const previewResp = await fetch(`/Administration/MasterTables/building-block/${currentBbId}/functionality-reassign-impact?functionalityId=${funcId}&targetCapabilityId=${targetCapId}`);
                    if (previewResp.ok) {
                        const impact = await previewResp.json();
                        funcTokenInput.value = impact.concurrencyToken || "";
                    }
                } catch {
                    funcTokenInput.value = "";
                }
            } else {
                isReassignMode = true;
                submitBtn.textContent = "Confirmar Reasignación";
                submitBtn.disabled = true;
                impactDetails.innerHTML = '<p class="text-muted" role="status">Calculando impacto…</p>';
                impactPanel.hidden = false;

                try {
                    const response = await fetch(`/Administration/MasterTables/building-block/${currentBbId}/functionality-reassign-impact?functionalityId=${funcId}&targetCapabilityId=${targetCapId}`, {
                        headers: { Accept: "application/json" }
                    });
                    if (!response.ok) throw new Error("Error al consultar impacto");
                    const impact = await response.json();

                    let banner = "";
                    if (impact.isCrossBuildingBlock) {
                        banner = `<div class="alert alert-danger py-2 mb-2 fw-bold" role="alert">ADVERTENCIA: Esta reasignación mueve la funcionalidad fuera del Building Block actual (de '${escapeHtml(impact.sourceBuildingBlockName || "Sin Asignar")}' a '${escapeHtml(impact.targetBuildingBlockName || "Actual")}').</div>`;
                    }

                    let warningsHtml = "";
                    if (impact.warnings && impact.warnings.length) {
                        warningsHtml = '<ul class="mb-2 text-danger">' + impact.warnings.map((w) => `<li>${escapeHtml(w)}</li>`).join("") + '</ul>';
                    }

                    impactDetails.innerHTML = `
                        ${banner}
                        ${warningsHtml}
                        <p class="mb-1"><strong>Funcionalidad:</strong> ${escapeHtml(impact.functionalityName)}</p>
                        <p class="mb-1"><strong>Capacidad Origen:</strong> ${escapeHtml(impact.sourceCapabilityName || "Sin Asignar")}</p>
                        <p class="mb-0"><strong>Capacidad Destino:</strong> ${escapeHtml(impact.targetCapabilityName)}</p>
                    `;

                    reassignFuncId.value = impact.functionalityId;
                    reassignFuncSourceCapId.value = impact.sourceCapabilityId || "";
                    reassignFuncTargetCapId.value = impact.targetCapabilityId;
                    reassignFuncToken.value = impact.concurrencyToken;
                    submitBtn.disabled = false;
                } catch {
                    impactDetails.innerHTML = '<p class="text-danger" role="alert">No fue posible calcular el impacto de la reasignación.</p>';
                }
            }
        }

        searchInput?.addEventListener("input", (e) => {
            const query = e.target.value.trim().toLowerCase();
            const filtered = candidates.filter((f) => f.name.toLowerCase().includes(query));
            renderCandidates(filtered);
        });

        submitBtn?.addEventListener("click", () => {
            if (isReassignMode) {
                if (reassignFuncJustification && assocFuncJustification) {
                    reassignFuncJustification.value = assocFuncJustification.value;
                }
                reassignAssocForm.submit();
            } else {
                assocForm.submit();
            }
        });

        assocFuncModal.addEventListener("show.bs.modal", () => {
            if (searchInput) searchInput.value = "";
            loadCandidates();
        });
    }

    // ==========================================
    // 4. Reassign Functionality Modal
    // ==========================================
    const reassignFuncModal = document.getElementById("reassign-functionality-modal");
    if (reassignFuncModal) {
        setupAccessibility(reassignFuncModal);
        const displayName = document.getElementById("reassign-func-display-name");
        const targetCapSelect = document.getElementById("reassign-func-target-cap");
        const impactPanel = document.getElementById("reassign-func-impact-panel");
        const impactContent = document.getElementById("reassign-func-impact-content");
        const confirmBtn = document.getElementById("btn-confirm-reassign-func");
        const form = document.getElementById("reassign-functionality-form");
        const modalFuncId = document.getElementById("reassign-modal-func-id");
        const modalSourceCapId = document.getElementById("reassign-modal-func-source-cap-id");
        const modalTargetCapId = document.getElementById("reassign-modal-func-target-cap-id");
        const modalToken = document.getElementById("reassign-modal-func-token");

        let activeFuncId = 0;
        let activeFuncName = "";
        let activeCapId = 0;
        let activeCapName = "";
        let currentBuildingBlockId = 0;

        document.querySelectorAll(".btn-reassign-func").forEach((btn) => {
            btn.addEventListener("click", () => {
                activeFuncId = parseInt(btn.dataset.funcId, 10);
                activeFuncName = btn.dataset.funcName || "";
                activeCapId = parseInt(btn.dataset.capId, 10) || 0;
                activeCapName = btn.dataset.capName || "";

                if (displayName) displayName.textContent = activeFuncName;
                if (modalFuncId) modalFuncId.value = activeFuncId;
                if (modalSourceCapId) modalSourceCapId.value = activeCapId;
            });
        });

        async function loadCapabilityOptions() {
            targetCapSelect.innerHTML = '<option value="">Cargando opciones…</option>';
            confirmBtn.disabled = true;
            impactPanel.hidden = true;

            const actionUrl = form ? form.getAttribute("action") : "";
            const bbIdMatch = actionUrl.match(/building-block\/(\d+)/i);
            currentBuildingBlockId = bbIdMatch ? parseInt(bbIdMatch[1], 10) : 0;

            try {
                const response = await fetch("/Administration/MasterTables/capability-options", {
                    headers: { Accept: "application/json" }
                });
                if (!response.ok) throw new Error("Error al obtener opciones");
                const options = await response.json();
                targetCapSelect.innerHTML = '<option value="">-- Seleccione la Capacidad de destino --</option>';
                options.forEach((opt) => {
                    if (opt.id !== activeCapId) {
                        const el = document.createElement("option");
                        el.value = opt.id;
                        el.textContent = opt.name;
                        targetCapSelect.appendChild(el);
                    }
                });
            } catch {
                targetCapSelect.innerHTML = '<option value="">Error al cargar opciones</option>';
            }
        }

        targetCapSelect?.addEventListener("change", async () => {
            const targetCapId = parseInt(targetCapSelect.value, 10);
            if (!targetCapId) {
                confirmBtn.disabled = true;
                impactPanel.hidden = true;
                return;
            }

            confirmBtn.disabled = true;
            impactPanel.hidden = false;
            impactContent.innerHTML = '<p class="text-muted" role="status">Calculando impacto…</p>';

            try {
                const response = await fetch(`/Administration/MasterTables/building-block/${currentBuildingBlockId}/functionality-reassign-impact?functionalityId=${activeFuncId}&targetCapabilityId=${targetCapId}`, {
                    headers: { Accept: "application/json" }
                });
                if (!response.ok) throw new Error("Error al consultar impacto");
                const impact = await response.json();

                let banner = "";
                if (impact.isCrossBuildingBlock) {
                    banner = `<div class="alert alert-danger py-2 mb-2 fw-bold" role="alert">ADVERTENCIA: Esta reasignación mueve la funcionalidad fuera del Building Block actual (de '${escapeHtml(impact.sourceBuildingBlockName || "Sin Asignar")}' a '${escapeHtml(impact.targetBuildingBlockName || "Actual")}').</div>`;
                }

                let warningsHtml = "";
                if (impact.warnings && impact.warnings.length) {
                    warningsHtml = '<ul class="mb-2 text-danger">' + impact.warnings.map((w) => `<li>${escapeHtml(w)}</li>`).join("") + '</ul>';
                }

                impactContent.innerHTML = `
                    ${banner}
                    ${warningsHtml}
                    <p class="mb-1"><strong>Funcionalidad:</strong> ${escapeHtml(impact.functionalityName)}</p>
                    <p class="mb-1"><strong>Capacidad Origen:</strong> ${escapeHtml(impact.sourceCapabilityName || "Sin Asignar")}</p>
                    <p class="mb-0"><strong>Capacidad Destino:</strong> ${escapeHtml(impact.targetCapabilityName)}</p>
                `;

                modalTargetCapId.value = targetCapId;
                modalToken.value = impact.concurrencyToken;
                confirmBtn.disabled = false;
            } catch {
                impactContent.innerHTML = '<p class="text-danger" role="alert">No fue posible calcular el impacto de la reasignación.</p>';
            }
        });

        confirmBtn?.addEventListener("click", () => {
            form.submit();
        });

        reassignFuncModal.addEventListener("show.bs.modal", () => {
            loadCapabilityOptions();
        });
    }
})();
