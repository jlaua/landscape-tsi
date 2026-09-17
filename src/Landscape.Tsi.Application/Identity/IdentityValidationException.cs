namespace Landscape.Tsi.Application.Identity;

public sealed class IdentityValidationException(IEnumerable<string> errorCodes)
    : InvalidOperationException("La operación no cumple las validaciones de Identity.")
{
    public IReadOnlyList<string> ErrorCodes { get; } = errorCodes.Distinct(StringComparer.Ordinal).ToArray();
}