(() => {
    'use strict';

    const vendorModalEl = document.getElementById('vendor-technologies-modal');
    if (!vendorModalEl) {
        initDetailsDisassociateButtons();
        return;
    }

    const vendorNameEl = document.getElementById('vendor-modal-vendor-name');
    const loadingEl = document.getElementById('vendor-modal-loading');
    const errorEl = document.getElementById('vendor-modal-error');
    const contentEl = document.getElementById('vendor-modal-content');
    const badgeCountEl = document.getElementById('vendor-modal-badge-count');
    const tbodyEl = document.getElementById('vendor-modal-tbody');
    const emptyEl = document.getElementById('vendor-modal-empty');
    const notificationEl = document.getElementById('vendor-modal-notification');

    // Tab buttons
    const tabListBtn = document.getElementById('tab-vtech-list-btn');
    const tabAssociateBtn = document.getElementById('tab-vtech-associate-btn');
    const tabCreateBtn = document.getElementById('tab-vtech-create-btn');

    // Tab switch shortcuts
    const btnSwitchToAssociate = document.getElementById('btn-switch-to-associate');
    const btnSwitchToCreate = document.getElementById('btn-switch-to-create');
    const linkToCreateTech = document.getElementById('linkToCreateTech');
    const btnCancelQuickCreateTech = document.getElementById('btnCancelQuickCreateTech');

    // Tab 2 elements
    const inputSearch = document.getElementById('inputVendorTechSearch');
    const btnResetSearch = document.getElementById('btnResetVendorTechSearch');
    const availableCountEl = document.getElementById('vendorTechAvailableCount');
    const availableLoadingEl = document.getElementById('vendor-available-loading');
    const tbodyAvailableEl = document.getElementById('tbodyVendorAvailableResults');

    // Tab 3 elements
    const formQuickCreate = document.getElementById('formQuickCreateVendorTech');
    const selectFamilia = document.getElementById('qvTechFamilia');
    const selectVersionDesplegada = document.getElementById('qvTechVersionDesplegada');
    const selectEstadoAdopcion = document.getElementById('qvTechEstadoAdopcion');
    const selectPosturaRoadmap = document.getElementById('qvTechPosturaRoadmap');
    const btnSubmitQuickCreate = document.getElementById('btnSubmitQuickCreateTech');

    let currentVendorId = null;
    let currentVendorName = null;
    let cachedCreateOptions = null;
    let searchDebounceTimer = null;
    let triggerElement = null;

    function getCsrfToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    function showNotification(message, type = 'success') {
        if (!notificationEl) return;
        notificationEl.hidden = false;
        notificationEl.innerHTML = `
            <div class="alert alert-${type} alert-dismissible fade show py-2 px-3 mb-0" role="alert">
                <span>${message}</span>
                <button type="button" class="btn-close py-2" data-bs-dismiss="alert" aria-label="Cerrar"></button>
            </div>`;
        setTimeout(() => {
            if (notificationEl && !notificationEl.hidden) {
                notificationEl.hidden = true;
                notificationEl.innerHTML = '';
            }
        }, 5000);
    }

    function switchTab(tabButton) {
        if (tabButton && window.bootstrap && bootstrap.Tab) {
            bootstrap.Tab.getOrCreateInstance(tabButton).show();
        }
    }

    if (btnSwitchToAssociate) btnSwitchToAssociate.addEventListener('click', () => switchTab(tabAssociateBtn));
    if (btnSwitchToCreate) btnSwitchToCreate.addEventListener('click', () => switchTab(tabCreateBtn));
    if (linkToCreateTech) linkToCreateTech.addEventListener('click', () => switchTab(tabCreateBtn));
    if (btnCancelQuickCreateTech) btnCancelQuickCreateTech.addEventListener('click', () => switchTab(tabListBtn));

    // When clicking tabs
    if (tabAssociateBtn) {
        tabAssociateBtn.addEventListener('shown.bs.tab', () => {
            loadAvailableTechnologies();
        });
    }

    if (tabCreateBtn) {
        tabCreateBtn.addEventListener('shown.bs.tab', () => {
            loadCreateOptions();
        });
    }

    vendorModalEl.addEventListener('show.bs.modal', async (event) => {
        triggerElement = event.relatedTarget;
        if (!triggerElement) return;

        currentVendorId = triggerElement.getAttribute('data-vendor-id');
        currentVendorName = triggerElement.getAttribute('data-vendor-name') || `Vendor #${currentVendorId}`;
        const initialTab = triggerElement.getAttribute('data-initial-tab');

        if (vendorNameEl) vendorNameEl.textContent = currentVendorName;
        if (notificationEl) {
            notificationEl.hidden = true;
            notificationEl.innerHTML = '';
        }

        // Switch to appropriate initial tab
        if (initialTab === 'associate') {
            switchTab(tabAssociateBtn);
        } else if (initialTab === 'create') {
            switchTab(tabCreateBtn);
        } else {
            switchTab(tabListBtn);
        }

        await loadVendorTechnologies();
    });

    async function loadVendorTechnologies() {
        if (!currentVendorId) return;

        if (loadingEl) loadingEl.hidden = false;
        if (errorEl) errorEl.hidden = true;
        if (contentEl) contentEl.hidden = true;
        if (emptyEl) emptyEl.hidden = true;
        if (tbodyEl) tbodyEl.innerHTML = '';

        try {
            const response = await fetch(`/Administration/MasterTables/vendor/${encodeURIComponent(currentVendorId)}/technologies`, {
                headers: {
                    'Accept': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const data = await response.json();
            const items = data.items || [];
            const count = data.totalCount ?? items.length;

            if (badgeCountEl) {
                badgeCountEl.textContent = `${count}`;
            }

            // Also update count badge on the trigger button if it exists in the main grid
            if (triggerElement && triggerElement.classList.contains('badge')) {
                triggerElement.textContent = `${count} ${count === 1 ? 'tecnología' : 'tecnologías'}`;
            }

            if (tbodyEl) tbodyEl.innerHTML = '';

            if (items.length === 0) {
                if (emptyEl) emptyEl.hidden = false;
            } else {
                if (emptyEl) emptyEl.hidden = true;
                items.forEach((item) => {
                    const tr = document.createElement('tr');

                    const tdId = document.createElement('td');
                    tdId.className = 'text-muted';
                    tdId.textContent = item.id;
                    tr.appendChild(tdId);

                    const tdCorp = document.createElement('td');
                    const strongCorp = document.createElement('strong');
                    strongCorp.textContent = item.nombreCorporativo || '—';
                    tdCorp.appendChild(strongCorp);
                    tr.appendChild(tdCorp);

                    const tdLocal = document.createElement('td');
                    tdLocal.textContent = item.nombreLocal || '—';
                    tr.appendChild(tdLocal);

                    const tdFam = document.createElement('td');
                    tdFam.textContent = item.familia || '—';
                    tr.appendChild(tdFam);

                    const tdEstado = document.createElement('td');
                    if (item.estadoAdopcion && item.estadoAdopcion !== '—') {
                        const badge = document.createElement('span');
                        badge.className = 'badge bg-light text-dark border';
                        badge.textContent = item.estadoAdopcion;
                        tdEstado.appendChild(badge);
                    } else {
                        tdEstado.textContent = '—';
                    }
                    tr.appendChild(tdEstado);

                    const tdLic = document.createElement('td');
                    tdLic.textContent = item.licenciamiento || '—';
                    tr.appendChild(tdLic);

                    const tdEntorno = document.createElement('td');
                    tdEntorno.textContent = item.entorno || '—';
                    tr.appendChild(tdEntorno);

                    const tdActions = document.createElement('td');
                    tdActions.className = 'text-end pe-2';
                    const btnGroup = document.createElement('div');
                    btnGroup.className = 'btn-group btn-group-sm';

                    if (item.detailsUrl) {
                        const link = document.createElement('a');
                        link.className = 'btn btn-sm btn-outline-secondary';
                        link.href = item.detailsUrl;
                        link.textContent = 'Ver detalle';
                        btnGroup.appendChild(link);
                    }

                    const btnDisassociate = document.createElement('button');
                    btnDisassociate.type = 'button';
                    btnDisassociate.className = 'btn btn-sm btn-outline-danger';
                    btnDisassociate.textContent = 'Desasociar';
                    btnDisassociate.title = 'Desvincular del fabricante';
                    btnDisassociate.addEventListener('click', () => disassociateTechnology(item.id, item.nombreCorporativo));
                    btnGroup.appendChild(btnDisassociate);

                    tdActions.appendChild(btnGroup);
                    tr.appendChild(tdActions);

                    tbodyEl.appendChild(tr);
                });
            }

            if (loadingEl) loadingEl.hidden = true;
            if (contentEl) contentEl.hidden = false;
        } catch (err) {
            console.error('Error al cargar tecnologías del fabricante:', err);
            if (loadingEl) loadingEl.hidden = true;
            if (errorEl) errorEl.hidden = false;
        }
    }

    async function loadAvailableTechnologies() {
        if (!currentVendorId) return;

        if (availableLoadingEl) availableLoadingEl.hidden = false;
        if (tbodyAvailableEl) tbodyAvailableEl.innerHTML = '';

        const searchQuery = inputSearch ? inputSearch.value.trim() : '';

        try {
            const url = `/Administration/MasterTables/vendor/${encodeURIComponent(currentVendorId)}/technologies/available?search=${encodeURIComponent(searchQuery)}`;
            const response = await fetch(url, {
                headers: {
                    'Accept': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const data = await response.json();
            const items = data.items || [];

            if (availableCountEl) {
                availableCountEl.textContent = `${items.length} ${items.length === 1 ? 'tecnología disponible' : 'tecnologías disponibles'}`;
            }

            if (tbodyAvailableEl) {
                tbodyAvailableEl.innerHTML = '';
                if (items.length === 0) {
                    const tr = document.createElement('tr');
                    tr.innerHTML = `<td colspan="6" class="text-center text-muted py-4">No se encontraron tecnologías TSI disponibles para asociar.</td>`;
                    tbodyAvailableEl.appendChild(tr);
                } else {
                    items.forEach((item) => {
                        const tr = document.createElement('tr');
                        tr.innerHTML = `
                            <td class="text-muted">${item.id}</td>
                            <td><strong>${escapeHtml(item.nombreCorporativo)}</strong></td>
                            <td class="text-muted small">${escapeHtml(item.nombreLocal || '—')}</td>
                            <td><span class="badge bg-light text-dark border">${escapeHtml(item.familia || '—')}</span></td>
                            <td><span class="small">${escapeHtml(item.estadoAdopcion || '—')}</span></td>
                            <td class="text-end pe-2">
                                <button type="button" class="btn btn-sm btn-primary py-0 px-2 btn-assoc-action" data-tech-id="${item.id}" data-tech-name="${escapeHtml(item.nombreCorporativo)}">
                                    ➕ Asociar
                                </button>
                            </td>`;
                        tbodyAvailableEl.appendChild(tr);
                    });

                    // Add associate click handlers
                    tbodyAvailableEl.querySelectorAll('.btn-assoc-action').forEach(btn => {
                        btn.addEventListener('click', async () => {
                            const techId = parseInt(btn.getAttribute('data-tech-id'), 10);
                            const techName = btn.getAttribute('data-tech-name');
                            await associateTechnology(techId, techName, btn);
                        });
                    });
                }
            }
        } catch (err) {
            console.error('Error al cargar tecnologías disponibles:', err);
            if (tbodyAvailableEl) {
                tbodyAvailableEl.innerHTML = `<tr><td colspan="6" class="text-center text-danger py-3">Error al consultar catálogo de tecnologías.</td></tr>`;
            }
        } finally {
            if (availableLoadingEl) availableLoadingEl.hidden = true;
        }
    }

    async function associateTechnology(techId, techName, buttonEl) {
        if (!currentVendorId || !techId) return;

        if (buttonEl) {
            buttonEl.disabled = true;
            buttonEl.textContent = 'Asociando...';
        }

        try {
            const response = await fetch(`/Administration/MasterTables/vendor/${encodeURIComponent(currentVendorId)}/technologies/associate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken(),
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify({ tecnologiaId: techId })
            });

            const result = await response.json();
            if (response.ok && result.success) {
                showNotification(`Tecnología "${techName}" asociada exitosamente.`);
                await loadVendorTechnologies();
                await loadAvailableTechnologies();
                switchTab(tabListBtn);
            } else {
                alert(result.message || 'Error al asociar tecnología.');
                if (buttonEl) {
                    buttonEl.disabled = false;
                    buttonEl.textContent = '➕ Asociar';
                }
            }
        } catch (err) {
            console.error('Error asociando tecnología:', err);
            alert('Ocurrió un error inesperado al asociar la tecnología.');
            if (buttonEl) {
                buttonEl.disabled = false;
                buttonEl.textContent = '➕ Asociar';
            }
        }
    }

    async function disassociateTechnology(techId, techName) {
        if (!currentVendorId || !techId) return;

        if (!confirm(`¿Está seguro de desasociar la tecnología "${techName}" de este fabricante?`)) {
            return;
        }

        try {
            const response = await fetch(`/Administration/MasterTables/vendor/${encodeURIComponent(currentVendorId)}/technologies/${encodeURIComponent(techId)}/disassociate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken(),
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            const result = await response.json();
            if (response.ok && result.success) {
                showNotification(`Tecnología "${techName}" desasociada exitosamente.`);
                await loadVendorTechnologies();
                if (tabAssociateBtn && tabAssociateBtn.classList.contains('active')) {
                    await loadAvailableTechnologies();
                }
            } else {
                alert(result.message || 'Error al desasociar tecnología.');
            }
        } catch (err) {
            console.error('Error desasociando tecnología:', err);
            alert('Ocurrió un error inesperado al desasociar la tecnología.');
        }
    }

    async function loadCreateOptions() {
        if (cachedCreateOptions) {
            populateCreateSelects(cachedCreateOptions);
            return;
        }

        if (!currentVendorId) return;

        try {
            const response = await fetch(`/Administration/MasterTables/vendor/${encodeURIComponent(currentVendorId)}/technologies/create-options`, {
                headers: {
                    'Accept': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            cachedCreateOptions = await response.json();
            populateCreateSelects(cachedCreateOptions);
        } catch (err) {
            console.error('Error al cargar opciones de creación:', err);
        }
    }

    function populateCreateSelects(options) {
        if (selectFamilia && selectFamilia.options.length <= 1 && options.familias) {
            options.familias.forEach(f => {
                const opt = document.createElement('option');
                opt.value = f.id;
                opt.textContent = f.nombre;
                selectFamilia.appendChild(opt);
            });
        }

        if (selectVersionDesplegada && selectVersionDesplegada.options.length <= 1 && options.versionesDesplegadas) {
            options.versionesDesplegadas.forEach(v => {
                const opt = document.createElement('option');
                opt.value = v.id;
                opt.textContent = v.nombre;
                selectVersionDesplegada.appendChild(opt);
            });
        }

        if (selectEstadoAdopcion && selectEstadoAdopcion.options.length <= 1 && options.estadosAdopcion) {
            options.estadosAdopcion.forEach(e => {
                const opt = document.createElement('option');
                opt.value = e.id;
                opt.textContent = e.nombre;
                selectEstadoAdopcion.appendChild(opt);
            });
        }

        if (selectPosturaRoadmap && selectPosturaRoadmap.options.length <= 1 && options.posturasRoadmap) {
            options.posturasRoadmap.forEach(p => {
                const opt = document.createElement('option');
                opt.value = p.id;
                opt.textContent = p.nombre;
                selectPosturaRoadmap.appendChild(opt);
            });
        }
    }

    if (inputSearch) {
        inputSearch.addEventListener('input', () => {
            clearTimeout(searchDebounceTimer);
            searchDebounceTimer = setTimeout(loadAvailableTechnologies, 300);
        });
    }

    if (btnResetSearch) {
        btnResetSearch.addEventListener('click', () => {
            if (inputSearch) inputSearch.value = '';
            loadAvailableTechnologies();
        });
    }

    if (formQuickCreate) {
        formQuickCreate.addEventListener('submit', async (e) => {
            e.preventDefault();
            if (!currentVendorId) return;

            const corpName = document.getElementById('qvTechNombreCorp')?.value.trim();
            if (!corpName) {
                alert('El nombre corporativo es obligatorio.');
                return;
            }

            if (btnSubmitQuickCreate) {
                btnSubmitQuickCreate.disabled = true;
                btnSubmitQuickCreate.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Guardando...';
            }

            const payload = {
                nombreCorporativo: corpName,
                nombreLocal: document.getElementById('qvTechNombreLocal')?.value.trim() || null,
                idFamilia: selectFamilia?.value ? parseInt(selectFamilia.value, 10) : null,
                idVersionDesplegada: selectVersionDesplegada?.value ? parseInt(selectVersionDesplegada.value, 10) : null,
                grupo: document.getElementById('qvTechGrupo')?.value.trim() || null,
                idEstadoAdopcion: selectEstadoAdopcion?.value ? parseInt(selectEstadoAdopcion.value, 10) : null,
                idPosturaRoadmap: selectPosturaRoadmap?.value ? parseInt(selectPosturaRoadmap.value, 10) : null,
                licenciamiento: document.getElementById('qvTechLicenciamiento')?.value.trim() || null,
                entorno: document.getElementById('qvTechEntorno')?.value.trim() || null
            };

            try {
                const response = await fetch(`/Administration/MasterTables/vendor/${encodeURIComponent(currentVendorId)}/technologies/create`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getCsrfToken(),
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();
                if (response.ok && result.success) {
                    formQuickCreate.reset();
                    showNotification(`Tecnología "${corpName}" registrada y asociada exitosamente.`);
                    await loadVendorTechnologies();
                    switchTab(tabListBtn);
                } else {
                    alert(result.message || 'Error al registrar tecnología.');
                }
            } catch (err) {
                console.error('Error registrando tecnología:', err);
                alert('Ocurrió un error inesperado al registrar la tecnología.');
            } finally {
                if (btnSubmitQuickCreate) {
                    btnSubmitQuickCreate.disabled = false;
                    btnSubmitQuickCreate.textContent = '💾 Guardar y Asociar a Fabricante';
                }
            }
        });
    }

    function initDetailsDisassociateButtons() {
        document.querySelectorAll('.btn-details-disassociate-tech').forEach(btn => {
            btn.addEventListener('click', async () => {
                const vendorId = btn.getAttribute('data-vendor-id');
                const techId = btn.getAttribute('data-tech-id');
                const techName = btn.getAttribute('data-tech-name');

                if (!vendorId || !techId) return;

                if (!confirm(`¿Está seguro de desasociar la tecnología "${techName}" de este fabricante?`)) {
                    return;
                }

                btn.disabled = true;
                btn.textContent = 'Desasociando...';

                try {
                    const response = await fetch(`/Administration/MasterTables/vendor/${encodeURIComponent(vendorId)}/technologies/${encodeURIComponent(techId)}/disassociate`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': getCsrfToken(),
                            'X-Requested-With': 'XMLHttpRequest'
                        }
                    });

                    const result = await response.json();
                    if (response.ok && result.success) {
                        const tr = btn.closest('tr');
                        if (tr) {
                            tr.remove();
                        } else {
                            window.location.reload();
                        }
                    } else {
                        alert(result.message || 'Error al desasociar tecnología.');
                        btn.disabled = false;
                        btn.textContent = 'Desasociar';
                    }
                } catch (err) {
                    console.error('Error desasociando:', err);
                    alert('Error inesperado al desasociar la tecnología.');
                    btn.disabled = false;
                    btn.textContent = 'Desasociar';
                }
            });
        });
    }

    initDetailsDisassociateButtons();

    function escapeHtml(text) {
        if (!text) return '';
        const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return text.toString().replace(/[&<>"']/g, m => map[m]);
    }
})();
