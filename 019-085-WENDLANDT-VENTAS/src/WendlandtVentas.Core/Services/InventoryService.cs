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
                            $"Pedido {orderId} - Lote: {batch.BatchNumber}",
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

                foreach (var productPresentation in productsPresentations)
                {
                    // 1. Buscamos los movimientos de SALIDA originales de este pedido
                    // Esto es vital para saber a qué Lotes regresar la cerveza
                    var originalMovements = await _repository.GetQueryable<Movement>()
                        .Where(m => m.ProductPresentationId == productPresentation.Id
                               && m.Operation == Operation.Out
                               && m.Comment.Contains($"Pedido {orderId}"))
                        .ToListAsync();

                    if (!originalMovements.Any()) continue;

                    foreach (var move in originalMovements)
                    {
                        // 2. Buscamos el lote específico
                        var batch = await _repository.GetByIdAsync<Batch>(move.BatchId.Value);

                        if (batch != null)
                        {
                            // 3. Regresamos la cantidad al lote
                            batch.CurrentQuantity += move.Quantity;

                            // 4. REACTIVACIÓN: Si estaba en 0 (isActive = 0), lo volvemos a activar
                            if (batch.CurrentQuantity > 0)
                            {
                                batch.IsActive = true;
                            }

                            await _repository.UpdateAsync(batch);

                            // 5. Creamos el movimiento de entrada para la bitácora
                            var returnMovement = new Movement(
                                productPresentation.Id,
                                move.Quantity,
                                Operation.In,
                                batch.CurrentQuantity,
                                $"Devolución Pedido {orderId} - Reingreso Lote {batch.BatchNumber}",
                                user.Id
                            );
                            returnMovement.BatchId = batch.Id;
                            movements.Add(returnMovement);
                        }
                    }
                }

                if (movements.Any())
                {
                    await _repository.AddRangeAsync(movements);
                }

                return new Response(true, "Retorno a inventario y lotes realizado con éxito");
            }
            catch (Exception e)
            {
                return new Response(false, $"Error al retornar: {e.Message}");
            }
        }

        // En tu InventoryService
        public async Task<int> GetAvailableStock(int productPresentationId)
        {
            return await _repository.GetQueryable<Batch>()
                .Where(b => b.ProductPresentationId == productPresentationId && b.IsActive)
                .SumAsync(b => b.CurrentQuantity);
        }
    }
}
