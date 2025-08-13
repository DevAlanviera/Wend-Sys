using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WendlandtVentas.Core.Entities.Enums;
using static WendlandtVentas.Core.Models.OrderViewModels.OrderWithShipmentsDto;

namespace WendlandtVentas.Core.Models.OrderViewModels
{
    public class OrdersIncomeDto
    {
        [JsonPropertyName("orderId")]
        public int OrderId { get; set; }
        public string RemissionCode { get; set; }
        public string InvoiceCode { get; set; }
        public OrderType OrderType { get; set; }
        [JsonPropertyName("currencyType")]
        public CurrencyType CurrencyType { get; set; }
        [JsonPropertyName("amount")]
        public double Amount { get; set; }
        public string AmountString { get; set; }

        //Agregamos esta propiedad para mostrar el saldo pendiente
        public double PendingAmount { get; set; }
        public double InitialAmount { get; set; }
        public string User { get; set; }
        public string TransferToken { get; set; }
        public DateTime DeliveryDate { get; set; }
        public DateTime DueDate { get; set; }
        public List<int> ClientOrders { get; set; } = new List<int>();

        // Obtenemos la propiedad realamount y en caso de que tenga, la utilizamos
        public decimal RealAmount { get; set; }

        public bool isPaid { get; set; }
    }
}