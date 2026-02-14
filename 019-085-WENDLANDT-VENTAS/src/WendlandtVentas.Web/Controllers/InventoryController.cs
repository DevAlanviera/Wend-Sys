using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using OpenXmlPowerTools.HtmlToWml.CSS;
using Syncfusion.EJ2.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Specifications; 
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;
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

            // La especificación debe tener el .AddInclude(x => x.Batches) para que esto funcione
            var data = await _repository.ListExistingAsync<ProductPresentation>(new ListByFiltersProductPresentationSpecification(filters));

            var dataSource = data
                .OrderBy(c => c.Product.Name)
                .Select(c =>
                {
                    // 1. Filtramos los lotes activos y con existencias para el stock real
                    var activeBatches = c.Batches
                        .Where(b => b.IsActive && !b.IsDeleted && b.CurrentQuantity > 0)
                        .OrderBy(b => b.ExpiryDate)
                        .ToList();

                    // 2. Calculamos el stock sumando las cantidades enteras de los lotes
                    int realStock = activeBatches.Sum(b => b.CurrentQuantity);

                    return new InventoryTableModel
                    {
                        Id = c.Id,
                        Name = c.Product.Name,
                        Presentation = $"{c.Presentation.Name} {c.Presentation.Liters.FormatCommasNullableTwoDecimals()} lts.",

                        // Calculamos litros totales basados en el stock de lotes
                        Liters = (double)c.Presentation.Liters * realStock,
                        Stock = realStock.ToString(),

                        // 3. Mapeamos la lista de lotes para el detailTemplate del Grid
                        Batches = activeBatches.Select(b => new BatchRowModel
                        {
                            BatchNumber = b.BatchNumber,
                            CurrentQuantity = b.CurrentQuantity, // Mantenemos como int
                            ExpiryDateFormatted = b.ExpiryDate.ToString("dd/MM/yyyy"),
                            StatusText = b.ExpiryDate < DateTime.Now ? "Vencido" : "En buen estado",
                            StatusColor = b.ExpiryDate < DateTime.Now ? "badge-danger" : "badge-success"
                        }).ToList()
                    };
                });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);

            // Resolvemos la ambigüedad de tipo para evitar errores de compilación CS8957
            return Json(dm.RequiresCounts
                ? (object)new { result = dataResult.DataResult, dataResult.Count }
                : (object)dataResult.DataResult);
        }

        [HttpGet]
        public IActionResult In(int id)
        {
            ViewData["Action"] = nameof(In);
            ViewData["ModalTitle"] = "Agregar entrada";

            // Inicializamos el modelo con una lista vacía para que el Dropdown de la vista compartida no falle
            var model = new InOutViewModel()
            {
                ProductPresentationId = id,
                IsAdjustment = false, // <--- ASIGNACIÓN EXPLÍCITA AQUÍ
                AvailableBatches = new List<SelectListItem>(), // <--- Vital para evitar el error de ViewData
                Quantity = 0,
                Comment = string.Empty
            };

            return PartialView("_AddInOutModal", model);
        }

        [HttpGet]
        public async Task<IActionResult> Out(int id)
        {
            ViewData["Action"] = nameof(Out);
            ViewData["ModalTitle"] = "Agregar salida";

            // Usamos la especificación para traer los lotes de la BD
            var batches = await _repository.ListAsync(new BatchSpecification(id));

            var model = new InOutViewModel()
            {
                ProductPresentationId = id,
                IsAdjustment = false, // <--- ASIGNACIÓN EXPLÍCITA AQUÍ
                AvailableBatches = batches.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"{b.BatchNumber} (Disp: {b.CurrentQuantity})"
                }).ToList()
            };

            return PartialView("_AddInOutModal", model);
        }

        [HttpGet]
        public async Task<IActionResult> Adjustment(int id)
        {
            ViewData["Action"] = nameof(Adjustment);
            ViewData["ModalTitle"] = "Realizar ajuste de inventario";

            var batches = await _repository.ListAsync(new BatchSpecification(id));

            var model = new InOutViewModel()
            {
                ProductPresentationId = id,
                IsAdjustment = true,
                AvailableBatches = batches.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"{b.BatchNumber} - Actual: {b.CurrentQuantity}"
                }).ToList()
            };

            return PartialView("_AddInOutModal", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchDetails(int id)
        {
            var batch = await _repository.GetByIdAsync<Batch>(id);
            if (batch == null) return NotFound();

            return Json(new
            {
                currentQuantity = batch.CurrentQuantity,
                batchNumber = batch.BatchNumber,
                expiryDate = batch.ExpiryDate
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> In(InOutViewModel model)
        {

            // 1. Validación manual: El BatchNumber es obligatorio solo para Entradas
            if (string.IsNullOrWhiteSpace(model.BatchNumber))
            {
                ModelState.AddModelError(nameof(model.BatchNumber), "El número de lote es obligatorio para registrar una entrada.");
            }
            // Validación manual: Si es entrada, la fecha DEBE existir
            if (!model.ExpiryDate.HasValue)
            {
                ModelState.AddModelError("ExpiryDate", "La fecha de caducidad es obligatoria para nuevas entradas.");
            }

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
            if (!ModelState.IsValid) {
                var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { status = "error", message = string.Join(" | ", errores) });
            }// Esto te dirá exactamente qué campo falla en la consola del navegador
                

            try
            {
                if (model.Quantity <= 0)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Ingrese una cantidad mayor a cero"));

                // 1. Validar que se seleccionó un lote
                if (!model.BatchId.HasValue)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Debe seleccionar un lote de origen"));

                var productPresentation = await _repository.GetAsync<ProductPresentation>(new ProductPresentationExtendedSpecification(model.ProductPresentationId));
                if (productPresentation == null) return NotFound();

                // 2. Obtener el lote específico para descontar
                var batch = await _repository.GetByIdAsync<Batch>(model.BatchId.Value);
                if (batch == null)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "El lote seleccionado no existe"));

                // 3. Validar existencia EN EL LOTE
                if (model.Quantity > batch.CurrentQuantity)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, $"El lote {batch.BatchNumber} solo tiene {batch.CurrentQuantity} disponibles"));

                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                int quantityCurrentGlobal = productPresentation.Movements.Where(c => !c.IsDeleted)?.LastOrDefault()?.QuantityCurrent ?? 0;

                // 4. Actualizar el stock del Lote
                batch.CurrentQuantity -= model.Quantity;
                batch.UpdatedAt = DateTime.Now;

                // Si el lote llega a cero, aplicar borrado lógico
                if (batch.CurrentQuantity <= 0)
                {
                    batch.IsActive = false;
                }
                await _repository.UpdateAsync(batch);

                // 5. Registrar el movimiento vinculado al lote
                // Nota: Enviamos model.Quantity como positivo porque el constructor de Movement 
                // probablemente maneja el signo según el Operation.Out interno
                var newMov = new Movement(
                    model.ProductPresentationId,
                    model.Quantity,
                    Operation.Out,
                    quantityCurrentGlobal,
                    model.Comment,
                    user.Id,
                    batch.Id // Pasamos el ID del lote
                );

                await _repository.AddAsync(newMov);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Salida guardada correctamente"));
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error en Out: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar la salida"));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjustment(InOutViewModel model)
        {

            if (!ModelState.IsValid) {
                var error = string.Join(" | ", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));
                // ESTO VA A APARECER EN TU ALERTA ROJA
                return Json(AjaxFunctions.GenerateJsonError("ERROR REAL: " + error));
            }

            try
            {
                if (model.Quantity < 0)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Ingrese una cantidad mayor o igual a cero"));

                // 1. Validar que se seleccionó un lote
                if (!model.BatchId.HasValue)
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Debe seleccionar un lote para ajustar"));


                var productPresentation = await _repository.GetAsync(new ProductPresentationExtendedSpecification(model.ProductPresentationId));
                if (productPresentation == null) return NotFound();

                // 2. Obtener el lote específico
                var batch = await _repository.GetByIdAsync<Batch>(model.BatchId.Value);
                if (batch == null) return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "El lote seleccionado no existe"));

                var user = await _userManager.FindByNameAsync(User.Identity.Name);

                // 3. Lógica de Inventario:
                // Calculamos la diferencia (Delta) para el movimiento histórico
                // Si el lote tiene 100 y el usuario pone 80, el movimiento es de -20.
                int difference = model.Quantity - batch.CurrentQuantity;
                var quantityGlobalCurrent = productPresentation.Movements.Where(c => !c.IsDeleted)?.LastOrDefault()?.QuantityCurrent ?? 0;


                // 4. Actualizar el Lote físicamente
                batch.CurrentQuantity = model.Quantity;
                batch.ExpiryDate = model.ExpiryDate ?? batch.ExpiryDate;
                batch.UpdatedAt = DateTime.Now;

                if (batch.CurrentQuantity <= 0)
                {
                    batch.IsActive = false;
                }

                await _repository.UpdateAsync(batch);

                // 5. Crear el movimiento SOLO si hubo un cambio real en la cantidad
                if (difference != 0)
                {
                    var newMov = new Movement(
                        model.ProductPresentationId,
                        difference,
                        Operation.Adjustment,
                        quantityGlobalCurrent,
                        model.Comment,
                        user.Id,
                        batch.Id
                    );
                    await _repository.AddAsync(newMov);
                }
                else if (model.Comment != null)
                {
                    // Opcional: Registrar un log simple si solo cambió la fecha pero hay un comentario
                    _logger.LogInformation($"Se actualizó la fecha del lote {batch.Id} sin cambios en cantidad.");
                }

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Ajuste realizado correctamente"));
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo realizar el ajuste"));
            }
        }
    }
}