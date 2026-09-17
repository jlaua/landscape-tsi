(() => {
    document.querySelectorAll(".modal").forEach((modal) => {
        const panel = modal.querySelector("[data-delete-impact-panel]");
        if (!panel) return;

        const status = modal.querySelector("[data-delete-impact-status]");
        const content = modal.querySelector("[data-delete-impact-content]");
        const tree = modal.querySelector("[data-delete-impact-tree]");
        const emptyDependents = modal.querySelector("[data-delete-empty-dependents]");
        const submit = modal.querySelector("[data-delete-submit]");
        const confirmationField = modal.querySelector("[data-delete-confirmation-field]");
        const confirmation = modal.querySelector("[data-delete-confirmation]");
        const blocking = modal.querySelector("[data-delete-blocking]");
        const form = modal.querySelector("form");
        const rootName = modal.querySelector("[data-delete-root-name]");
        const dependentTotal = modal.querySelector("[data-delete-dependent-total]");
        const deleteTotal = modal.querySelector("[data-delete-total]");

        const appendNode = (node, parent) => {
            const item = document.createElement("li");
            const tableLabel = node.physicalTableName ? ` (${node.physicalTableName})` : "";
            const countLabel = node.recordCount === 1 ? "1 registro" : `${node.recordCount} registros`;
            item.textContent = `${node.entityName}${tableLabel} — ${countLabel}`;
            item.setAttribute("aria-label", `${node.entityName}: ${countLabel}`);
            if (node.children?.length) {
                const children = document.createElement("ul");
                node.children.forEach((child) => appendNode(child, children));
                item.appendChild(children);
            }
            parent.appendChild(item);
        };

        modal.addEventListener("show.bs.modal", async (event) => {
            const trigger = event.relatedTarget || document.querySelector(`[data-bs-target="#${modal.id}"][data-delete-impact-url]`) || document.querySelector("[data-delete-impact-url]");
            if (!trigger) return;

            const impactUrl = trigger.dataset.deleteImpactUrl;
            const actionUrl = trigger.dataset.deleteActionUrl;
            const rowName = trigger.dataset.deleteRowName;

            if (actionUrl && form) {
                form.action = actionUrl;
            }

            if (rootName) {
                rootName.textContent = rowName || "";
            }

            status.textContent = "Analizando dependencias…";
            status.className = "";
            status.hidden = false;
            content.hidden = true;
            submit.disabled = true;
            confirmationField.hidden = true;
            confirmation.value = "";
            blocking.hidden = true;
            tree.replaceChildren();
            if (emptyDependents) emptyDependents.hidden = true;

            try {
                const response = await fetch(impactUrl, { headers: { Accept: "application/json" } });
                if (!response.ok) throw new Error("No fue posible analizar el impacto.");
                const impact = await response.json();

                if (rootName) {
                    rootName.textContent = impact.rootDisplayName || rowName || "";
                }
                if (dependentTotal) {
                    dependentTotal.textContent = impact.totalDependentRecords;
                }
                if (deleteTotal) {
                    deleteTotal.textContent = impact.totalRecordsToDelete;
                }

                if (impact.directDependents?.length > 0) {
                    impact.directDependents.forEach((node) => appendNode(node, tree));
                    if (emptyDependents) emptyDependents.hidden = true;
                } else {
                    if (emptyDependents) emptyDependents.hidden = false;
                }

                content.hidden = false;
                status.hidden = true;
                const recordsWord = impact.totalRecordsToDelete === 1 ? "1 registro" : `${impact.totalRecordsToDelete} registros`;
                submit.textContent = `Eliminar ${recordsWord}`;

                if (impact.requiresTypedConfirmation) {
                    confirmationField.hidden = false;
                    confirmation.oninput = () => {
                        submit.disabled = confirmation.value !== "ELIMINAR" || !impact.canDelete;
                    };
                }
                if (!impact.canDelete) {
                    blocking.textContent = impact.blockingReason || "La eliminación no está permitida.";
                    blocking.hidden = false;
                    submit.disabled = true;
                } else if (!impact.requiresTypedConfirmation) {
                    submit.disabled = false;
                }
            } catch {
                status.textContent = "No fue posible analizar las dependencias. La operación fue cancelada.";
                status.classList.add("app-alert", "app-alert-error");
            }
        });
    });
})();
