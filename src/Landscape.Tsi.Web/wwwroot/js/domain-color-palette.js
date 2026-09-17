/**
 * Paleta estándar de colores para Dominios TSI (WCAG AA)
 * Refleja la definición canónica de CatalogDomainColorPalette.cs
 */
(() => {
    const paletteByKeywords = [
        { key: 'data', bg: '#D1E7DD', border: '#A3CFBB', text: '#0F5132' },
        { key: 'application', bg: '#FFF3CD', border: '#FFE69C', text: '#664D03' },
        { key: 'appsec', bg: '#FFF3CD', border: '#FFE69C', text: '#664D03' },
        { key: 'identity', bg: '#E2D9F3', border: '#C5B3E6', text: '#432874' },
        { key: 'iam', bg: '#E2D9F3', border: '#C5B3E6', text: '#432874' },
        { key: 'access', bg: '#E2D9F3', border: '#C5B3E6', text: '#432874' },
        { key: 'cloud', bg: '#CFF4FC', border: '#9EEAF9', text: '#055160' },
        { key: 'network', bg: '#F8D7DA', border: '#F1AEB5', text: '#842029' },
        { key: 'red', bg: '#F8D7DA', border: '#F1AEB5', text: '#842029' },
        { key: 'operac', bg: '#E0E7FF', border: '#C7D2FE', text: '#1E1B4B' },
        { key: 'secops', bg: '#E0E7FF', border: '#C7D2FE', text: '#1E1B4B' },
        { key: 'endpoint', bg: '#FEF3C7', border: '#FDE68A', text: '#78350F' },
        { key: 'mail', bg: '#E0F2FE', border: '#BAE6FD', text: '#0369A1' },
        { key: 'correo', bg: '#E0F2FE', border: '#BAE6FD', text: '#0369A1' },
        { key: 'threat', bg: '#FDE8E8', border: '#F8B4B4', text: '#9B1C1C' },
        { key: 'intel', bg: '#FDE8E8', border: '#F8B4B4', text: '#9B1C1C' },
        { key: 'risk', bg: '#FCE7F3', border: '#FBCFE8', text: '#831843' },
        { key: 'riesgo', bg: '#FCE7F3', border: '#FBCFE8', text: '#831843' },
        { key: 'governance', bg: '#FCE7F3', border: '#FBCFE8', text: '#831843' },
        { key: 'gobierno', bg: '#FCE7F3', border: '#FBCFE8', text: '#831843' },
        { key: 'cumplimiento', bg: '#FCE7F3', border: '#FBCFE8', text: '#831843' }
    ];

    const fallbackPastels = [
        { bg: '#D1E7DD', border: '#A3CFBB', text: '#0F5132' },
        { bg: '#FFF3CD', border: '#FFE69C', text: '#664D03' },
        { bg: '#E2D9F3', border: '#C5B3E6', text: '#432874' },
        { bg: '#CFF4FC', border: '#9EEAF9', text: '#055160' },
        { bg: '#F8D7DA', border: '#F1AEB5', text: '#842029' },
        { bg: '#E0E7FF', border: '#C7D2FE', text: '#1E1B4B' },
        { bg: '#FCE7F3', border: '#FBCFE8', text: '#831843' },
        { bg: '#FEF3C7', border: '#FDE68A', text: '#78350F' }
    ];

    const defaultColor = { bg: '#E2E8F0', border: '#CBD5E1', text: '#1E293B' };

    window.getDomainPaletteColor = (domainName, domainId) => {
        if (domainName && typeof domainName === 'string') {
            const lower = domainName.toLowerCase();
            for (const item of paletteByKeywords) {
                if (lower.includes(item.key)) {
                    return item;
                }
            }
        }
        if (domainId && Number(domainId) > 0) {
            return fallbackPastels[Math.abs(Number(domainId)) % fallbackPastels.length];
        }
        return defaultColor;
    };
})();
