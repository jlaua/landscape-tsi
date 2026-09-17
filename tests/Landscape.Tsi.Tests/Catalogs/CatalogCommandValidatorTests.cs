using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Tests.Catalogs;

public sealed class CatalogCommandValidatorTests
{
    private static readonly string[] MutableCatalogCodes =
    [
        "dominio", "building-block", "capacidad-seguridad", "funcionalidad",
        "tecnologia-tsi", "familia", "casos-uso", "empresa-subsidiaria",
        "ciso", "postura-roadmap", "modalidad-laboral", "tipo-operacion"
    ];

    [Fact]
    public void Normalize_AcceptsAValidAllowlistedPayloadForEveryMutableCatalog()
    {
        foreach (var definition in MutableCatalogCodes.Select(GetDefinition))
        {
            var payload = definition.Columns.ToDictionary(
                column => column.Code,
                column => (string?)(column.Type switch
                {
                    CatalogFieldType.ForeignKey => "41",
                    CatalogFieldType.DateTime => "2026-09-08",
                    _ => $" ITEST_{Guid.NewGuid():N} "
                }),
                StringComparer.Ordinal);

            var normalized = CatalogCommandValidator.Normalize(definition, payload);

            Assert.Equal(definition.Columns.Count, normalized.Count);
            Assert.Equal(definition.Columns.Select(column => column.Code).Order(), normalized.Keys.Order());
            Assert.All(definition.Columns.Where(column => column.Type == CatalogFieldType.ForeignKey),
                column => Assert.Equal(41, normalized[column.Code]));
            Assert.All(definition.Columns.Where(column => column.Type == CatalogFieldType.Text),
                column => Assert.StartsWith("ITEST_", Assert.IsType<string>(normalized[column.Code]), StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Normalize_RejectsMissingRequiredFieldsForEveryMutableCatalog()
    {
        foreach (var definition in MutableCatalogCodes.Select(GetDefinition))
        {
            Assert.Throws<CatalogValidationException>(() =>
                CatalogCommandValidator.Normalize(definition, new Dictionary<string, string?>()));
        }
    }

    [Fact]
    public void Normalize_RejectsMalformedForeignKeys()
    {
        foreach (var definition in MutableCatalogCodes.Select(GetDefinition)
                     .Where(definition => definition.Columns.Any(column => column.Type == CatalogFieldType.ForeignKey)))
        {
            var payload = ValidPayload(definition);
            var foreignKey = definition.Columns.First(column => column.Type == CatalogFieldType.ForeignKey);
            payload[foreignKey.Code] = "not-an-id";

            Assert.Throws<CatalogValidationException>(() => CatalogCommandValidator.Normalize(definition, payload));
        }
    }

    [Fact]
    public void Normalize_IgnoresOverpostedAndPrimaryKeyValues()
    {
        foreach (var definition in MutableCatalogCodes.Select(GetDefinition))
        {
            var payload = ValidPayload(definition);
            payload[definition.PrimaryKeyColumn] = "999999";
            payload["unexpectedProperty"] = "must-not-pass";

            var normalized = CatalogCommandValidator.Normalize(definition, payload);

            Assert.DoesNotContain(definition.PrimaryKeyColumn, normalized.Keys);
            Assert.DoesNotContain("unexpectedProperty", normalized.Keys);
            Assert.Equal(definition.Columns.Count, normalized.Count);
        }
    }

    [Fact]
    public void Normalize_ValidatesContactoVendorAndPartner()
    {
        var vendorDef = GetDefinition("contacto-vendor");
        var partnerDef = GetDefinition("contacto-partner");

        var validVendorPayload = new Dictionary<string, string?>
        {
            ["vendor"] = "10",
            ["nombreContactoVendor"] = "Juan Perez",
            ["rol"] = "Account Manager",
            ["email"] = "juan@vendor.com",
            ["telefono"] = "+51999999999",
            ["notas"] = "Contacto principal"
        };
        var normVendor = CatalogCommandValidator.Normalize(vendorDef, validVendorPayload);
        Assert.Equal(10, normVendor["vendor"]);
        Assert.Equal("Juan Perez", normVendor["nombreContactoVendor"]);

        var validPartnerPayload = new Dictionary<string, string?>
        {
            ["partner"] = "5",
            ["vendor"] = "10",
            ["nombreContactoPartner"] = "Maria Lopez",
            ["rol"] = "Lead Architect",
            ["email"] = "maria@partner.com",
            ["telefono"] = "+51988888888",
            ["notas"] = "Contacto técnico"
        };
        var normPartner = CatalogCommandValidator.Normalize(partnerDef, validPartnerPayload);
        Assert.Equal(5, normPartner["partner"]);
        Assert.Equal("Maria Lopez", normPartner["nombreContactoPartner"]);

        // Missing required
        Assert.Throws<CatalogValidationException>(() =>
            CatalogCommandValidator.Normalize(vendorDef, new Dictionary<string, string?> { ["vendor"] = "10" }));
        Assert.Throws<CatalogValidationException>(() =>
            CatalogCommandValidator.Normalize(partnerDef, new Dictionary<string, string?> { ["partner"] = "5" }));
    }

    private static MasterCatalogDefinition GetDefinition(string code) =>
        MasterCatalogRegistry.GetByCode(code) ?? throw new InvalidOperationException($"Missing catalog {code}.");

    private static Dictionary<string, string?> ValidPayload(MasterCatalogDefinition definition) =>
        definition.Columns.ToDictionary(
            column => column.Code,
            column => (string?)(column.Type switch
            {
                CatalogFieldType.ForeignKey => "41",
                CatalogFieldType.DateTime => "2026-09-08",
                _ => $"ITEST_{Guid.NewGuid():N}"
            }),
            StringComparer.Ordinal);
}