using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WendlandtVentas.Web.Models.ClientViewModels
{
    public class AddressViewModel
    {
        public int Id { get; set; }
        [Display(Name="Nombre")]
        [Required(ErrorMessage ="El campo {0} es requerido.")]
        public string Name { get;  set; }
        [Display(Name = "Dirección")]
        [Required(ErrorMessage ="El campo {0} es requerido.")]
        public string Address { get;  set; }
        public int ClientId { get;  set; }

    }
}
