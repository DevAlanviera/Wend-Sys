using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Models.OrderViewModels
{
    public class OrderStatusViewModel
    {
        public int OrderId { get; set; }

        [Display(Name = "Estado")]
        [Required(ErrorMessage ="El campo {0} es requerido.")]
        public OrderStatus Status { get; set; }
        public SelectList StatusList { get; set; }

        [Display(Name = "Monto")]
        public double InitialAmount { get; set; }

        [Display(Name = "Comentarios")]
        public string Comments { get; set; }
        public ClientTableModel Client{ get; set; }

        [Display(Name = "Factura")]
        public string InvoiceCode { get; set; }
        public OrderType Type { get; set; }

        [Display(Name = "Día de entrega")]
        public string DeliveryDay { get; set; }

        // Nuevas propiedades para calcular pronto pago
        public bool IsProntoPago { get; set; } // Checkbox para "Pronto Pago"

        public double NuevoTotal { get; set; } // Nuevo total con precio especial
        public double TotalOriginal { get; set; } // Total original de la orden

        [Display(Name = "Monto de la orden")]
        [DataType(DataType.Currency)]

        //Propiedad para introducir el monto real con descuento
        public decimal? RealAmount { get; set; }

        [Display(Name = "¿Precion especial?")]
        public bool PrecioEspecial { get; set; } // Propiedad para el checkbox

        

    }
}