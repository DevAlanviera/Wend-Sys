using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum OrderType
    {
        [Display(Name = "Remisión")] Remission,
        [Display(Name = "Factura")] Invoice,
        [Display(Name = "Exportación")] Export,
        [Display(Name = "Devolución")] Return
    }
}