using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Core.Models.ProductPresentationViewModels;
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;

namespace WendlandtVentas.Core.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAsyncRepository _repository;
        private readonly INotificationService _notificationService;
        public InventoryService(UserManager<ApplicationUser> userManager, IAsyncRepository repository)
        {
            _userManager = userManager;
            _repository = repository;
        }

        public async Task<Response> OrderDiscount(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId)
        {
            try
            {
                var productsPresentationsDb = await _repository.ListAsync(new ProductPresentationByIdsExtendedSpecification(productsPresentations.Select(c => c.Id)));
                var user = await _userManager.FindByNameAsync(email);
                var movements = new List<Movement>();

                foreach (var productPresentation in productsPresentations)
                {
                    var productPresentationDb = productsPresentationsDb.FirstOrDefault(c => c.Id == productPresentation.Id);

                    if (productPresentationDb != null)
                    {
                        var quantityCurrent = productPresentationDb.Movements.Where(c => !c.IsDeleted)?.LastOrDefault()?.QuantityCurrent ?? 0;
                        movements.Add(new Movement(productPresentation.Id, productPresentation.Quantity, Operation.Out, quantityCurrent, $"Pedido {orderId}", user.Id));
                    }
                }
                await _repository.AddRangeAsync(movements);

                return new Response(true, "Descuento de inventario");
            }
            catch (Exception e)
            {
                return new Response(false, "No se pudo realizar el descuento de inventario");
            }
        }

        public async Task<Response> OrderReturn(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId)
        {
            try
            {
                var productsPresentationsDb = await _repository.ListAsync(new ProductPresentationByIdsExtendedSpecification(productsPresentations.Select(c => c.Id)));
                var user = await _userManager.FindByNameAsync(email);
                var movements = new List<Movement>();

                foreach (var productPresentation in productsPresentations)
                {
                    var productPresentationDb = productsPresentationsDb.FirstOrDefault(c => c.Id == productPresentation.Id);

                    if (productPresentationDb != null)
                    {
                        var quantityCurrent = productPresentationDb.Movements.Where(c => !c.IsDeleted)?.LastOrDefault()?.QuantityCurrent ?? 0;
                        movements.Add(new Movement(productPresentation.Id, productPresentation.Quantity, Operation.In, quantityCurrent, $"Pedido {orderId}", user.Id));
                    }
                }
                await _repository.AddRangeAsync(movements);

                return new Response(true, "Retorno a inventario");
            }
            catch (Exception e)
            {
                return new Response(false, "No se pudo realizar el retorno a inventario");
            }
        }

        // En tu InventoryService
        public async Task<int> GetAvailableStock(int productPresentationId)
        {
            // Buscamos el último movimiento registrado para esta presentación que no esté borrado
            var lastMovement = await _repository.GetQueryable<Movement>()
                .Where(m => m.ProductPresentationId == productPresentationId && !m.IsDeleted)
                .OrderByDescending(m => m.Id) // El ID más alto es el más reciente
                .Select(m => new { m.QuantityCurrent }) // Solo traemos la columna necesaria
                .FirstOrDefaultAsync();

            return lastMovement?.QuantityCurrent ?? 0;
        }

    }
}
