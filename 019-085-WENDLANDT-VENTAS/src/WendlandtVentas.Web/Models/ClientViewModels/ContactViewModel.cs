using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WendlandtVentas.Web.Models.ClientViewModels
{
    public class ContactViewModel
    {
        public int Id { get; set; }
        [Display(Name="Nombre")]
        public string Name { get;  set; }
        [Display(Name = "Número de celular")]
        public string Cellphone { get;  set; }
        [Display(Name = "Número de oficina")]
        public string OfficePhone { get;  set; }

        //[Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Debe ser un correo válido.")]
        public string Email { get;  set; }
        [Display(Name = "Comentarios")]
        public string Comments { get;  set; }
        public int ClientId { get;  set; }

    }
}
