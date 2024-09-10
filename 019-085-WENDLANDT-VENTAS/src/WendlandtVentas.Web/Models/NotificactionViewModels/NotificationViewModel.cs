using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using WendlandtVentas.Core.Models.Enums;

namespace WendlandtVentas.Web.Models.NotificationViewModels
{
    public class NotificationViewModel
    {
        [Display(Name = "Título")]
        public string Title { get; set; }

        [Display(Name = "Mensaje")]
        public string Message { get; set; }

        [Display(Name = "Usuario")]
        public string UserId { get; set; }
        public SelectList Users { get; set; }

        [Display(Name = "Rol")]
        public string Role { get; set; }
        public SelectList Roles { get; set; }
        public bool ByUser { get; set; }
    }
}