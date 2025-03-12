using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Syncfusion.EJ2.Base;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial, Billing, BillingAssistant")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SfGridOperations _gridOperations;

        public UsersController(UserManager<ApplicationUser> userManager, SfGridOperations gridOperations)
        {
            _userManager = userManager;
            _gridOperations = gridOperations;
        }

        public IActionResult Index(int id)
        {

            return View();
        }

        [HttpPost]
        public IActionResult GetData([FromBody] DataManagerRequest dm)
        {            
            var dataSource = _userManager.Users.Select(ci => new UserTableModel
            {
                Id = ci.Id,
                Email = ci.Email,
                Name = ci.Name,
                Roles = string.Join(", ", ci.UserRoles.Select(r => r.Role.Name.Humanize()).ToList()),
                IsActive = ci.IsActive,
                CanDisableOrDelete = !User.Identity.Name.Equals(ci.Email)
            }).AsEnumerable();
            var dataResult = _gridOperations.FilterDataSource(dataSource, dm);
           
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }

        public IActionResult AddView()
        {
            ViewData["Action"] = nameof(Add);
            ViewData["ModalTitle"] = "Agregar usuario";
            return PartialView("_CreateEditModal", new UserViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> EditView(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            ViewData["Action"] = nameof(Update);
            ViewData["ModalTitle"] = "Editar usuario";

            return PartialView("_CreateEditModal", new UserViewModel
            {
                Email = user.Email,
                Name = user.Name,
                Roles = await _userManager.GetRolesAsync(user),
                IsActive = user.IsActive,
                Id = user.Id
            });
        }

        public async Task<IActionResult> DeleteView(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return Json(AjaxFunctions.GenerateJsonError("Usuario no encontrado"));

            ViewData["Action"] = nameof(Delete);
            ViewData["ModalTitle"] = "Eliminar usuario";
            ViewData["ModalDescription"] = $"el usuario de {user.Name}";

            return PartialView("_DeleteModal", user.Id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(UserViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateJsonError(string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage))));

            if (model.Roles.Count == 0)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Al menos debe tener un rol."));

            var usuarioExistente = await _userManager.FindByEmailAsync(model.Email);

            if (usuarioExistente != null)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error,
                    "El correo electrónico ya está registrado."));

            IdentityResult result;

            try
            {
                result = await _userManager.CreateAsync(new ApplicationUser
                {
                    Name = model.Name,
                    Email = model.Email,
                    UserName = model.Email,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true,
                }, model.Password);

                if (result.Succeeded)
                {
                    var usuario = await _userManager.FindByEmailAsync(model.Email);
                    await _userManager.AddToRolesAsync(usuario, model.Roles);
                }
            }
            catch (Exception ex)
            {
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, ex.Message));
            }

            return Json(result.Succeeded
                ? AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Usuario añadido correctamente")
                : AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error,
                    result.Errors.Aggregate("", (c, e) => c + e.Description + "<br>")));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UserViewModel model)
        {
            if (model.Roles.Count == 0)
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Al menos debe tener un rol."));

            var user = await _userManager.FindByIdAsync(model.Id);

            user.Name = model.Name;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRolesAsync(user, model.Roles);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Usuario editado correctamente."));
            }

            return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo editar el usuario."));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded
                ? Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Usuario editado correctamente."))
                : Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo cambiar el estado."));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user != null && user.Name == "Monobits")
                return Json(
                    AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se puede eliminar el usuario Monobits"));

            var result = await _userManager.DeleteAsync(user);

            return result.Succeeded
                ? Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Usuario eliminado correctamente."))
                : Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo eliminar el usuario."));
        }
    }
}