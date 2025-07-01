using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Specifications;
using System.Linq;
using Monobits.SharedKernel.Interfaces;
using System.Collections.Generic;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Models.OrderViewModels;
using WendlandtVentas.Web.Models.ClientViewModels;
using WendlandtVentas.Core.Specifications.OrderSpecifications;
using System;

using Humanizer;
using Microsoft.AspNetCore.Authorization;

public class ClientStateAccountController : Controller
{
    private readonly ITreasuryApi _treasuryApi;
    private readonly IAsyncRepository _repository;

    
    public ClientStateAccountController(IAsyncRepository repository, ITreasuryApi treasuryApi)
    {
        _repository = repository;
        _treasuryApi = treasuryApi;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(int clientId)
    {
        ViewData["Title"] = "Estado de Cuenta";

        // Obtener el cliente (existente)
        var client = await _repository.GetByIdAsync<Client>(clientId);
        if (client == null)
        {
            return NotFound("Cliente no encontrado");
        }

        // Crear el modelo (existente)
        var model = new ClientOrdersViewModel
        {
            ClientName = client.Name
        };

        // Obtener todas las órdenes del cliente (existente)
        var clientOrders = (await _repository.ListAsync(new OrdersByClientIdSpecification(clientId)))
        .Where(o =>
            // Mostrar órdenes normales (ProntoPago = false)
            !o.ProntoPago ||

            // Mostrar órdenes ProntoPago SÓLO si están pagadas
            (o.ProntoPago && o.OrderStatus == OrderStatus.Paid)
        )
        .ToList();


        // Filtrar órdenes (existente)
        var deliveredOrders = clientOrders.Where(c => c.OrderStatus == OrderStatus.Delivered).ToList();
        var unpaidOrders = clientOrders.Where(c => c.OrderStatus != OrderStatus.Paid).ToList();
        var ordersWithPayments = clientOrders
            .Where(c => c.OrderStatus == OrderStatus.PartialPayment || c.OrderStatus == OrderStatus.Paid)
            .ToList();

        // --- Resto del código existente ---
        model.Balances.AddRange(
    clientOrders.Select(c => new OrdersIncomeDto
    {
        RemissionCode = c.RemissionCode,
        InvoiceCode = !string.IsNullOrEmpty(c.InvoiceCode) ? c.InvoiceCode : "-",
        DeliveryDate = c.DeliveryDate.ToLocalTime(),
        DueDate = c.DueDate.ToLocalTime() != DateTime.MinValue
            ? c.DueDate.ToLocalTime()
            : c.CreatedAt.ToLocalTime().AddDays(client.CreditDays + 1),
        AmountString = $"{((c.RealAmount ?? 0.0m) != 0.0m ? c.RealAmount.Value : c.Total):C2} {c.CurrencyType.Humanize()}",
        RealAmount = c.RealAmount ?? c.Total,

        isPaid = c.OrderStatus == OrderStatus.Paid
    })
    .OrderByDescending(c => c.DeliveryDate)
    .ToList()
);


        // Cálculos existentes
        model.PendingAmount = deliveredOrders.Where(c => c.CurrencyType.Equals(CurrencyType.MXN)).Sum(c => (double)c.Total);
        model.PendingAmountUsd = deliveredOrders.Where(c => c.CurrencyType.Equals(CurrencyType.USD)).Sum(c => (double)c.Total);

        var paidOrders = clientOrders.Where(c => c.OrderStatus == OrderStatus.Paid).ToList();
        model.PaidAmount = paidOrders.Where(c => c.CurrencyType.Equals(CurrencyType.MXN)).Sum(c => (double)c.Total);
        model.PaidAmountUsd = paidOrders.Where(c => c.CurrencyType.Equals(CurrencyType.USD)).Sum(c => (double)c.Total);

        model.TotalPorPagar = clientOrders
        .Where(c => c.OrderStatus != OrderStatus.Paid && c.CurrencyType.Equals(CurrencyType.MXN))
        .Sum(c => (double)(c.RealAmount ?? c.Total));  // Cambio clave aquí

        model.TotalPendientes = clientOrders.Count(c => c.OrderStatus != OrderStatus.Paid);

        return View(model);
    }


}

