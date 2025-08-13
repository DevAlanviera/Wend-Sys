using System;
using System.Collections.Generic;
using System.Text;
using WendlandtVentas.Core;

public class OrderResponse : Response
{
    public int OrderId { get; set; }

    public OrderResponse(bool isSuccess, string message, int orderId)
        : base(isSuccess, message)
    {
        OrderId = orderId;
    }
}
