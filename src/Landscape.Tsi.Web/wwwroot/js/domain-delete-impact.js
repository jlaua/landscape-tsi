(() => {
    const trigger = document.querySelector("[data-delete-impact-url]");
    const modal = document.querySelector("[data-delete-impact-panel]")?.closest(".modal");
    if (!trigger || !modal) return;

    const status = modal.querySelector("[data-delete-impact-status]");
    const content = modal.querySelector("[data-delete-impact-content]");
    const tree = modal.querySelector("[data-delete-impact-tree]");
    const submit = modal.querySelector("[data-delete-submit]");
    const confirmationField = modal.querySelector("[data-delete-confirmation-field]");
    const confirmation = modal.querySelector("[data-delete-confirmation]");
    const blocking = modal.querySelector("[data-delete-blocking]");

    const appendNode = (node, parent) => {
        const item = document.createElement("li");
        item.textContent = `${node.entityName} — ${node.recordCount}`;
        item.setAttribute("aria-label", `${node.entityName}: ${node.recordCount} registros`);
        if (node.children?.length) {
            const children = document.createElement("ul");
            node.children.forEach((child) => appendNode(child, children));
            item.appendChild(children);
        }
        parent.appendChild(item);
    };

    modal.addEventListener("show.bs.modal", async () => {
        status.textContent = "Analizando dependencias…";
        status.hidden = false;
        content.hidden = true;
        submit.disabled = true;
        confirmationField.hidden = true;
        confirmation.value = "";
        blocking.hidden = true;
        tree.replaceChildren();
        try {
            const response = await fetch(trigger.dataset.deleteImpactUrl, { headers: { Accept: "application/json" } });
            if (!response.ok) throw new Error("No fue posible analizar el impacto.");
            const impact = await response.json();
            document.querySelector("[data-delete-root-name]").textContent = impact.rootDisplayName;
            document.querySelector("[data-delete-dependent-total]").textContent = impact.totalDependentRecords;
            document.querySelector("[data-delete-total]").textContent = impact.totalRecordsToDelete;
            impact.directDependents.forEach((node) => appendNode(node, tree));
            content.hidden = false;
            status.hidden = true;
            submit.textContent = `Eliminar ${impact.totalRecordsToDelete} registros`;
            if (impact.requiresTypedConfirmation) {
                confirmationField.hidden = false;
                confirmation.addEventListener("input", () => { submit.disabled = confirmation.value !== "ELIMINAR" || !impact.canDelete; }, { once: false });
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
})();
