using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Validation;
using Humanizer;
using LinqKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Core.Models.Enums;
using WendlandtVentas.Core.Models.OrderViewModels;
using WendlandtVentas.Core.Models.ProductPresentationViewModels;
using WendlandtVentas.Core.Models.PromotionViewModels;
using WendlandtVentas.Core.Specifications.OrderExtendedSpecifications;
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;
using WendlandtVentas.Core.Specifications.PromotionSpecifications;

namespace WendlandtVentas.Core.Services
{
    public class OrderService : IOrderService
    {
        private readonly CacheService _cacheService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAsyncRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<OrderService> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IExcelReadService _excelReaderService;
        private readonly IBitacoraService _bitacoraService;

        public OrderService(UserManager<ApplicationUser> userManager,
            IAsyncRepository repository, INotificationService notificationService,
            IBitacoraService bitacoraService,
            IInventoryService inventoryService, ILogger<OrderService> logger,
            CacheService cacheService, IEmailSender emailSender, IExcelReadService excelReaderService)
        {
            _userManager = userManager;
            _repository = repository;
            _notificationService = notificationService;
            _inventoryService = inventoryService;
            _bitacoraService = bitacoraService;
            _logger = logger;
            _cacheService = cacheService;
            _emailSender = emailSender;
            _excelReaderService = excelReaderService;
        }


        public async Task<Response> AddOrderAsync(OrderViewModel model, string currrentUserEmail, string clienteEmail)
        {
            var user = await _userManager.FindByEmailAsync(currrentUserEmail);
            var rolesUser = await _userManager.GetRolesAsync(user);
            var role = rolesUser != null ? rolesUser.First() : string.Empty;

            var client = await _repository.GetByIdAsync<Client>(model.ClientId);
            var dates = GetParsePaymentDate(model.PaymentDate, model.PaymentPromiseDate, model.DeliveryDay);
            var dueDate = dates.DeliveryDay > DateTime.MinValue
                ? dates.DeliveryDay.AddDays(client.CreditDays + 1)
                : DateTime.UtcNow.ToLocalTime().AddDays(client.CreditDays + 1);

            var productPresentations = await _repository.GetQueryableExisting<ProductPresentation>()
            .Include(pp => pp.Product)
            .Include(pp => pp.Presentation)
            .Where(c => model.ProductPresentationIds.Any(m => m == c.Id))
            .ToListAsync();


            var orderProducts = new List<OrderProduct>();
            var orderPromotions = new List<OrderPromotion>();
            var orderPromotionsItems = new List<PromotionItemModel>();

            // Validación final de RFC (redundante por seguridad)
            if (model.IsInvoice == OrderType.Invoice)
            {
               
                if (client == null || string.IsNullOrEmpty(client.RFC))
                    return new Response(false, "No se puede facturar: el cliente no tiene RFC registrado.");
            }

            foreach (var productPresentation in productPresentations)
            {
                var i = model.ProductPresentationIds.FindIndex(x => x == productPresentation.Id);

                if (i < 0 || i >= model.ProductPresentationQuantities.Count ||
                    i >= model.ProductIsPresent.Count || i >= model.ProductPrices.Count)
                    continue;

                var quantity = model.ProductPresentationQuantities[i];
                var isPresent = model.ProductIsPresent[i];
                var price = model.ProductPrices[i];

               

                orderProducts.Add(new OrderProduct(productPresentation, quantity, isPresent, price));
            }

            if (model.Promotions != null)
            {
                foreach (var promotion in model.Promotions)
                {
                    try
                    {
                        var items = JsonConvert.DeserializeObject<List<PromotionItemModel>>(promotion);
                        orderPromotionsItems = orderPromotionsItems.Union(items).ToList();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Error al deserializar promoción.");
                    }
                }

                orderPromotions = await GetOrderProductsValidate(model.ClientId, orderPromotionsItems);
            }

            if (model.AddressId != null)
            {
                var address = await _repository.GetByIdAsync<Address>(model.AddressId.Value);
                if (address != null)
                {
                    model.Address = address.AddressLocation;
                    model.AddressName = address.Name;
                }
            }

            var order = new Order(
                model.InvoiceCode, model.IsInvoice, OrderStatus.New, model.Paid,
                dates.PaymentPromiseDate.ToUniversalTime(), dates.PaymentDate.ToUniversalTime(),
                user.Id, model.ClientId, model.Comment, model.Delivery, model.DeliverySpecification,
                orderProducts, orderPromotions, model.Address, model.AddressName,
                dates.DeliveryDay.ToUniversalTime(), dueDate.ToUniversalTime(),
                model.PayType, model.CurrencyType);

            

            try
            {
                await _repository.AddAsync(order);

                var orderTypeName = "Pedido";
                if (model.IsInvoice == OrderType.Return)
                {
                    orderTypeName = "Devolución";
                    order.UpdateReturnInformation(model.ReturnRemisionNumber, model.ReturnReason);
                }
                else
                {
                    order.GenerateRemisionCode();
                }

                await _repository.UpdateAsync(order);

                var clientName = client != null ? client.Name : string.Empty;
                var roles = new List<Role>() { Role.Administrator, Role.Storekeeper, Role.Billing, Role.BillingAssistant };
                var title = $"{orderTypeName} {order.Id}";
                var message = $"{orderTypeName} nuevo: #{order.Id} - {clientName} - {order.Total:C2}";

                await _notificationService.AddAndSendNotificationByRoles(roles, title, message, user.Id, role);
                var bitacora = new Bitacora(order.Id, user.Name, "Crear pedido");
                await _bitacoraService.AddAsync(bitacora);
                
                _cacheService.InvalidateOrderCache();
               // string mensaje = (client.Channel == Entities.Enums.Channel.Distributor)
                // ? "Confirmado: Es un Distribuidor"
                // : $"Cuidado: El canal actual es {client.Channel}";
               // Console.WriteLine(mensaje  + "\n \n \n \n \n \n \n \n \n \n \n" + "UUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUU");
                if (client.Channel == Entities.Enums.Channel.Distributor)
                {
                    // Console.WriteLine("SI ES DISTRIBUIDORRR" + "\n \n \n \n \n \n \n \n \n \n \n" + "UUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUU");
                    try
                    {
                        // 1. Generar PDF del pedido usando ExcelReaderService
                        var pdfBytes = await _excelReaderService.FillDataAndReturnPdfAsync("wwwroot/resources", order);

                       
                        // 2.3 Enviar correo al cliente con el PDF adjunto
                        var enviado = await EnviarEstadoCuentaAsync(order.Id, clienteEmail, pdfBytes);

                        if (!enviado)
                            _logger.LogWarning("No se pudo enviar el estado de cuenta del pedido {OrderId}", order.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al generar PDF o enviar correo para el pedido {OrderId}", order.Id);
                    }
                
               }
               // else
               // {
                //    Console.WriteLine("no es distribuidorrrr: " + "\n \n \n \n \n \n \n \n \n \n \n" + "UUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUU");
               // }

                    return new Response(true, "Pedido guardado");

                
            }
            catch (Exception e)
            {
                await _repository.DeleteAsync(order);
                _logger.LogError(e, $"Error al agregar orden");
                return new Response(false, e.Message);
            }
        }

        public async Task<Response> UpdateOrderAsync(OrderViewModel model, string currrentUserEmail)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(currrentUserEmail);
                var order = await _repository.GetAsync(new OrderExtendedSpecification(model.Id));
                var client = await _repository.GetByIdAsync<Client>(model.ClientId);
                var dates = GetParsePaymentDate(model.PaymentDate, model.PaymentPromiseDate, model.DeliveryDay);
                var dueDate = dates.DeliveryDay > DateTime.MinValue ? dates.DeliveryDay.AddDays(client.CreditDays + 1) : order.CreatedAt.ToLocalTime().AddDays(client.CreditDays + 1);
                var productPresentations = (await _repository.ListAllAsync<ProductPresentation>()).Where(c => model.ProductPresentationIds.Any(m => m == c.Id));
                var orderProducts = new List<OrderProduct>();
                var currentOrderProduct = new OrderProduct();
                var orderPromotions = new List<OrderPromotion>();
                var orderPromotionsItems = new List<PromotionItemModel>();

                if (model.IsInvoice == OrderType.Invoice)
                {

                    if (client == null || string.IsNullOrEmpty(client.RFC))
                        return new Response(false, "No se puede facturar: el cliente no tiene RFC registrado.");
                }

                if (order.InventoryDiscount)
                {
                    await _inventoryService.OrderReturn(order.OrderProducts.Select(c => new ProductPresentationQuantity { Id = c.ProductPresentationId, Quantity = c.Quantity }), user.Email, order.Id);
                }

                foreach (var productPresentation in productPresentations)
                {
                    var i = model.ProductPresentationIds.FindIndex(x => x == productPresentation.Id);
                    var quantity = model.ProductPresentationQuantities[i];
                    var isPresent = model.ProductIsPresent[i];
                    var price = model.ProductPrices[i];
                    currentOrderProduct = order.OrderProducts.Where(o => o.ProductPresentation == productPresentation &&
                                                                         o.Quantity == quantity).SingleOrDefault();
                    if (currentOrderProduct != null)
                    {
                        if (currentOrderProduct.Price != price)
                        {
                            currentOrderProduct.EditPrice(price);
                        }
                        orderProducts.Add(currentOrderProduct);
                    }
                    else
                    {
                        // Si el producto es nuevo, se añade con el precio base o el precio con descuento, según corresponda
                        orderProducts.Add(new OrderProduct(productPresentation, quantity, isPresent, price));
                    }
                }

                if (model.Promotions != null)
                {
                    foreach (var promotion in model.Promotions)
                    {
                        orderPromotionsItems = orderPromotionsItems.Union(JsonConvert.DeserializeObject<List<PromotionItemModel>>(promotion)).ToList();
                    }
                    orderPromotions = await GetOrderProductsValidate(model.ClientId, orderPromotionsItems);
                }

                if (model.AddressId != null)
                {
                    var address = await _repository.GetByIdAsync<Address>(model.AddressId.Value);
                    if (address != null)
                    {
                        model.Address = address.AddressLocation;
                        model.AddressName = address.Name;
                    }
                }

                // Asignar los nuevos valores al pedido
                order.ProntoPago = model.ProntoPago;
                order.Edit(model.InvoiceCode, model.IsInvoice, OrderStatus.New, model.Paid,
                    dates.PaymentPromiseDate.ToUniversalTime(), dates.PaymentDate.ToUniversalTime(),
                    model.ClientId, model.Comment, model.Delivery, model.DeliverySpecification,
                    orderProducts, orderPromotions, model.Address, model.AddressName,
                    dates.DeliveryDay.ToUniversalTime(), dueDate.ToUniversalTime(), model.PayType, model.CurrencyType);

                if (model.IsInvoice == OrderType.Return)
                {
                    order.UpdateReturnInformation(model.ReturnRemisionNumber, model.ReturnReason);
                }

                // Actualizar la fecha de LastModified antes de guardar la orden
                await _repository.UpdateAsync(order);

                if (order.InventoryDiscount)
                {
                    await _inventoryService.OrderDiscount(order.OrderProducts.Select(c => new ProductPresentationQuantity { Id = c.ProductPresentationId, Quantity = c.Quantity }), user.Email, order.Id);
                }

                _cacheService.InvalidateOrderCache();
                return new Response(true, "Pedido editado");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error al editar orden");
                return new Response(false, e.Message);
            }
        }

        //Agregar en este metodo una llamada a la api para actualizar el monto de tesoreria
        public async Task<Response> ActualizarTotalAsync(int orderId, decimal nuevoTotal, bool precioEspecial, string currrentUserEmail)
        {
            var user = await _userManager.FindByEmailAsync(currrentUserEmail);

            try
            {
                var order = await _repository.GetByIdAsync<Order>(orderId);
                if (order == null)
                {
                    return new Response(false, "La orden no existe.");
                }

                // Guardamos el estado del checkbox
                order.PrecioEspecial = precioEspecial;

                if (precioEspecial)
                {
                    // Si el usuario activó el precio especial, asignamos el nuevo monto
                    order.RealAmount = nuevoTotal;
                }
                else
                {
                    // Si se desactiva, eliminamos el monto especial
                    order.RealAmount = null;
                }

                await _repository.UpdateAsync(order);

                var bitacora = new Bitacora(order.Id, user.Name, "Cambio a monto real");
                await _bitacoraService.AddAsync(bitacora);

                return new Response(true, "Total actualizado correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el total.");
                return new Response(false, "Error al actualizar el total.");
            }
        }
        //Se necesita desarrollar para cuando esta desactivado el valor del realamount se haga 0.0

        //public IQueryable<Order> FilterValues(FilterViewModel filter)
        //{
        //    var orders = _repository.GetQueryableExisting<Order>();
        //    var predicate = PredicateBuilder.New<Order>(true);

        //    if (!string.IsNullOrEmpty(filter.OrderType))
        //    {
        //        var orderType = (OrderType)Enum.Parse(typeof(OrderType), filter.OrderType);
        //        predicate.And(c => c.Type == orderType);
        //    }

        //    if (filter.OrderStatus.Any())
        //    {

        //        var orderStatus = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().AsEnumerable().Where(s => filter.OrderStatus.Any(o => o == s.Humanize()));
        //        //(OrderStatus)Enum.Parse(typeof(OrderStatus), filter.OrderStatus);
        //        foreach (var status in orderStatus)
        //        {
        //            predicate.Or(c => c.OrderStatus == status);
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(filter.DateStart))
        //    {
        //        var dateStart = ParseExact(filter.DateStart).Date;

        //        switch (filter.FilterDate)
        //        {
        //            case FilterDate.CreatedDate:
        //                predicate.And(c => c.CreatedAt.AddHours(-8).Date >= dateStart);
        //                break;
        //            case FilterDate.PaymentPromiseDate:
        //                predicate.And(c => c.PaymentPromiseDate.AddHours(-8).Date >= dateStart);
        //                break;
        //            case FilterDate.PaymentDate:
        //                predicate.And(c => c.PaymentDate.AddHours(-8).Date >= dateStart);
        //                break;
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(filter.DateEnd))
        //    {
        //        var dateEnd = ParseExact(filter.DateEnd).Date;

        //        switch (filter.FilterDate)
        //        {
        //            case FilterDate.CreatedDate:
        //                predicate.And(c => c.CreatedAt.AddHours(-8).Date <= dateEnd);
        //                break;
        //            case FilterDate.PaymentPromiseDate:
        //                predicate.And(c => c.PaymentPromiseDate.AddHours(-8).Date <= dateEnd);
        //                break;
        //            case FilterDate.PaymentDate:
        //                predicate.And(c => c.PaymentDate.AddHours(-8).Date <= dateEnd);
        //                break;
        //        }
        //    }

        //    if (filter.StateId.Any())
        //    {
        //        foreach (var state in filter.StateId)
        //        {
        //            predicate.Or(c => c.Client.StateId == state);
        //        }
        //    }

        //    if (filter.ClientId.Any())
        //    {
        //        foreach (var client in filter.ClientId)
        //        {
        //            predicate.Or(c => c.ClientId == client);
        //        }

        //    }

        //    if (filter.ProductId.Any())
        //    {
        //        foreach (var product in filter.ProductId)
        //        {
        //            predicate.Or(c => c.OrderProducts.Any(c => c.Id == product));
        //        }
        //    }

        //    if (filter.PresentationId.Any())
        //    {
        //        foreach (var presentation in filter.PresentationId)
        //        {
        //            predicate.Or(c => c.OrderProducts.Any(c => c.ProductPresentation.Product.Id == presentation));
        //        }
        //    }

        //    return orders.Where(predicate);
        //}

        public async Task<List<Order>> FilterValues(FilterViewModel filter)
        {
            var orders = _repository.GetQueryable(new OrderExtendedSpecification()).Where(c => !c.IsDeleted);
            //var orders = (await _repository.ListExistingAsync(new OrderExtendedSpecification())).AsEnumerable();
            var predicate = PredicateBuilder.New<Order>(true);

            if (!string.IsNullOrEmpty(filter.OrderType))
            {
                var orderType = (OrderType)Enum.Parse(typeof(OrderType), filter.OrderType);
                predicate.And(c => c.Type == orderType);
            }

            if (predicate.IsStarted)
            {
                orders = orders.Where(predicate);
                predicate = PredicateBuilder.New<Order>(true);
            }

            if (filter.OrderStatus.Any())
            {
                foreach (var status in filter.OrderStatus)
                {
                    var orderStatus = (OrderStatus)Enum.Parse(typeof(OrderStatus), status);
                    predicate.Or(c => c.OrderStatus == orderStatus);
                }
            }

            if (predicate.IsStarted)
            {
                orders = orders.Where(predicate);
                predicate = PredicateBuilder.New<Order>(true);
            }

            if (filter.City != null && filter.City.Any())
            {
               foreach(var city in filter.City)
                {
                    predicate.Or(d => EF.Functions.Like(city, d.Client.City));
                }
            }

            if (!string.IsNullOrEmpty(filter.DateStart))
            {
                var dateStart = ParseExact(filter.DateStart).Date;

                switch (filter.FilterDate)
                {
                    case FilterDate.CreatedDate:
                        predicate.And(c => c.CreatedAt/*.AddHours(-8)*/.Date >= dateStart);
                        break;
                    case FilterDate.PaymentPromiseDate:
                        predicate.And(c => c.PaymentPromiseDate/*.AddHours(-8)*/.Date >= dateStart);
                        break;
                    case FilterDate.PaymentDate:
                        predicate.And(c => c.PaymentDate/*.AddHours(-8)*/.Date >= dateStart);
                        break;
                    case FilterDate.DeliveryDate:
                        predicate.And(c => c.DeliveryDate/*.AddHours(-8)*/.Date >= dateStart);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(filter.DateEnd))
            {
                var dateEnd = ParseExact(filter.DateEnd).Date;

                switch (filter.FilterDate)
                {
                    case FilterDate.CreatedDate:
                        predicate.And(c => c.CreatedAt/*.AddHours(-8)*/.Date <= dateEnd);
                        break;
                    case FilterDate.PaymentPromiseDate:
                        predicate.And(c => c.PaymentPromiseDate/*.AddHours(-8)*/.Date <= dateEnd);
                        break;
                    case FilterDate.PaymentDate:
                        predicate.And(c => c.PaymentDate/*.AddHours(-8)*/.Date <= dateEnd);
                        break;
                    case FilterDate.DeliveryDate:
                        predicate.And(c => c.DeliveryDate/*.AddHours(-8)*/.Date <= dateEnd);
                        break;
                }
            }

            if (predicate.IsStarted)
            {
                orders = orders.Where(predicate);
                predicate = PredicateBuilder.New<Order>(true);
            }

            if (filter.StateId.Any())
            {
                foreach (var state in filter.StateId)
                {
                    predicate.Or(c => c.Client.StateId == state);
                }
            }

            if (predicate.IsStarted)
            {
                orders = orders.Where(predicate);
                predicate = PredicateBuilder.New<Order>(true);
            }

            if (filter.ClientId.Any())
            {
                foreach (var client in filter.ClientId)
                {
                    predicate.Or(c => c.ClientId == client);
                }
            }

            if (predicate.IsStarted)
            {
                orders = orders.Where(predicate);
                predicate = PredicateBuilder.New<Order>(true);
            }

            if (filter.UserId.Any())
            {
                foreach (var user in filter.UserId)
                {
                    predicate.Or(c => c.UserId == user);
                }
            }

            if (predicate.IsStarted)
            {
                orders = orders.Where(predicate);
                predicate = PredicateBuilder.New<Order>(true);
            }

            if (filter.ProductId.Any())
            {
                foreach (var product in filter.ProductId)
                {
                    predicate.Or(c => c.OrderProducts.Any(c => c.ProductPresentation.ProductId == product));
                }
            }

            if (predicate.IsStarted)
            {
                orders = orders.Where(predicate);
                predicate = PredicateBuilder.New<Order>(true);
            }

            if (filter.PresentationId.Any())
            {
                foreach (var presentation in filter.PresentationId)
                {
                    predicate.Or(c => c.OrderProducts.Any(c => c.ProductPresentation.PresentationId == presentation));
                }
            }

            return await orders.Where(predicate).ToListAsync();
            //return orders.Where(predicate).ToList();
        }

        private (DateTime PaymentDate, DateTime PaymentPromiseDate, DateTime DeliveryDay) GetParsePaymentDate(string paymentDateVal, string paymentPromiseDateVal, string deliveryDayVal)
        {
            var paymentDate = DateTime.MinValue;
            var paymentPromiseDate = DateTime.MinValue;
            var deliveryDay = DateTime.MinValue;

            if (!string.IsNullOrEmpty(paymentDateVal))
                paymentDate = ParseExact(paymentDateVal);

            if (!string.IsNullOrEmpty(paymentPromiseDateVal))
                paymentPromiseDate = ParseExact(paymentPromiseDateVal);

            if (!string.IsNullOrEmpty(deliveryDayVal))
                deliveryDay = ParseExact(deliveryDayVal);

            return (paymentDate, paymentPromiseDate, deliveryDay);
        }

        private DateTime ParseExact(string date)
        {
            return DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        private async Task<List<OrderPromotion>> GetOrderProductsValidate(int clientId, IEnumerable<PromotionItemModel> promotions)
        {
            var client = await _repository.GetByIdAsync<Client>(clientId);
            var promotionsClient = await _repository.ListExistingAsync(new PromotionByClientSpecification(client));
            var productsPresentations = await _repository.ListExistingAsync(new ProductPresentationByIdsSpecification(promotions.Select(c => c.Products.FirstOrDefault().Id)));
            var orderPromotions = new List<OrderPromotion>();
            
            foreach (var promotionModel in promotions.Where(c => promotionsClient.Any(d => d.Id == c.Id)))
            {
                var orderPromotionProducts = new List<OrderPromotionProduct>();
                var promotion = promotionsClient.SingleOrDefault(c => c.Id == promotionModel.Id);
                if (promotion != null)
                {
                    foreach (var productPresentation in promotionModel.Products)
                    {
                        var productPresentationDb = productsPresentations.SingleOrDefault(c => c.Id == promotionModel.Products.FirstOrDefault().Id);
                        if (productPresentationDb != null && productPresentationDb.Product.Distinction != Distinction.Season)
                        {
                            orderPromotionProducts.Add(new OrderPromotionProduct(productPresentationDb, productPresentation.Quantity));
                        }
                    }
                    orderPromotions.Add(new OrderPromotion(promotion.Id, orderPromotionProducts));
                }
            }
            return orderPromotions;
        }

        public Task<List<SelectListItem>> GetInvoiceRemissionNumbersAsync()
        {
            return _repository.GetQueryableExisting<Order>().Select(c => new SelectListItem(c.RemissionCode, c.RemissionCode)).ToListAsync();
        }


        public async Task<bool> EnviarEstadoCuentaAsync(int orderId, string clienteEmail, byte[] pdfAdjunto = null)
        {
            //Obtener la orden y validar
            var order = await _repository.GetByIdAsync<Order>(orderId);
            if (order == null)
                return false;

            var nombreCliente = order.Client.Name;

            //Generar asunto y mensaje HTML
            var asunto = $"¡Pedido #{order.Id} realizado con éxito! - Cervecería Wendlandt de México";
            var mensaje = GenerarMensajeHtmlEstadoCuenta(nombreCliente, order.ClientId);
            //Console.WriteLine("ENVIANDO CORREOOO");

           // Console.WriteLine("\n \n \n \n \n \n \n \n \n \n \n" + "Enviando a Email destino: " + clienteEmail + "\n \n \n \n \n \n \n \n \n \n \n" + "UUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUU");

            //Enviar correo
            return await _emailSender.SendEmailAsync(
                       email: clienteEmail, //clienteEmail,
                        subject: asunto,
                        message: mensaje,
                        file: null,
                        attachmentBytes: pdfAdjunto,
                        attachmentName: pdfAdjunto != null ? $"Pedido_{order.Id}.pdf" : null,
                        perfil: "Emailpagos"
            );
        }

        // Método auxiliar para generar el mensaje HTML
        private string GenerarMensajeHtmlEstadoCuenta(string nombreCliente, int clientId)
        {
            return $@"
            <p>Hola {nombreCliente},</p>
            <p>¡Tu pedido ha sido registrado correctamente! 🎉</p>
            <p>Adjunto a este correo encontrarás un <strong>PDF con el detalle de tu pedido</strong>, incluyendo la lista de productos que solicitaste.</p>

            <p>También puedes consultar tus facturas y el estado de tu cuenta haciendo clic en el siguiente enlace:</p>

            <a href='https://sistemawendlandt.com/ClientStateAccount/{clientId}' 
               style='display:inline-block;padding:12px 24px;background-color:#d6f5f5;
                      color:#005f5f;text-decoration:none;border-radius:12px;font-weight:500;'>
                Revisar estado de cuenta
            </a>

            <p>Gracias por confiar en Cervecería Wendlandt de México. ¡Esperamos que disfrutes tu pedido! 🍺</p>

            <p>Salud,<br/>
            El equipo de Wendlandt</p>";
        }


        public async Task<bool> EnviarEstadoCuentaAsync(int orderId, string clienteEmailManual = null)
        {
            // Es vital cargar el Cliente y sus Contactos para que no sean null
            // Nota: Asegúrate que tu repositorio soporte Include o usa una especificación
            var order = await _repository.GetByIdAsync<Order>(orderId);

            if (order == null || order.Client == null)
                return false;

            // Lógica de prioridad para el Email:
            // 1. El email que viene por parámetro (si existe)
            // 2. El email del primer contacto del cliente
            // 3. Email de respaldo (hardcoded)
            string emailDestino = clienteEmailManual
                ?? order.Client.Contacts?.FirstOrDefault()?.Email
                ?? "alan.cordova@wendlandt.com.mx";

            Console.WriteLine("ENVIANDO CORREOOO");

            Console.WriteLine("\n \n \n \n \n \n \n \n \n \n \n" + "Enviando a Email destino: " + order.Client.Contacts?.FirstOrDefault()?.Email + "\n \n \n \n \n \n \n \n \n \n \n" + "UUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUU");

            var nombreCliente = order.Client.Name;
            var asunto = "¡Pedido realizado con éxito! - Cervecería Wendlandt de México";

            // Construcción del mensaje con el enlace dinámico al portal de clientes
            var mensaje = $@"
            <div style='font-family: sans-serif; color: #333;'>
                <p>Hola <strong>{nombreCliente}</strong>,</p>
                <p>¡Tu pedido ha sido registrado correctamente! 🎉</p>
                <p>Adjunto a este correo encontrarás un <strong>PDF con el detalle de tu pedido</strong>, incluyendo la lista de productos que solicitaste.</p>

                <p>También puedes consultar tus facturas y el estado de tu cuenta haciendo clic en el siguiente enlace:</p>

                <div style='margin: 20px 0;'>
                    <a href='https://sistemawendlandt.com/ClientStateAccount/{order.ClientId}' 
                       style='display:inline-block;padding:12px 24px;background-color:#d6f5f5;
                              color:#005f5f;text-decoration:none;border-radius:12px;font-weight:bold;'>
                        Revisar estado de cuenta
                    </a>
                </div>

                <p>Gracias por confiar en Cervecería Wendlandt de México. ¡Esperamos que disfrutes tu pedido! 🍺</p>

                <p>Salud,<br/>
                <strong>El equipo de Wendlandt</strong></p>
            </div>";
            Console.WriteLine("\n \n \n \n \n \n \n \n \n \n \n" + "Enviando a Email destino: " + emailDestino + "\n \n \n \n \n \n \n \n \n \n \n" + "UUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUU");
            return await _emailSender.SendEmailAsync(
                "alan.cordova@wendlandt.com.mx",
                asunto,
                mensaje,
                perfil: "Emailpagos"
            );
        }

        /*public async Task<bool> EnviarEncuestaSatisfaccionAsync()
        {
            try
            {
                // 1. Obtener todos los clientes con su contacto
                var clientes = await _repository.GetAsync<Client>();

                if (clientes == null || !clientes.Any())
                    return false;

                foreach (var cliente in clientes)
                {
                    var nombreCliente = cliente.Name;
                    var clienteEmail = cliente.Contact?.Email;

                    // Si el cliente no tiene correo, saltamos
                    if (string.IsNullOrWhiteSpace(clienteEmail))
                        continue;

                    // 2. Construir asunto y cuerpo del correo
                    var asunto = "Queremos saber tu opinión 🍺 - Cervecería Wendlandt de México";

                    var mensaje = $@"
                <p>Hola {nombreCliente},</p>
                <p>Gracias por confiar en <strong>Cervecería Wendlandt</strong>.</p>
                <p>Tu opinión es muy importante para nosotros. Por eso, queremos invitarte a contestar 
                nuestra breve encuesta de satisfacción para seguir mejorando tu experiencia con nosotros.</p>

                <a href='https://sistemawendlandt.com/EncuestaSatisfaccion/{cliente.Id}' 
                   style='display:inline-block;padding:12px 24px;background-color:#ffd966;
                          color:#000;text-decoration:none;border-radius:12px;font-weight:500;'>
                    Responder encuesta
                </a>

                <p>¡Gracias por tu tiempo y por ser parte de nuestra comunidad! 🍻</p>
                <p>Salud,<br/>El equipo de Wendlandt</p>";

                    // 3. Enviar correo
                    await _emailSender.SendEmailAsync(
                        clienteEmail,
                        asunto,
                        mensaje,
                        perfil: "Emailpagos"
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar encuestas de satisfacción a los clientes");
                return false;
            }
        }*/
    }
}