using System.Collections.Generic;
using System.Threading.Tasks;
using WendlandtVentas.Core.DTO;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Models.ProductPresentationViewModels;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IInventoryService
    {
        Task<Response> OrderDiscount(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId);
        Task<Response> OrderReturn(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId);
        // Nuevo método para consultar disponibilidad total de lotes activos
        Task<int> GetAvailableStock(int productPresentationId);

        Task ReverseOrderInventory(int orderId);
        Task VerificarStockBajoAsync(ProductPresentation p);

        Task ProcesarYEnviarReporteMatutinoAsync();

        Task<List<FilaReporteInventario>> ObtenerDatosParaReporteExcelAsync();
    }

}