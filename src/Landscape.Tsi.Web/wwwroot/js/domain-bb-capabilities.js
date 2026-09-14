(() => {
    const canvas = document.querySelector('#domain-bb-capabilities-chart');
    const container = document.querySelector('#domain-bb-capabilities-container');
    const status = document.querySelector('[data-bb-cap-status]');
    if (!canvas || !container || typeof Chart === 'undefined') return;

    const domainId = canvas.getAttribute('data-domain-id');
    if (!domainId) return;

    const barValueLabelsPlugin = {
        id: 'barValueLabelsBB',
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

    const load = async () => {
        try {
            if (status) status.textContent = 'Cargando capacidades por Building Block…';
            const response = await fetch(`/Administration/MasterTables/Domain/${domainId}/building-blocks-capacidades`, {
                headers: { Accept: 'application/json' }
            });
            if (!response.ok) throw new Error('No fue posible cargar las capacidades por Building Block.');

            const data = await response.json();
            const items = data.items ?? [];

            if (items.length === 0) {
                if (status) status.textContent = 'No existen Building Blocks registrados en este dominio.';
                return;
            }

            if (status) status.remove();

            // Horizontal scroll adaptation: width proportional to items count
            const calculatedWidth = Math.max(800, items.length * 60);
            canvas.style.minWidth = `${calculatedWidth}px`;
            canvas.parentElement.style.minWidth = `${calculatedWidth}px`;

            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: items.map(item => item.nombre),
                    datasets: [{
                        label: 'Capacidades de Seguridad',
                        data: items.map(item => item.capacidadesCount),
                        backgroundColor: '#16a34a',
                        borderColor: '#15803d',
                        borderWidth: 1,
                        borderRadius: 4
                    }]
                },
                plugins: [barValueLabelsPlugin],
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    onClick: (_, elements) => {
                        const selected = elements[0] && items[elements[0].index];
                        if (selected) {
                            window.location.href = `/Administration/MasterTables/building-block/${selected.id}`;
                        }
                    },
                    plugins: {
                        legend: { display: true },
                        tooltip: {
                            callbacks: {
                                label: context => ` ${context.parsed.y} Capacidades registradas`
                            }
                        }
                    },
                    scales: {
                        x: {
                            title: { display: true, text: 'Building Block' },
                            ticks: { autoSkip: false, maxRotation: 45, minRotation: 25 }
                        },
                        y: {
                            beginAtZero: true,
                            grace: '10%',
                            ticks: { precision: 0 },
                            title: { display: true, text: 'Cantidad de Capacidades' }
                        }
                    }
                }
            });
        } catch (error) {
            if (status) {
                status.textContent = 'Error al cargar el gráfico de capacidades por Building Block.';
                status.classList.add('app-alert', 'app-alert-error');
            }
        }
    };

    load();
})();
