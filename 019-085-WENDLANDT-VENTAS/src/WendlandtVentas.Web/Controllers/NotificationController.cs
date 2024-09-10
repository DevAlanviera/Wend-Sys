using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using Syncfusion.EJ2.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Specifications.MovementSpecifications;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Web.Extensions;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial, Billing")]
    public class NotificationController : Controller
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly SfGridOperations _sfGridOperations;
        private readonly IAsyncRepository _repository;
        private readonly IOneSignalService _oneSignalSenderService;
        private readonly UserManager<ApplicationUser> _userManager;
        public NotificationController(ILogger<NotificationController> logger, SfGridOperations sfGridOperations, IAsyncRepository repository, IOneSignalService oneSignalSenderService, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _sfGridOperations = sfGridOperations;
            _repository = repository;
            _oneSignalSenderService = oneSignalSenderService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataManagerRequest dm)
        {            
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var notifications = await _repository.ListExistingAsync(new NotificationFilterByRolSpecification(user.Id));
            var notificationDataSource = notifications.OrderByDescending(c => c.Id).Select(c => new NotificationTableModel
            {
                Id = c.Id,
                Title = c.Notification.Title,
                Message = c.Notification.Message,
                CreatedAt = c.CreatedAt.FormatDateShortMx(),
                Active = c.Active
            });
            var dataResult = _sfGridOperations.FilterDataSource(notificationDataSource, dm);

            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var notification = await _repository.GetByIdAsync<NotificationUser>(id);
                notification.ToggleStatus();
                await _repository.UpdateAsync(notification);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Estado cambiado"));
            }
            catch (Exception e)
            {
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo cambiar el estado"));
            }
        }
    }
}