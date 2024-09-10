using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Web.Models.PromotionViewModels
{
    public class PromotionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [Display(Name = "Nombre")]
        [StringLength(200, ErrorMessage = "El campo tiene que ser menor o igual a 200 caracteres")]
        public string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo tiene que ser mayor a cero")]
        [Required(ErrorMessage = "Campo requerido")]
        [Display(Name = "Compra")]
        public int Buy { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo tiene que ser mayor a cero")]
        [Required(ErrorMessage = "Campo requerido")]
        [Display(Name = "Obsequio")]
        public int Present { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [Display(Name = "Descuento %")]
        public double Discount { get; set; }

        [Display(Name = "Tipo")]
        public PromotionType Type { get; set; }

        [Display(Name = "Clasificación")]
        public Classification? Classification { get; set; }
        public SelectList Classifications { get; set; }

        [Display(Name = "Presentacion(es)")]
        public List<int> PresentationIds { get; set; } = new List<int>();
        public SelectList Presentations { get; set; }

        [Display(Name = "Cliente(s)")]
        public List<int> ClientIds { get; set; } = new List<int>();
        public SelectList Clients { get; set; }
        public bool PresentationsAllSelected { get; set; }
        public IEnumerable<PromotionType> PromotionTypes { get; set; }
    }
}