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
            // 1. Validar Cliente
            var clientId = filters.ContainsKey("ClientId") && !string.IsNullOrEmpty(filters["ClientId"]) ?
                int.Parse(filters["ClientId"]) : 0;

            // 2. Validar Lista de Estatus
            var orderStatus = filters.ContainsKey("StatusList") && !string.IsNullOrEmpty(filters["StatusList"]) ?
                filters["StatusList"].Split(',').Select(a => (OrderStatus)Enum.Parse(typeof(OrderStatus), a)) : null;

            // 3. NUEVO: Validar Clasificación (Cerveza vs Wellen)
            var classificationId = filters.ContainsKey("OrderClassification") && !string.IsNullOrEmpty(filters["OrderClassification"]) ?
                int.Parse(filters["OrderClassification"]) : 0;

            if (clientId > 0)
                AppendCriteria(c => c.ClientId == clientId, true);

            if (orderStatus != null)
                AppendCriteria(c => orderStatus.Contains(c.OrderStatus), true);

            // Aplicamos el filtro de clasificación si viene en el diccionario
            if (classificationId > 0)
                AppendCriteria(c => (int)c.OrderClassification == classificationId, true);
        }
    }
}