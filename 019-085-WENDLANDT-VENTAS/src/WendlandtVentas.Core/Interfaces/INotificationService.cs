using System.Collections.Generic;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities.Enums;

namespace WendlandtVentas.Core.Interfaces
{
    public interface INotificationService
    {
        Task<bool> NotifyChangeOrderStateAsync(int orderId, OrderStatus orderStatus, string email);
        Task<bool> AddAndSendNotificationByEmail(string email, string title, string message);
        Task<bool> AddAndSendNotificationByRoles(List<Role> roles, string title, string message , string userId, string role);
    }
}