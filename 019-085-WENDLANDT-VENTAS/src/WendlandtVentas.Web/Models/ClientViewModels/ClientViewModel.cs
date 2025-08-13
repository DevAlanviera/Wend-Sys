using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Web.Models.ClientViewModels
{
    public class ClientViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [Display(Name = "Nombre")]
        [StringLength(200, ErrorMessage = "El campo tiene que ser menor o igual a 200 caracteres")]
        public string Name { get; set; }

        [Display(Name = "Clasificación")]
        public Classification? Classification { get; set; }
        public SelectList Classifications { get; set; }

        [Display(Name = "Canal")]
        public Channel? Channel { get; set; }
        public SelectList Channels { get; set; }

        [Display(Name = "Estado")]
        public int? StateId { get; set; }
        public SelectList States { get; set; }

        [Display(Name = "¿Factura?")]
        public bool RequiereFactura { get; set; } // Propiedad para el checkbox

        [RequiredIf("RequiereFactura", true, ErrorMessage = "El RFC es obligatorio cuando se requiere factura.")]
        public string RFC { get; set; }

        [Display(Name = "Dirección")]
        public string Address { get; set; }

        [Display(Name = "Ciudad")]
        public string City { get; set; }

        [Display(Name = "Forma de pago")]
        public PayType? PayType { get; set; }
        public SelectList PayTypes { get; set; }

        [Display(Name = "Vendedor")]
        public string SellerId { get; set; }
        public List<SelectListItem> Sellers { get; set; }

        [Display(Name = "Días de crédito")]
        [Range(0, int.MaxValue, ErrorMessage = "Favor de introducir un número válido")]
        public int CreditDays { get; set; } = 15;

        public ContactViewModel Contact { get; set; } = new ContactViewModel(); // ✅ Evita nulos

        public bool IsNew { get; set; } = true; // Por defecto, asumimos es nuevo

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var parent = validationContext.ObjectInstance as ClientViewModel;

            if (parent != null && parent.IsNew)
            {
                if (string.IsNullOrWhiteSpace(Contact.Name))
                    yield return new ValidationResult("El nombre del contacto es obligatorio.", new[] { "Contact.Name" });

                if (string.IsNullOrWhiteSpace(Contact.Email))
                    yield return new ValidationResult("El correo electrónico es obligatorio.", new[] { "Contact.Email" });
            }
        }

    }
}