namespace Landscape.Tsi.Infrastructure.Identity;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";
    public bool Enabled { get; set; }
    public string? Password { get; set; }
    public string[] AllowedEnvironments { get; set; } = ["Development"];
}