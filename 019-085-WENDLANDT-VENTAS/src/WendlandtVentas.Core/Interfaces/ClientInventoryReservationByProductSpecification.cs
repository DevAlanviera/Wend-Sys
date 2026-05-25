using System;
using System.Collections.Generic;
using System.Text;
using WendlandtVentas.Core.Specifications;
using WendlandtVentas.Web.Models.InventoryViewModels;

namespace WendlandtVentas.Core.Interfaces
{
    public class ClientInventoryReservationByProductSpecification : BaseSpecification<ClientInventoryReservation>
    {
        public ClientInventoryReservationByProductSpecification(int productPresentationId)
            : base(r => r.ProductPresentationId == productPresentationId && !r.IsDeleted)
        {
            AddInclude(r => r.Client);
            ApplyOrderBy(r => r.CreatedAt);
        }
    }
}
