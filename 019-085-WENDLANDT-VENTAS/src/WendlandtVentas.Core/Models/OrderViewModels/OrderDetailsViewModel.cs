using Humanizer;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Models.ProductViewModels;
using WendlandtVentas.Core.Models.PromotionViewModels;
using WendlandtVentas.Core.Models.ClientViewModels;
using System;
using WendlandtVentas.Core.Models.BitacoraViewModel;

namespace WendlandtVentas.Core.Models.OrderViewModels
{
    public class OrderDetailsViewModel
    {
        public int Id { get; set; }

        public OrderType TypeEnum { get; set; }

        [Display(Name = "Tipo")]
        public string Type => TypeEnum.Humanize();

        [Display(Name = "Estado")]
        public string Status { get; set; }

        [Display(Name = "Remisión")]
        public string RemissionCode { get; set; }

        [Display(Name = "Factura")]
        public string InvoiceCode { get; set; }

        [Display(Name = "Pagado")]
        public bool IsPaid { get; set; }

        [Display(Name = "Fecha")]
        public DateTime CreateDate { get; set; }

        [Display(Name = "Fecha promesa de pago")]
        public string PaymentPromiseDate { get; set; }

        [Display(Name = "Fecha de pago")]
        public string PaymentDate { get; set; }

        [Display(Name = "Distribución")]
        public string Distribution { get; set; }

        [Display(Name = "Importe base")]
        public string BaseAmount { get; set; }

        [Display(Name = "IEPS")]
        public string IEPS { get; set; }

        [Display(Name = "IVA")]
        public string IVA { get; set; }
        public string SubTotal { get; set; }
        public string Total { get; set; }
        public string Discount { get; set; }
        public string Comment { get; set; }
        public string CollectionComment { get; set; }
        public decimal Weight { get; set; }

        [Display(Name = "Usuario")]
        public string User { get; set; }
        
        [Display(Name = "Dirección")]
        public AddressItemModel Address { get; set; }

        [Display(Name = "Cliente")]
        public ClientItemModel Client { get; set; }
        public IEnumerable<ProductItemModel> Products { get; set; }
        public IEnumerable<PromotionItemModel> Promotions { get; set; }

        //Agregamos la referencia de bitacoraItemModel
        public List<BitacoraItemModel> BitacoraEntries { get; set; }

        //Agregamos la propiedad para pronto pago
        public bool ProntoPago { get; set; }


    }
}