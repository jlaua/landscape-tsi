(() => {
    'use strict';

    const modalEl = document.getElementById('bb-capabilities-modal');
    if (!modalEl) return;

    const bbNameEl = document.getElementById('bb-cap-modal-bb-name');
    const detailLinkEl = document.getElementById('bb-cap-modal-detail-link');
    const loadingEl = document.getElementById('bb-cap-modal-loading');
    const errorEl = document.getElementById('bb-cap-modal-error');
    const contentEl = document.getElementById('bb-cap-modal-content');
    const badgeCountEl = document.getElementById('bb-cap-modal-badge-count');
    const tbodyEl = document.getElementById('bb-cap-modal-tbody');
    const emptyEl = document.getElementById('bb-cap-modal-empty');
    const noSearchEl = document.getElementById('bb-cap-modal-no-search');
    const searchInput = document.getElementById('bb-cap-modal-search');

    let currentItems = [];
    let currentTotal = 0;

    function renderItems(items) {
        if (!tbodyEl) return;
        tbodyEl.innerHTML = '';

        if (items.length === 0) {
            if (currentItems.length === 0) {
                if (emptyEl) emptyEl.hidden = false;
                if (noSearchEl) noSearchEl.hidden = true;
            } else {
                if (emptyEl) emptyEl.hidden = true;
                if (noSearchEl) noSearchEl.hidden = false;
            }
            return;
        }

        if (emptyEl) emptyEl.hidden = true;
        if (noSearchEl) noSearchEl.hidden = true;

        items.forEach((item) => {
            const tr = document.createElement('tr');

            // ID
            const tdId = document.createElement('td');
            tdId.className = 'text-muted fw-semibold';
            tdId.textContent = item.id;
            tr.appendChild(tdId);

            // Capacidad de Seguridad
            const tdName = document.createElement('td');
            const strongName = document.createElement('strong');
            strongName.className = 'text-dark';
            strongName.textContent = item.nombre || '—';
            tdName.appendChild(strongName);
            tr.appendChild(tdName);

            // Estado
            const tdEstado = document.createElement('td');
            if (item.estado && item.estado !== '—') {
                const badge = document.createElement('span');
                badge.className = 'badge bg-light text-dark border';
                badge.textContent = item.estado;
                tdEstado.appendChild(badge);
            } else {
                tdEstado.textContent = '—';
            }
            tr.appendChild(tdEstado);

            // Descripción
            const tdDesc = document.createElement('td');
            tdDesc.className = 'text-secondary small';
            tdDesc.textContent = item.descripcion || '—';
            tr.appendChild(tdDesc);

            // Acciones
            const tdActions = document.createElement('td');
            tdActions.style.textAlign = 'right';
            if (item.detailsUrl) {
                const link = document.createElement('a');
                link.className = 'md-button md-button-text';
                link.href = item.detailsUrl;
                link.textContent = 'Ver detalle';
                link.title = `Ver detalle de ${item.nombre}`;
                tdActions.appendChild(link);
            }
            tr.appendChild(tdActions);

            tbodyEl.appendChild(tr);
        });
    }

    function applyFilter() {
        const query = (searchInput?.value || '').trim().toLowerCase();
        if (!query) {
            if (badgeCountEl) {
                badgeCountEl.textContent = `${currentTotal} ${currentTotal === 1 ? 'capacidad registrada' : 'capacidades registradas'}`;
            }
            renderItems(currentItems);
            return;
        }

        const filtered = currentItems.filter((item) => {
            const name = (item.nombre || '').toLowerCase();
            const desc = (item.descripcion || '').toLowerCase();
            const estado = (item.estado || '').toLowerCase();
            return name.includes(query) || desc.includes(query) || estado.includes(query);
        });

        if (badgeCountEl) {
            badgeCountEl.textContent = `${filtered.length} de ${currentTotal} capacidades`;
        }

        renderItems(filtered);
    }

    if (searchInput) {
        searchInput.addEventListener('input', applyFilter);
    }

    modalEl.addEventListener('show.bs.modal', async (event) => {
        const triggerBtn = event.relatedTarget;
        if (!triggerBtn) return;

        const bbId = triggerBtn.getAttribute('data-bb-id');
        const bbName = triggerBtn.getAttribute('data-bb-name') || `Building Block #${bbId}`;

        if (bbNameEl) bbNameEl.textContent = bbName;
        if (detailLinkEl) {
            detailLinkEl.href = `/Administration/MasterTables/building-block/${encodeURIComponent(bbId)}`;
            detailLinkEl.title = `Ver ficha completa de ${bbName}`;
        }

        if (searchInput) searchInput.value = '';
        if (loadingEl) loadingEl.hidden = false;
        if (errorEl) errorEl.hidden = true;
        if (contentEl) contentEl.hidden = true;
        if (emptyEl) emptyEl.hidden = true;
        if (noSearchEl) noSearchEl.hidden = true;
        if (tbodyEl) tbodyEl.innerHTML = '';
        currentItems = [];
        currentTotal = 0;

        try {
            const response = await fetch(`/Administration/MasterTables/building-block/${encodeURIComponent(bbId)}/capacidades`, {
                headers: {
                    'Accept': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const data = await response.json();
            currentItems = data.items || [];
            currentTotal = data.totalCount ?? currentItems.length;

            if (badgeCountEl) {
                badgeCountEl.textContent = `${currentTotal} ${currentTotal === 1 ? 'capacidad registrada' : 'capacidades registradas'}`;
            }

            renderItems(currentItems);

            if (loadingEl) loadingEl.hidden = true;
            if (contentEl) contentEl.hidden = false;
        } catch (err) {
            if (loadingEl) loadingEl.hidden = true;
            if (errorEl) errorEl.hidden = false;
            console.error('Error cargando capacidades del Building Block:', err);
        }
    });
})();
