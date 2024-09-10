using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Models.PromotionViewModels;

namespace WendlandtVentas.Core.Models.OrderViewModels
{
    public class OrderViewModel
    {
        public int Id { get; set; }

        //Remisión se requiere que sea automatica, si se llega a ocupar habilitar otra vez descomentar
        [Display(Name = "Remisión")]
        public string RemissionCode { get; set; }

        [Display(Name = "Factura")]
        public string InvoiceCode { get; set; }

        [Display(Name = "Exportación")]
        public string ExportCode { get; set; }

        [Display(Name = "Tipo")]
        public OrderType IsInvoice { get; set; }
        
        [Display(Name = "Pagado")]
        public bool Paid { get; set; }

        [Display(Name = "Promesa de pago")]
        public string PaymentPromiseDate { get; set; }

        [Display(Name = "Fecha de pago")]
        public string PaymentDate { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        [Display(Name = "Cliente")]
        public int ClientId { get; set; }
        public SelectList Clients { get; set; }
        public IEnumerable<ProductPresentationItem> ProductsEdit { get; set; } = new List<ProductPresentationItem>();

        [Required(ErrorMessage = "Es necesario agregar uno o más productos")]
        public List<int> ProductPresentationIds { get; set; }
        public List<int> ProductPresentationQuantities { get; set; }
        public List<bool> ProductIsPresent { get; set; }
        public List<decimal> ProductPrices { get; set; }

        [Display(Name = "Comentario")]
        public string Comment { get; set; }

        public OrderType Type { get; set; }//=> IsInvoice ? OrderType.Invoice : OrderType.Remission;

        [Display(Name = "Lugar de entrega")]
        public string Delivery { get; set; }

        [Display(Name = "Indicaciones de entrega")]
        public string DeliverySpecification { get; set; }

        public List<string> Promotions { get; set; }
        public IEnumerable<PresentationPromotionModel> PresentationPromotionsEdit { get; set; } = new List<PresentationPromotionModel>();

        //[Required(ErrorMessage = "El campo {0} es requerido")]
        [Display(Name = "Dirección de entrega")]
        public int? AddressId { get; set; }
        public SelectList Addresses { get; set; }
        public string AddressName { get; set; }
        public string Address { get; set; }

        [Display(Name = "Día de entrega")]
        public string DeliveryDay { get; set; }

        [Display(Name = "Tipo de pago")]
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public PayType PayType { get; set; }
        public SelectList PayTypes { get; set; }

        [Display(Name = "Tipo de moneda")]
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public CurrencyType CurrencyType { get; set; }
        public SelectList CurrencyTypes { get; set; }

        public bool CanEditProducts { get; set; } = true;

        public SelectList ReturnRemisionNumberOptions { get; set; }

        [Display(Name = "No. de remisión de factura")]
        public string ReturnRemisionNumber { get; set; }
        [Display(Name = "Motivo de devolución")]
        public string ReturnReason { get; set; }
    }
}