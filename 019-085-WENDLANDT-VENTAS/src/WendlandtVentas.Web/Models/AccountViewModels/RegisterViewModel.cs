
using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Favor de ingresar el nombre.")]
        [Display(Name = "Nombre(s)")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Favor de ingresar apellido(s).")]
        [Display(Name = "Apellido(s)")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Favor de ingresar el correo electrónico.")]
        [EmailAddress(ErrorMessage = "El campo de correo electrónico no es una dirección de correo valida.")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Favor de ingresar contraseña.")]
        [StringLength(100, ErrorMessage = "La {0} debería tener al menos {2} y un máximo {1} de carácteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmación de la contraseña no coinciden.")]
        public string ConfirmPassword { get; set; }
    }
}