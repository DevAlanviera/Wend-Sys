using System.Collections.Generic;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace WendlandtVentas.Web.Models.ClientViewModels
{
    public class BalanceViewModel
    {
        public string ClientName { get; set; }
        public double PendingAmount { get; set; }
        public double PendingAmountUsd { get; set; }
        public List<OrdersIncomeDto> Balances { get; set; } = new List<OrdersIncomeDto>();
    }
}