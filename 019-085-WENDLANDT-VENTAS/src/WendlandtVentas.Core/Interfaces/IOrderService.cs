using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IOrderService
    {
        Task<Response> AddOrderAsync(OrderViewModel orderViewModel, string currrentUserEmail);
        Task<Response> UpdateOrderAsync(OrderViewModel orderViewModel, string currrentUserEmail);

        Task<Response> ActualizarTotalAsync(int orderId, decimal nuevoTotal);
        

        //IQueryable<Order> FilterValues(FilterViewModel filter);

        Task<List<Order>> FilterValues(FilterViewModel filter);
        Task<List<SelectListItem>> GetInvoiceRemissionNumbersAsync();
    }
}