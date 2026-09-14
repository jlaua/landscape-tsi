using System.Globalization;

namespace Landscape.Tsi.Application.Catalogs;

public static class CatalogCommandValidator
{
    private static readonly IReadOnlyDictionary<string, string[]> RequiredColumns =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["dominio"] = ["dominio"],
            ["building-block"] = ["dominio", "nombre"],
            ["capacidad-seguridad"] = ["buildingBlock", "nombre"],
            ["funcionalidad"] = ["capacidad", "nombre"],
            ["tecnologia-tsi"] = ["nombreCorporativo"],
            ["familia"] = ["nombre"],
            ["casos-uso"] = ["tecnologia", "nombre"],
            ["empresa-subsidiaria"] = ["nombre"],
            ["ciso"] = ["empresa", "nombre"],
            ["postura-roadmap"] = ["nombre"],
            ["modalidad-laboral"] = ["nombre"],
            ["tipo-operacion"] = ["nombre"],
            ["tipo-servicio"] = ["codigo", "nombre"],
            ["actividad-nivel-soporte"] = ["nivelSoporte", "descripcionActividad"],
            ["modelo-operacion"] = ["tecnologiaImplementada", "tipoOperacion"],
            ["contrato-tecnologia"] = ["tecnologiaImplementada", "numeroContrato"],
            ["proceso-adopcion-tsi"] = ["codigoProceso", "nombreProceso"],
            ["proceso-adopcion-empresa"] = ["proceso", "empresa"],
            ["estandar-tecnologia-historico"] = ["buildingBlock", "tecnologia"],
            ["tecnologia-tsi-implementada"] = ["empresa", "tecnologia"],
            ["driver"] = ["tecnologiaImplementada", "descripcionDriver"],
            ["servicio-tecnologia"] = ["codigoServicio", "nombreServicio"],
            ["tarifario-proyecto-horas"] = ["servicio", "complejidad"],
            ["tarifario-operacion"] = ["servicio", "nivelSoporte"],
            ["vendor"] = ["nombreVendor"],
            ["contacto-vendor"] = ["vendor", "nombreContactoVendor"],
            ["contacto-partner"] = ["vendor", "nombreContactoPartner"]
        };

    public static Dictionary<string, object?> Normalize(
        MasterCatalogDefinition definition,
        IReadOnlyDictionary<string, string?> values)
    {
        var required = RequiredColumns.TryGetValue(definition.Code, out var requiredColumns)
            ? requiredColumns
            : [];
        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var column in definition.Columns)
        {
            values.TryGetValue(column.Code, out var input);
            if (string.IsNullOrWhiteSpace(input))
            {
                if (required.Contains(column.Code, StringComparer.Ordinal))
                {
                    throw new CatalogValidationException($"Ingrese un valor para {column.Label}.");
                }

                normalized[column.Code] = null;
                continue;
            }

            normalized[column.Code] = column.Type switch
            {
                CatalogFieldType.ForeignKey when int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var foreignKey) => foreignKey,
                CatalogFieldType.DateTime when DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date) => date,
                CatalogFieldType.Text => input.Trim(),
                CatalogFieldType.ForeignKey => throw new CatalogValidationException($"Seleccione un valor válido para {column.Label}."),
                CatalogFieldType.DateTime => throw new CatalogValidationException($"Ingrese una fecha válida para {column.Label}."),
                _ => input.Trim()
            };
        }

        return normalized;
    }
}