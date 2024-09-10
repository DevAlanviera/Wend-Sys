using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum Channel
    {
        [Display(Name = "Venta directa")] DirectSale,
        [Display(Name = "Mayorista")] Wholesaler,
        [Display(Name = "Distribuidor")] Distributor
    }
}