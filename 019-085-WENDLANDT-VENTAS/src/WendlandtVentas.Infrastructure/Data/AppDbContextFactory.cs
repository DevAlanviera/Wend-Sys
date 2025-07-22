using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace WendlandtVentas.Infrastructure.Data
{
    public class AppDbContextFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AppDbContextFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public AppDbContext CreateDbContext()
        {
            return _serviceProvider.GetRequiredService<AppDbContext>();
        }
    }
}
