using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.MovementViewModels
{
    public class FilterViewModel
    {
        public int ProductPresentationId { get; set; }

        [Display(Name = "Usuario")]
        public string UserId { get; set; }

        public SelectList Users { get; set; }

        [Display(Name = "Fecha inicial")]
        public string DateStart { get; set; }

        [Display(Name = "Fecha final")]
        public string DateEnd { get; set; }

        [Display(Name = "Operación")]
        public string Operation { get; set; }
        public SelectList Operations { get; set; }
    }
}