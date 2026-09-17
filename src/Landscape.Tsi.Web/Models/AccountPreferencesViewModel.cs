namespace Landscape.Tsi.Web.Models;

public sealed class AccountPreferencesViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string SelectedTheme { get; set; } = "light";
    public string? StatusMessage { get; set; }
}

public sealed class SetThemeRequest
{
    public string Theme { get; set; } = "light";
    public string? ReturnUrl { get; set; }
}