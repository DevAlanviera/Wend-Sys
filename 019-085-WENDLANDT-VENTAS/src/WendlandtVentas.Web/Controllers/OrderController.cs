using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using Newtonsoft.Json;
using OpenXmlPowerTools;
using Syncfusion.EJ2.Base;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Core.Models.BitacoraViewModel;
using WendlandtVentas.Core.Models.ClientViewModels;
using WendlandtVentas.Core.Models.OrderViewModels;
using WendlandtVentas.Core.Models.ProductPresentationViewModels;
using WendlandtVentas.Core.Models.ProductViewModels;
using WendlandtVentas.Core.Models.PromotionViewModels;
using WendlandtVentas.Core.Services;
using WendlandtVentas.Core.Specifications.ClientSpecifications;
using WendlandtVentas.Core.Specifications.OrderExtendedSpecifications;
using WendlandtVentas.Core.Specifications.OrderSpecifications;
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;
using WendlandtVentas.Core.Specifications.ProductSpecifications;
using WendlandtVentas.Core.Specifications.PromotionSpecifications;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Infrastructure.Data;
using WendlandtVentas.Infrastructure.Repositories;
using WendlandtVentas.Infrastructure.Services;
using WendlandtVentas.Web.Extensions;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.ClientViewModels;
using WendlandtVentas.Web.Models.OrderViewModels;
using WendlandtVentas.Web.Models.ProductViewModels;
using WendlandtVentas.Web.Models.PromotionViewModels;
using WendlandtVentas.Web.Models.TableModels;
using Order = WendlandtVentas.Core.Entities.Order;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly CacheService _cacheService;
        private readonly IMemoryCache _memoryCache;
        private readonly AppDbContext _dbContext;
        private readonly IOrderService _orderService;
        private readonly IInventoryService _inventoryService;
        private readonly INotificationService _notificationService;
        private readonly IAsyncRepository _repository;
        private readonly ILogger<ProductController> _logger;
        private readonly SfGridOperations _sfGridOperations;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExcelReadService _excelReadService;
        private readonly ITreasuryApi _treasuryApi;
        private readonly IBitacoraService _bitacoraService;
        private readonly IBitacoraRepository _bitacoraRepository;
        private readonly IEmailSender _emailSender;

        public OrderController(IAsyncRepository repository,
            ILogger<ProductController> logger,
            SfGridOperations sfGridOperations,
            UserManager<ApplicationUser> userManager,
            IOrderService orderService,
            INotificationService notificationService,
            IInventoryService inventoryService,
            IExcelReadService excelReadService,
            ITreasuryApi treasuryApi,
            IBitacoraService bitacoraService,
            IBitacoraRepository bitacoraRepository,
            AppDbContext dbContext,
            IMemoryCache memoryCache,
            CacheService cacheService,
            IEmailSender emailSender)
        {
            _repository = repository;
            _logger = logger;
            _sfGridOperations = sfGridOperations;
            _userManager = userManager;
            _orderService = orderService;
            _notificationService = notificationService;
            _inventoryService = inventoryService;
            _excelReadService = excelReadService;
            _bitacoraService = bitacoraService;
            _treasuryApi = treasuryApi;
            _bitacoraRepository = bitacoraRepository;
            _dbContext = dbContext;
            _memoryCache = memoryCache;
            _cacheService = cacheService;
            _emailSender = emailSender;
        }

        public class CachedDataResult
        {
            public object DataResult { get; set; }
            public int? Count { get; set; }
        }



        public async Task<IActionResult> Index(FilterViewModel filter)
        {
            // Invalida el caché antes de obtener los datos frescos
            // Obtiene los datos desde el repositorio (sin caché)
            var orderTypes = Enum.GetValues(typeof(OrderType))
                                 .Cast<OrderType>()
                                 .Where(c => c != OrderType.Return);

            var orderStatus = Enum.GetValues(typeof(OrderStatus))
                                  .Cast<OrderStatus>()
                                  .Where(c => !(c == OrderStatus.ReadyDeliver || c == OrderStatus.Faceted));

            var products = await _repository.ListAllExistingAsync<Product>();
            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var states = await _repository.ListAllExistingAsync<State>();
            var clients = await _repository.ListAllExistingAsync<Client>();

            // Obtiene las ciudades distintas de los clientes
            var cities = clients.Select(d => d.City).Distinct();

            // Aplica los filtros al obtener los clientes por estado
            if (filter.StateId?.Any() == true)
            {
                clients = clients.Where(c => filter.StateId.Contains(c.StateId)).ToList();
            }

            // Configura las listas para los filtros en la vista
            filter.OrderStatusAll = new SelectList(orderStatus.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text", filter.OrderStatus);
            filter.OrderTypeAll = new SelectList(orderTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text");
            filter.Products = new SelectList(products.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            filter.Presentations = new SelectList(presentations.Select(x => new { Value = x.Id, Text = $"{x.Name} {x.Liters} lts" }), "Value", "Text");
            filter.StatesAll = new SelectList(states.OrderBy(s => s.Name).Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            filter.Clients = new SelectList(clients.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            filter.CityAll = new SelectList(cities.Select(x => new { Value = x, Text = x }), "Value", "Text");

            // Retorna la vista con los filtros aplicados
            return View(filter);
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataManagerRequest dm, FilterViewModel filter)
        {

            // 1. Detectar la clasificación (si es null o 0, forzar a 1)
            int classificationId = 1;
            if (Request.Query.ContainsKey("classificationId"))
            {
                int.TryParse(Request.Query["classificationId"], out classificationId);
            }

            // 2. IMPORTANTE: Ahora permitimos que sea 1, 2 o 3. 
            // Si viene algo fuera de ese rango por error, podrías forzarlo a 1.
            if (classificationId < 1 || classificationId > 3) classificationId = 1;

            filter.OrderClassification = classificationId;

            // 3. La llave de caché ahora incluirá el ID 3 cuando corresponda
            var cacheKey = $"OrderTableData_Class_{classificationId}_{System.Text.Json.JsonSerializer.Serialize(filter)}";

            // Intentar obtener datos del caché
            if (!_memoryCache.TryGetValue(cacheKey, out List<OrderTableModel> cachedData))
    {
        // Recuperar datos desde la fuente
        var users = _userManager.Users.ToDictionary(c => c.Id, c => c.Name); 
        
        // El servicio FilterValues debe estar preparado para recibir el classificationId = 3
        var filteredOrders = (await _orderService.FilterValues(filter)).ToList();

        // Mapear datos al modelo de la tabla
        cachedData = filteredOrders
        .Where(c => c.Type != OrderType.Return)
        .Select(c => new
        {
            Order = c,
            Model = new OrderTableModel
            {
                Id = c.Id,
                OrderClassification = (int)c.OrderClassification,
                OrderClassificationCode = c.OrderClassificationCode,
                // Si es Cotización, podrías querer mostrar un texto diferente en Type
                Type = classificationId == 3 ? "Cotización" : (c.PayType.HasValue ? $"{c.Type.Humanize()} ({c.PayType.Value.Humanize()})" : c.Type.Humanize()),
                InvoiceCode = c.InvoiceCode ?? string.Empty,
                RemissionCode = c.RemissionCode,
                IsPaid = c.Paid,
                PaymentDate = c.PaymentDate == DateTime.MinValue ? string.Empty : c.PaymentDate.ToLocalTime().FormatDateShortMx(),
                PaymentPromiseDate = c.PaymentPromiseDate == DateTime.MinValue ? string.Empty : c.PaymentPromiseDate.ToLocalTime().FormatDateShortMx(),
                CreateDate = c.CreatedAt.ToLocalTime().FormatDateShortMx(),
                Total = c.RealAmount.HasValue && c.RealAmount.Value != 0
                ? c.RealAmount.Value.FormatCurrency()
                : (c.Type != OrderType.Invoice ? c.SubTotal.FormatCurrency() : c.Total.FormatCurrency()),
                Client = c.Client.Name,
                StatusEnum = c.OrderStatus,
                Comment = c.Comment,
                Address = c.Address ?? string.Empty,
                // Las cotizaciones suelen ser editables siempre, o podrías aplicar otra lógica aquí
                CanEdit = classificationId == 3 || (c.OrderStatus != OrderStatus.PartialPayment && c.OrderStatus != OrderStatus.Paid || User.IsInRole(Role.Administrator.ToString())),
            }
        })
        .OrderBy(x => x.Order.OrderStatus != OrderStatus.InProcess)
        .ThenByDescending(x => x.Order.CreatedAt)
        .Select(x => x.Model)
        .ToList();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10));
                _memoryCache.Set(cacheKey, cachedData, cacheEntryOptions);
            }

            var dataResult = _sfGridOperations.FilterDataSource(cachedData.AsQueryable(), dm);

            return dm.RequiresCounts
                ? new JsonResult(new { result = dataResult.DataResult, count = dataResult.Count })
                : new JsonResult(dataResult.DataResult);
        }
   

        [HttpGet]
        public async Task<IActionResult> ValidateClientOrders(int clientId, int classificationId)
        {
            if (clientId <= 0)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "ClienteId no recibido."));

            var filters = new Dictionary<string, string>
            {
                {
                    "ClientId",
                    $"{clientId}"

                },
                {
                    "StatusList",
                    $"{OrderStatus.OnRoute},{OrderStatus.InProcess}"
                },
                // 2. Agregamos el filtro de clasificación
                { "OrderClassification", $"{classificationId}" }
            };
            var ordersPending = await _repository.ListExistingAsync(new OrdersFiltersSpecification(filters));

            if (ordersPending.Any())
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Warning, "Existe un pedido previo sin entregar."));

            return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Cliente verificado."));
        }

        [HttpGet]
        public async Task<IActionResult> GetClientAddresses(int clientId)
        {
            var addresses = await _repository.ListAsync(new AddressesByClientIdSpecification(clientId));

            if (!addresses.Any())
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se encontraron direcciones para este cliente."));

            var addressesList = new SelectList(addresses.Select(x => new { Value = x.Id, Text = $"{x.Name } - {x.AddressLocation}" }), "Value", "Text");

            return new JsonResult(new { status = "Ok", body = addressesList });
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing, BillingAssistant")]
        [HttpGet]
        public async Task<IActionResult> Add(int classificationId = 1)
        {
            ViewData["Action"] = nameof(Add);
            ViewData["Title"] = classificationId == 3 ? "Generar Cotización" : "Agregar pedido";
            var clients = (await _repository.ListAllExistingAsync<Client>()).OrderBy(c => c.Name);
            var payTypes = Enum.GetValues(typeof(PayType)).Cast<PayType>().AsEnumerable();
            var currencyTypes = Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>().AsEnumerable();
            var addresses = new List<Address>() { };
            // return remision number options
            var remissionsForReturn = await _orderService.GetInvoiceRemissionNumbersAsync();
            

            var model = new OrderViewModel
            {
                OrderClassification = classificationId,
                IsInvoice = OrderType.Invoice,
                Clients = new SelectList(clients.Select(x => new { Value = x.Id, Text = $"{x.Name}" + (x.Classification.HasValue ? $" - {x.Classification.Humanize()}" : string.Empty) }), "Value", "Text"),
                Addresses = new SelectList(addresses.Select(x => new { Value = x.Id, Text = x.AddressLocation }), "Value", "Text"),
                PayType = PayType.Cash,
                PayTypes = new SelectList(payTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                CurrencyType = CurrencyType.MXN,
                CurrencyTypes = new SelectList(currencyTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                ReturnRemisionNumberOptions = new SelectList(remissionsForReturn, "Value", "Text")
            };

            // Esto sirve para que en la vista sepamos si poner un título diferente
            ViewBag.ClassificationId = classificationId;

            return View("AddEdit", model);
        }


        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing, BillingAssistant")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(OrderViewModel model)
        {

            // Validación condicional:
            // Si es devolución, no validamos estas fechas
            if (model.OrderClassification == 3 || model.IsInvoice == OrderType.Return)
            {
                ModelState.Remove(nameof(model.PaymentPromiseDate));
                ModelState.Remove(nameof(model.PaymentDate));
                ModelState.Remove(nameof(model.DeliveryDay));
            }


            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage))));

            // 2. Validación de RFC: SOLO si es Factura Y NO es Cotización
            if (model.IsInvoice == OrderType.Invoice && model.OrderClassification != 3)
            {
                var rfcValidation = await ValidateClientRFCAsync(model.ClientId);
                if (rfcValidation != null)
                    return rfcValidation;
            }

            // 3. Validación de Pedidos Pendientes: Ignorar si es Cotización
            if (model.OrderClassification != 3)
            {
                var filters = new Dictionary<string, string>
                {
            { nameof(model.ClientId), $"{model.ClientId}" },
            { "StatusList", $"{OrderStatus.OnRoute},{OrderStatus.InProcess}" },
            { "OrderClassification", $"{model.OrderClassification}" }
                };
                var ordersPending = await _repository.ListExistingAsync(new OrdersFiltersSpecification(filters));

                if (ordersPending.Any() && model.Type != OrderType.Return)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Warning, "Existe un pedido previo sin entregar."));
            }

            var client = await GetByIdWithContactsAsync(model.ClientId);
            var primerEmail = client?.Contacts?.FirstOrDefault()?.Email;

            var response = await _orderService.AddOrderAsync(model, User.Identity.Name, primerEmail);

            if (response.IsSuccess)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, response.Message));

            _logger.LogError($"Error: {response.Message}");
            return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar el pedido"));
        }

        public async Task<Client> GetByIdWithContactsAsync(int clientId)
        {
            return await _dbContext.Clients
                .Include(c => c.Contacts)
                .FirstOrDefaultAsync(c => c.Id == clientId);
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing, BillingAssistant")]
        [HttpGet]
        public async Task<IActionResult> AddProduct([FromQuery] CurrencyType currencyType, [FromQuery] int classificationId)
        {
            try
            {
                ViewData["Action"] = nameof(AddProduct);
                ViewData["ModalTitle"] = "Agregar producto";

                // Incluimos la clasificación en la llave de caché
                string cacheKey = $"ProductsInStock_{currencyType}_{classificationId}";

                var productsInStock = await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        var allProducts = await _repository.ListExistingAsync(new ProductPresentationExtendedSpecification());

                        // Filtrar por moneda
                        var filtered = currencyType == CurrencyType.MXN
                            ? allProducts.Where(c => c.Price >= 0)
                            : allProducts.Where(c => c.PriceUsd >= 0);

                        // FILTRO USANDO EL ENUM DISTINCTION
                        if (classificationId == 2)
                        {
                            // Solo productos de la marca Wellen
                            filtered = filtered.Where(c => c.Product.Distinction == Distinction.Wellen);
                        }
                        else
                        {
                            // Pedidos normales: Todo lo que NO sea Wellen
                            filtered = filtered.Where(c => c.Product.Distinction != Distinction.Wellen);
                        }

                        return filtered.ToList();
                    },
                    absoluteExpiration: null
                );

                var model = new OrderAddProductViewModel
                {
                    ProductsPresentations = new SelectList(productsInStock.Select(x => new { Value = $"{x.Id}-{x.PresentationId}", Text = $"{x.NameExtended()}" }), "Value", "Text")
                };

                return PartialView("_AddProductModal", model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en método AddProduct: {e.Message}");
                return PartialView("_AddProductModal");
            }
        }

        [ResponseCache(Duration = 300)]
        [HttpGet]
        public async Task<IActionResult> SearchProductsAjax(
     [FromQuery] CurrencyType currencyType,
     [FromQuery] int classificationId, // Agregado
     [FromQuery] string term,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 20)
        {
            try
            {
                // Incluir classificationId en la llave para evitar resultados cruzados en el buscador
                string cacheKey = $"ProductsSearch_{currencyType}_{classificationId}_{term?.ToLower() ?? "all"}_{page}_{pageSize}";

                var result = await _cacheService.GetOrSetAsync(cacheKey, async () => {
                    var allProducts = await _repository.ListExistingAsync(new ProductPresentationExtendedSpecification());

                    var filtered = currencyType == CurrencyType.MXN
                        ? allProducts.Where(c => c.Price >= 0)
                        : allProducts.Where(c => c.PriceUsd >= 0);

                    // APLICAR FILTRO DE DISTINCIÓN IGUAL QUE EN ADDPRODUCT
                    if (classificationId == 2)
                        filtered = filtered.Where(c => c.Product.Distinction == Distinction.Wellen);
                    else
                        filtered = filtered.Where(c => c.Product.Distinction != Distinction.Wellen);

                    if (!string.IsNullOrWhiteSpace(term))
                    {
                        term = term.ToLower();
                        filtered = filtered.Where(p => p.NameExtended().ToLower().Contains(term));
                    }

                    var total = filtered.Count();
                    var products = filtered
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(x => new {
                            id = $"{x.Id}-{x.PresentationId}",
                            text = x.NameExtended(),
                            price = currencyType == CurrencyType.MXN ? x.Price : x.PriceUsd
                        })
                        .ToList();

                    return new
                    {
                        results = products,
                        pagination = new { more = (page * pageSize) < total }
                    };
                }, TimeSpan.FromMinutes(5));

                return Json(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error searching products");
                return Json(new { results = new List<object>(), pagination = new { more = false } });
            }
        }

        private async Task<IActionResult> ValidateClientRFCAsync(int clientId)
        {
            var client = await _repository.GetByIdAsync<Client>(clientId);
            if (client == null)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Cliente no encontrado."));

            if (string.IsNullOrEmpty(client.RFC))
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se puede facturar: el cliente no tiene RFC registrado."));

         

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> CheckClientRFC(int clientId)
        {
            var client = await _repository.GetByIdAsync<Client>(clientId);
            if (client == null)
            {
                return Json(new
                {
                    hasRFC = false,
                    isValid = false,
                    message = "Cliente no encontrado."
                });
            }

            bool hasRFC = !string.IsNullOrWhiteSpace(client.RFC);

            return Json(new
            {
                hasRFC,
                isValid = hasRFC, // Solo lo igualamos para que el script funcione
                message = hasRFC ? "" : "Este cliente no tiene RFC registrado."
            });
        }


        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing, BillingAssistant")]
        [HttpGet]
        public async Task<decimal> GetProductPrice([FromQuery] CurrencyType currencyType, [FromQuery] int id)
        {
            var product = await _repository.GetByIdAsync<ProductPresentation>(id);
            var price = currencyType == CurrencyType.MXN ? product.Price : product.PriceUsd;

            return price;
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing, BillingAssistant")]

        [HttpPost]
        public async Task<IActionResult> AddProductRow(OrderAddProductViewModel model)
        {
            var productPresentation = await _repository.GetAsync(new ProductPresentationExtendedSpecification(model.ProductPresentationId));

            var item = new ProductPresentationItem
            {
                ProductId = productPresentation.ProductId,
                PresentationId = productPresentation.PresentationId,
                ProductPresentationId = productPresentation.Id,
                PresentationName = $"{productPresentation.Presentation.Name.ToUpper()} {productPresentation.Presentation.Liters} lts.",
                ProductName = productPresentation.NameExtended(),
                PriceString = model.Price.FormatCurrency(),
                Price = model.Price,
                Quantity = model.Quantity,
                Subtotal = model.IsPresent ? "$0.00" : (model.Quantity * model.Price).FormatCurrency(),
                SubtotalDouble = model.Quantity * model.Price,
                ExistPresentation = model.ExistPresentation,
                IsSeason = productPresentation.Product.Distinction == Distinction.Season,
                IsPresent = model.IsPresent,
                CanDelete = true
            };

            return PartialView("_RowProduct", item);
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing, BillingAssistant")]
        [HttpPost]
        public async Task<IActionResult> AddPromotions(CheckPromotionModel checkPromotion)
        {
            try
            {
                ViewData["Action"] = nameof(AddPromotions);
                ViewData["ModalTitle"] = "Agregar promociones";

                var presentationId = checkPromotion.PresentationQuantities.First().PresentationId;
                var quantity = checkPromotion.PresentationQuantities.SelectMany(c => c.ProductQuantities).Where(p => !p.IsPresent).Sum(d => d.Quantity);
                var productsQuantities = checkPromotion.PresentationQuantities.SelectMany(c => c.ProductQuantities).Where(p => !p.IsPresent).ToList();
                var productIds = productsQuantities.Select(d => d.ProductId);
                var products = (await _repository.ListAsync(new ProductByIdsSpecification(productIds))).Where(c => c.Distinction != Distinction.Season);
                var productsPresentationAll = await _repository.ListAsync(new ProductPresentationByPresentationExtendedSpecification(presentationId));
                var productPresentation = productsPresentationAll.Where(b => products.Any(a => a.Id == b.ProductId)).ToList();
                var client = await _repository.GetByIdAsync<Client>(checkPromotion.ClientId);
                var presentation = await _repository.GetByIdAsync<Presentation>(presentationId);
                var clientPromotionsAll = (await _repository.ListExistingAsync(new PromotionByClientSpecification(client)));
                var presentationPromotions = clientPromotionsAll.SelectMany(d => d.PresentationPromotions).Where(c => c.PresentationId == presentationId && c.Promotion.Buy <= quantity);
                var promotions = new List<PromotionItemModel>();
                var productPromotionQuantities = new List<ProductItemModel>();

                productPresentation.ForEach(c =>
                {
                    var quantity = productsQuantities.Single(d => d.ProductId == c.ProductId).Quantity;
                    for (var i = 0; i < quantity; i++)
                        productPromotionQuantities.Add(new ProductItemModel
                        {
                            Id = c.Id,
                            Name = c.Product.Name,
                            Price = checkPromotion.CurrencyType == CurrencyType.MXN ? c.Price : c.PriceUsd,
                            Quantity = 1
                        });
                });

                productPromotionQuantities = productPromotionQuantities.OrderBy(c => c.Price).ToList();
                foreach (var pp in presentationPromotions.OrderByDescending(c => c.Promotion.Discount))
                {
                    var dividend = pp.Promotion.Buy + pp.Promotion.Present;
                    var result = dividend > 0 ? quantity / dividend : 0;
                    var buy = pp.Promotion.Buy;
                    var present = pp.Promotion.Present;
                    var auxProductPromQuantities = new ProductItemModel[productPromotionQuantities.Count];
                    productPromotionQuantities.CopyTo(auxProductPromQuantities);

                    for (var i = 0; i < result; i++)
                        if (auxProductPromQuantities.Count() >= present)
                        {
                            var productsPromo = auxProductPromQuantities.Take(present).ToList();
                            promotions.Add(new PromotionItemModel
                            {
                                Id = pp.PromotionId,
                                Name = pp.Promotion.Name,
                                Buy = buy,
                                Present = present,
                                Discount = pp.Promotion.Discount,
                                Products = productsPromo
                            });
                            auxProductPromQuantities = auxProductPromQuantities.Except(productsPromo).ToArray();
                        }
                }

                var model = new PresentationPromotionModel
                {
                    PresentationId = presentation.Id,
                    Presentation = presentation.NameExtended(),
                    Quantity = quantity,
                    Promotions = promotions.OrderByDescending(c => c.Discount)
                };

                return PartialView("_AddPromotionsModal", model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en método AddProduct: {e.Message}");
                return PartialView("_AddEditModal");
            }
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing, BillingAssistant")]
        [HttpPost]
        public IActionResult AddPromotionsRow(PresentationPromotionModel model)
        {
            return PartialView("_RowPromotion", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetBalances(int clientId)
        {
            ViewData["ModalTitle"] = "Balance de facturas";
            var client = await _repository.GetByIdAsync<Client>(clientId);
            var model = new BalanceViewModel
            {
                ClientName = client != null ? client.Name : "No se encontró cliente"
            };

            try
            {
                var clientOrders = (await _repository.ListAsync(new OrdersByClientIdSpecification(clientId))).ToList();
                var income = new OrdersIncomeDto
                {
                    Amount = 1,
                    ClientOrders = clientOrders.Select(c => c.Id).ToList()
                };

                var deliveredOrders = clientOrders.Where(c => c.OrderStatus == OrderStatus.Delivered);
                model.Balances.AddRange(
                    deliveredOrders.Select(c => new OrdersIncomeDto
                    {
                        RemissionCode = c.RemissionCode,
                        InvoiceCode = !string.IsNullOrEmpty(c.InvoiceCode) ? c.InvoiceCode : "-",
                        DeliveryDate = c.DeliveryDate.ToLocalTime() != DateTime.MinValue
                        ? c.DeliveryDate.ToLocalTime()
                        : c.CreatedAt.ToLocalTime(),
                        DueDate = c.DueDate.ToLocalTime() != DateTime.MinValue
                        ? c.DueDate.ToLocalTime()
                        : c.CreatedAt.ToLocalTime().AddDays(client.CreditDays + 1),
                        AmountString = $"{c.Total:C2} {c.CurrencyType.Humanize()}"
                    }).ToList());

                model.PendingAmount = deliveredOrders.Where(c => c.CurrencyType.Equals(CurrencyType.MXN)).Sum(c => (double)c.Total);
                model.PendingAmountUsd = deliveredOrders.Where(c => c.CurrencyType.Equals(CurrencyType.USD)).Sum(c => (double)c.Total);

                var apiResult = await _treasuryApi.GetBalancesAsync(income);
                if (apiResult.IsSuccess)
                {
                    var res = System.Text.Json.JsonSerializer.Deserialize<List<OrdersIncomeDto>>(apiResult.Response);

                    model.PendingAmount += res.Where(c => c.CurrencyType.Equals(CurrencyType.MXN)).Sum(c => c.Amount);
                    model.PendingAmountUsd += res.Where(c => c.CurrencyType.Equals(CurrencyType.USD)).Sum(c => c.Amount);
                    model.Balances.AddRange(res.Select(c =>
                    {
                        var order = clientOrders.FirstOrDefault(d => d.Id == c.OrderId);

                        return new OrdersIncomeDto
                        {
                            RemissionCode = order.RemissionCode,
                            InvoiceCode = !string.IsNullOrEmpty(order.InvoiceCode) ? order.InvoiceCode : "-",
                            DeliveryDate = order.DeliveryDate.ToLocalTime() != DateTime.MinValue
                            ? order.DeliveryDate.ToLocalTime()
                            : order.CreatedAt.ToLocalTime(),
                            DueDate = order.DueDate.ToLocalTime() != DateTime.MinValue
                            ? order.DueDate.ToLocalTime()
                            : order.CreatedAt.ToLocalTime().AddDays(client.CreditDays + 1),
                            AmountString = $"{c.Amount:C2} {c.CurrencyType.Humanize()}"
                        };
                    }).ToList());
                }
                else
                {
                    _logger.LogError($"No se pudieron consultar los balances en Tesorería: {apiResult.Response}");
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudieron consultar los balances en Tesorería"));
                }

                model.Balances = model.Balances.OrderByDescending(c => c.DueDate).ToList();

                return PartialView("_BalanceModal", model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en método GetBalances: {e.Message}");
                return PartialView("_BalanceModal", model);
            }
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Billing, BillingAssistant, Storekeeper")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Action"] = nameof(Edit);
            ViewData["Title"] = "Editar pedido";

            // Ejecutar las consultas de manera secuencial para evitar conflictos con DbContext
            var order = await _repository.GetAsync(new OrderExtendedSpecification(id));
            var remissionsForReturn = await _orderService.GetInvoiceRemissionNumbersAsync(); // No usa DbContext
            var clients = await _repository.ListAllExistingAsync<Client>();
            var addresses = await _repository.ListAsync(new AddressesByClientIdSpecification(order.ClientId));

            var isMxnCurrency = order.CurrencyType == CurrencyType.MXN;

            // Transformación de promociones
            var presentationPromotions = order.OrderPromotions
                .SelectMany(op => op.OrderPromotionProducts)
                .GroupBy(pp => pp.ProductPresentation)
                .Select(g => new PresentationPromotionModel
                {
                    PresentationId = g.Key.PresentationId,
                    Presentation = $"{g.Key.Presentation.Name} {g.Key.Presentation.Liters} lts.",
                    Quantity = g.Count(),
                    Promotions = order.OrderPromotions
                        .Where(p => g.Key.Id == p.OrderPromotionProducts.FirstOrDefault()?.ProductPresentationId)
                        .Select(p => new PromotionItemModel
                        {
                            Id = p.PromotionId,
                            Buy = p.Promotion.Buy,
                            Discount = p.Promotion.Discount,
                            Name = p.Promotion.Name,
                            Present = p.Promotion.Present,
                            PresentationId = g.Key.Id,
                            Products = p.OrderPromotionProducts.Select(pp => new ProductItemModel
                            {
                                Id = pp.ProductPresentationId,
                                Name = pp.ProductPresentation.Product.Name,
                                Price = isMxnCurrency ? pp.ProductPresentation.Price : pp.ProductPresentation.PriceUsd,
                                Quantity = pp.Quantity
                            }).ToList()
                        }).ToList()
                }).ToList();

            // Filtrar direcciones que no estén eliminadas
            var filteredAddresses = addresses?.Where(c => !c.IsDeleted).ToList() ?? new List<Address>();

            // Buscar la dirección asociada al pedido
            var address = order.Address != null
                ? filteredAddresses.FirstOrDefault(c => c.AddressLocation == order.Address && c.ClientId == order.ClientId && c.Name == order.AddressName)
                : null;

            // Construir el modelo de vista
            var model = new OrderViewModel
            {
                Id = id,
                IsInvoice = order.Type,
                RemissionCode = order.RemissionCode,
                ReturnReason = order.CollectionComment,
                ReturnRemisionNumber = order.RemissionCode,
                ReturnRemisionNumberOptions = new SelectList(remissionsForReturn, "Value", "Text"),
                InvoiceCode = order.InvoiceCode,
                Paid = order.Paid,
                PaymentPromiseDate = order.PaymentPromiseDate.ToLocalTime().FormatDateShortMx(),
                PaymentDate = order.PaymentDate.ToLocalTime().FormatDateShortMx(),
                DeliveryDay = order.DeliveryDate.ToLocalTime().FormatDateShortMx(),
                ClientId = order.ClientId,
                AddressId = address?.Id ?? 0,
                PayType = order.PayType ?? PayType.Cash,
                CurrencyType = order.CurrencyType,
                Delivery = order.Delivery,
                DeliverySpecification = order.DeliverySpecification,
                ProductsEdit = order.OrderProducts
                .Where(c => !c.IsDeleted)
                .Select(c =>
                {
                    var price = c.Price != 0 ? c.Price : (isMxnCurrency ? c.ProductPresentation.Price : c.ProductPresentation.PriceUsd);
                    var subtotal = price * c.Quantity;

                    return new ProductPresentationItem
                    {
                        ProductPresentationId = c.ProductPresentationId,
                        ProductName = c.ProductPresentation.NameExtended(),
                        Price = price,
                        PriceString = price.FormatCurrency(),                 // ✅ Agregado
                        Quantity = c.Quantity,
                        SubtotalDouble = subtotal,
                        Subtotal = subtotal.FormatCurrency(),                 // ✅ Agregado
                        PresentationId = c.ProductPresentation.PresentationId,
                        PresentationName = c.ProductPresentation.Presentation.Name,
                        ProductId = c.ProductPresentation.ProductId,
                        IsPresent = c.IsPresent,
                        CanDelete = order.OrderStatus == OrderStatus.New || order.OrderStatus == OrderStatus.InProcess,
                    };
                })
                .OrderBy(d => d.PresentationId)
                .ThenBy(d => d.IsPresent)
                .ToList(),
                PresentationPromotionsEdit = presentationPromotions,
                Clients = new SelectList(clients.OrderBy(c => c.Name), "Id", "Name"),
                Addresses = new SelectList(filteredAddresses, "Id", "AddressLocation"),
                PayTypes = new SelectList(Enum.GetValues(typeof(PayType)).Cast<PayType>().Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                CurrencyTypes = new SelectList(new[] { order.CurrencyType }.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                CanEditProducts = order.OrderStatus == OrderStatus.New || order.OrderStatus == OrderStatus.InProcess,
                Comment = order.Comment,
                ProntoPago = order.ProntoPago
            };

            return View("AddEdit", model);
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Billing, BillingAssistant, Storekeeper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderViewModel model)
        {


            
            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateJsonError(string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage))));



            // Validación de RFC antes de actualizar
            if (model.IsInvoice == OrderType.Invoice)
            {
                var rfcValidation = await ValidateClientRFCAsync(model.ClientId);
                if (rfcValidation != null)
                    return rfcValidation;
            }

            var response = await _orderService.UpdateOrderAsync(model, User.Identity.Name);

            if (response.IsSuccess)
            {
                
                var user = await _userManager.FindByNameAsync(User.Identity.Name); // Asegúrate de que _userManager esté inyectado en tu controlador
                var bitacora = new Bitacora(model.Id, user.Name, "Editar pedido");
               
   

                await _bitacoraService.AddAsync(bitacora);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, response.Message));
            }
               

            _logger.LogError($"Error: {response.Message}");
            return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo editar el pedido"));
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, AdministratorAssistant, Sales, Distributor, Storekeeper, Billing, BillingAssistant")]
        [HttpGet]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            ViewData["Action"] = nameof(ChangeStatus);
            ViewData["ModalTitle"] = "Cambiar estado";
            var order = await _repository.GetByIdAsync<Order>(id);
            var status = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Where(c => !(c == OrderStatus.ReadyDeliver || c == OrderStatus.Faceted)).OrderBy(x => x).AsEnumerable();
            if (User.IsInRole(Role.Sales.ToString()))
            {
                status = status.Where(c => c != OrderStatus.Paid && c != OrderStatus.Cancelled);
            }
            else if (User.IsInRole(Role.AdministratorAssistant.ToString()) ||
             User.IsInRole(Role.Administrator.ToString()))
            {
                // Administradores ven TODOS los estados, no filtramos nada
                // status = status.Where(c => c.Equals(OrderStatus.Paid));  // ❌ Eliminado
            }
            else if (User.IsInRole(Role.Storekeeper.ToString()) || User.IsInRole(Role.Distributor.ToString()))
            {
                status = status.Where(c => !c.Equals(OrderStatus.Paid) && !c.Equals(OrderStatus.PartialPayment));
            }
            else if (User.IsInRole(Role.BillingAssistant.ToString()))
            {
                status = status.Where(c => !c.Equals(OrderStatus.Delivered) && !c.Equals(OrderStatus.CancelRequest));
            }

            var client = await _repository.GetByIdAsync<Client>(order.ClientId);
            var model = new OrderStatusViewModel
            {
                OrderId = order.Id,
                Status = order.OrderStatus,
                StatusList = !order.OrderStatus.Equals(OrderStatus.PartialPayment)
                ? new SelectList(status.Where(c => c != OrderStatus.New).Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text")
                : new SelectList(status.Where(c => c == OrderStatus.Paid || c == OrderStatus.PartialPayment).Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                Comments = order.Comment,
                Type = order.Type,
                InvoiceCode = order.InvoiceCode,
                PrecioEspecial = order.PrecioEspecial,
                RealAmount = order.RealAmount,
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

        [Authorize(Roles = "Administrator, AdministratorCommercial, AdministratorAssistant, Sales, Distributor, Storekeeper, Billing, BillingAssistant")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(OrderStatusViewModel model)
        {

            if (!model.PrecioEspecial)
            {
                ModelState.Remove("RealAmount");
            }

            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateJsonError(string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage))));

            // Validaciones específicas por estado
            if (model.Type == OrderType.Invoice && string.IsNullOrEmpty(model.InvoiceCode) && model.Status == OrderStatus.OnRoute)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo cambiar el estado. El número de factura es requerido."));

            if (model.Status == OrderStatus.PartialPayment && model.InitialAmount == 0)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Monto inicial no puede ser cero."));

            if (model.Status == OrderStatus.Delivered && string.IsNullOrEmpty(model.DeliveryDay))
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Seleccione una fecha de entrega."));

            // Nueva validación para RealAmount
            if (model.Status == OrderStatus.OnRoute && model.PrecioEspecial && model.RealAmount <= 0)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Debe ingresar un monto real válido cuando activa precio especial"));

            if (model.PrecioEspecial && (model.RealAmount == null || model.RealAmount <= 0))
            {
                return BadRequest("El monto real es requerido para Pronto Pago.");
            }

            try
            {
                var email = User.Identity.Name;
                var order = await _repository.GetAsync(new OrderExtendedSpecification(model.OrderId));
                var user = await _userManager.FindByIdAsync(order.UserId);

                // Validación de monto inicial
                var orderTotal = order.Type == OrderType.Export ? (double)order.SubTotal : (double)order.Total;
                if (orderTotal < model.InitialAmount)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Monto inicial no puede ser mayor que el total del pedido."));


                var currentStatusOrder = order.OrderStatus;
                // Actualizar estado y campos adicionales
                order.ChangeStatus(model.Status, model.Comments ?? "", model.InvoiceCode ?? "");

                // 🚨 Enviar correo si se solicita cancelación
                if (model.Status == OrderStatus.CancelRequest)
                {

                    var clientName = order.Client?.Name ?? "Cliente desconocido";

                    var emailBody = $@"
                    Se ha solicitado la cancelación del pedido #{order.Id} 
                    del cliente {clientName} 
                    con fecha {DateTime.Now:dd/MM/yyyy HH:mm}.";
                    //noreplywendlandt@gmail.com
                    //Noreply1234!
                    await _emailSender.SendEmailAsync(
                        "cobranza@wendlandt.com.mx", // Cambia esto por el correo real que debe recibir la notificación
                        $"Solicitud de cancelación del pedido #{order.Id}",
                        emailBody
                    );
                }

                // --- MODIFICADO: Validación lógica unificada ---
                if (model.PrecioEspecial)
                {
                    if (model.RealAmount == null || model.RealAmount <= 0)
                    {
                        return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "El monto real es requerido cuando se activa precio especial."));
                    }
                }

                // Manejo de fecha de entrega
                if (model.Status == OrderStatus.Delivered)
                {
                    var client = await _repository.GetByIdAsync<Client>(order.ClientId);
                    var deliveryDay = DateTime.ParseExact(model.DeliveryDay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    order.Delivered(deliveryDay.ToUniversalTime(), deliveryDay.AddDays(client.CreditDays + 1).ToUniversalTime());
                }

                await _repository.UpdateAsync(order);

                // Manejo de inventario
                var messageDiscountInventory = string.Empty;
                if (!order.InventoryDiscount && model.Status != OrderStatus.Cancelled)
                {
                    var response = await _inventoryService.OrderDiscount(order.OrderProducts.Select(c => new ProductPresentationQuantity { Id = c.ProductPresentationId, Quantity = c.Quantity }), User.Identity.Name, order.Id);
                    order.ToggleInventoryDiscount();
                    await _repository.UpdateAsync(order);
                    messageDiscountInventory = response.Message;
                }
                else if (order.InventoryDiscount && model.Status == OrderStatus.Cancelled)
                {
                    var response = await _inventoryService.OrderReturn(order.OrderProducts.Select(c => new ProductPresentationQuantity { Id = c.ProductPresentationId, Quantity = c.Quantity }), User.Identity.Name, order.Id);
                    order.ToggleInventoryDiscount();
                    await _repository.UpdateAsync(order);
                }

                if (model.Status == OrderStatus.Paid || model.Status == OrderStatus.PartialPayment)
                {
                    var income = new OrdersIncomeDto
                    {
                        OrderId = order.Id,
                        OrderType = order.Type,
                        CurrencyType = order.CurrencyType,
                        RemissionCode = order.RemissionCode,
                        Amount = order.Type == OrderType.Export ? (double)order.SubTotal : (double)order.Total,
                        InitialAmount = model.InitialAmount,
                        User = User.Identity.Name
                    };

                    var apiResult = await _treasuryApi.AddIncomeAsync(income);
                    if (apiResult.IsSuccess)
                    {
                        // se espera que el valor regresado sea el nuevo estado de la orden
                        // si es Paid significa que el monto enviado es igual a lo faltante
                        var isParsed = Enum.TryParse(apiResult.Response, true, out OrderStatus paymentStatus);
                        if (isParsed)
                        {
                            order.ChangeStatus(paymentStatus, "", "");
                            await _repository.UpdateAsync(order);
                        }
                        else
                        {
                            // el monto enviado sobrepasa a lo restante de la orden
                            return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, $"No se pudo enviar el pago a Tesorería: El monto restante es de {apiResult.Response}"));
                        }
                    }
                    else
                    {
                        _logger.LogError($"No se pudo enviar el pago a Tesorería: {apiResult.Response}");
                        order.ChangeStatus(currentStatusOrder, "", "");
                        await _repository.UpdateAsync(order);

                        return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Cambio de estado no guardado. No se pudo enviar el pago a Tesorería"));
                    }
                }

                // Notificaciones y bitácora
                var success = await _notificationService.NotifyChangeOrderStateAsync(order.Id, order.OrderStatus, email);
                if (success)
                {
                    var usuario = await _userManager.FindByNameAsync(User.Identity.Name);
                    var bitacora = new Bitacora(order.Id, usuario.Name, "Cambio status");
                    await _bitacoraRepository.AddAsync(bitacora);
                    _cacheService.InvalidateOrderCache();
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, $"Cambio de estado guardado. Notificación enviada. Inventario actualizado"));
                }

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, $"Cambio de estado guardado. Notificación no enviada. Inventario actualizado"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en método ChangeStatus: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo cambiar el estado."));
            }
        }


        [HttpPost]
        public async Task<IActionResult> ActualizarTotal([FromBody] ActualizarTotalViewModel model)
        {
            // Si model llega null aquí, es porque el JSON de JS no coincide con la clase
            if (model == null) return BadRequest("No se recibieron datos.");

            // MODIFICADO: Forzamos la limpieza si el booleano es falso
            if (!model.PrecioEspecial)
            {
                ModelState.Remove(nameof(model.RealAmount));
                model.RealAmount = null; // Limpiamos manualmente por seguridad
            }


            if (!ModelState.IsValid)
                return BadRequest("Datos inválidos");

            // Actualizar en la base de datos principal
            var result = await _orderService.ActualizarTotalAsync(
                model.Id,
                model.RealAmount ?? 0,
                model.PrecioEspecial,
                User.Identity.Name
            );

            if (!result.IsSuccess)
                return StatusCode(500, result.Message);

            return Ok("Monto actualizado correctamente.");
        }

        [HttpGet]
        public async Task<IActionResult> Recalculate(int id, bool prontoPago)
        {
            var order = await _repository.GetAsync(new OrderExtendedSpecification(id));
            if (order == null) return NotFound();

            var recalculatedItems = order.OrderProducts.Select(op =>
            {
                var precioBase = prontoPago ? op.Price * 0.95m : op.Price;

                var presentationPromotions = op.ProductPresentation.Presentation.PresentationPromotions
                    .Where(pp => pp.Promotion.IsActive);

                // Convertimos el descuento (double) a decimal
                var totalDiscount = presentationPromotions
                    .Sum(pp => (decimal)pp.Promotion.Discount * precioBase); // aplica sobre precioBase

                var precioFinal = precioBase - totalDiscount;

                return new
                {
                    ProductId = op.ProductPresentation.Product.Id,
                    ProductName = op.ProductPresentation.Product.Name,
                    PrecioBase = Math.Round(precioBase, 2),
                    TotalDiscount = Math.Round(totalDiscount, 2),
                    PrecioFinal = Math.Round(precioFinal, 2)
                };
            }).ToList();

            var total = recalculatedItems.Sum(x => x.PrecioFinal);

            return Json(new { precios = recalculatedItems, total = Math.Round(total, 2) });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            ViewData["ModalTitle"] = "Detalles del pedido";
            var order = await _repository.GetAsync(new OrderExtendedSpecification(id));
            var user = await _userManager.FindByIdAsync(order.UserId);
            var weight = await CalcOrderWeight(id);

            var bitacoraEntries = await _bitacoraRepository.GetBitacorasByOrderIdAsync(id);

            var model = new OrderDetailsViewModel
            {
                Id = order.Id,
                OrderClassification = (int)order.OrderClassification,
                OrderClassificationCode = order.OrderClassificationCode,
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
                RealAmount = order.RealAmount,
                PrecioEspecial = order.PrecioEspecial,
                Address = new AddressItemModel
                {
                    Name = order.AddressName,
                    DeliveryDay = order.DeliveryDate.ToLocalTime() == DateTime.MinValue ? order.CreatedAt.ToLocalTime().FormatDateShortMx() : order.DeliveryDate.ToLocalTime().FormatDateShortMx(),
                    AddressLocation = order.Address,
                    DeliverySpecification = order.DeliverySpecification
                },
                ProntoPago = order.ProntoPago,
                Client = new ClientItemModel
                {
                    Id = order.ClientId,
                    Channel = order.Client.Channel == null ? "-" : order.Client.Channel.Humanize(),
                    PayType = order.PayType.HasValue ? order.PayType.Humanize() : "-",
                    Name = order.Client.Name,
                    Classification = order.Client.Classification == null ? "-" : order.Client.Classification.Humanize(),
                    State = order.Client.State == null ? "-" : order.Client.State.Name,
                    Comments = (order.Client.Comment != null && order.Client.Comment.Any())
                    ? new List<CommentsItemModel>
                      {
                          new CommentsItemModel
                          {
                              Id = order.Client.Comment.First().Id,
                              Comments = order.Client.Comment.First().Comments
                          }
                      }
                    : new List<CommentsItemModel>()
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
                }),
                BitacoraEntries = bitacoraEntries.Select(b => new BitacoraItemModel
                {
                    Id = b.Id,
                    Usuario = b.Usuario,
                    FechaModificacion = b.Fecha_modificacion.ToLocalTime(), // Formatear la fecha
                     Accion = b.Accion,
                }).ToList()
            };

           
            return PartialView("_DetailsModal", model);
        }

        public async Task<IActionResult> PrintOrder(int id)
        {
            var currentPath = Environment.CurrentDirectory;
            var path = Path.Combine(currentPath, "wwwroot/resources/orders");
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

            // Cálculo del total de descuento por pronto pago
            decimal totalProntoPagoDescuento = 0;

            foreach (var product in order.OrderProducts.Where(p => !p.IsDeleted))
            {
                var precioBase = order.CurrencyType == CurrencyType.MXN
                    ? product.ProductPresentation.Price
                    : product.ProductPresentation.PriceUsd;

                var precioUsado = product.Price != 0 ? product.Price : precioBase;

                if (precioUsado < precioBase)
                {
                    var descuentoUnitario = precioBase - precioUsado;
                    totalProntoPagoDescuento += descuentoUnitario * product.Quantity;
                }
            }

            var model = new OrderDetailsViewModel
            {
                Id = order.Id,
                TypeEnum = order.Type,
                RemissionCode = order.RemissionCode,
                InvoiceCode = order.InvoiceCode,
                IsPaid = order.Paid,
                CreateDate = order.CreatedAt.ToLocalTime(),
                PaymentPromiseDate = order.PaymentPromiseDate.ToLocalTime() == DateTime.MinValue ? string.Empty : order.PaymentPromiseDate.ToLocalTime().FormatDateShortMx(),
                PaymentDate = order.PaymentDate.ToLocalTime() == DateTime.MinValue ? string.Empty : order.PaymentDate.ToLocalTime().FormatDateShortMx(),
                IEPS = order.IEPS.FormatCurrency(),
                BaseAmount = order.BaseAmount.FormatCurrency(),
                //Distribution = order.Distribution.FormatCurrency(),
                IVA = order.IVA.FormatCurrency(),
                Status = order.OrderStatus.Humanize(),
                SubTotal = order.SubTotal.FormatCurrency(),
                Total = (order.RealAmount.HasValue && order.RealAmount.Value != 0.0m || order.PrecioEspecial)
                ? order.RealAmount.GetValueOrDefault().FormatCurrency()
                : order.Total.FormatCurrency(),
                Discount = order.Discount.FormatCurrency(),
                User = user != null ? user.Name : "-",
                Comment = order.Comment,
                CollectionComment = order.CollectionComment,
                Weight = weight,
                ProntoPago = order.ProntoPago,
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
                    Name = order.Client.Name,
                    Classification = order.Client.Classification == null ? "-" : order.Client.Classification.Humanize(),
                    State = order.Client.State == null ? "-" : order.Client.State.Name,
                    City = string.IsNullOrEmpty(order.Client.City) ? "-" : order.Client.City,
                    RFC = string.IsNullOrEmpty(order.Client.RFC) ? "-" : order.Client.RFC,
                    Comments = (order.Client.Comment != null && order.Client.Comment.Any())
                    ? new List<CommentsItemModel>
                      {
                          new CommentsItemModel
                          {
                              Id = order.Client.Comment.First().Id,
                              Comments = order.Client.Comment.First().Comments
                          }
                      }
                    : new List<CommentsItemModel>()
                },
                Products = order.OrderProducts.Where(c => !c.IsDeleted).Select(c => new ProductItemModel
                {
                    Id = c.Id,
                    Name = c.ProductPresentation.Product.Name,
                    PresentationLiters = $"{c.ProductPresentation.Product.Name } - {c.ProductPresentation.Presentation.Name}({c.ProductPresentation.Presentation.Liters})",
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
                }),
                TotalDescuentoProntoPago = totalProntoPagoDescuento,
                
            };
            try
            {
                var archivo_generado = await _excelReadService.FillData(currentPath, model);

                FileStream fileStream = new FileStream(archivo_generado, FileMode.Open, FileAccess.ReadWrite);
                return File(fileStream, "application/octet-stream", $"Pedido{(model.TypeEnum == OrderType.Invoice ? model.InvoiceCode : model.RemissionCode)}.pdf");
            }
            catch (Exception err)
            {
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, err.Message));
            }
        }

        /*[HttpPost]
        public async Task<IActionResult> EnviarEstadoCuenta(int orderId)
        {
            Console.WriteLine("ENVIANDO EMAIL DESDE SERVICIO...");

            var enviado = await _orderService.EnviarEstadoCuentaAsync(orderId);

            if (enviado)
                return Ok("Estado de cuenta enviado con éxito.");
            else
                return StatusCode(500, "Error al enviar el correo.");
        }*/


        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Billing, BillingAssistant")]
        [HttpGet("{controller}/Delete/{id}")]
        public IActionResult DeleteView(int id)
        {
            ViewData["Action"] = nameof(Delete);
            ViewData["ModalTitle"] = "Eliminar pedido";
            ViewData["ModalDescription"] = $"el pedido";

            return PartialView("_DeleteModal", $"{id}");
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var order = await _repository.GetAsync(new OrderExtendedSpecification(id));

                if (order == null)
                    return Json(AjaxFunctions.GenerateJsonError("El pedido no existe"));

                if (order.OrderStatus != OrderStatus.New)
                    return Json(AjaxFunctions.GenerateJsonError("Solo se puede eliminar el pedido si se encuentra en estado de nuevo"));

                order.OrderProducts.ToList().ForEach(c => c.Delete());
                order.OrderPromotions.ToList().ForEach(c => c.Delete());
                order.Delete();
                await _repository.UpdateAsync(order);
                _cacheService.InvalidateOrderCache();
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Pedido eliminado"));
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo eliminar el producto"));
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


        [Authorize(Roles = "Administrator, AdministratorAssistant, Billing, BillingAssistant")]
        [HttpGet]
        public async Task<IActionResult> AddCollectionComment(int id)
        {
            ViewData["Action"] = nameof(AddCollectionComment);
            ViewData["ModalTitle"] = "Agregar comentario de cobranza";

            var order = await _repository.GetByIdAsync<Order>(id);

            return PartialView("_AddCollectionCommentModal", new OrderStatusViewModel { OrderId = id, Comments = order.CollectionComment });
        }


        [Authorize(Roles = "Administrator, AdministratorCommercial, Billing, BillingAssistant")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCollectionComment(OrderStatusViewModel model)
        {
            try
            {
                var order = await _repository.GetByIdAsync<Order>(model.OrderId);

                order.AddCollectionComment(model.Comments);
                await _repository.UpdateAsync(order);
                var usuario = await _userManager.FindByNameAsync(User.Identity.Name);
                var bitacora = new Bitacora(order.Id, usuario.Name, "Agregar Comentario");
                await _bitacoraRepository.AddAsync(bitacora);
                _cacheService.InvalidateOrderCache();

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Comentario guardado exitosamente"));
            }
            catch (Exception e)
            {
                _logger.LogError($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar el comentario"));
            }
        }

        #region AJAX

        [HttpPost]
        public async Task<IActionResult> CheckPromotions(CheckPromotionModel checkPromotion)
        {
            var promotionsResponse = new List<dynamic>();
            try
            {
                var productIds = checkPromotion.PresentationQuantities.SelectMany(c => c.ProductQuantities.Select(d => d.ProductId));
                var products = await _repository.ListAsync(new ProductByIdsSpecification(productIds));

                //Valida si puede aplicar alguna promocion a la presentación en base a sus productos
                checkPromotion.PresentationQuantities.ToList().ForEach(presentation =>
                {
                    presentation.ProductQuantities.ToList().ForEach(presentationProduct =>
                    {
                        if (!presentationProduct.IsPresent)
                        {
                            var product = products.SingleOrDefault(c => c.Id == presentationProduct.ProductId);
                            if (product.Distinction != Distinction.Season)
                                presentation.HasPossibilityPromotion = true;
                        }
                    });
                });

                var client = await _repository.GetByIdAsync<Client>(checkPromotion.ClientId);
                var promotions = (await _repository.ListExistingAsync(new PromotionByClientSpecification(client))).SelectMany(d => d.PresentationPromotions);

                //Regresa las presentaciones que tienen una o más promociones
                checkPromotion.PresentationQuantities.Where(c => c.HasPossibilityPromotion).ToList().ForEach(presentationQuantity =>
                {
                    var presentationHasPromotions = promotions.Any(e => e.PresentationId == presentationQuantity.PresentationId && e.Promotion.Buy <= presentationQuantity.Quantity);
                    dynamic objectModel = new ExpandoObject();
                    if (presentationHasPromotions)
                    {
                        objectModel.Value = presentationQuantity.PresentationId;
                        objectModel.Text = "PresentationId";
                        promotionsResponse.Add(objectModel);
                    }
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error en OrderController --> CheckPromotions");
            }
            return Json(promotionsResponse);
        }
        #endregion
    }
}