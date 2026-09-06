(() => {
    const canvas = document.querySelector('#catalog-report-chart');
    const filter = document.querySelector('[data-report-filter]');
    const status = document.querySelector('[data-report-status]');
    const table = document.querySelector('[data-report-table]');
    if (!canvas || !filter || !status || !table || typeof Chart === 'undefined') return;
    let chart;
    const colors = ['#2457a6', '#7a4ba8', '#087f8c', '#b45309', '#2f6f44', '#a23d3d'];
    const refresh = async () => {
        status.textContent = 'Actualizando datos…';
        filter.disabled = true;
        try {
            const query = filter.value ? `?group=${encodeURIComponent(filter.value)}` : '';
            const response = await fetch(`/reporteria/api/catalogos${query}`, { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('No fue posible obtener los datos.');
            const payload = await response.json();
            const items = payload.items || [];
            const labels = items.map(x => x.name);
            const values = items.map(x => x.total);
            if (chart) { chart.data.labels = labels; chart.data.datasets[0].data = values; chart.update(); }
            else { chart = new Chart(canvas, { type: 'bar', data: { labels, datasets: [{ label: 'Registros', data: values, backgroundColor: labels.map((_, i) => colors[i % colors.length]), borderColor: '#17345f', borderWidth: 1 }] }, options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: true, labels: { generateLabels: chart => Chart.defaults.plugins.legend.labels.generateLabels(chart) } }, tooltip: { callbacks: { label: context => ` ${context.parsed.y} registros` } } }, scales: { x: { ticks: { autoSkip: false, maxRotation: 45, minRotation: 0 } }, y: { beginAtZero: true, precision: 0, title: { display: true, text: 'Cantidad de registros' } } } } }); }
            table.innerHTML = items.length ? items.map(x => `<tr><td>${escapeHtml(x.name)}</td><td>${escapeHtml(x.group)}</td><td>${x.total}</td></tr>`).join('') : '<tr><td colspan="3">No hay datos para los filtros seleccionados.</td></tr>';
            status.textContent = `Actualizado: ${new Date(payload.generatedAt).toLocaleTimeString()}`;
        } catch (error) { status.textContent = error.message; }
        finally { filter.disabled = false; }
    };
    const escapeHtml = value => String(value).replace(/[&<>'"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[c]));
    filter.addEventListener('change', refresh);
    refresh();
})();
