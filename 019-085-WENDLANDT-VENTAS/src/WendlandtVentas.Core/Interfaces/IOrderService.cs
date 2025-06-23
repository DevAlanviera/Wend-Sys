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

        //Este metodo es para actualizar el monto real de la orden
        Task<Response> ActualizarTotalAsync(int orderId, decimal nuevoTotal, bool precioEspecial, string currrentUserEmail);

        //Agregamos este metodo para poder enviar el correo
        Task<bool> EnviarEstadoCuentaAsync(int orderId);


        //IQueryable<Order> FilterValues(FilterViewModel filter);

        Task<List<Order>> FilterValues(FilterViewModel filter);
        Task<List<SelectListItem>> GetInvoiceRemissionNumbersAsync();
    }
}