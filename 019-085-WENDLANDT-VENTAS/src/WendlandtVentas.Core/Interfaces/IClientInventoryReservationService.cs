using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IClientInventoryReservationService
    {
        Task<Response> CreateReservationAsync(int clientId, int productPresentationId, int quantity, string notes, string createdBy);
        Task<Response> UseReservationAsync(int clientId, int productPresentationId, int quantity, string orderId);
        Task<int> GetAvailableStockForClientAsync(int clientId, int productPresentationId);
        Task<int> GetTotalAvailableStockAsync(int productPresentationId);
        Task<Response> CancelReservationAsync(int reservationId, string cancelledBy);
    }
}
