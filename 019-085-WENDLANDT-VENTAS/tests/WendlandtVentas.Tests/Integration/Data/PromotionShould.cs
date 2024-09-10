using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Monobits.SharedKernel.Interfaces;
using Moq;
using System.Collections.Generic;
using System.Linq;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Infrastructure.Data;
using Xunit;

namespace WendlandtVentas.Tests.Integration.Data
{
    public class PromotionShould
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

        private Promotion GetPromotion(PromotionType type)
        {
           return new Promotion("Promotion test", 1, 1, type, Classification.Bronze, new List<PresentationPromotion>() { new PresentationPromotion(1) }, new List<ClientPromotion>() { new ClientPromotion(1) });
        }
        [Fact]
        public void ValidatePromotionTypeGeneral()
        {
            var repository = GetRepository();
            var promotion = GetPromotion(PromotionType.General);

            repository.Add(promotion);

            var response = repository.ListAll<Promotion>().FirstOrDefault();

            Assert.NotNull(response);
            Assert.True(response.PresentationPromotions.Any());
            Assert.False(response.ClientPromotions.Any());
            Assert.Null(response.Classification);
        }
        [Fact]
        public void ValidatePromotionTypeClassification()
        {
            var repository = GetRepository();
            var promotion = GetPromotion(PromotionType.Classification);

            repository.Add(promotion);

            var response = repository.ListAll<Promotion>().FirstOrDefault();

            Assert.NotNull(response);
            Assert.True(response.PresentationPromotions.Any());
            Assert.False(response.ClientPromotions.Any());
            Assert.NotNull(response.Classification);
        }
        [Fact]
        public void ValidatePromotionTypeClients()
        {
            var repository = GetRepository();
            var promotion = GetPromotion(PromotionType.Clients);

            repository.Add(promotion);

            var response = repository.ListAll<Promotion>().FirstOrDefault();

            Assert.NotNull(response);
            Assert.True(response.PresentationPromotions.Any());
            Assert.True(response.ClientPromotions.Any());
            Assert.Null(response.Classification);
        }
    }
}