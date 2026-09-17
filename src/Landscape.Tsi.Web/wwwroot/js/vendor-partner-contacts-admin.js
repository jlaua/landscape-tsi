(() => {
    'use strict';

    const contactsModalEl = document.getElementById('contacts-modal');
    if (!contactsModalEl) return;

    const eyebrowEl = document.getElementById('contacts-modal-eyebrow');
    const entityTypeLabelEl = document.getElementById('contacts-modal-entity-type-label');
    const entityNameEl = document.getElementById('contacts-modal-entity-name');
    const badgeCountEl = document.getElementById('contacts-modal-badge-count');
    const searchInputEl = document.getElementById('contacts-modal-search');
    const loadingEl = document.getElementById('contacts-modal-loading');
    const alertEl = document.getElementById('contacts-modal-alert');
    const tbodyEl = document.getElementById('contacts-modal-tbody');
    const emptyEl = document.getElementById('contacts-modal-empty');
    const noSearchEl = document.getElementById('contacts-modal-no-search');

    const toggleFormBtn = document.getElementById('contacts-modal-toggle-form-btn');
    const formCardEl = document.getElementById('contacts-form-card');
    const formCloseBtn = document.getElementById('contacts-form-close-btn');
    const formTitleEl = document.getElementById('contacts-form-title');
    const editForm = document.getElementById('contact-edit-form');
    const cancelFormBtn = document.getElementById('contact-form-cancel-btn');
    const submitFormBtn = document.getElementById('contact-form-submit-btn');

    const fieldId = document.getElementById('contact-field-id');
    const fieldNombre = document.getElementById('contact-field-nombre');
    const fieldRol = document.getElementById('contact-field-rol');
    const fieldEmail = document.getElementById('contact-field-email');
    const fieldTelefono = document.getElementById('contact-field-telefono');
    const fieldOtro = document.getElementById('contact-field-otro');
    const fieldNotas = document.getElementById('contact-field-notas');

    let currentEntityType = 'vendor'; // 'vendor' or 'partner'
    let currentEntityId = null;
    let currentEntityName = '';
    let contactsList = [];

    function showAlert(message, type = 'success') {
        if (!alertEl) return;
        alertEl.className = `app-alert app-alert-${type} mb-3`;
        alertEl.textContent = message;
        alertEl.style.display = 'block';
        setTimeout(() => {
            if (alertEl && alertEl.style.display === 'block') {
                alertEl.style.display = 'none';
            }
        }, 5000);
    }

    function hideAlert() {
        if (alertEl) {
            alertEl.style.display = 'none';
            alertEl.textContent = '';
        }
    }

    function resetForm() {
        if (fieldId) fieldId.value = '';
        if (fieldNombre) fieldNombre.value = '';
        if (fieldRol) fieldRol.value = '';
        if (fieldEmail) fieldEmail.value = '';
        if (fieldTelefono) fieldTelefono.value = '';
        if (fieldOtro) fieldOtro.value = '';
        if (fieldNotas) fieldNotas.value = '';
        if (formTitleEl) formTitleEl.textContent = 'Registrar Nuevo Contacto';
        if (submitFormBtn) submitFormBtn.textContent = 'Guardar Contacto';
    }

    function openCreateForm() {
        resetForm();
        if (formCardEl) formCardEl.style.display = 'block';
        if (fieldNombre) fieldNombre.focus();
    }

    function closeForm() {
        if (formCardEl) formCardEl.style.display = 'none';
        resetForm();
    }

    function openEditForm(contact) {
        if (fieldId) fieldId.value = contact.id;
        if (fieldNombre) fieldNombre.value = contact.nombre === '—' ? '' : contact.nombre;
        if (fieldRol) fieldRol.value = contact.rol === '—' ? '' : contact.rol;
        if (fieldEmail) fieldEmail.value = contact.email === '—' ? '' : contact.email;
        if (fieldTelefono) fieldTelefono.value = contact.telefono === '—' ? '' : contact.telefono;
        if (fieldOtro) fieldOtro.value = contact.otro === '—' ? '' : contact.otro;
        if (fieldNotas) fieldNotas.value = contact.notas === '—' ? '' : contact.notas;
        if (formTitleEl) formTitleEl.textContent = 'Editar Contacto';
        if (submitFormBtn) submitFormBtn.textContent = 'Actualizar Contacto';
        if (formCardEl) formCardEl.style.display = 'block';
        if (fieldNombre) fieldNombre.focus();
    }

    function updateRowBadge(count) {
        if (!currentEntityId) return;
        const badgeEl = document.getElementById(`contact-badge-row-${currentEntityId}`);
        const labelEl = document.getElementById(`contact-label-row-${currentEntityId}`);
        if (badgeEl) {
            badgeEl.textContent = count;
            if (count > 0) {
                badgeEl.classList.remove('bg-secondary');
                badgeEl.classList.add('bg-success');
            } else {
                badgeEl.classList.remove('bg-success');
                badgeEl.classList.add('bg-secondary');
            }
        }
        if (labelEl) {
            labelEl.textContent = count === 1 ? 'contacto' : 'contactos';
        }
        const btn = document.querySelector(`.entity-contact-count-btn[data-entity-id="${currentEntityId}"]`);
        if (btn) {
            btn.setAttribute('data-contact-count', count);
            btn.setAttribute('title', `Ver los ${count} contactos de ${currentEntityName}`);
            btn.setAttribute('aria-label', `Ver ${count} contactos de ${currentEntityName}`);
        }
    }

    function renderContactsTable(items) {
        if (!tbodyEl) return;
        tbodyEl.innerHTML = '';

        if (contactsList.length === 0) {
            if (emptyEl) emptyEl.hidden = false;
            if (noSearchEl) noSearchEl.hidden = true;
            return;
        }

        if (items.length === 0) {
            if (emptyEl) emptyEl.hidden = true;
            if (noSearchEl) noSearchEl.hidden = false;
            return;
        }

        if (emptyEl) emptyEl.hidden = true;
        if (noSearchEl) noSearchEl.hidden = true;

        items.forEach((contact) => {
            const tr = document.createElement('tr');

            // Nombre
            const tdNombre = document.createElement('td');
            const strong = document.createElement('strong');
            strong.textContent = contact.nombre || '—';
            tdNombre.appendChild(strong);
            tr.appendChild(tdNombre);

            // Rol / Cargo
            const tdRol = document.createElement('td');
            tdRol.textContent = contact.rol || '—';
            tr.appendChild(tdRol);

            // Email
            const tdEmail = document.createElement('td');
            if (contact.email && contact.email !== '—') {
                const a = document.createElement('a');
                a.href = `mailto:${encodeURIComponent(contact.email)}`;
                a.textContent = contact.email;
                tdEmail.appendChild(a);
            } else {
                tdEmail.textContent = '—';
            }
            tr.appendChild(tdEmail);

            // Teléfono
            const tdTel = document.createElement('td');
            if (contact.telefono && contact.telefono !== '—') {
                const a = document.createElement('a');
                a.href = `tel:${encodeURIComponent(contact.telefono.replace(/\s+/g, ''))}`;
                a.textContent = contact.telefono;
                tdTel.appendChild(a);
            } else {
                tdTel.textContent = '—';
            }
            tr.appendChild(tdTel);

            // Notas / Otro
            const tdNotas = document.createElement('td');
            const noteText = [contact.notas, contact.otro].filter(x => x && x !== '—').join(' | ');
            tdNotas.className = 'small text-muted';
            tdNotas.textContent = noteText || '—';
            tr.appendChild(tdNotas);

            // Acciones
            const tdActions = document.createElement('td');
            tdActions.style.textAlign = 'right';
            tdActions.className = 'table-actions';

            const editBtn = document.createElement('button');
            editBtn.type = 'button';
            editBtn.className = 'md-button md-button-text py-1 px-2 me-1';
            editBtn.textContent = 'Editar';
            editBtn.addEventListener('click', () => openEditForm(contact));
            tdActions.appendChild(editBtn);

            const deleteBtn = document.createElement('button');
            deleteBtn.type = 'button';
            deleteBtn.className = 'md-button md-button-danger py-1 px-2';
            deleteBtn.textContent = 'Eliminar';
            deleteBtn.addEventListener('click', () => handleDeleteContact(contact));
            tdActions.appendChild(deleteBtn);

            tr.appendChild(tdActions);

            tbodyEl.appendChild(tr);
        });
    }

    function applySearch() {
        const query = (searchInputEl?.value || '').trim().toLowerCase();
        if (!query) {
            renderContactsTable(contactsList);
            return;
        }

        const filtered = contactsList.filter(c => {
            return (c.nombre && c.nombre.toLowerCase().includes(query)) ||
                   (c.rol && c.rol.toLowerCase().includes(query)) ||
                   (c.email && c.email.toLowerCase().includes(query)) ||
                   (c.telefono && c.telefono.toLowerCase().includes(query)) ||
                   (c.otro && c.otro.toLowerCase().includes(query)) ||
                   (c.notas && c.notas.toLowerCase().includes(query));
        });

        renderContactsTable(filtered);
    }

    async function loadContacts() {
        if (!currentEntityId) return;

        if (loadingEl) loadingEl.hidden = false;
        if (emptyEl) emptyEl.hidden = true;
        if (noSearchEl) noSearchEl.hidden = true;

        try {
            const url = `/Administration/MasterTables/${encodeURIComponent(currentEntityType)}/${encodeURIComponent(currentEntityId)}/contacts`;
            const response = await fetch(url, {
                headers: {
                    'Accept': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const data = await response.json();
            contactsList = data.items || [];
            const count = data.totalCount ?? contactsList.length;

            if (badgeCountEl) {
                badgeCountEl.textContent = `${count} ${count === 1 ? 'contacto registrado' : 'contactos registrados'}`;
            }

            updateRowBadge(count);
            applySearch();
        } catch (err) {
            console.error('Error al cargar contactos:', err);
            showAlert('No fue posible cargar los contactos. Intente nuevamente.', 'error');
        } finally {
            if (loadingEl) loadingEl.hidden = true;
        }
    }

    async function handleDeleteContact(contact) {
        const confirmed = window.confirm(`¿Está seguro de eliminar el contacto "${contact.nombre}"?`);
        if (!confirmed) return;

        try {
            const url = `/Administration/MasterTables/${encodeURIComponent(currentEntityType)}/${encodeURIComponent(currentEntityId)}/contacts/${encodeURIComponent(contact.id)}/delete`;
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                const errData = await response.json().catch(() => ({}));
                throw new Error(errData.message || `HTTP ${response.status}`);
            }

            showAlert(`Contacto "${contact.nombre}" eliminado correctamente.`, 'success');
            await loadContacts();
        } catch (err) {
            console.error('Error al eliminar contacto:', err);
            showAlert(err.message || 'No fue posible eliminar el contacto.', 'error');
        }
    }

    // Modal show event
    contactsModalEl.addEventListener('show.bs.modal', async (event) => {
        const triggerBtn = event.relatedTarget;
        if (!triggerBtn) return;

        currentEntityType = triggerBtn.getAttribute('data-entity-type') || 'vendor';
        currentEntityId = triggerBtn.getAttribute('data-entity-id');
        currentEntityName = triggerBtn.getAttribute('data-entity-name') || '';

        const isVendor = currentEntityType === 'vendor';
        const isPartner = currentEntityType === 'partner';
        const isEmpresa = currentEntityType === 'empresa-subsidiaria';

        if (eyebrowEl) {
            eyebrowEl.textContent = isVendor ? 'CONTACTOS DEL FABRICANTE' : (isPartner ? 'CONTACTOS DEL PARTNER' : 'CONTACTOS DE LA EMPRESA / SUBSIDIARIA');
        }
        if (entityTypeLabelEl) {
            entityTypeLabelEl.textContent = isVendor ? 'Fabricante' : (isPartner ? 'Partner' : 'Empresa Subsidiaria');
        }
        if (entityNameEl) entityNameEl.textContent = currentEntityName || '—';

        if (searchInputEl) searchInputEl.value = '';
        hideAlert();
        closeForm();

        await loadContacts();
    });

    // Toggle form button
    if (toggleFormBtn) {
        toggleFormBtn.addEventListener('click', () => {
            if (formCardEl && formCardEl.style.display !== 'none') {
                closeForm();
            } else {
                openCreateForm();
            }
        });
    }

    if (formCloseBtn) formCloseBtn.addEventListener('click', closeForm);
    if (cancelFormBtn) cancelFormBtn.addEventListener('click', closeForm);

    // Search input event
    if (searchInputEl) {
        searchInputEl.addEventListener('input', applySearch);
    }

    // Submit form (create / edit)
    if (editForm) {
        editForm.addEventListener('submit', async (event) => {
            event.preventDefault();

            const nombre = (fieldNombre?.value || '').trim();
            if (!nombre) {
                showAlert('El nombre del contacto es obligatorio.', 'error');
                if (fieldNombre) fieldNombre.focus();
                return;
            }

            const contactId = (fieldId?.value || '').trim();
            const isEditing = !!contactId;

            const payload = {
                nombre: nombre,
                rol: (fieldRol?.value || '').trim(),
                email: (fieldEmail?.value || '').trim(),
                telefono: (fieldTelefono?.value || '').trim(),
                otro: (fieldOtro?.value || '').trim(),
                notas: (fieldNotas?.value || '').trim()
            };

            const url = isEditing
                ? `/Administration/MasterTables/${encodeURIComponent(currentEntityType)}/${encodeURIComponent(currentEntityId)}/contacts/${encodeURIComponent(contactId)}/edit`
                : `/Administration/MasterTables/${encodeURIComponent(currentEntityType)}/${encodeURIComponent(currentEntityId)}/contacts`;

            if (submitFormBtn) {
                submitFormBtn.disabled = true;
                submitFormBtn.textContent = 'Guardando...';
            }

            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: JSON.stringify(payload)
                });

                if (!response.ok) {
                    const errData = await response.json().catch(() => ({}));
                    throw new Error(errData.message || `HTTP ${response.status}`);
                }

                const result = await response.json();
                showAlert(result.message || (isEditing ? 'Contacto actualizado exitosamente.' : 'Contacto registrado exitosamente.'), 'success');
                closeForm();
                await loadContacts();
            } catch (err) {
                console.error('Error al guardar contacto:', err);
                showAlert(err.message || 'Error al procesar la solicitud.', 'error');
            } finally {
                if (submitFormBtn) {
                    submitFormBtn.disabled = false;
                    submitFormBtn.textContent = isEditing ? 'Actualizar Contacto' : 'Guardar Contacto';
                }
            }
        });
    }
})();