using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.InventoryViewModels
{
    public class FilterViewModel
    {
        [Display(Name = "Producto")]
        public int? ProductId { get; set; }
        public SelectList Products { get; set; }

        [Display(Name = "Presentación")]
        public int? PresentationId { get; set; }
        public SelectList Presentations { get; set; }
    }
}