using System;
using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Core.Specifications.OrderSpecifications
{
    public class OrdersFiltersSpecification : BaseSpecification<Order>
    {
        public OrdersFiltersSpecification(Dictionary<string, string> filters) : base(c => true)
        {
            var clientId = !string.IsNullOrEmpty(filters["ClientId"]) ? 
                int.Parse(filters["ClientId"]) : 0;

            var orderStatus = !string.IsNullOrEmpty(filters["StatusList"]) ?
                filters["StatusList"].Split(',').Select(a => (OrderStatus)Enum.Parse(typeof(OrderStatus), a)) : null;

            if (clientId > 0)
                AppendCriteria(c => c.ClientId == clientId, true);

            if (orderStatus != null)
                AppendCriteria(c => orderStatus.Contains(c.OrderStatus), true);
        }
    }
}