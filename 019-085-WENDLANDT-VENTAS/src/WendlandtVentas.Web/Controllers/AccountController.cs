using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Web.Extensions;
using WendlandtVentas.Web.Models;
using WendlandtVentas.Web.Models.AccountViewModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize]
    //[Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, true, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.Email);

                if (!user.EmailConfirmed)
                {
                    ModelState.AddModelError(string.Empty, "Favor de confirmar tu correo electrónico.");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Tu usuario se encuentra inactivo.");
                    return View(model);
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(Role.Administrator.ToString()) ||
                    roles.Contains(Role.AdministratorAssistant.ToString()) ||
                    roles.Contains(Role.AdministratorAssistant.ToString()) ||
                    roles.Contains(Role.Sales.ToString()) ||
                    roles.Contains(Role.Storekeeper.ToString()) || 
                    roles.Contains(Role.Distributor.ToString()) ||
                    roles.Contains(Role.Billing.ToString()) ||
                    roles.Contains(Role.BillingAssistant.ToString()) )
                {
                    _logger.LogInformation("User logged in.");

                    return Redirect("/");
                    /*Antes
                    return RedirectToLocal(returnUrl);*/
                }
                else
                {
                    return Unauthorized("No cuentas con los permisos necesarios para ingresar al sistema.");
                }               
            }
           
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Intento de inicio de sesión no válido.");
                return View(model);
            }

            // If we got this far, something failed, redisplay form
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateJsonError(string.Join("; ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage))));

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


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(Login), "Account");
        }
        
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(Login), "Account");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"No se puede cargar al usuario con ID '{userId}'.");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
                await _emailSender.SendEmailAsync(model.Email, "Restablecer contraseña",
                    $"Restablezca su contraseña haciendo clic aquí: <a href='{callbackUrl}'>link</a>");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("Se debe proporcionar un código para restablecer la contraseña.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion Helpers
    }
}