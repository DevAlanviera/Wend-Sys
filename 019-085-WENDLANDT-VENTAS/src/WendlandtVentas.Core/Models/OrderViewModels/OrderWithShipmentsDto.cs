using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Core.Models.OrderViewModels
{
    public class OrderWithShipmentsDto
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalShipments { get; set; }
        public List<ShipmentDetailDto> Shipments { get; set; }

        public class ShipmentDetailDto
        {
            public string ShipmentNumber { get; set; }
            public string Status { get; set; }
            public decimal Amount { get; set; }
            // Agrega aquí los demás campos que necesites de ShipmentDetailDto
        }
    }

}
