using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Models.Enums;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace WendlandtVentas.Web.Models.ClientViewModels
{
    public class ClientOrdersViewModel
    {
        public string ClientName { get; set; } // Nombre del cliente
        public List<OrdersIncomeDto> Balances { get; set; } = new List<OrdersIncomeDto>(); // Lista de balances (órdenes entregadas)
        public double PendingAmount { get; set; } // Monto pendiente en MXN
        public double PendingAmountUsd { get; set; } // Monto pendiente en USD

        // Propiedades para órdenes pagadas
        public double PaidAmount { get; set; } // Monto total pagado en MXN
        public double PaidAmountUsd { get; set; } // Monto total pagado en USD
        public List<OrdersIncomeDto> PaidOrders { get; set; } = new List<OrdersIncomeDto>(); // Lista de órdenes pagadas

        public int TotalPendientes { get; set; } // Total de registros no pagados

        public double TotalPorPagar { get; set; } // Total por pagar en MXN

        //Columnas
        public double Monto { get; set; } // Monto total de la orden
        public double Pagado { get; set; } // Monto pagado
        public double PorPagar { get; set; } // Monto por pagar (Monto - Pagado)
    }

}
