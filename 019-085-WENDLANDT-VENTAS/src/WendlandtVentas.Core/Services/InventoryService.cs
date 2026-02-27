using Microsoft.AspNetCore.Identity;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Core.Models.ProductPresentationViewModels;
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;
using Microsoft.Extensions.Logging;

namespace WendlandtVentas.Core.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAsyncRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<InventoryService> _logger; // <-- 1. Declarar
        public InventoryService(UserManager<ApplicationUser> userManager, IAsyncRepository repository, ILogger<InventoryService> logger)
        {
            _userManager = userManager;
            _repository = repository;
            _logger = logger;
        }

        public async Task<Response> OrderDiscount(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(email);
                var movements = new List<Movement>();

                foreach (var item in productsPresentations)
                {
                    // 1. Buscamos los lotes que tienen stock de este producto, ordenados por fecha (PEPS)
                    var batches = await _repository.GetQueryable<Batch>()
                     .Where(b => b.ProductPresentationId == item.Id && b.CurrentQuantity > 0 && b.IsActive)
                     .OrderBy(b => b.ExpiryDate)    // Primero por caducidad
                     .ThenBy(b => b.Id)             // Si expiran igual, el que se registró primero (ID más bajo)
                     .ToListAsync();

                    // --- MEJORA: Validar stock total antes de empezar ---
                    int totalDisponible = batches.Sum(b => b.CurrentQuantity);
                    if (totalDisponible < item.Quantity)
                    {
                        return new Response(false, $"Stock insuficiente para el producto ID {item.Id}. Requerido: {item.Quantity}, Disponible: {totalDisponible}");
                    }

                    decimal pendientePorDescontar = item.Quantity;

                    foreach (var batch in batches)
                    {
                        // Si ya no hay nada que descontar, salimos del bucle
                        if (pendientePorDescontar <= 0) break;

                        // IMPORTANTE: cantidadATomar debe ser el mínimo entre lo que tiene el lote 
                        // y lo que nos falta por surtir
                        int cantidadATomar = Math.Min(batch.CurrentQuantity, (int)pendientePorDescontar);

                        if (cantidadATomar <= 0) continue; // Si este lote está vacío por error, saltar al siguiente

                        var movement = new Movement(
                            item.Id,
                            cantidadATomar,
                            Operation.Out,
                            batch.CurrentQuantity,
                            $"Pedido {orderId}",
                            user.Id
                        );
                        movement.BatchId = batch.Id;
                        movements.Add(movement);

                        // --- EL PUNTO CRÍTICO ---
                        batch.CurrentQuantity -= cantidadATomar;

                        // Si el lote llegó a 0, lo desactivamos
                        if (batch.CurrentQuantity <= 0)
                        {
                            batch.CurrentQuantity = 0; // Aseguramos que no sea negativo
                            batch.IsActive = false;
                        }

                        // Restamos lo que ya tomamos de la deuda total del pedido
                        pendientePorDescontar -= cantidadATomar;

                        // Guardamos los cambios del lote actual antes de pasar al siguiente
                        await _repository.UpdateAsync(batch);
                    }

                    // Opcional: Si después de recorrer lotes aún falta cantidad, 
                    // significa que vendiste algo que no tienes en batches.
                }

                await _repository.AddRangeAsync(movements);
                return new Response(true, "Descuento de inventario por lotes realizado");
            }
            catch (Exception e)
            {
                return new Response(false, "No se pudo realizar el descuento por lotes");
            }
        }

        public async Task<Response> OrderReturn(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(email);
                var movements = new List<Movement>();

                foreach (var item in productsPresentations)
                {
                    // Buscamos el lote activo más reciente (el que caduca más tarde)
                    // Esto asegura que la cerveza devuelta se sume a un lote vigente.
                    var batch = await _repository.GetQueryable<Batch>()
                        .Where(b => b.ProductPresentationId == item.Id && b.IsActive)
                        .OrderByDescending(b => b.ExpiryDate)
                        .FirstOrDefaultAsync();

                    if (batch != null)
                    {
                        // 1. Aumentamos el stock del lote seleccionado
                        batch.CurrentQuantity += (int)item.Quantity;

                        // 2. Registramos el movimiento de entrada para la auditoría
                        var movement = new Movement(
                            item.Id,
                            item.Quantity,
                            Operation.In, // ENTRADA
                            batch.CurrentQuantity,
                            $"Devolución Nueva (Pedido {orderId})",
                            user.Id
                        );
                        movement.BatchId = batch.Id;
                        movements.Add(movement);

                        // 3. Persistimos el cambio en el lote
                        await _repository.UpdateAsync(batch);
                    }
                    else
                    {
                        _logger.LogWarning($"No hay lotes activos para el producto ID {item.Id}. No se pudo procesar la devolución de la Orden {orderId}.");
                        // Opcional: Podrías crear un lote "Genérico" o de "Devoluciones" si esto ocurre.
                    }
                }

                if (movements.Any())
                {
                    await _repository.AddRangeAsync(movements);
                }

                return new Response(true, "El inventario ha sido actualizado correctamente.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al procesar devolución");
                return new Response(false, "Error crítico al retornar productos al inventario.");
            }
        }

        // En tu InventoryService
        public async Task<int> GetAvailableStock(int productPresentationId)
        {
            return await _repository.GetQueryable<Batch>()
                .Where(b => b.ProductPresentationId == productPresentationId && b.IsActive)
                .SumAsync(b => b.CurrentQuantity);
        }

        public async Task ReverseOrderInventory(int orderId)
        {
            var movements = await _repository.GetQueryable<Movement>()
        .Where(m => m.Comment.Contains($"(Orden {orderId})") || m.Comment.Contains($"Pedido {orderId}"))
        .ToListAsync();

            foreach (var mov in movements)
            {
                if (mov.BatchId.HasValue)
                {
                    var batch = await _repository.GetByIdAsync<Batch>(mov.BatchId.Value);
                    if (batch != null)
                    {
                        // REVERSA: Si salió, suma. Si entró, resta.
                        if (mov.Operation == Operation.Out) batch.CurrentQuantity += (int)mov.Quantity;
                        else batch.CurrentQuantity -= (int)mov.Quantity;

                        await _repository.UpdateAsync(batch);
                    }
                }

                // BORRADO INDIVIDUAL: Usamos el método que ya conoce tu repositorio
                await _repository.DeleteAsync(mov);
            }
        }
    }
}
