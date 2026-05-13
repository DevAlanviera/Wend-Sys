using System;
using System.Collections.Generic;
using System.Text;
using WendlandtVentas.Web.Models.InventoryViewModels;

namespace WendlandtVentas.Core.Specifications.InventorySpecifications
{
    public class ClientInventoryReservationByClientSpecification : BaseSpecification<ClientInventoryReservation>
    {
        public ClientInventoryReservationByClientSpecification(int clientId, int productPresentationId)
            : base(r => r.ClientId == clientId
                     && r.ProductPresentationId == productPresentationId
                     && !r.IsDeleted)
        {
            ApplyOrderBy(r => r.CreatedAt);
        }
    }
}
