using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Core.Entities.Enums
{
    public enum OrderStatus
    {
        [Display(Name = "Nuevo")] New, //0
        [Display(Name = "En proceso")] InProcess, //1
        [Display(Name = "Facetado")] Faceted, //2
        [Display(Name = "En ruta")] OnRoute, //3
        [Display(Name = "Listo para entregar")] ReadyDeliver, //4
        [Display(Name = "Entregado")] Delivered, //5
        [Display(Name = "Cancelado")] Cancelled, //6
        [Display(Name = "Pago parcial")] PartialPayment, //7
        [Display(Name = "Pagado")] Paid, //8
        [Display(Name = "Notificado de cancelación")] CancelRequest, //9
    }
}