(() => {
    'use strict';

    // ----------------------------------------------------
    // 1. Selección Múltiple y Eliminación Masiva
    // ----------------------------------------------------
    const selectAllCheckbox = document.getElementById('select-all-tech');
    const rowCheckboxes = document.querySelectorAll('.tech-row-select');
    const bulkBar = document.getElementById('bulk-actions-bar');
    const bulkCount = document.getElementById('bulk-selection-count');
    const clearBtn = document.getElementById('clear-selection-btn');
    const bulkModal = document.getElementById('bulk-delete-modal');
    const bulkModalCount = document.getElementById('bulk-modal-count');
    const bulkModalList = document.getElementById('bulk-modal-list');
    const bulkConfirmationInput = document.getElementById('bulk-confirmation-input');
    const bulkModalSubmitBtn = document.getElementById('bulk-modal-submit-btn');
    const bulkSelectedIdsContainer = document.getElementById('bulk-selected-ids-container');

    const updateSelectionState = () => {
        const checkedBoxes = Array.from(document.querySelectorAll('.tech-row-select:checked'));
        const totalChecked = checkedBoxes.length;

        if (bulkCount) bulkCount.textContent = totalChecked;

        if (bulkBar) {
            bulkBar.style.display = totalChecked > 0 ? 'flex' : 'none';
        }

        if (selectAllCheckbox) {
            if (totalChecked === 0) {
                selectAllCheckbox.checked = false;
                selectAllCheckbox.indeterminate = false;
            } else if (totalChecked === rowCheckboxes.length) {
                selectAllCheckbox.checked = true;
                selectAllCheckbox.indeterminate = false;
            } else {
                selectAllCheckbox.checked = false;
                selectAllCheckbox.indeterminate = true;
            }
        }
    };

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', () => {
            const isChecked = selectAllCheckbox.checked;
            rowCheckboxes.forEach(cb => {
                cb.checked = isChecked;
            });
            updateSelectionState();
        });
    }

    rowCheckboxes.forEach(cb => {
        cb.addEventListener('change', updateSelectionState);
    });

    if (clearBtn) {
        clearBtn.addEventListener('click', () => {
            rowCheckboxes.forEach(cb => {
                cb.checked = false;
            });
            updateSelectionState();
        });
    }

    if (bulkModal) {
        bulkModal.addEventListener('show.bs.modal', () => {
            const checkedBoxes = Array.from(document.querySelectorAll('.tech-row-select:checked'));
            if (bulkModalCount) bulkModalCount.textContent = checkedBoxes.length;
            if (bulkSelectedIdsContainer) bulkSelectedIdsContainer.innerHTML = '';
            if (bulkModalList) bulkModalList.innerHTML = '';
            if (bulkConfirmationInput) bulkConfirmationInput.value = '';
            if (bulkModalSubmitBtn) bulkModalSubmitBtn.disabled = true;

            checkedBoxes.forEach(cb => {
                if (bulkSelectedIdsContainer) {
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = 'selectedIds';
                    input.value = cb.value;
                    bulkSelectedIdsContainer.appendChild(input);
                }

                if (bulkModalList) {
                    const li = document.createElement('li');
                    const company = cb.getAttribute('data-company-name') || 'Empresa';
                    const tech = cb.getAttribute('data-tech-name') || cb.value;
                    li.innerHTML = `<strong>${escapeHtml(company)}:</strong> ${escapeHtml(tech)}`;
                    bulkModalList.appendChild(li);
                }
            });
        });
    }

    if (bulkConfirmationInput && bulkModalSubmitBtn) {
        bulkConfirmationInput.addEventListener('input', () => {
            bulkModalSubmitBtn.disabled = bulkConfirmationInput.value.trim() !== 'ELIMINAR';
        });
    }

    // ----------------------------------------------------
    // 2. Reporte Gráfico y Drilldown de Tecnologías por Empresa
    // ----------------------------------------------------
    const canvas = document.getElementById('company-tech-chart');
    const drilldownSection = document.getElementById('company-tech-drilldown');
    const drilldownTitle = document.getElementById('drilldown-company-title');
    const drilldownSubtitle = document.getElementById('drilldown-company-subtitle');
    const drilldownTbody = document.getElementById('drilldown-tech-tbody');
    const drilldownCloseBtn = document.getElementById('drilldown-close-btn');

    if (drilldownCloseBtn && drilldownSection) {
        drilldownCloseBtn.addEventListener('click', () => {
            drilldownSection.hidden = true;
        });
    }

    function escapeHtml(value) {
        return String(value ?? '').replace(/[&<>"']/g, c => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }[c]));
    }

    const loadCompanyDrilldown = async (company) => {
        if (!drilldownSection || !drilldownTbody) return;

        drilldownSection.hidden = false;
        if (drilldownTitle) {
            drilldownTitle.textContent = `Tecnologías en ${company.companyName} (${company.count})`;
        }
        if (drilldownSubtitle) {
            drilldownSubtitle.textContent = `Listado de tecnologías implementadas para ${company.companyName}.`;
        }

        drilldownTbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">Cargando tecnologías…</td></tr>';

        try {
            const response = await fetch(`/Administration/MasterTables/tecnologia-tsi-implementada/companies/${company.companyId}/technologies`, {
                headers: { Accept: 'application/json' }
            });

            if (!response.ok) throw new Error('No fue posible cargar el detalle de tecnologías.');
            const data = await response.json();
            const items = data.items || [];

            if (items.length === 0) {
                drilldownTbody.innerHTML = '<tr><td colspan="6" class="empty-state text-center py-4">No hay tecnologías implementadas registradas para esta empresa.</td></tr>';
                return;
            }

            drilldownTbody.innerHTML = items.map(item => `
                <tr>
                    <td data-label="Tecnología TSI"><strong>${escapeHtml(item.tecnologia)}</strong></td>
                    <td data-label="Building Block">${escapeHtml(item.buildingBlock)}</td>
                    <td data-label="Versión desplegada">${escapeHtml(item.versionDesplegada)}</td>
                    <td data-label="Instancia corporativa">${escapeHtml(item.esInstanciaCorporativa)}</td>
                    <td data-label="Primaria">${escapeHtml(item.esTecnologiaPrimaria)}</td>
                    <td data-label="Acciones" class="table-actions" style="text-align: right;">
                        <button class="md-button md-button-danger" type="button"
                                data-bs-toggle="modal" data-bs-target="#delete-catalog-modal"
                                data-delete-impact-url="${item.deleteImpactUrl}"
                                data-delete-action-url="${item.deleteActionUrl}"
                                data-delete-row-name="${escapeHtml(item.deleteRowName)}">
                            Eliminar
                        </button>
                        <a class="md-button md-button-text" href="${item.detailsUrl}">Ver detalle</a>
                    </td>
                </tr>
            `).join('');

            drilldownSection.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        } catch (error) {
            drilldownTbody.innerHTML = `<tr><td colspan="6" class="app-alert app-alert-error">${escapeHtml(error.message)}</td></tr>`;
        }
    };

    const initDashboard = async () => {
        if (!canvas || typeof Chart === 'undefined') return;

        try {
            const response = await fetch('/Administration/MasterTables/tecnologia-tsi-implementada/company-metrics', {
                headers: { Accept: 'application/json' }
            });

            if (!response.ok) throw new Error('No fue posible cargar las métricas de empresas.');
            const data = await response.json();
            const summary = data.summary || {};
            const companies = data.companies || [];

            const kpiCompanies = document.getElementById('kpi-total-companies');
            const kpiTechs = document.getElementById('kpi-total-technologies');
            const kpiAvg = document.getElementById('kpi-avg-technologies');

            if (kpiCompanies) kpiCompanies.textContent = summary.totalCompanies ?? '0';
            if (kpiTechs) kpiTechs.textContent = summary.totalImplementations ?? '0';
            if (kpiAvg) kpiAvg.textContent = summary.averagePerCompany ?? '0';

            if (companies.length === 0) {
                const parent = canvas.parentElement;
                if (parent) parent.innerHTML = '<p class="empty-state text-center py-4">No se encontraron empresas con tecnologías implementadas.</p>';
                return;
            }

            const chart = new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: companies.map(c => c.companyName),
                    datasets: [{
                        label: 'Tecnologías Implementadas',
                        data: companies.map(c => c.count),
                        backgroundColor: '#2457a6',
                        hoverBackgroundColor: '#17345f',
                        borderColor: '#17345f',
                        borderWidth: 1,
                        borderRadius: 4
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    onClick: (event, activeElements) => {
                        if (activeElements && activeElements.length > 0) {
                            const index = activeElements[0].index;
                            const selectedCompany = companies[index];
                            if (selectedCompany) {
                                loadCompanyDrilldown(selectedCompany);
                            }
                        }
                    },
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: context => ` ${context.parsed.y} tecnología(s) implementada(s) (Clic para ver detalle)`
                            }
                        }
                    },
                    scales: {
                        x: {
                            title: { display: true, text: 'Empresa Subsidiaria' },
                            ticks: {
                                autoSkip: false,
                                maxRotation: 45,
                                font: { size: 11 }
                            },
                            grid: { display: false }
                        },
                        y: {
                            beginAtZero: true,
                            grace: '10%',
                            ticks: { precision: 0 },
                            title: { display: true, text: 'Cantidad de Tecnologías' }
                        }
                    }
                }
            });

            // Si hay empresas, pre-seleccionar o permitir clic directo
            canvas.style.cursor = 'pointer';
        } catch (error) {
            console.error('Error al inicializar dashboard de tecnologías implementadas:', error);
        }
    };

    initDashboard();
})();
