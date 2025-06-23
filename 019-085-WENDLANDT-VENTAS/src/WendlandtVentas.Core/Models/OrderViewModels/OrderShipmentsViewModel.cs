using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WendlandtVentas.Core.Entities.Enums;

public class OrderShipmentsViewModel
{
    public int OrderId { get; set; }
    public string FormattedTotalAmount { get; set; } // Ej: "$1,000.00 MXN"
    public List<ShipmentViewModel> Shipments { get; set; }
}

public class ShipmentViewModel
{
    public string ShipmentNumber { get; set; }
    public string StatusBadgeClass { get; set; } // Ej: "badge-success"
    public string FormattedAmount { get; set; } // Ej: "$500.00 USD"
}