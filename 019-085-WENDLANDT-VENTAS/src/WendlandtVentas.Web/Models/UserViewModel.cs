using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Debe ser un {0} válido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public ICollection<string> Roles { get; set; } = new List<string>();
    }
}