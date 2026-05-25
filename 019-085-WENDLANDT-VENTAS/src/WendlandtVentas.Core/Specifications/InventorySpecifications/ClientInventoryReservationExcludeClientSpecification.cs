using System;
using System.Collections.Generic;
using System.Text;
using WendlandtVentas.Web.Models.InventoryViewModels;

namespace WendlandtVentas.Core.Specifications.InventorySpecifications
{
    public class ClientInventoryReservationExcludeClientSpecification : BaseSpecification<ClientInventoryReservation>
    {
        // Constructor para filtrar por ProductPresentationId y excluir un cliente
        // Si excludeClientId = 0, no excluye a nadie (toma todos los apartados)
        public ClientInventoryReservationExcludeClientSpecification(int productPresentationId, int excludeClientId)
            : base(r => r.ProductPresentationId == productPresentationId
                     && (excludeClientId == 0 || r.ClientId != excludeClientId)
                     && r.Status == "Active"
                     && !r.IsDeleted)
        {
        }
    }
}
