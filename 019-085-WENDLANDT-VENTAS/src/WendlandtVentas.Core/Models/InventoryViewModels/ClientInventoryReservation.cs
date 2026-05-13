using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using System;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Web.Models.InventoryViewModels
{
    public class ClientInventoryReservation : BaseEntity, IAggregateRoot
    {
        public int ClientId { get; set; }
        public virtual Client Client { get; set; }

        public int ProductPresentationId { get; set; }
        public virtual ProductPresentation ProductPresentation { get; set; }

        public int ReservedQuantity { get; set; }      // 300 botellas apartadas
        public int UsedQuantity { get; set; }          // 0 inicialmente
        public int AvailableQuantity => ReservedQuantity - UsedQuantity;  // 300 disponibles

        public string Status { get; set; } = "Active"; // Active, Completed, Cancelled, Expired
        public DateTime? ExpirationDate { get; set; }  // Fecha de vencimiento del apartado
        public string Notes { get; set; }

        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
