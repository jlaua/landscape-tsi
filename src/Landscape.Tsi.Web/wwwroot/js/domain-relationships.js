(() => {
    const canvas = document.querySelector('#domain-building-blocks-chart');
    const status = document.querySelector('[data-domain-relation-status]');
    const table = document.querySelector('[data-domain-relation-table]');
    if (!canvas || !status || !table || typeof Chart === 'undefined') return;

    const escapeHtml = value => String(value ?? '').replace(/[&<>\"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[character]));
    const load = async () => {
        try {
            const response = await fetch('/reporteria/api/catalogos/dominio/relaciones', { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('No fue posible cargar la relación con Building Block.');
            const relation = (await response.json()).items?.find(item => item.childCode === 'building-block');
            const buckets = relation?.buckets ?? [];
            if (!relation) throw new Error('No existe una relación registrada entre Dominio y Building Block.');
            table.innerHTML = buckets.length
                ? buckets.map(bucket => `<tr><td><a href="/Administration/MasterTables/Domain/${bucket.parentId}">${escapeHtml(bucket.parentName)}</a></td><td>${bucket.total}</td></tr>`).join('')
                : '<tr><td colspan="2">No hay dominios registrados.</td></tr>';
            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: buckets.map(bucket => bucket.parentName),
                    datasets: [{ label: 'Building Blocks', data: buckets.map(bucket => bucket.total), backgroundColor: '#2457a6', borderColor: '#17345f', borderWidth: 1 }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    onClick: (_, elements) => { const selected = elements[0] && buckets[elements[0].index]; if (selected) window.location.href = `/Administration/MasterTables/Domain/${selected.parentId}`; },
                    plugins: { legend: { display: true }, tooltip: { callbacks: { label: context => ` ${context.parsed.y} Building Block` } } },
                    scales: { x: { title: { display: true, text: 'Dominio' }, ticks: { autoSkip: false, maxRotation: 45 } }, y: { beginAtZero: true, ticks: { precision: 0 }, title: { display: true, text: 'Cantidad de Building Blocks' } } }
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
