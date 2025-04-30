using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WendlandtVentas.Web.Models.ClientViewModels
{
    public class CommentsViewModel
    {
        public int Id { get; set; }
        [Display(Name = "Comentario")]
        [Required(ErrorMessage = "El campo {0} es requerido.")]
        public string Comment { get; set; }
        [Display(Name = "Comentario")]
        [Required(ErrorMessage = "El campo {0} es requerido.")]
        public int ClientId { get; set; }

    }
}
