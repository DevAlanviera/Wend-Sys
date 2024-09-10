using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Specifications.OrderSpecifications
{
    public class OrdersByClientIdSpecification : BaseSpecification<Order>
    {
        public OrdersByClientIdSpecification(int id) : base(c => c.ClientId == id)
        {
        }
    }
}