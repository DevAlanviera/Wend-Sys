using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum PromotionType
    {
        [Display(Name = "General")] General,
        [Display(Name = "Clasificación")] Classification,
        [Display(Name = "Clientes")] Clients
    }
}