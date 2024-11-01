﻿using Microsoft.AspNetCore.Mvc.Rendering;
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
    }
}