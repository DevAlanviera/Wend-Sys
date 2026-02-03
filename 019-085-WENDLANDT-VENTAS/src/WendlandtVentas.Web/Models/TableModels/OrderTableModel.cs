using Humanizer;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Web.Models.TableModels
{
    public class OrderTableModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public OrderStatus StatusEnum { get; set; }
        public string Status => StatusEnum.Humanize();
        public string RemissionCode { get; set; }
        public string InvoiceCode { get; set; }
        public bool IsPaid { get; set; }
        public string CreateDate { get; set; }
        public string PaymentPromiseDate { get; set; }
        public string PaymentDate { get; set; }
        public string Distribution { get; set; }
        public string BaseAmount { get; set; }
        public string IEPS { get; set; }
        public string IVA { get; set; }
        public string SubTotal { get; set; }
        public string Total { get; set; }
        public string User { get; set; }
        public string Client { get; set; }
        public string Comment { get; set; }
        public string Address { get; set; }
        public bool CanDelete => StatusEnum == OrderStatus.New;
        public bool CanEdit { get; set; }

        //Se agrega esta propiedad para obtener el monto real
        public decimal? RealAmount { get; set; }

        // Agrega estas dos:
        public int OrderClassification { get; set; }
        public int? OrderClassificationCode { get; set; }
    }
}