using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.InventoryViewModels
{
    public class InOutViewModel
    {
        [Display(Name = "Cantidad")]
        [Required(ErrorMessage = "Campo requerido")]
        public int Quantity { get; set; }
        public int ProductPresentationId { get; set; }

        [Display(Name = "Comentario")]
        public string Comment { get; set; }

        public bool IsAdjustment { get; set; }

    }
}