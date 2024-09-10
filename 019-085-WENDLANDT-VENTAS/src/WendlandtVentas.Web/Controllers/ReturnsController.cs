using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Syncfusion.EJ2.Base;
using System.Threading.Tasks;
using System;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Web.Libs;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models.OrderViewModels;
using Humanizer;
using WendlandtVentas.Web.Models.TableModels;
using WendlandtVentas.Web.Extensions;
using System.IO;
using WendlandtVentas.Core.Models.ClientViewModels;
using WendlandtVentas.Core.Models.ProductViewModels;
using WendlandtVentas.Core.Models.PromotionViewModels;
using WendlandtVentas.Core.Specifications.OrderExtendedSpecifications;
using WendlandtVentas.Infrastructure.Commons;
using System.Globalization;
using WendlandtVentas.Core.Models.ProductPresentationViewModels;
using WendlandtVentas.Web.Models.OrderViewModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize]
    public class ReturnsController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IInventoryService _inventoryService;
        private readonly INotificationService _notificationService;
        private readonly IAsyncRepository _repository;
        private readonly ILogger<ProductController> _logger;
        private readonly SfGridOperations _sfGridOperations;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExcelReadService _excelReadService;
        private readonly ITreasuryApi _treasuryApi;

        public ReturnsController(IAsyncRepository repository,
            ILogger<ProductController> logger,
            SfGridOperations sfGridOperations,
            UserManager<ApplicationUser> userManager,
            IOrderService orderService,
            INotificationService notificationService,
            IInventoryService inventoryService,
            IExcelReadService excelReadService,
            ITreasuryApi treasuryApi)
        {
            _repository = repository;
            _logger = logger;
            _sfGridOperations = sfGridOperations;
            _userManager = userManager;
            _orderService = orderService;
            _notificationService = notificationService;
            _inventoryService = inventoryService;
            _excelReadService = excelReadService;
            _treasuryApi = treasuryApi;
        }
        public async Task<IActionResult> Index(FilterViewModel filter)
        {
            var orderStatus = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Where(c => (c == OrderStatus.New || c == OrderStatus.InProcess || c == OrderStatus.Delivered)).AsEnumerable();
            var products = await _repository.ListAllExistingAsync<Product>();
            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var states = await _repository.ListAllExistingAsync<State>();
            var clients = await _repository.ListAllExistingAsync<Client>();
            var cities = clients.Select(d => d.City).Distinct();

            if (filter.StateId.Any())
                clients = clients.Where(c => filter.StateId.Any(s => s == c.StateId)).ToList();

            filter.FilterDate = filter.FilterDate;
            filter.OrderStatusAll = new SelectList(orderStatus.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text", filter.OrderStatus);
            filter.Products = new SelectList(products.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            filter.Presentations = new SelectList(presentations.Select(x => new { Value = x.Id, Text = $"{x.Name} {x.Liters} lts" }), "Value", "Text");
            filter.StatesAll = new SelectList(states.OrderBy(s => s.Name).Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            filter.Clients = new SelectList(clients.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            filter.CityAll = new SelectList(cities.Select(x => new { Value = x, Text = x }), "Value", "Text");

            return View(filter);
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataManagerRequest dm, FilterViewModel filter)
        {
            var users = _userManager.Users.ToDictionary(c => c.Id, c => c.Name);
            var filteredOrders = await _orderService.FilterValues(filter);
            var dataSource = filteredOrders
                .Where(c => c.Type == OrderType.Return)
                .Select(c => new OrderTableModel
                {
                    Id = c.Id,
                    Type = c.PayType.HasValue ? $"{c.Type.Humanize()} ({c.PayType.Value.Humanize()})" : c.Type.Humanize(),
                    InvoiceCode = c.InvoiceCode ?? string.Empty,
                    RemissionCode = c.RemissionCode,
                    IsPaid = c.Paid,
                    PaymentDate = c.PaymentDate == DateTime.MinValue ? string.Empty : c.PaymentDate.ToLocalTime().FormatDateShortMx(),
                    PaymentPromiseDate = c.PaymentPromiseDate == DateTime.MinValue ? string.Empty : c.PaymentPromiseDate.ToLocalTime().FormatDateShortMx(),
                    CreateDate = c.CreatedAt.ToLocalTime().FormatDateShortMx(),
                    Total = c.Type != OrderType.Invoice ? c.SubTotal.FormatCurrency() : c.Total.FormatCurrency(),
                    //User = users[c.UserId],
                    Client = c.Client.Name,
                    StatusEnum = c.OrderStatus,
                    Comment = c.Comment,
                    Address = c.Address ?? string.Empty,
                    CanEdit = c.OrderStatus != OrderStatus.Delivered
                });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            ViewData["ModalTitle"] = "Detalles de la devolución";
            var order = await _repository.GetAsync(new OrderExtendedSpecification(id));
            var user = await _userManager.FindByIdAsync(order.UserId);
            var weight = await CalcOrderWeight(id);
            var model = new OrderDetailsViewModel
            {
                Id = order.Id,
                TypeEnum = order.Type,
                RemissionCode = order.RemissionCode,
                InvoiceCode = order.InvoiceCode,
                IsPaid = order.Paid,
                CreateDate = order.CreatedAt.ToLocalTime(),
                PaymentPromiseDate = order.PaymentPromiseDate.ToLocalTime() == DateTime.MinValue ? "-" : order.PaymentPromiseDate.ToLocalTime().FormatDateShortMx(),
                PaymentDate = order.PaymentDate.ToLocalTime() == DateTime.MinValue ? "-" : order.PaymentDate.ToLocalTime().FormatDateShortMx(),
                IEPS = order.IEPS.FormatCurrency(),
                BaseAmount = order.BaseAmount.FormatCurrency(),
                //Distribution = order.Distribution.FormatCurrency(),
                IVA = order.IVA.FormatCurrency(),
                Status = order.OrderStatus.Humanize(),
                SubTotal = order.SubTotal.FormatCurrency(),
                Total = order.Total.FormatCurrency(),
                Discount = order.Discount.FormatCurrency(),
                User = user != null ? user.Name : "-",
                Comment = order.Comment,
                CollectionComment = order.CollectionComment,
                Weight = weight,
                Address = new AddressItemModel
                {
                    Name = order.AddressName,
                    DeliveryDay = order.DeliveryDate.ToLocalTime() == DateTime.MinValue ? order.CreatedAt.ToLocalTime().FormatDateShortMx() : order.DeliveryDate.ToLocalTime().FormatDateShortMx(),
                    AddressLocation = order.Address,
                    DeliverySpecification = order.DeliverySpecification
                },
                Client = new ClientItemModel
                {
                    Id = order.ClientId,
                    Channel = order.Client.Channel == null ? "-" : order.Client.Channel.Humanize(),
                    PayType = order.PayType.HasValue ? order.PayType.Humanize() : "-",
                    Name = order.Client.Name,
                    Classification = order.Client.Classification == null ? "-" : order.Client.Classification.Humanize(),
                    State = order.Client.State == null ? "-" : order.Client.State.Name,
                },
                Products = order.OrderProducts.Where(c => !c.IsDeleted).Select(c => new ProductItemModel
                {
                    Id = c.Id,
                    Name = c.ProductPresentation.Product.Name,
                    PresentationLiters = c.ProductPresentation.NameExtended(),
                    Price = c.Price != 0 ? c.Price : order.CurrencyType == CurrencyType.MXN
                    ? c.ProductPresentation.Price
                    : c.ProductPresentation.PriceUsd,
                    Quantity = c.Quantity,
                    IsPresent = c.IsPresent
                }),
                Promotions = order.OrderPromotions.Where(c => !c.IsDeleted).Select(c => new PromotionItemModel
                {
                    Id = c.PromotionId,
                    Name = c.Promotion.Name,
                    Products = c.OrderPromotionProducts.Where(c => !c.IsDeleted).Select(d => new ProductItemModel
                    {
                        Id = d.Id,
                        Name = d.ProductPresentation.NameExtended(),
                        Quantity = d.Quantity,
                        Price = order.OrderProducts.FirstOrDefault(x => x.ProductPresentationId == d.ProductPresentationId).Price != 0
                        ? order.OrderProducts.FirstOrDefault(x => x.ProductPresentationId == d.ProductPresentationId).Price
                        : order.CurrencyType == CurrencyType.MXN
                        ? d.ProductPresentation.Price
                        : d.ProductPresentation.PriceUsd
                    }).ToList()
                })
            };

            return PartialView("_DetailsModal", model);
        }

        public async Task<IActionResult> PrintOrder(int id)
        {
            var currentPath = Environment.CurrentDirectory;
            var path = Path.Combine(currentPath, "wwwroot/resources/returns");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
            }
            else
            {
                Directory.CreateDirectory(path);
            }

            var order = await _repository.GetAsync(new OrderExtendedSpecification(id));
            var user = await _userManager.FindByIdAsync(order.UserId);
            var weight = await CalcOrderWeight(id);
            var model = new OrderDetailsViewModel
            {
                Id = order.Id,
                TypeEnum = order.Type,
                RemissionCode = order.RemissionCode,
                InvoiceCode = order.InvoiceCode,
                CreateDate = order.CreatedAt.ToLocalTime(),
                BaseAmount = order.BaseAmount.FormatCurrency(),
                //Distribution = order.Distribution.FormatCurrency(),
                Status = order.OrderStatus.Humanize(),
                Total = order.Total.FormatCurrency(),
                User = user != null ? user.Name : "-",
                Comment = order.Comment,
                CollectionComment = order.CollectionComment,
                Weight = weight,
               
                Client = new ClientItemModel
                {
                    Id = order.ClientId,
                    Channel = order.Client.Channel == null ? "-" : order.Client.Channel.Humanize(),
                    Name = order.Client.Name,
                    Classification = order.Client.Classification == null ? "-" : order.Client.Classification.Humanize(),
                    State = order.Client.State == null ? "-" : order.Client.State.Name,
                    City = string.IsNullOrEmpty(order.Client.City) ? "-" : order.Client.City,
                    RFC = string.IsNullOrEmpty(order.Client.RFC) ? "-" : order.Client.RFC
                },
                Products = order.OrderProducts.Where(c => !c.IsDeleted).Select(c => new ProductItemModel
                {
                    Id = c.Id,
                    Name = c.ProductPresentation.Product.Name,
                    PresentationLiters = $"{c.ProductPresentation.Product.Name} - {c.ProductPresentation.Presentation.Name}({c.ProductPresentation.Presentation.Liters})",
                    Price = c.Price != 0 ? c.Price : order.CurrencyType == CurrencyType.MXN
                    ? c.ProductPresentation.Price
                    : c.ProductPresentation.PriceUsd,
                    Quantity = c.Quantity,
                    IsPresent = c.IsPresent
                }),
               
            };
            try
            {
                var archivo_generado = await _excelReadService.FillData(currentPath, model);

                FileStream fileStream = new FileStream(archivo_generado, FileMode.Open, FileAccess.ReadWrite);
                return File(fileStream, "application/octet-stream", $"Devolucion{(model.TypeEnum == OrderType.Invoice ? model.InvoiceCode : model.RemissionCode)}.pdf");
            }
            catch (Exception err)
            {
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, err.Message));
            }
        }

        public async Task<decimal> CalcOrderWeight(int id)
        {
            try
            {
                var order = await _repository.GetAsync(new OrderExtendedSpecification(id));

                var orderWeight = order.OrderProducts.Sum(c => c.ProductPresentation.Weight * c.Quantity);

                return Math.Round(orderWeight, 2);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo calcular el peso del pedido"));
            }
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, AdministratorAssistant, Sales, Distributor, Storekeeper, Billing")]
        [HttpGet]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            ViewData["Action"] = nameof(ChangeStatus);
            ViewData["ModalTitle"] = "Cambiar estado";
            var order = await _repository.GetByIdAsync<Order>(id);
            var status = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Where(c => (c == OrderStatus.InProcess || c == OrderStatus.Delivered)).OrderBy(x => x).AsEnumerable();
            
            var client = await _repository.GetByIdAsync<Client>(order.ClientId);
            var model = new OrderStatusViewModel
            {
                OrderId = order.Id,
                Status = order.OrderStatus,
                StatusList = new SelectList(status.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                Comments = order.Comment,
                Type = order.Type,
                Client = new ClientTableModel
                {
                    Name = client.Name,
                    Classification = client.Classification == null ? "-" : client.Classification.Humanize(),
                    Channel = client.Channel == null ? "-" : client.Channel.Humanize(),
                    State = client.State == null ? "-" : client.State.Name,
                    RFC = string.IsNullOrEmpty(client.RFC) ? "-" : client.RFC,
                    City = string.IsNullOrEmpty(client.City) ? "-" : client.City,
                    CreationDate = $"{client.CreatedAt:dd MMM yyyy}",
                    PayType = client.PayType == null ? "-" : client.PayType.Humanize()
                },
                DeliveryDay = order.DeliveryDate.ToLocalTime() != DateTime.MinValue ? order.DeliveryDate.ToLocalTime().FormatDateShortMx() : DateTime.Today.ToString("dd/MM/yyyy")
            };

            return PartialView("_ChangeStatusModal", model);
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, AdministratorAssistant, Sales, Distributor, Storekeeper, Billing")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(OrderStatusViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateJsonError(string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage))));

            if (model.Status == OrderStatus.Delivered && string.IsNullOrEmpty(model.DeliveryDay))
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Seleccione una fecha de entrega."));

            try
            {
                var email = User.Identity.Name;
                var order = await _repository.GetAsync(new OrderExtendedSpecification(model.OrderId));

                var currentStatusOrder = order.OrderStatus;
                order.ChangeStatus(model.Status, model.Comments ?? "", model.InvoiceCode ?? "");

                if (model.Status == OrderStatus.Delivered)
                {
                    var client = await _repository.GetByIdAsync<Client>(order.ClientId);
                    var deliveryDay = string.IsNullOrEmpty(model.DeliveryDay) ? DateTime.MinValue : DateTime.ParseExact(model.DeliveryDay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    order.Delivered(deliveryDay.ToUniversalTime(), deliveryDay.AddDays(client.CreditDays + 1).ToUniversalTime());

                    var response = await _inventoryService.OrderReturn(order.OrderProducts.Select(c => new ProductPresentationQuantity { Id = c.ProductPresentationId, Quantity = c.Quantity }), User.Identity.Name, order.Id);
                    order.ToggleInventoryDiscount();
                }

                await _repository.UpdateAsync(order);

                var success = await _notificationService.NotifyChangeOrderStateAsync(order.Id, order.OrderStatus, email);
                if (success)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, $"Cambio de estado guardado. Notificación enviada. Inventario actualizado"));
                //return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, $"Cambio de estado guardado. Notificación enviada. {messageDiscountInventory}"));

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, $"Cambio de estado guardado. Notificación no enviada. Inventario actualizado"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en método ChangeStatus: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo cambiar el estado."));
            }
        }

    }
}
