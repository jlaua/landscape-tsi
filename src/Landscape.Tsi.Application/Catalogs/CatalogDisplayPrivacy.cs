using Landscape.Tsi.Application.Identity;

namespace Landscape.Tsi.Application.Catalogs;

public static class CatalogDisplayPrivacy
{
    public const string RedactedValue = "[REDACTED]";

    public static bool IsSensitive(MasterCatalogDefinition definition, string columnCode) =>
        (definition.Code == "ciso" && columnCode is "email" or "telefono") ||
        (definition.Code is "contacto-vendor" or "contacto-partner" or "contacto-empresa" && columnCode is "email" or "telefono") ||
        (definition.Code == "empresa-subsidiaria" && columnCode == "contactoCiso");

    public static bool IsMasked(string? value) =>
        !string.IsNullOrWhiteSpace(value) && (value.Contains("***") || value.Contains("••••") || value.Contains("•••"));

    public static string? Protect(MasterCatalogDefinition definition, string columnCode, string? value, bool canViewSensitive = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!IsSensitive(definition, columnCode))
        {
            return value;
        }

        if (canViewSensitive)
        {
            return value;
        }

        if (columnCode == "email")
        {
            return SensitiveDataMasker.MaskEmail(value);
        }

        if (columnCode == "telefono")
        {
            return SensitiveDataMasker.MaskPhone(value);
        }

        return RedactedValue;
    }

    public static object? PreserveExistingOnBlankUpdate(
        MasterCatalogDefinition definition,
        string columnCode,
        object? submittedValue,
        object? currentValue)
    {
        if (!IsSensitive(definition, columnCode))
        {
            return submittedValue;
        }

        if (submittedValue is null)
        {
            return currentValue;
        }

        if (submittedValue is string text && (string.IsNullOrWhiteSpace(text) || IsMasked(text)))
        {
            return currentValue;
        }

        return submittedValue;
    }
}