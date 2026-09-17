const barValueLabelsPlugin = {
    id: 'barValueLabels',
    afterDatasetsDraw(chart, _args, options) {
        const { ctx, chartArea } = chart;
        const fontSize = options.fontSize ?? 12;
        const padding = options.padding ?? 4;
        const numberFormat = new Intl.NumberFormat(document.documentElement.lang || 'es-PE', { maximumFractionDigits: 0 });
        ctx.save();
        ctx.font = `700 ${fontSize}px ${options.fontFamily ?? 'inherit'}`;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';

        chart.data.datasets.forEach((dataset, datasetIndex) => {
            const meta = chart.getDatasetMeta(datasetIndex);
            if (meta.hidden || meta.type !== 'bar') return;
            meta.data.forEach((bar, index) => {
                const value = Number(dataset.data[index]);
                if (!Number.isFinite(value)) return;
                const height = Math.abs(bar.base - bar.y);
                const inside = height >= fontSize + (padding * 2);
                const y = inside
                    ? bar.y + ((bar.base - bar.y) / 2)
                    : Math.max(chartArea.top + (fontSize / 2), bar.y - padding - (fontSize / 2));
                const background = Array.isArray(dataset.backgroundColor)
                    ? dataset.backgroundColor[index]
                    : dataset.backgroundColor;
                ctx.fillStyle = inside ? contrastingTextColor(background) : (options.outsideColor ?? '#17345f');
                ctx.fillText(numberFormat.format(value), bar.x, y);
            });
        });
        ctx.restore();
    }
};

const contrastingTextColor = color => {
    const match = String(color ?? '').match(/^#([0-9a-f]{6})$/i);
    if (!match) return '#ffffff';
    const red = Number.parseInt(match[1].slice(0, 2), 16);
    const green = Number.parseInt(match[1].slice(2, 4), 16);
    const blue = Number.parseInt(match[1].slice(4, 6), 16);
    return ((red * 299) + (green * 587) + (blue * 114)) / 1000 >= 150 ? '#17345f' : '#ffffff';
};

const configureBarValueLabels = () => ({
    fontSize: 12,
    padding: 4,
    outsideColor: '#17345f'
});
window.configureBarValueLabels = configureBarValueLabels;

if (typeof Chart !== 'undefined') Chart.register(barValueLabelsPlugin);

(() => {
    const canvas = document.querySelector('#catalog-report-chart');
    const filter = document.querySelector('[data-report-filter]');
    const status = document.querySelector('[data-report-status]');
    const table = document.querySelector('[data-report-table]');
    const summary = document.querySelector('[data-report-summary-wrap]');
    const prompt = document.querySelector('[data-report-selection-prompt]');
    const panel = document.querySelector('[data-report-detail-panel]');
    if (!canvas || !filter || !status || !table || !summary || !prompt || !panel || typeof Chart === 'undefined') return;
    let chart, chartItems = [], selectedCode, selectedName, detailPage = 1, detailMode = 'catalog', relationSelection, sortColumn = '', requestVersion = 0;
    const relationCharts = [];
    let sortDirection = 'asc';
    const colors = ['#2457a6', '#7a4ba8', '#087f8c', '#b45309', '#2f6f44', '#a23d3d'];
    const esc = value => String(value ?? '').replace(/[&<>'"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[c]));
    const json = async url => { const response = await fetch(url, { headers: { Accept: 'application/json' } }); if (!response.ok) throw new Error('No fue posible obtener los datos.'); return response.json(); };
    const setBreadcrumb = (recordName = null, childName = null) => {
        const breadcrumb = document.querySelector('[data-report-breadcrumb]');
        breadcrumb.innerHTML = '<a href="/reporteria" data-report-root>Todos los catálogos</a>' + (selectedName ? ` <span aria-hidden="true">›</span> <span>${esc(selectedName)}</span>` : '') + (recordName ? ` <span aria-hidden="true">›</span> <span>${esc(recordName)}</span>` : '') + (childName ? ` <span aria-hidden="true">›</span> <span>${esc(childName)}</span>` : '');
        breadcrumb.querySelector('[data-report-root]').addEventListener('click', event => { event.preventDefault(); resetToSummary(); });
    };
    const renderDetail = payload => {
        document.querySelector('[data-report-detail-title]').textContent = detailMode === 'related' ? `${payload.name} relacionados con ${esc(relationSelection.parentName)}` : `Detalle de ${payload.name}`;
        document.querySelector('[data-report-detail-context]').textContent = detailMode === 'related' ? `Registros hijos filtrados por la relación registrada en la base de datos.` : `${payload.totalCount} registro(s) disponibles en este catálogo.`;
        const sort = document.querySelector('[data-report-detail-sort]');
        sort.innerHTML = '<option value="">Predeterminado</option>' + payload.columns.map(c => `<option value="${esc(c.code)}">${esc(c.label)}</option>`).join(''); sort.value = sortColumn;
        document.querySelector('[data-report-detail-head]').innerHTML = `<tr>${payload.columns.map(c => `<th scope="col">${esc(c.label)}</th>`).join('')}</tr>`;
        document.querySelector('[data-report-detail-body]').innerHTML = payload.rows.length ? payload.rows.map(row => `<tr tabindex="0" data-report-record-id="${row.id}" data-report-record-name="${esc(row.values[payload.columns[0]?.code])}">${payload.columns.map(c => `<td data-label="${esc(c.label)}" title="${esc(row.values[c.code])}">${esc(row.values[c.code])}</td>`).join('')}</tr>`).join('') : `<tr><td colspan="${Math.max(1, payload.columns.length)}">No hay resultados.</td></tr>`;
        document.querySelector('[data-report-detail-page]').textContent = `Página ${payload.page} de ${payload.totalPages} · ${payload.totalCount} registros encontrados`;
        document.querySelector('[data-report-detail-prev]').disabled = payload.page <= 1;
        document.querySelector('[data-report-detail-next]').disabled = payload.page >= payload.totalPages;
        document.querySelectorAll('[data-report-detail-body] tr[data-report-record-id]').forEach(row => { const select = () => selectRecord(Number(row.dataset.reportRecordId), row.dataset.reportRecordName); row.addEventListener('click', select); row.addEventListener('keydown', event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); select(); } }); });
    };
    const loadKpis = async (recordId, recordName) => {
        const version = ++requestVersion;
        const host = document.querySelector('[data-report-context-kpis]'); host.hidden = false; host.innerHTML = '<p class="loading-status">Cargando indicadores…</p>';
        try { const payload = await json(`/reporteria/api/catalogos/${encodeURIComponent(selectedCode)}/registros/${recordId}/kpis`); if (version !== requestVersion) return; host.innerHTML = payload.items?.length ? payload.items.map(item => `<div class="report-kpi"><span class="report-kpi-value">${item.value}</span><span class="report-kpi-label">${esc(item.label)} relacionados</span></div>`).join('') : '<p class="empty-state">No hay indicadores contextuales disponibles.</p>'; setBreadcrumb(recordName); }
        catch (error) { host.innerHTML = `<p class="error-text">${esc(error.message)}</p>`; }
    };
    const selectRecord = (recordId, recordName) => { if (detailMode === 'catalog') { relationSelection = { parentId: recordId, parentName: recordName }; loadKpis(recordId, recordName); } };
    const loadDetail = async () => {
        if (!selectedCode) return;
        const version = ++requestVersion;
        const search = document.querySelector('[data-report-detail-search]').value, detailStatus = document.querySelector('[data-report-detail-status]'); detailStatus.textContent = 'Cargando detalle…';
        try {
            const url = detailMode === 'related' ? `/reporteria/api/catalogos/${encodeURIComponent(relationSelection.childCode)}/relacionados?parentCode=${encodeURIComponent(selectedCode)}&parentId=${relationSelection.parentId}&search=${encodeURIComponent(search)}&page=${detailPage}&pageSize=10` : `/reporteria/api/catalogos/${encodeURIComponent(selectedCode)}/detalle?search=${encodeURIComponent(search)}&page=${detailPage}&pageSize=10&sortColumn=${encodeURIComponent(sortColumn)}&sortDirection=${sortDirection}`;
            const payload = await json(url); if (version !== requestVersion) return; renderDetail(payload); detailStatus.textContent = 'Detalle actualizado.';
        } catch (error) { detailStatus.textContent = error.message; }
    };
    const loadRelations = async () => {
        relationCharts.splice(0).forEach(chartInstance => chartInstance.destroy());
        const host = document.querySelector('[data-report-relations]'); host.innerHTML = '<p class="loading-status">Cargando relaciones…</p>';
        try {
            const payload = await json(`/reporteria/api/catalogos/${encodeURIComponent(selectedCode)}/relaciones`);
            if (!payload.items?.length) { host.innerHTML = '<p class="empty-state">No hay relaciones uno-a-muchos disponibles para este catálogo.</p>'; return; }
            host.innerHTML = '';
            payload.items.forEach((relation, index) => {
                const id = `relation-chart-${index}`; host.insertAdjacentHTML('beforeend', `<article class="report-relation"><h3>${esc(relation.childName)} relacionados</h3><p class="supporting-text">Seleccione una barra para consultar los registros hijos.</p><div class="report-chart-wrap"><canvas id="${id}" aria-label="${esc(relation.childName)} relacionados"></canvas></div></article>`);
                const labels = relation.buckets.map(x => x.parentName), values = relation.buckets.map(x => x.total);
                const colors = labels.map((l, i) => window.getDomainPaletteColor ? window.getDomainPaletteColor(l, relation.buckets[i]?.parentId).bg : '#087f8c');
                const borderColors = labels.map((l, i) => window.getDomainPaletteColor ? window.getDomainPaletteColor(l, relation.buckets[i]?.parentId).border : '#065f68');
                const relationChart = new Chart(document.getElementById(id), { type: 'bar', data: { labels, datasets: [{ label: 'Registros relacionados', data: values, backgroundColor: colors, borderColor: borderColors, borderWidth: 1 }] }, options: { responsive: true, maintainAspectRatio: false, onClick: (_, elements) => { const item = elements[0], selected = item && relation.buckets[item.index]; if (!selected) return; relationSelection = { childCode: relation.childCode, parentId: selected.parentId, parentName: selected.parentName }; detailMode = 'related'; detailPage = 1; setBreadcrumb(selected.parentName, relation.childName); loadDetail(); }, plugins: { barValueLabels: configureBarValueLabels(), tooltip: { callbacks: { label: c => ` ${c.parsed.y} registros` } } }, scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } } });
                relationCharts.push(relationChart);
            });
        } catch (error) { host.innerHTML = `<p class="error-text">${esc(error.message)}</p>`; }
    };
    const selectCatalog = (code, name) => { selectedCode = code; selectedName = name; detailMode = 'catalog'; detailPage = 1; relationSelection = null; panel.hidden = false; summary.hidden = true; prompt.hidden = true; document.querySelector('[data-report-context-kpis]').hidden = true; setBreadcrumb(); document.querySelector('[data-report-detail-title]').textContent = `Analizando: ${name}`; if (chart) { chart.data.datasets[0].backgroundColor = chartItems.map((item, index) => item.code === code ? '#b45309' : colors[index % colors.length]); chart.update(); } loadDetail(); loadRelations(); panel.scrollIntoView({ behavior: 'smooth', block: 'start' }); table.querySelectorAll('[data-report-code]').forEach(row => row.classList.toggle('report-selected-row', row.dataset.reportCode === code)); };
    const resetToSummary = () => { selectedCode = null; selectedName = null; detailMode = 'catalog'; relationSelection = null; panel.hidden = true; summary.hidden = true; prompt.hidden = false; document.querySelector('[data-report-context-kpis]').hidden = true; setBreadcrumb(); relationCharts.splice(0).forEach(chartInstance => chartInstance.destroy()); document.querySelector('[data-report-relations]').innerHTML = ''; table.querySelectorAll('[data-report-code]').forEach(row => row.classList.remove('report-selected-row')); if (chart) { chart.data.datasets[0].backgroundColor = chartItems.map((_, index) => colors[index % colors.length]); chart.update(); } };
    const refresh = async () => {
        status.textContent = 'Actualizando datos…'; filter.disabled = true;
        try {
            const query = filter.value ? `?group=${encodeURIComponent(filter.value)}` : '', payload = await json(`/reporteria/api/catalogos${query}`), items = payload.items || []; chartItems = items;
            const labels = items.map(x => x.name), values = items.map(x => x.total); document.querySelector('[data-report-kpi-catalogs]').textContent = items.length; document.querySelector('[data-report-kpi-records]').textContent = values.reduce((sum, value) => sum + value, 0); document.querySelector('[data-report-kpi-relations]').textContent = payload.relationCount ?? '—';
            if (chart) { chart.data.labels = labels; chart.data.datasets[0].data = values; chart.update(); } else chart = new Chart(canvas, { type: 'bar', data: { labels, datasets: [{ label: 'Registros', data: values, backgroundColor: labels.map((_, i) => colors[i % colors.length]), borderColor: '#17345f', borderWidth: 1 }] }, options: { responsive: true, maintainAspectRatio: false, onClick: (_, elements) => { const item = elements[0], selected = item && chartItems[item.index]; if (selected) selectCatalog(selected.code, selected.name); }, plugins: { barValueLabels: configureBarValueLabels(), tooltip: { callbacks: { label: c => ` ${c.parsed.y} registros` } } }, scales: { x: { ticks: { autoSkip: false, maxRotation: 45 } }, y: { beginAtZero: true, ticks: { precision: 0 } } } } });
            table.innerHTML = items.length ? items.map(x => `<tr tabindex="0" data-report-code="${esc(x.code)}" data-report-name="${esc(x.name)}"><td data-label="Catálogo">${esc(x.name)}</td><td data-label="Grupo">${esc(x.group)}</td><td data-label="Registros">${x.total}</td></tr>`).join('') : '<tr><td colspan="3">No hay datos para los filtros seleccionados.</td></tr>';
            table.querySelectorAll('[data-report-code]').forEach(row => { const select = () => selectCatalog(row.dataset.reportCode, row.dataset.reportName); row.addEventListener('click', select); row.addEventListener('keydown', event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); select(); } }); });
            status.textContent = `Actualizado: ${new Date(payload.generatedAt).toLocaleTimeString()}`;
        } catch (error) { status.textContent = error.message; } finally { filter.disabled = false; }
    };
    filter.addEventListener('change', refresh);
    document.querySelector('[data-report-detail-search]').addEventListener('input', () => { detailPage = 1; loadDetail(); });
    document.querySelector('[data-report-detail-prev]').addEventListener('click', () => { detailPage--; loadDetail(); });
    document.querySelector('[data-report-detail-next]').addEventListener('click', () => { detailPage++; loadDetail(); });
    document.querySelector('[data-report-back]').addEventListener('click', resetToSummary);
    document.querySelector('[data-report-detail-sort]').addEventListener('change', event => { sortColumn = event.target.value; detailPage = 1; loadDetail(); });
    document.querySelector('[data-report-detail-sort-direction]').addEventListener('click', event => { sortDirection = sortDirection === 'asc' ? 'desc' : 'asc'; event.target.textContent = sortDirection === 'asc' ? 'Ascendente' : 'Descendente'; event.target.setAttribute('aria-pressed', sortDirection === 'desc'); detailPage = 1; loadDetail(); });
    refresh();
})();
