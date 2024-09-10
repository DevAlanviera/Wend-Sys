using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Infrastructure.Data
{
    public class AppDbContextSeed
    {
        public static async Task SeedAsync(AppDbContext catalogContext, ILoggerFactory loggerFactory, int? retry = 0)
        {
            int retryForAvailability = retry.Value;
            try
            {
                if (!catalogContext.States.Any())
                {
                    catalogContext.States.AddRange(GetPreconfiguredCatalogStates());
                    await catalogContext.SaveChangesAsync();
                }

                if (!catalogContext.Presentations.Any())
                {
                    catalogContext.Presentations.AddRange(GetPreconfiguredCatalogPresentations());
                    await catalogContext.SaveChangesAsync();
                }
                if (!catalogContext.Addresses.Any())
                {
                    catalogContext.Addresses.AddRange(GetPreconfiguredClientAddress(catalogContext.Clients.ToList()));
                    await catalogContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                if (retryForAvailability < 10)
                {
                    retryForAvailability++;
                    var log = loggerFactory.CreateLogger<AppDbContextSeed>();
                    log.LogError(ex.Message);
                    await SeedAsync(catalogContext, loggerFactory, retryForAvailability);
                }
            }
        }
        private static IEnumerable<State> GetPreconfiguredCatalogStates()
        {
            return new List<State>()
            {
                new State("Aguascalientes"),
                new State("Baja California"),
                new State("Baja California Sur"),
                new State("Campeche"),
                new State("Chiapas"),
                new State("Chihuahua"),
                new State("Ciudad de México"),
                new State("Coahuila"),
                new State("Colima"),
                new State("Durango"),
                new State("Guanajuato"),
                new State("Guerrero"),
                new State("Hidalgo"),
                new State("Jalisco"),
                new State("México"),
                new State("Michoacán"),
                new State("Morelos"),
                new State("Nayarit"),
                new State("Nuevo León"),
                new State("Oaxaca"),
                new State("Puebla"),
                new State("Querétaro"),
                new State("Quintana Roo"),
                new State("San Luis Potosí"),
                new State("Sinaloa"),
                new State("Sonora"),
                new State("Tabasco"),
                new State("Tamaulipas"),
                new State("Tlaxcala"),
                new State("Veracruz"),
                new State("Yucatán"),
                new State("Zacatecas"),
            };
        }
        private static IEnumerable<Presentation> GetPreconfiguredCatalogPresentations()
        {
            return new List<Presentation>()
            {
                new Presentation("Barril Pet", 20),
                //new Presentation("Barril Pet", 30),
                new Presentation("Barril Inox", 20),
                //new Presentation("Barril Inox", 30),
                new Presentation("Barril Inox", 60),
                new Presentation("Botella", 0.35),
                new Presentation("Lata", 0.35),
                new Presentation("Tasting",1)
            };
        }

        private static IEnumerable<Address> GetPreconfiguredClientAddress(List<Client> clients)
        {
            return clients.Select(a => new Address(a.Name,a.Address,a.Id));
        }
    }
}