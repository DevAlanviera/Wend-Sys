using System.Collections.Generic;
using System.Threading.Tasks;
using WendlandtVentas.Core.Models.ProductPresentationViewModels;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IInventoryService
    {
        Task<Response> OrderDiscount(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId);
        Task<Response> OrderReturn(IEnumerable<ProductPresentationQuantity> productsPresentations, string email, int orderId);
    }
}