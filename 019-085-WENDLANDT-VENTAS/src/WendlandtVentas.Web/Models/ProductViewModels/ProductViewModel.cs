using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Web.Models.ProductViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [Display(Name = "Nombre")]
        [StringLength(200, ErrorMessage = "El campo tiene que ser menor o igual a 200 caracteres")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public Distinction Distinction { get; set; }

        [Display(Name = "Temporada")]
        public string Season { get; set; }
        public SelectList Distinctions { get; set; }

        public bool IsBundle { get; set; } // Identifica si es un paquete (como el 12-pack)

        [Required(ErrorMessage = "Campo requerido")]
        [BindRequired]
        public IEnumerable<PresentationPrice> PresentationPricesAdd { get; set; } = new List<PresentationPrice>();
        [BindNever]
        public IEnumerable<PresentationPrice> PresentationsEdit { get; set; } = new List<PresentationPrice>();
        [BindNever]
        public IEnumerable<PresentationPrice> Presentations { get; set; } = new List<PresentationPrice>();

        //Unificacion de inventario
        // El ID del maestro que el usuario seleccionará
        public int? InventorySourceId { get; set; }

        // La lista para llenar el dropdown en la vista
        public IEnumerable<SelectListItem> MasterProducts { get; set; } = new List<SelectListItem>();


        // --- PROPIEDADES PARA BUNDLES ---
        public decimal? BundlePriceMXN { get; set; }
        public decimal? BundlePriceUSD { get; set; }
        public int? BundleQuantityTarget { get; set; } // 6 o 12

        // Lista de componentes que vienen desde la tabla dinámica del Front
        public List<BundleComponentViewModel> Components { get; set; } = new List<BundleComponentViewModel>();

        public IEnumerable<SelectListItem> AvailableComponents { get; set; } = new List<SelectListItem>();
    }

    public class PresentationPrice
    {
        public int PresentationId { get; set; }
        public string PresentationName { get; set; }
        public decimal Price { get; set; }
        public decimal PriceUsd { get; set; }
        public decimal Weight { get; set; }
    }

    public class BundleComponentViewModel
    {
        public int ProductId { get; set; } // ID de la botella/lata individual
        public int Quantity { get; set; }  // Cuántas de esa cerveza van en el pack
    }
}