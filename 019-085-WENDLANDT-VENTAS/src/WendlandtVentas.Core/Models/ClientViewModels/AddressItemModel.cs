using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WendlandtVentas.Core.Models.ClientViewModels
{
    public class AddressItemModel
    {
        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [Display(Name = "Dirección")]
        public string AddressLocation { get;  set; }

        [Display(Name = "Día de entrega")]
        public string DeliveryDay { get; set; }

        public string  DeliverySpecification { get; set; }
    }
}
