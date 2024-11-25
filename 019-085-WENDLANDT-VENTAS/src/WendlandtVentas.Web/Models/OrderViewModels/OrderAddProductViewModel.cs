using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Web.Models.OrderViewModels
{
    public class OrderAddProductViewModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [Display(Name = "Cantidad")]
        [Range(1, int.MaxValue, ErrorMessage = "El campo tiene que ser mayor a cero")]
        public int Quantity { get; set; }
        
        [Required(ErrorMessage = "Campo requerido")]
        [Display(Name = "Precio")]
        [Range(1, int.MaxValue, ErrorMessage = "El campo tiene que ser mayor a cero")]
        public decimal Price { get; set; }

        [Display(Name = "¿Es Obsequio?")]
        public bool IsPresent { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [Display(Name = "Producto")]

        
        public int ProductPresentationId { get; set; }


        public SelectList ProductsPresentations { get; set; }

        public bool ExistPresentation { get; set; }

        public CurrencyType CurrencyType { get; set; }


        public bool IsAuthorized(ClaimsPrincipal user)
        {
            return user.IsInRole("Administrator") || user.IsInRole("AdministratorCommercial") || user.IsInRole("Storekeeper") || user.IsInRole("Billing");
        }
    }
}