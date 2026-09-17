(() => {
    'use strict';

    const vendorModalEl = document.getElementById('vendor-technologies-modal');
    if (!vendorModalEl) return;

    const vendorNameEl = document.getElementById('vendor-modal-vendor-name');
    const loadingEl = document.getElementById('vendor-modal-loading');
    const errorEl = document.getElementById('vendor-modal-error');
    const contentEl = document.getElementById('vendor-modal-content');
    const badgeCountEl = document.getElementById('vendor-modal-badge-count');
    const tbodyEl = document.getElementById('vendor-modal-tbody');
    const emptyEl = document.getElementById('vendor-modal-empty');

    vendorModalEl.addEventListener('show.bs.modal', async (event) => {
        const triggerBtn = event.relatedTarget;
        if (!triggerBtn) return;

        const vendorId = triggerBtn.getAttribute('data-vendor-id');
        const vendorName = triggerBtn.getAttribute('data-vendor-name') || `Vendor #${vendorId}`;

        if (vendorNameEl) vendorNameEl.textContent = vendorName;
        if (loadingEl) loadingEl.hidden = false;
        if (errorEl) errorEl.hidden = true;
        if (contentEl) contentEl.hidden = true;
        if (emptyEl) emptyEl.hidden = true;
        if (tbodyEl) tbodyEl.innerHTML = '';

        try {
            const response = await fetch(`/Administration/MasterTables/vendor/${encodeURIComponent(vendorId)}/technologies`, {
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
                badgeCountEl.textContent = `${count} ${count === 1 ? 'tecnología registrada' : 'tecnologías registradas'}`;
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
                    tdActions.style.textAlign = 'right';
                    if (item.detailsUrl) {
                        const link = document.createElement('a');
                        link.className = 'md-button md-button-text';
                        link.href = item.detailsUrl;
                        link.textContent = 'Ver detalle';
                        tdActions.appendChild(link);
                    }
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
    });
})();
