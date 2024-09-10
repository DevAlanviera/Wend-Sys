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

        [Required(ErrorMessage = "Campo requerido")]
        public IEnumerable<PresentationPrice> PresentationPricesAdd { get; set; } = new List<PresentationPrice>();
        public IEnumerable<PresentationPrice> PresentationsEdit { get; set; } = new List<PresentationPrice>();
        public IEnumerable<PresentationPrice> Presentations { get; set; } = new List<PresentationPrice>();
    }

    public class PresentationPrice
    {
        public int PresentationId { get; set; }
        public string PresentationName { get; set; }
        public decimal Price { get; set; }
        public decimal PriceUsd { get; set; }
        public decimal Weight { get; set; }
    }
}