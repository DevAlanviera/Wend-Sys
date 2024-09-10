using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Web.Models.TableModels
{
    public class ClientTableModel
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [Display(Name = "Clasificación")]
        public string Classification { get; set; }

        [Display(Name = "Canal")]
        public string Channel { get; set; }

        [Display(Name = "Estado")]
        public string State { get; set; }

        [Display(Name = "Fecha de alta")]
        public string CreationDate { get; set; }

        public string RFC { get; set; }

        [Display(Name = "Direcciones")]
        public int Addresses { get; set; }

        [Display(Name = "Ciudad")]
        public string City { get; set; }

        [Display(Name = "Forma de pago")]
        public string PayType { get; set; }

        public string Seller { get; set; }

        public int Contacts { get; set; }

        public int CreditDays { get; set; } = 0;
    }
}