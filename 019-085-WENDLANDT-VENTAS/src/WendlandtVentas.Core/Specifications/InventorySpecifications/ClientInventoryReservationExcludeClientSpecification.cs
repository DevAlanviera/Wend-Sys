using System;
using System.Collections.Generic;
using System.Text;
using WendlandtVentas.Web.Models.InventoryViewModels;

namespace WendlandtVentas.Core.Specifications.InventorySpecifications
{
    public class ClientInventoryReservationExcludeClientSpecification : BaseSpecification<ClientInventoryReservation>
    {
        public ClientInventoryReservationExcludeClientSpecification(int productPresentationId, int excludeClientId)
            : base(r => r.ProductPresentationId == productPresentationId
                     && r.ClientId != excludeClientId
                     && r.Status == "Active"
                     && !r.IsDeleted)
        {
        }
    }
}
