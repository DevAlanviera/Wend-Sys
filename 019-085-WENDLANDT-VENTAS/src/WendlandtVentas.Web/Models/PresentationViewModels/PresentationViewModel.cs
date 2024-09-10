using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.PresentationViewModels
{
    public class PresentationViewModel
    {
        [Display(Name = "Cantidad")]
        [Required(ErrorMessage = "Campo requerido")]
        public int Quantity { get; set; }
        public int PresentationId { get; set; }

        [Display(Name = "Comentario")]
        public string Comment { get; set; }

        public bool IsAdjustment { get; set; }

    }
}