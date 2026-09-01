using System.ComponentModel.DataAnnotations;

namespace Landscape.Tsi.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese su usuario.")]
    [Display(Name = "Usuario")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
    public bool OAuthEnabled { get; set; }
}