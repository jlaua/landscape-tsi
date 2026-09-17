namespace Landscape.Tsi.Domain.Catalogs;

/// <summary>
/// Provee la paleta estándar de colores pastel armónicos y accesibles (WCAG AA) para cada Dominio TSI.
/// </summary>
public static class CatalogDomainColorPalette
{
    public sealed record DomainColor(string PastelHex, string BorderHex, string TextHex, string BadgeClass);

    private static readonly DomainColor DefaultColor = new("#E2E8F0", "#CBD5E1", "#1E293B", "bg-secondary-subtle text-dark border");

    private static readonly Dictionary<string, DomainColor> PaletteByKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["data"] = new("#D1E7DD", "#A3CFBB", "#0F5132", "bg-success-subtle text-success border border-success-subtle"),
        ["application"] = new("#FFF3CD", "#FFE69C", "#664D03", "bg-warning-subtle text-warning-emphasis border border-warning-subtle"),
        ["appsec"] = new("#FFF3CD", "#FFE69C", "#664D03", "bg-warning-subtle text-warning-emphasis border border-warning-subtle"),
        ["identity"] = new("#E2D9F3", "#C5B3E6", "#432874", "bg-info-subtle text-primary border border-info-subtle"),
        ["iam"] = new("#E2D9F3", "#C5B3E6", "#432874", "bg-info-subtle text-primary border border-info-subtle"),
        ["cloud"] = new("#CFF4FC", "#9EEAF9", "#055160", "bg-info-subtle text-dark border border-info-subtle"),
        ["network"] = new("#F8D7DA", "#F1AEB5", "#842029", "bg-danger-subtle text-danger border border-danger-subtle"),
        ["red"] = new("#F8D7DA", "#F1AEB5", "#842029", "bg-danger-subtle text-danger border border-danger-subtle"),
        ["operac"] = new("#E0E7FF", "#C7D2FE", "#1E1B4B", "bg-primary-subtle text-primary border border-primary-subtle"),
        ["secops"] = new("#E0E7FF", "#C7D2FE", "#1E1B4B", "bg-primary-subtle text-primary border border-primary-subtle"),
        ["access"] = new("#E2D9F3", "#C5B3E6", "#432874", "bg-info-subtle text-primary border border-info-subtle"),
        ["endpoint"] = new("#FEF3C7", "#FDE68A", "#78350F", "bg-warning-subtle text-dark border border-warning-subtle"),
        ["mail"] = new("#E0F2FE", "#BAE6FD", "#0369A1", "bg-info-subtle text-dark border border-info-subtle"),
        ["correo"] = new("#E0F2FE", "#BAE6FD", "#0369A1", "bg-info-subtle text-dark border border-info-subtle"),
        ["threat"] = new("#FDE8E8", "#F8B4B4", "#9B1C1C", "bg-danger-subtle text-danger border border-danger-subtle"),
        ["intel"] = new("#FDE8E8", "#F8B4B4", "#9B1C1C", "bg-danger-subtle text-danger border border-danger-subtle"),
        ["risk"] = new("#FCE7F3", "#FBCFE8", "#831843", "bg-danger-subtle text-dark border border-danger-subtle"),
        ["riesgo"] = new("#FCE7F3", "#FBCFE8", "#831843", "bg-danger-subtle text-dark border border-danger-subtle"),
        ["governance"] = new("#FCE7F3", "#FBCFE8", "#831843", "bg-danger-subtle text-dark border border-danger-subtle"),
        ["gobierno"] = new("#FCE7F3", "#FBCFE8", "#831843", "bg-danger-subtle text-dark border border-danger-subtle"),
        ["cumplimiento"] = new("#FCE7F3", "#FBCFE8", "#831843", "bg-danger-subtle text-dark border border-danger-subtle")
    };

    private static readonly DomainColor[] FallbackPastels =
    [
        new("#D1E7DD", "#A3CFBB", "#0F5132", "bg-success-subtle text-success border"),
        new("#FFF3CD", "#FFE69C", "#664D03", "bg-warning-subtle text-warning-emphasis border"),
        new("#E2D9F3", "#C5B3E6", "#432874", "bg-info-subtle text-primary border"),
        new("#CFF4FC", "#9EEAF9", "#055160", "bg-info-subtle text-dark border"),
        new("#F8D7DA", "#F1AEB5", "#842029", "bg-danger-subtle text-danger border"),
        new("#E0E7FF", "#C7D2FE", "#1E1B4B", "bg-primary-subtle text-primary border"),
        new("#FCE7F3", "#FBCFE8", "#831843", "bg-danger-subtle text-dark border"),
        new("#FEF3C7", "#FDE68A", "#78350F", "bg-warning-subtle text-dark border")
    ];

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, DomainColor> CustomOverridesById = new();

    public static void SetCustomColor(int domainId, string pastelHex, string? borderHex = null, string? textHex = null)
    {
        if (domainId <= 0) return;
        var validPastel = NormalizeHex(pastelHex, "#E2E8F0");
        var validBorder = !string.IsNullOrWhiteSpace(borderHex) ? NormalizeHex(borderHex, validPastel) : DarkenHex(validPastel, 0.20f);
        var validText = !string.IsNullOrWhiteSpace(textHex) ? NormalizeHex(textHex, "#1E293B") : DarkenHex(validPastel, 0.60f);

        CustomOverridesById[domainId] = new DomainColor(validPastel, validBorder, validText, "border");
    }

    public static void ResetCustomColor(int domainId)
    {
        CustomOverridesById.TryRemove(domainId, out _);
    }

    public static void ResetAllCustomColors()
    {
        CustomOverridesById.Clear();
    }

    public static IReadOnlyDictionary<int, DomainColor> GetAllCustomColors()
    {
        return CustomOverridesById;
    }

    public static DomainColor GetColorForDomain(string? domainName, int domainId = 0)
    {
        if (domainId > 0 && CustomOverridesById.TryGetValue(domainId, out var customColor))
        {
            return customColor;
        }

        if (!string.IsNullOrWhiteSpace(domainName))
        {
            var lower = domainName.ToLowerInvariant();
            foreach (var kvp in PaletteByKeywords)
            {
                if (lower.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
        }

        if (domainId > 0)
        {
            return FallbackPastels[Math.Abs(domainId) % FallbackPastels.Length];
        }

        return DefaultColor;
    }

    private static string NormalizeHex(string hex, string fallback)
    {
        hex = hex.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        return hex.Length == 7 ? hex.ToUpperInvariant() : fallback;
    }

    private static string DarkenHex(string hex, float factor)
    {
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length != 6) return "#1E293B";
            int r = Convert.ToInt32(hex[..2], 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            r = Math.Max(0, (int)(r * (1f - factor)));
            g = Math.Max(0, (int)(g * (1f - factor)));
            b = Math.Max(0, (int)(b * (1f - factor)));

            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return "#1E293B";
        }
    }
}