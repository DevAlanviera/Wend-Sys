
using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum PayType
    {
        [Display(Name = "Contado")] Cash,
        [Display(Name = "Especial")] Special,
        [Display(Name = "Crédito")] Credit
    }
}
