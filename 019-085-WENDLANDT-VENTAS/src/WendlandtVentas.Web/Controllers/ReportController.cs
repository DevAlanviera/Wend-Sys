using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models.OrderViewModels;
using WendlandtVentas.Web.Models.ReportViewModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial, Billing, Sales")]
    public class ReportController : Controller
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly IOrderService _orderService;
        private readonly IAsyncRepository _repository;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(ILogger<NotificationController> logger,
            IAsyncRepository repository,
            IOrderService orderService,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _repository = repository;
            _orderService = orderService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(FilterViewModel filter)
        {
            var orderTypes = Enum.GetValues(typeof(OrderType)).Cast<OrderType>().AsEnumerable();
            var orderStatus = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().AsEnumerable();
            var products = await _repository.ListAllAsync<Product>();
            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var states = await _repository.ListAllExistingAsync<State>();
            var clients = await _repository.ListAllExistingAsync<Client>();
            var cities = clients.Select(d => d.City).Distinct();
            var users = _userManager.Users;

            if (filter.StateId.Any())
                clients = clients.Where(c => filter.StateId.Any(s => s == c.StateId)).ToList();

            filter.FilterDate = filter.FilterDate;
            filter.OrderStatusAll = new SelectList(orderStatus.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text");
            filter.OrderTypeAll = new SelectList(orderTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text");
            filter.Products = new SelectList(products.Select(x => new { Value = x.Id, Text = x.IsDeleted? $"{x.Name} (Borrado)" : x.Name }), "Value", "Text");
            filter.Presentations = new SelectList(presentations.Select(x => new { Value = x.Id, Text = $"{x.Name} {x.Liters} lts" }), "Value", "Text");
            filter.StatesAll = new SelectList(states.OrderBy(s => s.Name).Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            filter.Clients = new SelectList(clients.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            filter.Users = new SelectList(users.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            filter.CityAll = new SelectList(cities.Select(x => new { Value = x, Text = x }), "Value", "Text");
            
            var data = await GetDataAsync(filter);
            ViewBag.DataSource = data;

            return View(filter);
        }

        public async Task<List<PivotDataOrderModel>> GetDataAsync(FilterViewModel filter)
        {
            if (string.IsNullOrEmpty(filter.DateStart) && string.IsNullOrEmpty(filter.DateEnd))
            {
                var currentDate = DateTime.Now;
                filter.DateEnd = currentDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                filter.DateStart = currentDate.AddDays(-7).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            var filteredOrders = await _orderService.FilterValues(filter);
            var products = filteredOrders.SelectMany(c => c.OrderProducts).Distinct().ToList();
            if (filter.ProductId.Any())
            {
                products = products.Where(c => filter.ProductId.Any(d => c.ProductPresentation.ProductId.Equals(d))).ToList();
            }
            
            if (filter.PresentationId.Any())
            {
                products = products.Where(c => filter.PresentationId.Any(d => c.ProductPresentationId.Equals(d))).ToList();
            }

            var users = _userManager.Users.ToDictionary(c => c.Id, c => c.Name);
            var pivotDataOrders = products.Select(c =>
            {
                var qty = c.Order.Type == OrderType.Return ? -c.Quantity : c.Quantity;

                //La lógica para calcular el total del producto se moverá en la siguiente historia y se hará uso del _logger
                var price = c.Price != 0 ? c.Price : c.ProductPresentation.Price;
                var totalProduct = !c.IsPresent ? qty * price : 0;
                if (c.Order.Type == OrderType.Invoice)
                    if (totalProduct > 0)
                    {
                        var baseAmount = totalProduct / 1.265M; //Formula anterior totalProduct / 1.265M * 0.8M;
                        //var distribution = baseAmount * 0.3163M;
                        var ieps = baseAmount * 0.265M;
                        var iva = (baseAmount + ieps) * 0.16M; //Formula anterior (baseAmount + distribution + ieps) * 0.16M;
                        totalProduct = baseAmount + ieps + iva; //Formula anterior baseAmount + distribution + ieps + iva;
                    }
                
                return new PivotDataOrderModel
                {
                    OrderId = c.OrderId,
                    Order = $"Pedido {c.OrderId}",
                    Client = c.Order.Client.Name,
                    State = c.Order.Client.State == null ? "Ninguno" : c.Order.Client.State.Name,
                    City = string.IsNullOrEmpty(c.Order.Client.City) ? "Ninguna" : c.Order.Client.City,
                    ProductId = c.ProductPresentation.ProductId,
                    Product = c.ProductPresentation.Product.Name,
                    Quantity = qty,
                    TotalProduct = totalProduct,
                    Type = c.Order.Type.Humanize(),
                    PresentationId = c.ProductPresentation.PresentationId,
                    Presentation = c.ProductPresentation.Presentation.Name,
                    CreationDate = $"{c.CreatedAt.AddHours(-8):dd MMM yyyy}",
                    Month = c.CreatedAt.AddHours(-8).Month,
                    User = users.FirstOrDefault(d => d.Key.Equals(c.Order.UserId)).Value,
                    Liters = c.ProductPresentation.Presentation.Liters * qty
                };
            }).ToList();

            pivotDataOrders = pivotDataOrders.OrderBy(d => d.OrderId).ToList();

            return pivotDataOrders;
        }
    }
}