using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WendlandtVentas.Core.Models.Enums;

namespace WendlandtVentas.Core.Models.OrderViewModels
{
    public class FilterViewModel
    {
        [Display(Name = "Tipo")]
        public string OrderType { get; set; }
        public SelectList OrderTypeAll { get; set; }

        [Display(Name = "Estatus")]
        public List<string> OrderStatus { get; set; } = new List<string>();
        public SelectList OrderStatusAll { get; set; }

        [Display(Name = "Cliente")]
        public List<int?> ClientId { get; set; } = new List<int?>();

        public int? ClientSelected { get; set; }
        public SelectList Clients { get; set; }

        [Display(Name = "Fecha inicial")]
        public string DateStart { get; set; }

        [Display(Name = "Fecha final")]
        public string DateEnd { get; set; }

        [Display(Name = "Campo a filtrar por fecha(s)")]
        public FilterDate FilterDate { get; set; }

        [Display(Name = "Ciudad")]
        public List<string?> City { get; set; }
        public SelectList CityAll { get; set; }

        [Display(Name = "Lugar")]
        public List<int?> StateId { get; set; } = new List<int?>();
        public SelectList StatesAll { get; set; }

        [Display(Name = "Pago realizado")]
        public bool Paid { get; set; }

        [Display(Name = "Producto")]
        public List<int?> ProductId { get; set; } = new List<int?>();
        public SelectList Products { get; set; }

        [Display(Name = "Presentación")]
        public List<int?> PresentationId { get; set; } = new List<int?>();
        public SelectList Presentations { get; set; }
        
        [Display(Name = "Usuarios")]
        public List<string> UserId { get; set; } = new List<string>();
        public SelectList Users { get; set; }

        [Display(Name = "Litros")]
        public decimal? Liters { get; set; }

        public int? Liters_Presentation { get; set; }

        public bool ProntoPago { get; set; }
    }  
}