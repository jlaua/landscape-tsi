// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.querySelectorAll("form[data-confirm-message]").forEach((form) => {
    form.addEventListener("submit", (event) => {
        if (!window.confirm(form.dataset.confirmMessage)) {
            event.preventDefault();
        }
    });
});

document.querySelectorAll("form[data-loading-form]").forEach((form) => {
    form.addEventListener("submit", () => {
        const status = form.querySelector(".loading-status");
        if (status) {
            status.hidden = false;
        }

        const submit = form.querySelector("button[type='submit']");
        if (submit) {
            submit.disabled = true;
            submit.setAttribute("aria-disabled", "true");
            if (submit.dataset.loadingText) {
                submit.textContent = submit.dataset.loadingText;
            }
        }
    });
});

const catalogMap = document.querySelector("[data-catalog-map]");
if (catalogMap) {
    const svg = catalogMap.querySelector(".catalog-map-connections");
    const relationships = document.querySelectorAll("[data-map-relation]");
    const drawRelationships = () => {
        svg.querySelectorAll(".catalog-relation").forEach((path) => path.remove());
        const canvasRect = catalogMap.getBoundingClientRect();
        relationships.forEach((relation) => {
            const from = catalogMap.querySelector(`[data-catalog-code='${relation.dataset.from}']`);
            const to = catalogMap.querySelector(`[data-catalog-code='${relation.dataset.to}']`);
            if (!from || !to) return;
            const a = from.getBoundingClientRect();
            const b = to.getBoundingClientRect();
            const scale = parseFloat(getComputedStyle(catalogMap).getPropertyValue("--map-scale")) || 1;
            const x1 = (a.left + a.width / 2 - canvasRect.left) / scale;
            const y1 = (a.top + a.height / 2 - canvasRect.top) / scale;
            const x2 = (b.left + b.width / 2 - canvasRect.left) / scale;
            const y2 = (b.top + b.height / 2 - canvasRect.top) / scale;
            const curve = Math.max(38, Math.abs(x2 - x1) * .35);
            const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
            path.setAttribute("d", `M ${x1} ${y1} C ${x1 + curve} ${y1}, ${x2 - curve} ${y2}, ${x2} ${y2}`);
            path.setAttribute("class", `catalog-relation ${relation.dataset.type === "Muchos a muchos" ? "catalog-relation-many" : ""}`);
            svg.appendChild(path);
        });
    };

    let mapScale = 1;
    document.querySelectorAll("[data-map-zoom]").forEach((button) => {
        button.addEventListener("click", () => {
            mapScale = button.dataset.mapZoom === "reset"
                ? 1
                : Math.min(1.25, Math.max(.75, mapScale + (button.dataset.mapZoom === "in" ? .1 : -.1)));
            catalogMap.style.setProperty("--map-scale", mapScale.toFixed(2));
            const reset = document.querySelector("[data-map-zoom='reset']");
            if (reset) reset.textContent = `${Math.round(mapScale * 100)} %`;
            window.setTimeout(drawRelationships, 180);
        });
    });
    new ResizeObserver(drawRelationships).observe(catalogMap);
    window.addEventListener("load", drawRelationships);
    drawRelationships();
}
