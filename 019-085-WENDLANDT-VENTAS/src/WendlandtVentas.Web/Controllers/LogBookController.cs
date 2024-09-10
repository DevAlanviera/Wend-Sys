using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Monobits.SharedKernel.Interfaces;
using Newtonsoft.Json;
using Syncfusion.EJ2.Base;
using WendlandtVentas.Core;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Models;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models;

namespace Agenda.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial")]
    public class LogBookController : Controller
    {

        private readonly IAsyncRepository _repository;
        private readonly IdentityServerSettings _identityServerSettings;
        private readonly ILogBookService _logBookService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SfGridOperations _sfGridOperations;

        public LogBookController(IAsyncRepository repository, UserManager<ApplicationUser> userManager, ILogBookService logBookService, SfGridOperations sfGridOperations, IOptions<IdentityServerSettings> identityServerSettings)
        {
            _repository = repository;
            _userManager = userManager;
            _logBookService = logBookService;
            _sfGridOperations = sfGridOperations;
            _identityServerSettings = identityServerSettings.Value;
        }

        [HttpGet]
        public IActionResult Index(LogBookViewModel filtros)
        {

            var users = _userManager.Users.ToList();
            var bitModel = new LogBookViewModel();
            List<SelectListItem> usersSubSelect = users.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToList();
            usersSubSelect.Add(new SelectListItem { Value = "0", Text = "Todos" });
            bitModel.Users = usersSubSelect.OrderBy(c => c.Value).ToList();
            bitModel.ActionType = filtros.ActionType;
            bitModel.UserId = filtros.UserId;
            bitModel.RegisterDate = filtros.RegisterDate;
            return View(bitModel);
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataManagerRequest dm, LogBookFilterModel filtros)
        {
            filtros.Take = dm.Take;
            filtros.Skip = dm.Skip;
            filtros.ClientId = _identityServerSettings.ClientId;
            var data = await _logBookService.GetData(filtros);

            var dataSource = data.Models
            .Select(m => new LogBookModel
             {
                 Id = m.Id,
                 ActionType = m.ActionType,
                 ClientId = m.ClientId,
                 Color = m.Color,
                 Target = m.Target,
                 User = m.User,
                 UserId = m.UserId,
                 Date = DateTime.Parse(m.RegisterDate, CultureInfo.CreateSpecificCulture("es-ES"))
             }).OrderByDescending(l => l.Date);

            return dm.RequiresCounts ? new JsonResult(new { result = dataSource, data.Count }) : new JsonResult(dataSource);
        }

        [HttpGet]
        public async Task<IActionResult> ViewObject(int id)
        {
            var clientId = _identityServerSettings.ClientId;
            var log = await _logBookService.GetLog(id, clientId);

            var vm = new LogBookViewModel
            {
                Content = JsonConvert.DeserializeObject<Dictionary<string, object>>(log.Json),
                ActionType = log.ActionType,
                Target = log.Target,
                User = log.User
            };

            return PartialView("_ObjectModal", vm);
        }
    }
}