using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Web.Models.InventoryViewModels;
using WendlandtVentas.Core.Specifications.InventorySpecifications;

namespace WendlandtVentas.Core.Services
{
    public class ClientInventoryReservationService : IClientInventoryReservationService
    {
        private readonly IAsyncRepository _repository;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<ClientInventoryReservationService> _logger;

        public ClientInventoryReservationService(IAsyncRepository repository, IInventoryService inventoryService, ILogger<ClientInventoryReservationService> logger)
        {
            _repository = repository;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        public async Task<int> GetTotalAvailableStockAsync(int productPresentationId)
        {
            // Stock físico total (sin importar apartados)
            return await _inventoryService.GetAvailableStock(productPresentationId);
        }

        public async Task<int> GetAvailableStockForClientAsync(int clientId, int productPresentationId)
        {
            // Stock físico total
            var physicalStock = await _inventoryService.GetAvailableStock(productPresentationId);

            // Sumar apartados de otros clientes (excluyendo al cliente actual)
            var spec = new ClientInventoryReservationExcludeClientSpecification(productPresentationId, clientId);
            var otherReservations = await _repository.ListAsync(spec);
            var otherReservationsSum = otherReservations.Sum(r => r.AvailableQuantity);

            // Stock disponible para este cliente = stock físico - apartados de otros
            return physicalStock - otherReservationsSum;
        }

        public async Task<Response> EditReservationAsync(int reservationId, int newQuantity, string notes, string updatedBy)
        {
            try
            {
                var reservation = await _repository.GetByIdAsync<ClientInventoryReservation>(reservationId);
                if (reservation == null)
                    return new Response(false, "Apartado no encontrado");

                if (reservation.Status != "Active")
                    return new Response(false, $"No se puede editar un apartado con estado '{reservation.Status}'");

                // Verificar que la nueva cantidad no exceda el stock disponible
                var usedQuantity = reservation.UsedQuantity;
                if (newQuantity < usedQuantity)
                    return new Response(false, $"No se puede reducir a menos de lo ya usado ({usedQuantity})");

                // Calcular stock disponible para este cliente (incluyendo este apartado)
                var physicalStock = await _inventoryService.GetAvailableStock(reservation.ProductPresentationId);
                var otherReservations = await _repository.ListAsync(
                    new ClientInventoryReservationExcludeClientSpecification(reservation.ProductPresentationId, reservation.ClientId));
                var otherReservationsSum = otherReservations.Sum(r => r.AvailableQuantity);

                var availableForClient = physicalStock - otherReservationsSum;
                var additionalNeeded = newQuantity - reservation.ReservedQuantity;

                if (additionalNeeded > 0 && additionalNeeded > availableForClient)
                    return new Response(false, $"No hay suficiente stock. Disponible: {availableForClient}, Necesitas {additionalNeeded} adicionales");

                reservation.ReservedQuantity = newQuantity;
                reservation.Notes = notes;
                reservation.UpdatedAt = DateTime.Now;
                reservation.UpdatedBy = updatedBy;

                await _repository.UpdateAsync(reservation);

                _logger.LogInformation($"Apartado {reservationId} editado: Cantidad de {reservation.ReservedQuantity} a {newQuantity}");

                return new Response(true, "Apartado editado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar apartado");
                return new Response(false, "Error al editar apartado");
            }
        }

        public async Task<Response> CreateReservationAsync(int clientId, int productPresentationId, int quantity, string notes, string createdBy)
        {
            try
            {
                // Validar que no exceda el stock disponible
                var availableForClient = await GetAvailableStockForClientAsync(clientId, productPresentationId);

                if (quantity > availableForClient)
                {
                    return new Response(false, $"No hay suficiente stock. Disponible: {availableForClient}, Solicitado: {quantity}");
                }

                var reservation = new ClientInventoryReservation
                {
                    ClientId = clientId,
                    ProductPresentationId = productPresentationId,
                    ReservedQuantity = quantity,
                    UsedQuantity = 0,
                    Status = "Active",
                    Notes = notes,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now
                };

                await _repository.AddAsync(reservation);

                _logger.LogInformation($"Apartado creado: Cliente {clientId}, Producto {productPresentationId}, Cantidad {quantity}");

                return new Response(true, "Apartado creado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear apartado");
                return new Response(false, "Error al crear apartado");
            }
        }

        public async Task<Response> UseReservationAsync(int clientId, int productPresentationId, int quantity, string orderId)
        {
            try
            {
                // Buscar apartados activos del cliente (FIFO - primero el más antiguo)
                var spec = new ClientInventoryReservationByClientSpecification(clientId, productPresentationId);
                var reservations = await _repository.ListAsync(spec);

                var activeReservations = reservations
                    .Where(r => r.Status == "Active" && !r.IsDeleted && r.AvailableQuantity > 0)
                    .OrderBy(r => r.CreatedAt)
                    .ToList();

                int remainingToUse = quantity;

                foreach (var reservation in activeReservations)
                {
                    int available = reservation.AvailableQuantity;
                    int toUse = Math.Min(remainingToUse, available);

                    reservation.UsedQuantity += toUse;
                    remainingToUse -= toUse;

                    if (reservation.AvailableQuantity == 0)
                    {
                        reservation.Status = "Completed";
                    }

                    await _repository.UpdateAsync(reservation);

                    if (remainingToUse == 0) break;
                }

                if (remainingToUse > 0)
                {
                    return new Response(false, $"El cliente solo tiene {quantity - remainingToUse} unidades apartadas. Faltan {remainingToUse}");
                }

                _logger.LogInformation($"Apartado usado: Cliente {clientId}, Producto {productPresentationId}, Cantidad {quantity}, Orden {orderId}");

                return new Response(true, "Apartado utilizado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al usar apartado");
                return new Response(false, "Error al usar apartado");
            }
        }

        public async Task<Response> CancelReservationAsync(int reservationId, string cancelledBy)
        {
            try
            {
                var reservation = await _repository.GetByIdAsync<ClientInventoryReservation>(reservationId);

                if (reservation == null)
                {
                    return new Response(false, "Apartado no encontrado");
                }

                if (reservation.Status != "Active")
                {
                    return new Response(false, $"No se puede cancelar un apartado con estado '{reservation.Status}'");
                }

                reservation.Status = "Cancelled";
                reservation.UpdatedAt = DateTime.Now;
                reservation.UpdatedBy = cancelledBy;

                await _repository.UpdateAsync(reservation);

                _logger.LogInformation($"Apartado {reservationId} cancelado por {cancelledBy}. Cliente: {reservation.ClientId}, Producto: {reservation.ProductPresentationId}, Cantidad: {reservation.AvailableQuantity}");

                return new Response(true, "Apartado cancelado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cancelar apartado {reservationId}");
                return new Response(false, "Error al cancelar apartado");
            }
        }

        public async Task<List<ClientInventoryReservation>> GetActiveReservationsByProductAsync(int productPresentationId)
        {
            var spec = new ClientInventoryReservationByProductSpecification(productPresentationId);
            var reservations = await _repository.ListAsync(spec);

            // Log para verificar si Client está cargado
            foreach (var r in reservations)
            {
                Console.WriteLine($"Reservation {r.Id}: Client is {(r.Client == null ? "NULL" : r.Client.Name)}");
            }

            Console.WriteLine($"GetActiveReservationsByProductAsync - ProductPresentationId: {productPresentationId}, Total: {reservations.Count}");

            var active = reservations
                .Where(r => r.Status == "Active" && !r.IsDeleted && r.AvailableQuantity > 0)
                .ToList();

            Console.WriteLine($"  Active only: {active.Count}");

            return active;
        }

        public async Task<ClientInventoryReservation> GetActiveReservationsByClientAndProductAsync(int clientId, int productPresentationId)
        {
            var spec = new ClientInventoryReservationByClientSpecification(clientId, productPresentationId);
            var reservations = await _repository.ListAsync(spec);
            return reservations.FirstOrDefault(r => r.Status == "Active" && !r.IsDeleted && r.AvailableQuantity > 0);
        }

        public async Task<List<ClientInventoryReservation>> GetUsedReservationsByOrderAsync(int orderId, int productPresentationId)
        {
            // Este método debe obtener las reservaciones que fueron usadas en esta orden
            // Dependiendo de cómo registres el uso de apartados en la orden

            var spec = new ClientInventoryReservationByProductSpecification(productPresentationId);
            var reservations = await _repository.ListAsync(spec);

            // Filtrar reservaciones que tengan un comentario o referencia a esta orden
            // Esto depende de cómo hayas implementado el registro del uso
            return reservations
                .Where(r => r.UsedQuantity > 0 && r.UpdatedBy?.Contains(orderId.ToString()) == true)
                .ToList();
        }

    }
}
