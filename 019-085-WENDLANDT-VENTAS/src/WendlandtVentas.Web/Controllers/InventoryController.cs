using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using Syncfusion.EJ2.Base;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;
using WendlandtVentas.Core.Specifications; 
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Web.Extensions;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.InventoryViewModels;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial, Storekeeper, Billing, BillingAssistant")]
    public class InventoryController : Controller
    {
        private readonly IAsyncRepository _repository;
        private readonly ILogger<InventoryController> _logger;
        private readonly SfGridOperations _sfGridOperations;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryController(IAsyncRepository repository, ILogger<InventoryController> logger, SfGridOperations sfGridOperations, UserManager<ApplicationUser> userManager)
        {
            _repository = repository;
            _logger = logger;
            _sfGridOperations = sfGridOperations;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(FilterViewModel model)
        {
            var products = await _repository.ListAllExistingAsync<Product>();
            var presentations = await _repository.ListAllExistingAsync<Presentation>();

            model.ProductId = model.ProductId;
            model.PresentationId = model.PresentationId;
            model.Products = new SelectList(products.Select(x => new { Value = x.Id, Text = $"{x.Name}" }), "Value", "Text");
            model.Presentations = new SelectList(presentations.Select(x => new { Value = x.Id, Text = $"{x.Name} ({x.Liters.FormatCommasNullableTwoDecimals()} lts.)" }), "Value", "Text");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataManagerRequest dm, FilterViewModel model)
        {
            var filters = new Dictionary<string, int?>
    {
        { nameof(model.ProductId), model.ProductId },
        { nameof(model.PresentationId), model.PresentationId }
    };

            // Obtenemos la lista base usando la especificación existente
            var data = await _repository.ListExistingAsync<ProductPresentation>(new ListByFiltersProductPresentationSpecification(filters));

            var dataSource = data
                .OrderBy(c => c.Product.Name)
                .Select(c =>
                    new InventoryTableModel
                    {
                        Id = c.Id,
                        Name = c.Product.Name,
                        Presentation = $"{c.Presentation.Name} {c.Presentation.Liters.FormatCommasNullableTwoDecimals()} lts.",
                        Liters = c.Presentation.Liters * (c.Movements.Where(m => !m.IsDeleted)?.LastOrDefault()?.QuantityCurrent ?? 0),
                        Stock = c.Movements.Where(m => !m.IsDeleted)?.LastOrDefault()?.QuantityCurrent.ToString() ?? "0",

                        // --- NUEVA SECCIÓN PARA LOTES ---
                        Batches = c.Batches
                            .Where(b => !b.IsDeleted && b.CurrentQuantity > 0)
                            .OrderBy(b => b.ExpiryDate) // Orden FIFO para la vista
                            .Select(b => new {
                                b.BatchNumber,
                                b.CurrentQuantity,
                                ExpiryDateFormatted = b.ExpiryDate.ToString("dd/MM/yyyy"),
                                IsExpired = b.ExpiryDate < DateTime.Now,
                                StatusText = b.ExpiryDate < DateTime.Now ? "Vencido" :
                                             (b.ExpiryDate - DateTime.Now).TotalDays < 30 ? "Próximo a vencer" : "En buen estado",
                                StatusColor = b.ExpiryDate < DateTime.Now ? "badge-danger" :
                                              (b.ExpiryDate - DateTime.Now).TotalDays < 30 ? "badge-warning" : "badge-success"
                            }).ToList()
                        // --------------------------------
                    });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }

        [HttpGet]
        public IActionResult In(int id)
        {
            ViewData["Action"] = nameof(In);
            ViewData["ModalTitle"] = "Agregar entrada";

            return PartialView("_AddInOutModal", new InOutViewModel() { ProductPresentationId = id });
        }

        [HttpGet]
        public IActionResult Out(int id)
        {
            ViewData["Action"] = nameof(Out);
            ViewData["ModalTitle"] = "Agregar salida";

            return PartialView("_AddInOutModal", new InOutViewModel() { ProductPresentationId = id });
        }

        [HttpGet]
        public async Task<IActionResult> Adjustment(int id)
        {
            ViewData["Action"] = nameof(Adjustment);
            ViewData["ModalTitle"] = "Realizar ajuste";
            var productPresentation = await _repository.GetAsync(new ProductPresentationExtendedSpecification(id));
            var quantityCurrent = productPresentation?.Movements.Where(c => !c.IsDeleted)?.LastOrDefault()?.QuantityCurrent ?? 0;
            var model = new InOutViewModel()
            {
                ProductPresentationId = id,
                Quantity = quantityCurrent,
                IsAdjustment = true,
                Comment = string.Empty
            };

            return PartialView("_AddInOutModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> In(InOutViewModel model)
        {
            if (!ModelState.IsValid) return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));

            try
            {
                if (model.Quantity <= 0)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Ingrese una cantidad mayor a cero"));

                var productPresentation = await _repository.GetAsync(new ProductPresentationExtendedSpecification(model.ProductPresentationId));

                if (productPresentation == null)
                    return NotFound();

                var user = await _userManager.FindByNameAsync(User.Identity.Name);

                var batch = await _repository.GetAsync<Batch>(new BatchSpecification(model.ProductPresentationId, model.BatchNumber));

                if (batch == null)
                {
                    // 2. Si no existe, creamos el nuevo lote
                    batch = new Batch
                    {
                        BatchNumber = model.BatchNumber,
                        ExpiryDate = model.ExpiryDate.Value,
                        ProductPresentationId = model.ProductPresentationId,
                        InitialQuantity = model.Quantity,
                        CurrentQuantity = model.Quantity,
                        CreatedAt = DateTime.Now
                    };
                    await _repository.AddAsync(batch);
                }
                else
                {
                    // 3. Si ya existe, actualizamos la cantidad actual
                    batch.CurrentQuantity += model.Quantity;
                    await _repository.UpdateAsync(batch);
                }

                var quantityCurrent = productPresentation.Movements.Where(c => !c.IsDeleted)?.LastOrDefault()?.QuantityCurrent ?? 0;
                var newMov = new Movement(model.ProductPresentationId, model.Quantity, Operation.In, quantityCurrent, model.Comment, user.Id)
                {
                    BatchId = batch.Id
                };

                await _repository.AddAsync(newMov);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Entrada guardada"));
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar la entrada"));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Out(InOutViewModel model)
        {
            if (!ModelState.IsValid) return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));

            try
            {
                if (model.Quantity <= 0)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Ingrese una cantidad mayor a cero"));

                var productPresentation = await _repository.GetAsync<ProductPresentation>(new ProductPresentationExtendedSpecification(model.ProductPresentationId));

                if (productPresentation == null)
                    return NotFound();

                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                var quantityCurrent = productPresentation.Movements.Where(c => !c.IsDeleted)?.LastOrDefault()?.QuantityCurrent ?? 0;

                if (model.Quantity > quantityCurrent)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Ingrese una cantidad menor o igual que " + quantityCurrent));

                var newMov = new Movement(model.ProductPresentationId, model.Quantity, Operation.Out, quantityCurrent, model.Comment, user.Id);

                await _repository.AddAsync(newMov);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Salida guardada"));
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar la salida"));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjustment(InOutViewModel model)
        {
            if (!ModelState.IsValid) return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));

            try
            {
                if (model.Quantity < 0)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Ingrese una cantidad mayor o igual a cero"));

                var productPresentation = await _repository.GetAsync(new ProductPresentationExtendedSpecification(model.ProductPresentationId));

                if (productPresentation == null)
                    return NotFound();

                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                var quantityCurrent = productPresentation.Movements.Where(c => !c.IsDeleted)?.LastOrDefault()?.QuantityCurrent ?? 0;
                var newMov = new Movement(model.ProductPresentationId, model.Quantity, Operation.Adjustment, quantityCurrent, model.Comment, user.Id);

                await _repository.AddAsync(newMov);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Ajuste realizado"));
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo realizar el ajuste"));
            }
        }
    }
}