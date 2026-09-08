(() => {
    const canvas = document.querySelector('#family-building-block-chart');
    const dataElement = document.querySelector('#family-report-data');
    const detail = document.querySelector('[data-family-report-detail]');
    const status = document.querySelector('#family-report-status');
    if (!canvas || !dataElement || !detail || !status || typeof Chart === 'undefined') return;

    const buckets = JSON.parse(dataElement.textContent || '[]');
    const esc = value => String(value ?? '').replace(/[&<>'"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[c]));
    const renderRows = payload => {
        const totalPages = payload.totalCount === 0 ? 1 : Math.ceil(payload.totalCount / payload.pageSize);
        const rows = payload.rows || [];
        detail.innerHTML = `<h3>Familia seleccionada: ${esc(payload.familyName)} <span class="result-count">(${payload.totalCount} Building Blocks)</span></h3>` +
            (rows.length ? `<div class="responsive-table" role="region" aria-label="Building Blocks por Familia" tabindex="0"><table><thead><tr><th>Building Block</th><th>Tecnologías TSI de esta Familia</th><th>Cantidad Tecnologías</th><th>Acciones</th></tr></thead><tbody>${rows.map(row => `<tr><td data-label="Building Block">${esc(row.buildingBlockName)}</td><td data-label="Tecnologías TSI">${esc((row.technologyNames || []).join(', '))}</td><td data-label="Cantidad">${row.technologyCount}</td><td><a class="md-button md-button-text" href="${esc(row.detailRoute)}">Ver</a></td></tr>`).join('')}</tbody></table></div><p class="result-count">Página ${payload.page} de ${totalPages} · ${payload.totalCount} registros</p>` : '<p class="empty-state">No existen Building Blocks relacionados con Tecnologías TSI de esta Familia.</p>');
    };
    const chart = new Chart(canvas, {
        type: 'bar',
        data: { labels: buckets.map(item => item.familyName), datasets: [{ label: 'Building Blocks distintos', data: buckets.map(item => item.buildingBlockCount), backgroundColor: '#2457a6', borderColor: '#17345f', borderWidth: 1 }] },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            onClick: async (_, elements) => {
                const selected = elements[0] && buckets[elements[0].index];
                if (!selected) return;
                status.textContent = `Cargando Building Blocks de ${selected.familyName}…`;
                try {
                    const response = await fetch(`/Administration/TechnologyMapping/Family/${selected.familyId}/BuildingBlocks?page=1`, { headers: { Accept: 'application/json' } });
                    if (!response.ok) throw new Error('No fue posible cargar el detalle de la Familia.');
                    renderRows(await response.json());
                    status.textContent = `Familia seleccionada: ${selected.familyName}`;
                    chart.data.datasets[0].backgroundColor = buckets.map(item => item.familyId === selected.familyId ? '#b45309' : '#2457a6');
                    chart.update();
                } catch (error) { status.textContent = error.message; }
            },
            plugins: { barValueLabels: window.configureBarValueLabels ? window.configureBarValueLabels() : { fontSize: 12, padding: 4, outsideColor: '#17345f' }, tooltip: { callbacks: { label: context => ` ${context.parsed.y} Building Blocks` } } },
            scales: { x: { ticks: { autoSkip: false, maxRotation: 45 } }, y: { beginAtZero: true, ticks: { precision: 0 } } }
        }
    });
})();
