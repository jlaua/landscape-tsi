namespace Landscape.Tsi.Application.Catalogs;

public static class CatalogDisplayPrivacy
{
    public const string RedactedValue = "[REDACTED]";

    public static bool IsSensitive(MasterCatalogDefinition definition, string columnCode) =>
        definition.Code == "ciso" && columnCode is "email" or "telefono";

    public static string? Protect(MasterCatalogDefinition definition, string columnCode, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return IsSensitive(definition, columnCode)
            ? RedactedValue
            : value;
    }

    public static object? PreserveExistingOnBlankUpdate(
        MasterCatalogDefinition definition,
        string columnCode,
        object? submittedValue,
        object? currentValue) =>
        IsSensitive(definition, columnCode) && submittedValue is null
            ? currentValue
            : submittedValue;
}