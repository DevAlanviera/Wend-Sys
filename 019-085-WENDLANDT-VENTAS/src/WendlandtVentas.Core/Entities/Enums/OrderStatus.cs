using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum OrderStatus
    {
        [Display(Name = "Nuevo")] New,
        [Display(Name = "En proceso")] InProcess,
        [Display(Name = "Facetado")] Faceted,
        [Display(Name = "En ruta")] OnRoute,
        [Display(Name = "Listo para entregar")] ReadyDeliver,
        [Display(Name = "Entregado")] Delivered,
        [Display(Name = "Cancelado")] Cancelled,
        [Display(Name = "Pago parcial")] PartialPayment,
        [Display(Name = "Pagado")] Paid,
    }
}