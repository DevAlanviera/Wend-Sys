﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using Syncfusion.EJ2.Base;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Core.Models.ClientViewModels;
using WendlandtVentas.Core.Models.OrderViewModels;
using WendlandtVentas.Core.Models.ProductPresentationViewModels;
using WendlandtVentas.Core.Models.ProductViewModels;
using WendlandtVentas.Core.Models.PromotionViewModels;
using WendlandtVentas.Core.Specifications.ClientSpecifications;
using WendlandtVentas.Core.Specifications.OrderExtendedSpecifications;
using WendlandtVentas.Core.Specifications.OrderSpecifications;
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;
using WendlandtVentas.Core.Specifications.ProductSpecifications;
using WendlandtVentas.Core.Specifications.PromotionSpecifications;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Web.Extensions;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.ClientViewModels;
using WendlandtVentas.Web.Models.OrderViewModels;
using WendlandtVentas.Web.Models.PromotionViewModels;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
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

        public OrderController(IAsyncRepository repository,
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
            var orderTypes = Enum.GetValues(typeof(OrderType)).Cast<OrderType>().Where(c => c != OrderType.Return);
            var orderStatus = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Where(c => !(c == OrderStatus.ReadyDeliver || c == OrderStatus.Faceted)).AsEnumerable();
            var products = await _repository.ListAllExistingAsync<Product>();
            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var states = await _repository.ListAllExistingAsync<State>();
            var clients = await _repository.ListAllExistingAsync<Client>();
            var cities = clients.Select(d => d.City).Distinct();

            if (filter.StateId.Any())
                clients = clients.Where(c => filter.StateId.Any(s => s == c.StateId)).ToList();

            filter.FilterDate = filter.FilterDate;
            filter.OrderStatusAll = new SelectList(orderStatus.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text", filter.OrderStatus);
            filter.OrderTypeAll = new SelectList(orderTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text");
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
            var filteredOrders = (await _orderService.FilterValues(filter)).ToList();
            var dataSource = filteredOrders
                .Where(c => c.Type != OrderType.Return)
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
                    CanEdit = c.OrderStatus != OrderStatus.PartialPayment && c.OrderStatus != OrderStatus.Paid || User.IsInRole(Role.Administrator.ToString())
                });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }

        [HttpGet]
        public async Task<IActionResult> ValidateClientOrders(int clientId)
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
                }
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

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing")]
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewData["Action"] = nameof(Add);
            ViewData["Title"] = "Agregar pedido";
            var clients = (await _repository.ListAllExistingAsync<Client>()).OrderBy(c => c.Name);
            var payTypes = Enum.GetValues(typeof(PayType)).Cast<PayType>().AsEnumerable();
            var currencyTypes = Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>().AsEnumerable();
            var addresses = new List<Address>() { };
            // return remision number options
            var remissionsForReturn = await _orderService.GetInvoiceRemissionNumbersAsync();

            var model = new OrderViewModel
            {
                IsInvoice = OrderType.Invoice,
                Clients = new SelectList(clients.Select(x => new { Value = x.Id, Text = $"{x.Name}" + (x.Classification.HasValue ? $" - {x.Classification.Humanize()}" : string.Empty) }), "Value", "Text"),
                Addresses = new SelectList(addresses.Select(x => new { Value = x.Id, Text = x.AddressLocation }), "Value", "Text"),
                PayType = PayType.Cash,
                PayTypes = new SelectList(payTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                CurrencyType = CurrencyType.MXN,
                CurrencyTypes = new SelectList(currencyTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                ReturnRemisionNumberOptions = new SelectList(remissionsForReturn, "Value", "Text")
            };

            return View("AddEdit", model);
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(OrderViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage))));
            var filters = new Dictionary<string, string>
            {
                {
                    nameof(model.ClientId),
                    $"{model.ClientId}"
                },
                {
                    "StatusList",
                    $"{OrderStatus.OnRoute},{OrderStatus.InProcess}"
                }
            };
            var ordersPending = await _repository.ListExistingAsync(new OrdersFiltersSpecification(filters));

            if (ordersPending.Any() && model.Type != OrderType.Return)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Warning, "Existe un pedido previo sin entregar."));

            var response = await _orderService.AddOrderAsync(model, User.Identity.Name);

            if (response.IsSuccess)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, response.Message));

            _logger.LogError($"Error: {response.Message}");
            return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar el pedido"));
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing")]
        [HttpGet]
        public async Task<IActionResult> AddProduct([FromQuery] CurrencyType currencyType)
        {
            try
            {
                ViewData["Action"] = nameof(AddProduct);
                ViewData["ModalTitle"] = "Agregar producto";

                var productsInStock = await _repository.ListExistingAsync(new ProductPresentationExtendedSpecification());
                if (currencyType == CurrencyType.MXN)
                    productsInStock = productsInStock.Where(c => !c.Price.Equals(decimal.Zero)).ToList();
                else
                    productsInStock = productsInStock.Where(c => !c.PriceUsd.Equals(decimal.Zero)).ToList();
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

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing")]
        [HttpGet]
        public async Task<decimal> GetProductPrice([FromQuery] CurrencyType currencyType, [FromQuery] int id)
        {
            var product = await _repository.GetByIdAsync<ProductPresentation>(id);
            var price = currencyType == CurrencyType.MXN ? product.Price : product.PriceUsd;

            return price;
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing")]
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

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing")]
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

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Storekeeper, Distributor, Billing")]
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
                    var res = JsonSerializer.Deserialize<List<OrdersIncomeDto>>(apiResult.Response);

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

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Billing, Storekeeper")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Action"] = nameof(Edit);
            ViewData["Title"] = "Editar pedido";
            var order = await _repository.GetAsync(new OrderExtendedSpecification(id));
            var remissionsForReturn = await _orderService.GetInvoiceRemissionNumbersAsync();
            var presentationPromotions = new List<PresentationPromotionModel>();
            var productPresentationList = order.OrderPromotions.SelectMany(c => c.OrderPromotionProducts).Select(d => d.ProductPresentation).Distinct();

            foreach (var presentation in productPresentationList)
            {
                presentationPromotions.Add(new PresentationPromotionModel
                {
                    PresentationId = presentation.PresentationId,
                    Presentation = $"{presentation.Presentation.Name} {presentation.Presentation.Liters} lts.",
                    Quantity = order.OrderPromotions.SelectMany(d => d.OrderPromotionProducts).Where(d => presentation.Id == d.ProductPresentationId).Count(),
                    Promotions = order.OrderPromotions.Where(c => presentation.Id == c.OrderPromotionProducts.FirstOrDefault().ProductPresentationId).Select(f =>
                    {
                        return new PromotionItemModel
                        {
                            Id = f.PromotionId,
                            Buy = f.Promotion.Buy,
                            Discount = f.Promotion.Discount,
                            Name = f.Promotion.Name,
                            Present = f.Promotion.Present,
                            PresentationId = presentation.Id,
                            Products = f.OrderPromotionProducts.Select(d =>
                            {
                                return new ProductItemModel
                                {
                                    Id = d.ProductPresentationId,
                                    Name = d.ProductPresentation.Product.Name,
                                    Price = order.CurrencyType == CurrencyType.MXN ? d.ProductPresentation.Price : d.ProductPresentation.PriceUsd,
                                    Quantity = d.Quantity
                                };
                            }).ToList()
                        };
                    })
                }); ;
            }

            var clients = (await _repository.ListAllExistingAsync<Client>()).OrderBy(c => c.Name);

            #region PRODUCTSCOMMENTED
            //var presentationPromotions = order.OrderPromotions
            //     .SelectMany(d => d.OrderPromotionProducts)
            //     .Select(d => d.ProductPresentation)
            //     .GroupBy(c => c.Presentation,
            //     c => c, (Presentation, Products) =>
            //     {
            //         return new PresentationPromotionModel
            //         {
            //             PresentationId = Presentation.Id,
            //             Presentation = Presentation.NameExtended(),
            //             Quantity = Products.Count(),
            //             //Promotions = order.OrderPromotions.Select(f =>
            //             Promotions = order.OrderPromotions.Where(c => Presentation.Id == c.Promotion.PresentationPromotions.FirstOrDefault().PresentationId).Select(f =>
            //             {
            //                 return new PromotionItemModel
            //                 {
            //                     Id = f.PromotionId,
            //                     Buy = f.Promotion.Buy,
            //                     Discount = f.Promotion.Discount,
            //                     Name = f.Promotion.Name,
            //                     Present = f.Promotion.Present,
            //                     Products = f.OrderPromotionProducts.Select(d => 
            //                     {
            //                        return new ProductItemModel 
            //                        {
            //                        Id = d.ProductPresentation.ProductId,
            //                        Name = d.ProductPresentation.Product.Name,
            //                        Price = d.ProductPresentation.Price,
            //                        Quantity = d.Quantity
            //                        };
            //                     }).ToList()
            //                 };
            //             })

            //Promotions =  Products.Select(d => d.Presentation).SelectMany(e => e.PresentationPromotions).Select(f =>
            //{
            //    return new PromotionItemModel
            //    {
            //        Id = f.PromotionId,
            //        Buy = f.Promotion.Buy,
            //        Discount = f.Promotion.Discount,
            //        Name = f.Promotion.Name,
            //        Present = f.Promotion.Present,
            //        Products = Products.Where(p => p.Presentation.PresentationPromotions.Any(pp => pp.Id == f.Id)).Select(g =>
            //        {
            //            return new ProductItemModel
            //            {
            //                Id = g.Id,
            //                Name = g.NameExtended(),
            //                Price = g.Price,
            //                Quantity = 1
            //            };
            //        }).ToList(),
            //    };
            //})
            //    };
            //}).ToList();
            #endregion

            var addresses = await _repository.ListAsync(new AddressesByClientIdSpecification(order.ClientId));
            if (addresses == null)
                addresses = new List<Address>() { };
            else
                addresses = addresses.Where(c => !c.IsDeleted).ToList();

            var payTypes = Enum.GetValues(typeof(PayType)).Cast<PayType>().AsEnumerable();
            var currencyTypes = Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>().Where(c => c.Equals(order.CurrencyType)).AsEnumerable();
            var address = order.Address != null ? addresses.FirstOrDefault(c => c.AddressLocation == order.Address && c.ClientId == order.ClientId && c.Name == order.AddressName) : null;

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
                PaymentPromiseDate = order.PaymentPromiseDate.ToLocalTime() != DateTime.MinValue ? order.PaymentPromiseDate.ToLocalTime().FormatDateShortMx() : string.Empty,
                PaymentDate = order.PaymentDate.ToLocalTime() != DateTime.MinValue ? order.PaymentDate.ToLocalTime().FormatDateShortMx() : string.Empty,
                DeliveryDay = order.DeliveryDate.ToLocalTime() != DateTime.MinValue ? order.DeliveryDate.ToLocalTime().FormatDateShortMx() : string.Empty,
                ClientId = order.ClientId,
                AddressId = address != null ? address.Id : 0,
                PayType = order.PayType ?? PayType.Cash,
                CurrencyType = order.CurrencyType,
                Delivery = order.Delivery,
                DeliverySpecification = order.DeliverySpecification,
                ProductsEdit = order.OrderProducts.Where(c => !c.IsDeleted).Select(c => new ProductPresentationItem
                {
                    ProductPresentationId = c.ProductPresentationId,
                    ProductName = c.ProductPresentation.NameExtended(),
                    PriceString = c.Price != 0 ? c.Price.FormatCurrency() : order.CurrencyType == CurrencyType.MXN
                    ? c.ProductPresentation.Price.FormatCurrency()
                    : c.ProductPresentation.PriceUsd.FormatCurrency(),
                    Price = c.Price != 0 ? c.Price : order.CurrencyType == CurrencyType.MXN
                    ? c.ProductPresentation.Price
                    : c.ProductPresentation.PriceUsd,
                    Quantity = c.Quantity,
                    Subtotal = c.Price != 0 ? (c.Price * c.Quantity).FormatCurrency() : order.CurrencyType == CurrencyType.MXN
                    ? (c.ProductPresentation.Price * c.Quantity).FormatCurrency()
                    : (c.ProductPresentation.PriceUsd * c.Quantity).FormatCurrency(),
                    SubtotalDouble = c.Price != 0 ? c.Price * c.Quantity : order.CurrencyType == CurrencyType.MXN
                    ? c.ProductPresentation.Price * c.Quantity
                    : c.ProductPresentation.PriceUsd * c.Quantity,
                    PresentationId = c.ProductPresentation.PresentationId,
                    PresentationName = c.ProductPresentation.Presentation.Name,
                    ProductId = c.ProductPresentation.ProductId,
                    IsPresent = c.IsPresent,
                    CanDelete = order.OrderStatus == OrderStatus.New || order.OrderStatus == OrderStatus.InProcess
                }),
                PresentationPromotionsEdit = presentationPromotions,
                Clients = new SelectList(clients.Select(x => new { Value = x.Id, Text = $"{x.Name}" }), "Value", "Text"),
                Addresses = new SelectList(addresses.Select(x => new { Value = x.Id, Text = string.IsNullOrEmpty(x.AddressLocation) ? string.Empty : $"{x.Name} - {x.AddressLocation}" }), "Value", "Text"),
                PayTypes = new SelectList(payTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                CurrencyTypes = new SelectList(currencyTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                CanEditProducts = order.OrderStatus == OrderStatus.New || order.OrderStatus == OrderStatus.InProcess,
                Comment = order.Comment
            };

            model.ProductsEdit = model.ProductsEdit.OrderBy(d => d.PresentationId).ThenBy(d => d.IsPresent).ToList();

            for (var counter = 1; counter < model.ProductsEdit.Count(); counter++)
            {
                if (model.ProductsEdit.ElementAt(counter).PresentationId == model.ProductsEdit.ElementAt(counter - 1).PresentationId)
                {
                    model.ProductsEdit.ElementAt(counter).ExistPresentation = true;
                }
            }

            return View("AddEdit", model);
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Billing, Storekeeper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateJsonError(string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage))));

            var response = await _orderService.UpdateOrderAsync(model, User.Identity.Name);

            if (response.IsSuccess)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, response.Message));

            _logger.LogError($"Error: {response.Message}");
            return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo editar el pedido"));
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, AdministratorAssistant, Sales, Distributor, Storekeeper, Billing")]
        [HttpGet]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            ViewData["Action"] = nameof(ChangeStatus);
            ViewData["ModalTitle"] = "Cambiar estado";
            var order = await _repository.GetByIdAsync<Order>(id);
            var status = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Where(c => !(c == OrderStatus.ReadyDeliver || c == OrderStatus.Faceted)).OrderBy(x => x).AsEnumerable();
            if (User.IsInRole(Role.Sales.ToString()))
            {
                status = status.Where(c => !c.Equals(OrderStatus.Paid));
            }
            else if (User.IsInRole(Role.AdministratorAssistant.ToString()))
            {
                status = status.Where(c => c.Equals(OrderStatus.Paid));
            }
            else if (User.IsInRole(Role.Storekeeper.ToString()) || User.IsInRole(Role.Distributor.ToString()))
            {
                status = status.Where(c => !c.Equals(OrderStatus.Paid) && !c.Equals(OrderStatus.PartialPayment));
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

            if (model.Type == OrderType.Invoice && string.IsNullOrEmpty(model.InvoiceCode) && model.Status == OrderStatus.OnRoute)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo cambiar el estado. El número de factura es requerido."));

            if (model.Status == OrderStatus.PartialPayment && model.InitialAmount == 0)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Monto inicial no puede ser cero."));

            if (model.Status == OrderStatus.Delivered && string.IsNullOrEmpty(model.DeliveryDay))
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Seleccione una fecha de entrega."));

            try
            {
                var email = User.Identity.Name;
                var order = await _repository.GetAsync(new OrderExtendedSpecification(model.OrderId));

                var orderTotal = order.Type == OrderType.Export ? (double)order.SubTotal : (double)order.Total;
                if (orderTotal < model.InitialAmount)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Monto inicial no puede ser mayor que el total del pedido."));

                var currentStatusOrder = order.OrderStatus;
                order.ChangeStatus(model.Status, model.Comments ?? "", model.InvoiceCode ?? "");

                if (model.Status == OrderStatus.Delivered)
                {
                    var client = await _repository.GetByIdAsync<Client>(order.ClientId);
                    var deliveryDay = string.IsNullOrEmpty(model.DeliveryDay) ? DateTime.MinValue : DateTime.ParseExact(model.DeliveryDay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    order.Delivered(deliveryDay.ToUniversalTime(), deliveryDay.AddDays(client.CreditDays + 1).ToUniversalTime());
                }

                await _repository.UpdateAsync(order);

                var messageDiscountInventory = string.Empty;
                if (!order.InventoryDiscount && model.Status != OrderStatus.Cancelled)
                {
                    var response = await _inventoryService.OrderDiscount(order.OrderProducts.Select(c => new ProductPresentationQuantity { Id = c.ProductPresentationId, Quantity = c.Quantity }), User.Identity.Name, order.Id);
                    order.ToggleInventoryDiscount();
                    await _repository.UpdateAsync(order);

                    messageDiscountInventory = response.Message;
                }

                if (order.InventoryDiscount && model.Status == OrderStatus.Cancelled)
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

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            ViewData["ModalTitle"] = "Detalles del pedido";
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
                })
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

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Billing")]
        [HttpGet("{controller}/Delete/{id}")]
        public IActionResult DeleteView(int id)
        {
            ViewData["Action"] = nameof(Delete);
            ViewData["ModalTitle"] = "Eliminar pedido";
            ViewData["ModalDescription"] = $"el pedido";

            return PartialView("_DeleteModal", $"{id}");
        }

        [Authorize(Roles = "Administrator, AdministratorCommercial, Sales, Billing")]
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


        [Authorize(Roles = "Administrator, AdministratorAssistant, Billing")]
        [HttpGet]
        public async Task<IActionResult> AddCollectionComment(int id)
        {
            ViewData["Action"] = nameof(AddCollectionComment);
            ViewData["ModalTitle"] = "Agregar comentario de cobranza";

            var order = await _repository.GetByIdAsync<Order>(id);

            return PartialView("_AddCollectionCommentModal", new OrderStatusViewModel { OrderId = id, Comments = order.CollectionComment });
        }


        [Authorize(Roles = "Administrator, AdministratorCommercial, Billing")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCollectionComment(OrderStatusViewModel model)
        {
            try
            {
                var order = await _repository.GetByIdAsync<Order>(model.OrderId);

                order.AddCollectionComment(model.Comments);
                await _repository.UpdateAsync(order);

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