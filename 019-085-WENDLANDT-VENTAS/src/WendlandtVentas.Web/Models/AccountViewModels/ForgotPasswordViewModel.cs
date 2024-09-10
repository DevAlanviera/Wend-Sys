using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Favor de ingresar corre electrónico.")]
        [EmailAddress]
        public string Email { get; set; }
    }
}