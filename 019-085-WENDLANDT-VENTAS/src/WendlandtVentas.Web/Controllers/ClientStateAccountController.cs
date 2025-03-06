using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Specifications;
using System.Linq;
using Monobits.SharedKernel.Interfaces;
using System.Collections.Generic;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;
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

        // Obtener el cliente
        var client = await _repository.GetByIdAsync<Client>(clientId);
        if (client == null)
        {
            return NotFound("Cliente no encontrado");
        }

        // Crear el modelo
        var model = new ClientOrdersViewModel
        {
            ClientName = client.Name
        };

        // Obtener todas las órdenes del cliente
        var clientOrders = (await _repository.ListAsync(new OrdersByClientIdSpecification(clientId))).ToList();

        // Filtrar órdenes entregadas
        var deliveredOrders = clientOrders.Where(c => c.OrderStatus == OrderStatus.Delivered).ToList();
        // Filtrar órdenes no pagadas
        var unpaidOrders = clientOrders.Where(c => c.OrderStatus != OrderStatus.Paid).ToList();

        // Agregar órdenes no pagadas al modelo
        model.Balances.AddRange(unpaidOrders.Select(c => new OrdersIncomeDto
        {
            RemissionCode = c.RemissionCode,
            InvoiceCode = !string.IsNullOrEmpty(c.InvoiceCode) ? c.InvoiceCode : "-",
            DeliveryDate = c.DeliveryDate.ToLocalTime(),
            DueDate = c.DueDate.ToLocalTime() != DateTime.MinValue ? c.DueDate.ToLocalTime() : c.CreatedAt.ToLocalTime().AddDays(client.CreditDays + 1),
            AmountString = $"{c.Total:C2} {c.CurrencyType.Humanize()}"
        }).ToList());

        // Calcular montos pendientes
        model.PendingAmount = deliveredOrders.Where(c => c.CurrencyType.Equals(CurrencyType.MXN)).Sum(c => (double)c.Total);
        model.PendingAmountUsd = deliveredOrders.Where(c => c.CurrencyType.Equals(CurrencyType.USD)).Sum(c => (double)c.Total);

        // Filtrar órdenes pagadas
        var paidOrders = clientOrders.Where(c => c.OrderStatus == OrderStatus.Paid).ToList();

        // Calcular montos pagados
        model.PaidAmount = paidOrders.Where(c => c.CurrencyType.Equals(CurrencyType.MXN)).Sum(c => (double)c.Total);
        model.PaidAmountUsd = paidOrders.Where(c => c.CurrencyType.Equals(CurrencyType.USD)).Sum(c => (double)c.Total);

        model.TotalPorPagar = clientOrders
       .Where(c => c.OrderStatus != OrderStatus.Paid && c.CurrencyType.Equals(CurrencyType.MXN))
       .Sum(c => (double)c.Total);

        // Calcular el total de registros no pagados
        model.TotalPendientes = clientOrders.Count(c => c.OrderStatus != OrderStatus.Paid);

        return View(model);
    }
}
