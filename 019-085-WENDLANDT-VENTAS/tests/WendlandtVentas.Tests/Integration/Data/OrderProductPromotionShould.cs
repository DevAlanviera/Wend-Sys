using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models.OrderViewModels;
using WendlandtVentas.Core.Models.ProductViewModels;
using WendlandtVentas.Core.Models.PromotionViewModels;
using WendlandtVentas.Core.Services;
using WendlandtVentas.Infrastructure.Data;
using WendlandtVentas.Tests.Integration.Identity;
using WendlandtVentas.Web.Controllers;
using WendlandtVentas.Web.Models;
using Xunit;

namespace WendlandtVentas.Tests.Integration.Data
{
    public class OrderProductPromotionShould
    {
        private AppDbContext _dbContext;

        private static DbContextOptions<AppDbContext> CreateNewContextOptions()
        {
            // Create a fresh service provider, and therefore a fresh
            // InMemory database instance.
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Create a new options instance telling the context to use an
            // InMemory database and the new service provider.
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            builder.UseInMemoryDatabase("catalog")
                .UseInternalServiceProvider(serviceProvider);

            return builder.Options;
        }

        private EfRepository GetRepository()
        {
            var options = CreateNewContextOptions();
            var mockDispatcher = new Mock<IDomainEventDispatcher>();
            var mockLogBookService = new Mock<ILogBookService>();
            _dbContext = new AppDbContext(options, mockDispatcher.Object, mockLogBookService.Object);
            return new EfRepository(_dbContext);
        }

        private List<ApplicationUser> _users => new List<ApplicationUser> { new ApplicationUser() { Email = "prueba@prueba.com" } };
        private readonly SignInManager<ApplicationUser> _signInManager;
        private Mock<UserManager<ApplicationUser>> GetUserManagerService()
        {
            var _userManager = MockUserManager.UserManagerMocked(_users);

            var _userViewModel = new UserViewModel()
            {
                Id = It.IsAny<string>(),
                Email = "prueba1@prueba.com",
                Name = "Prueba",
                Password = "1Z2x3,.",
                IsActive = true,
                Roles = new List<string> { "Administrator" }
            };

            var mockLogger = new Mock<ILogger<AccountController>>();
            var mockEmailSender = new Mock<IEmailSender>();
            var usersController = new AccountController(_userManager.Object, _signInManager, mockEmailSender.Object, mockLogger.Object);
            var resp = usersController.Register(_userViewModel).GetAwaiter().GetResult();
            return _userManager;
        }

        private OrderService GetOrderService(EfRepository efRepository)
        {

            var mockNotificationService = new Mock<INotificationService>();
            var mockInventoryService = new Mock<IInventoryService>();
            var mockLogger = new Mock<ILogger<OrderService>>();
            return new OrderService(GetUserManagerService().Object, efRepository, mockNotificationService.Object, mockInventoryService.Object, mockLogger.Object);
        }

        private Promotion GetPromotion(PromotionType type)
        {
            return new Promotion("Promotion test", 1, 1, type, Classification.Bronze, new List<PresentationPromotion>() { new PresentationPromotion(1) }, new List<ClientPromotion>() { new ClientPromotion(1) });
        }
        [Fact]
        public async Task ValidateOrderAddFormatDateAndProductAdd()
        {
            var repository = GetRepository();
            var orderService = GetOrderService(repository);
            var currentUser = "prueba@prueba.com";
            var paymentDate = new DateTime(2020, 02, 03);
            var paymentPromiseDate = new DateTime(2020, 02, 14);

            //guardar presentación
            var productPresentation = new ProductPresentation(1, 1, 100, 100, 100);
            repository.Add(productPresentation);

            //guardar promoción
            var promotion = GetPromotion(PromotionType.Clients);
            repository.Add(promotion);

            //guardar cliente
            var client = new Client("cliente one");
            repository.Add(client);

            var orderPromotionProducts = new List<OrderPromotionProduct> {
                new OrderPromotionProduct(productPresentation, 2)
            };
            var orderPromotions = new List<PromotionItemModel> {
                new PromotionItemModel{
                    Id = 1,
                    Buy = 1,
                    Discount = 0.2,
                    Name = "Promotion one",
                    Present = 1,
                    Products = new List<ProductItemModel>
                    {
                        new ProductItemModel{
                            Id = 1,
                            Name = "Product one",
                            PresentationLiters = "Lata 0.355 ml",
                            Price = 100,
                            Quantity = 1
                        }
                    }
                }
            };

            var model = new OrderViewModel
            {
                IsInvoice = OrderType.Invoice,
                Paid = true,
                InvoiceCode = "INVOICE_TEST",
                RemissionCode = "REMISSION_TEST",
                PaymentDate = "03/02/2020",
                PaymentPromiseDate = "14/02/2020",
                DeliveryDay = "14/03/2020",
                ClientId = 1,
                AddressId = 1,
                Address = "ADDRESS_TEST",
                AddressName = "ADDRESS_NAME_TEST",
                ProductPresentationIds = new List<int> { 1 },
                ProductPresentationQuantities = new List<int> { 10 },
                ProductIsPresent = new List<bool> { false },
                ProductPrices = new List<decimal>() { 10 },
                Promotions = new List<string> { JsonConvert.SerializeObject(orderPromotions) }
            };

            var response = await orderService.AddOrderAsync(model, currentUser);
            var order = repository.ListAll<Order>().FirstOrDefault();

            Assert.True(response.IsSuccess);
            Assert.True(order.OrderProducts.Any());
            Assert.True(order.OrderPromotions.Any());
            Assert.Equal(order.Discount, order.OrderPromotions.Sum(c => c.Discount));
        }



    }
}