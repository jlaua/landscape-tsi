using System.ComponentModel.DataAnnotations;

namespace Landscape.Tsi.Web.Models;

public sealed class ResetPasswordViewModel
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique la justificación administrativa.")]
    [StringLength(1024)]
    public string Justification { get; set; } = string.Empty;
}
