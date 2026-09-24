/**
 * Universal table auto-sort utility for Landscape TSI.
 * Enhances standard HTML tables with clickable, accessible column sorting.
 */
(() => {
    function initTableAutoSort() {
        const tables = document.querySelectorAll('.responsive-table table, table[data-sortable="true"]');
        tables.forEach(table => {
            if (table.dataset.noSort === 'true' || table.dataset.autoSortInitialized === 'true') return;
            table.dataset.autoSortInitialized = 'true';

            const thead = table.querySelector('thead');
            const tbody = table.querySelector('tbody');
            if (!thead || !tbody) return;

            const headerRow = thead.rows[thead.rows.length - 1];
            if (!headerRow) return;

            const ths = Array.from(headerRow.cells);
            ths.forEach((th, colIndex) => {
                // Ignore action columns, checkboxes, or explicitly disabled headers
                if (th.classList.contains('no-sort') ||
                    th.classList.contains('row-selector-cell') ||
                    th.classList.contains('table-actions') ||
                    th.querySelector('.visually-hidden:only-child') ||
                    th.textContent.trim().toLowerCase() === 'acciones') {
                    return;
                }

                th.classList.add('sortable-header');
                th.setAttribute('tabindex', '0');
                th.setAttribute('role', 'columnheader');
                if (!th.hasAttribute('aria-sort')) {
                    th.setAttribute('aria-sort', 'none');
                }

                const handleSort = () => {
                    const currentSort = th.getAttribute('aria-sort');
                    const newSort = currentSort === 'ascending' ? 'descending' : 'ascending';

                    ths.forEach(otherTh => {
                        if (otherTh !== th && otherTh.classList.contains('sortable-header')) {
                            otherTh.setAttribute('aria-sort', 'none');
                        }
                    });

                    th.setAttribute('aria-sort', newSort);
                    sortRows(tbody, colIndex, newSort === 'ascending');
                };

                th.addEventListener('click', (e) => {
                    // Don't intercept clicks on inner links/buttons that do server-side actions
                    if (e.target.tagName === 'A' && e.target.getAttribute('href')?.startsWith('?')) return;
                    handleSort();
                });

                th.addEventListener('keydown', (e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                        e.preventDefault();
                        handleSort();
                    }
                });
            });
        });
    }

    function parseCellValue(text) {
        if (!text) return { type: 'text', val: '' };
        const trimmed = text.trim();

        // 1. Check for pure number or numeric with currency / text units (e.g. "USD 40,963.68", "15 Building Blocks", "3 capacidades")
        const numRegex = /^[$€£¥]?\s*[-+]?[0-9]{1,3}(?:[,.][0-9]{3})*(?:[,.][0-9]+)?(?:\s*[a-zA-Z%]+)?$/;
        const pureDigits = trimmed.replace(/[^0-9.-]/g, '');
        if (pureDigits !== '' && !isNaN(pureDigits) && (numRegex.test(trimmed) || /^\d+\s+\w+/.test(trimmed))) {
            const num = parseFloat(pureDigits);
            if (!isNaN(num)) {
                return { type: 'number', val: num };
            }
        }

        // 2. Check for Date format: YYYY-MM-DD, DD/MM/YYYY
        if (/^\d{4}-\d{2}-\d{2}/.test(trimmed) || /^\d{2}\/\d{2}\/\d{4}/.test(trimmed)) {
            const parsedDate = Date.parse(trimmed);
            if (!isNaN(parsedDate)) {
                return { type: 'date', val: parsedDate };
            }
        }

        return { type: 'text', val: trimmed.toLowerCase() };
    }

    function sortRows(tbody, colIndex, isAscending) {
        const rows = Array.from(tbody.querySelectorAll('tr'));
        if (rows.length <= 1) return;

        // Extract cells & values
        const rowData = rows.map(row => {
            const cell = row.cells[colIndex];
            const text = cell ? cell.textContent.trim() : '';
            return { row, parsed: parseCellValue(text) };
        });

        rowData.sort((a, b) => {
            const va = a.parsed;
            const vb = b.parsed;

            if (va.type === 'number' && vb.type === 'number') {
                return isAscending ? va.val - vb.val : vb.val - va.val;
            }
            if (va.type === 'date' && vb.type === 'date') {
                return isAscending ? va.val - vb.val : vb.val - va.val;
            }

            // String comparison with Spanish collation
            const strA = String(va.val);
            const strB = String(vb.val);
            const comp = strA.localeCompare(strB, 'es', { numeric: true, sensitivity: 'base' });
            return isAscending ? comp : -comp;
        });

        // Re-append in sorted order
        const fragment = document.createDocumentFragment();
        rowData.forEach(item => fragment.appendChild(item.row));
        tbody.appendChild(fragment);

        tbody.closest('table')?.dispatchEvent(new CustomEvent('table:sorted', {
            bubbles: true,
            detail: { colIndex, isAscending }
        }));
    }

    // Auto-init on DOMContentLoaded and on dynamic changes
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTableAutoSort);
    } else {
        initTableAutoSort();
    }

    // Also observe DOM additions (modals, dynamic tabs, AJAX updates)
    const observer = new MutationObserver(() => initTableAutoSort());
    observer.observe(document.body || document.documentElement, { childList: true, subtree: true });
})();
