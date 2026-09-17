(() => {
    const canvas = document.querySelector('#domain-building-blocks-chart');
    const status = document.querySelector('[data-domain-relation-status]');
    const table = document.querySelector('[data-domain-relation-table]');
    if (!canvas || !status || !table || typeof Chart === 'undefined') return;

    const barValueLabelsPlugin = {
        id: 'barValueLabels',
        afterDatasetsDraw(chart) {
            const { ctx, chartArea } = chart;
            const isDark = document.documentElement.getAttribute('data-theme') === 'corporate';
            ctx.save();
            ctx.textAlign = 'center';

            chart.data.datasets.forEach((dataset, datasetIndex) => {
                const meta = chart.getDatasetMeta(datasetIndex);
                if (meta.hidden || meta.type !== 'bar') return;
                meta.data.forEach((bar, index) => {
                    const value = dataset.data[index];
                    if (value === undefined || value === null || isNaN(value)) return;

                    const height = Math.abs(bar.base - bar.y);
                    const isInside = height >= 20;

                    if (isInside) {
                        ctx.font = 'bold 13px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
                        ctx.textBaseline = 'middle';
                        ctx.fillStyle = '#ffffff';
                        ctx.shadowColor = 'rgba(0, 0, 0, 0.45)';
                        ctx.shadowBlur = 3;
                        ctx.shadowOffsetX = 0;
                        ctx.shadowOffsetY = 1;

                        const y = bar.y + Math.min(18, height / 2);
                        ctx.fillText(value, bar.x, y);
                    } else {
                        ctx.font = 'bold 12px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
                        ctx.textBaseline = 'bottom';
                        ctx.fillStyle = isDark ? '#f1f5f9' : '#0f172a';
                        ctx.shadowColor = 'transparent';
                        ctx.shadowBlur = 0;
                        ctx.shadowOffsetX = 0;
                        ctx.shadowOffsetY = 0;

                        const y = Math.max(chartArea.top + 6, bar.y - 4);
                        ctx.fillText(value, bar.x, y);
                    }
                });
            });
            ctx.restore();
        }
    };

    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[character]));
    const load = async () => {
        try {
            const response = await fetch('/reporteria/api/catalogos/dominio/relaciones', { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('No fue posible cargar la relación con Building Block.');
            const relation = (await response.json()).items?.find(item => item.childCode === 'building-block');
            const buckets = relation?.buckets ?? [];
            if (!relation) throw new Error('No existe una relación registrada entre Dominio y Building Block.');
            const colors = buckets.map(b => (window.getDomainPaletteColor ? window.getDomainPaletteColor(b.parentName, b.parentId) : { bg: '#2457a6', border: '#17345f', text: '#1e293b' }));
            table.innerHTML = buckets.length
                ? buckets.map((bucket, i) => {
                    const c = colors[i];
                    return `<tr><td><span style="display:inline-block;width:12px;height:12px;border-radius:3px;margin-right:8px;background-color:${c.bg};border:1px solid ${c.border};vertical-align:middle;"></span><a href="/Administration/MasterTables/Domain/${bucket.parentId}">${escapeHtml(bucket.parentName)}</a></td><td><strong>${bucket.total}</strong></td></tr>`;
                }).join('')
                : '<tr><td colspan="2">No hay dominios registrados.</td></tr>';
            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: buckets.map(bucket => bucket.parentName),
                    datasets: [{
                        label: 'Building Blocks',
                        data: buckets.map(bucket => bucket.total),
                        backgroundColor: colors.map(c => c.bg),
                        borderColor: colors.map(c => c.border),
                        borderWidth: 1.5,
                        borderRadius: 4
                    }]
                },
                plugins: [barValueLabelsPlugin],
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    onClick: (_, elements) => { const selected = elements[0] && buckets[elements[0].index]; if (selected) window.location.href = `/Administration/MasterTables/Domain/${selected.parentId}`; },
                    plugins: { legend: { display: true }, tooltip: { callbacks: { label: context => ` ${context.parsed.y} Building Block` } } },
                    scales: {
                        x: { title: { display: true, text: 'Dominio' }, ticks: { autoSkip: false, maxRotation: 45 } },
                        y: { beginAtZero: true, grace: '8%', ticks: { precision: 0 }, title: { display: true, text: 'Cantidad de Building Blocks' } }
                    }
                }
            });
            status.textContent = `Relación actualizada: ${buckets.length} dominio(s).`;
        } catch (error) {
            table.innerHTML = `<tr><td colspan="2">${escapeHtml(error.message)}</td></tr>`;
            status.textContent = error.message;
        }
    };
    load();
})();
