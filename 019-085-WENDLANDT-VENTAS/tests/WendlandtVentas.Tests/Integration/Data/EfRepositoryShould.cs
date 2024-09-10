using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Monobits.SharedKernel.Interfaces;
using Moq;
using System.Linq;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Infrastructure.Data;
using Xunit;

namespace WendlandtVentas.Tests.Integration.Data
{
    public class EfRepositoryShould
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
            _dbContext = new AppDbContext(options, mockDispatcher.Object,mockLogBookService.Object);
            return new EfRepository(_dbContext);
        }

    }
}