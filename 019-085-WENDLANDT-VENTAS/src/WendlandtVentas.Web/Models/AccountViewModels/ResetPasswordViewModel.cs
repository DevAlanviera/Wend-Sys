using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.AccountViewModels
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Favor de ingresar correo electrónico.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Favor de ingresar contraseña")]
        [StringLength(100, ErrorMessage = "El {0} debe de tener al menos {2} y un máximo de {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmación de contraseña no coinciden.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }
}