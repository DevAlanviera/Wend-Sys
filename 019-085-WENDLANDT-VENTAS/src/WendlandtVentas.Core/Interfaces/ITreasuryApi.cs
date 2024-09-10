using System.Threading.Tasks;
using WendlandtVentas.Core.Models.OrderViewModels;

namespace WendlandtVentas.Core.Interfaces
{
    public interface ITreasuryApi
    {
        public Task<(bool IsSuccess, string Response)> AddIncomeAsync(OrdersIncomeDto income);
        public Task<(bool IsSuccess, string Response)> GetBalancesAsync(OrdersIncomeDto income);
    }
}