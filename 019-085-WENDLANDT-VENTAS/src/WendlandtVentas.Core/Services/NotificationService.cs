using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models.Enums;

namespace WendlandtVentas.Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IAsyncRepository _repository;
        private readonly IOneSignalService _oneSignalSenderService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationService> _logger;
        public NotificationService(IAsyncRepository repository, IOneSignalService oneSignalSenderService, UserManager<ApplicationUser> userManager, ILogger<NotificationService> logger)
        {
            _repository = repository;
            _oneSignalSenderService = oneSignalSenderService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> NotifyChangeOrderStateAsync(int orderId, OrderStatus orderStatus, string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                var role = (await _userManager.GetRolesAsync(user)).First();
                var title = $"Pedido {orderId}";
                var message = $"Pedido {orderStatus.Humanize().ToLower()} - {user.Name}";
                var roles = new List<Role>() { Role.Administrator };
                var result = new List<bool>();
                var send = false;

                switch (orderStatus)
                {
                    case OrderStatus.New:
                        roles.AddRange(new List<Role>() { Role.Storekeeper, Role.Billing, Role.BillingAssistant });
                        send = await AddAndSendNotificationByRoles(roles, title, message, user.Id, role);
                        result.Add(send);
                        break;
                    case OrderStatus.ReadyDeliver:
                        roles.Add(Role.Billing);
                        roles.Add(Role.BillingAssistant);
                        send = await AddAndSendNotificationByRoles(roles, title, message, user.Id, role);
                        result.Add(send);
                        break;
                        
                    case OrderStatus.Delivered:
                        roles.Add(Role.Storekeeper);
                        send = await AddAndSendNotificationByRoles(roles, title, message, user.Id, role);
                        result.Add(send);
                        break;
                    default:
                        send = await AddAndSendNotificationByRoles(roles, title, message, user.Id, role);
                        result.Add(send);
                        break;
                }

                return result.All(c => c == true);
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error en método --> NotifyChangeOrderStateAsync: {e.Message}");
                return false;
            }
        }

        public async Task<bool> AddAndSendNotificationByRoles(List<Role> roles, string title, string message, string userId, string role)
        {
            try
            {
                var send = false;
                var result = new List<bool>();
                var usersId = new List<string>();

                foreach (var roleUser in roles)
                {
                    var users = _userManager.Users.Where(c => c.UserRoles.Any(d => d.Role.Name == roleUser.ToString())).ToList();
                    usersId.AddRange(users.Select(c => c.Id));
                }

                if (role.Equals(Role.Sales.ToString()))
                    usersId.Add(userId);

                var notification = new Notification(title, message, usersId);
                await _repository.AddAsync(notification);

                foreach (var roleUser in roles)
                {
                    send = await _oneSignalSenderService.SendNotificationAsync(Tag.role, roleUser.ToString(), title, message);
                    result.Add(send);
                }

                return result.All(c => c == true);
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error en método --> AddAndSendNotificationByRoles: {e.Message}");
                return false;
            }
        }
        public async Task<bool> AddAndSendNotificationByEmail(string email, string title, string message)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                var notification = new Notification(title, message, new List<string>() { user.Id });
                await _repository.AddAsync(notification);

                return await _oneSignalSenderService.SendNotificationAsync(Tag.email, email, title, message);
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error en método --> AddAndSendNotificationByEmail: {e.Message}");
                return false;
            }
        }
    }
}
