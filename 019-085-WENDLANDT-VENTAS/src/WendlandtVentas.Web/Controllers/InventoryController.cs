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
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Specifications; 
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Infrastructure.Services;
using WendlandtVentas.Web.Extensions;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.InventoryViewModels;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial, Storekeeper, Billing, BillingAssistant")]
    public class InventoryController : Controller
    {
        private readonly IExcelReadService _excelReaderService;
        private readonly IAsyncRepository _repository;
        private readonly ILogger<InventoryController> _logger;
        private readonly SfGridOperations _sfGridOperations;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IInventoryService _inventoryService;

        public InventoryController(IAsyncRepository repository, ILogger<InventoryController> logger, SfGridOperations sfGridOperations, UserManager<ApplicationUser> userManager,
            IInventoryService inventoryService, IExcelReadService excelReaderService)
        {
            _repository = repository;
            _logger = logger;
            _sfGridOperations = sfGridOperations;
            _userManager = userManager;
            _inventoryService = inventoryService;
            _excelReaderService = excelReaderService;
        }

        public async Task<IActionResult> Index(FilterViewModel model)
        {
            var products = await _repository.ListAllExistingAsync<Product>();
            var presentations = await _repository.ListAllExistingAsync<Presentation>();

            // 1. Definimos las mismas palabras prohibidas que en el GetData
            var excludedKeywords = new[] { "N.A.L.", "NAL.", "NAL", "FG", "F.G." };

            // 2. Filtramos los productos
            var filteredProducts = products
                .Where(x => !excludedKeywords.Any(key => x.Name.Contains(key, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(x => x.Name);

            // 3. Filtramos las presentaciones
            var filteredPresentations = presentations
                .Where(x => !excludedKeywords.Any(key => x.Name.Contains(key, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(x => x.Name);

            model.ProductId = model.ProductId;
            model.PresentationId = model.PresentationId;

            // 4. Usamos las listas filtradas para los SelectList
            model.Products = new SelectList(filteredProducts.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text");
            model.Presentations = new SelectList(filteredPresentations.Select(x => new { Value = x.Id, Text = $"{x.Name} ({x.Liters.FormatCommasNullableTwoDecimals()} lts.)" }), "Value", "Text");

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

            // Obtenemos los datos con los lotes incluidos
            var data = await _repository.ListExistingAsync<ProductPresentation>(new ListByFiltersProductPresentationSpecification(filters));

            // Definimos las palabras a excluir
            var excludedKeywords = new[] { "N.A.L.", "NAL.", "NAL", "FG", "F.G." };

            var dataSource = data
                .Select(c =>
                {
                    var activeBatches = c.Batches
                        .Where(b => b.IsActive && !b.IsDeleted && b.CurrentQuantity > 0)
                        .OrderBy(b => b.ExpiryDate)
                        .ToList();

                    int realStock = activeBatches.Sum(b => b.CurrentQuantity);

                    return new InventoryTableModel
                    {
                        Id = c.Id,
                        Name = c.Product.Name,
                        Presentation = $"{c.Presentation.Name} {c.Presentation.Liters.FormatCommasNullableTwoDecimals()} lts.",
                        Liters = (double)c.Presentation.Liters * realStock,
                        Stock = realStock.ToString(),

                        // --- NUEVA PROPIEDAD DE ORDEN ---
                        // 1 para cervezas de línea (B.C.), 2 para el resto
                        SortPriority = c.Product.Name.StartsWith("B.C.", StringComparison.OrdinalIgnoreCase) ? 1 : 2,

                        Batches = activeBatches.Select(b => new BatchRowModel
                        {
                            Id = b.Id,
                            BatchNumber = b.BatchNumber,
                            CurrentQuantity = b.CurrentQuantity,
                            ExpiryDateFormatted = b.ExpiryDate.ToString("dd/MM/yyyy"),
                            StatusText = b.ExpiryDate < DateTime.Now ? "Vencido" : "En buen estado",
                            StatusColor = b.ExpiryDate < DateTime.Now ? "badge-danger" : "badge-success"
                        }).ToList()
                    };
                })
                // --- APLICAMOS EXCLUSIÓN ---
                .Where(i => !excludedKeywords.Any(key => i.Name.Contains(key, StringComparison.OrdinalIgnoreCase)))
                // --- APLICAMOS ORDEN DE LÍNEA ---
                .OrderBy(i => i.SortPriority)
                .ThenBy(i => i.Name)
                .ToList();

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);

            return Json(dm.RequiresCounts
                ? (object)new { result = dataResult.DataResult, dataResult.Count }
                : (object)dataResult.DataResult);
        }

        [HttpGet]
        public async Task<IActionResult> In(int id, int? batchId = null)
        {
            // 1. Configuramos los títulos para la Vista Parcial compartida
            ViewData["Action"] = nameof(In);
            ViewData["ModalTitle"] = "Agregar entrada";

            // 2. Inicializamos el ViewModel
            var model = new InOutViewModel
            {
                ProductPresentationId = id,
                BatchId = batchId,      // <--- Clave: Si este ID llega, el modal se pondrá en readonly
                IsAdjustment = false,
                Quantity = 0,
                Comment = string.Empty,
                ExpiryDate = DateTime.Now, // Fecha por defecto para entradas nuevas
                AvailableBatches = new List<SelectListItem>() // Lista vacía para que el Dropdown de la vista no sea null
            };

            // 3. Lógica de carga para Lotes Existentes
            if (batchId.HasValue && batchId.Value > 0)
            {
                // Buscamos el lote en la base de datos
                var batch = await _repository.GetByIdAsync<Batch>(batchId.Value);

                if (batch != null)
                {
                    // Mapeamos los datos del lote al modelo
                    model.BatchNumber = batch.BatchNumber;
                    model.ExpiryDate = batch.ExpiryDate;
                }
                else
                {
                    // Opcional: Si el ID venía pero no se encontró en la DB (error de integridad)
                    // Podrías loguear esto o limpiar el BatchId para que no bloquee la vista
                    model.BatchId = null;
                }
            }

            // 4. Retornamos la vista parcial con el modelo ya poblado
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

        [HttpGet]
        public async Task<IActionResult> DescargarReportePlantilla()
        {
            // 1. Usamos el mismo método de extracción de datos que usa el correo
            var datos = await _inventoryService.ObtenerDatosParaReporteExcelAsync();

            if (datos == null || !datos.Any())
            {
                return BadRequest("No hay datos disponibles para generar el reporte.");
            }

            // 2. Reutilizamos TU método GenerarReporteInventario que usa la plantilla física
            // Este método ya sabe que debe ir a wwwroot/resources/Plantilla_Inventario.xlsx
            var content = _excelReaderService.GenerarReporteInventario(datos);

            // 3. Retornamos el archivo al navegador
            string nombreArchivo = $"Reporte_Inventario_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjustment(InOutViewModel model)
        {

            // 🔥 DEBUG: Ver qué está llegando
            System.Diagnostics.Debug.WriteLine($"=== Adjustment CALLED ===");
            System.Diagnostics.Debug.WriteLine($"BatchId: {model.BatchId}");
            System.Diagnostics.Debug.WriteLine($"IsAdjustment: {model.IsAdjustment}");
            System.Diagnostics.Debug.WriteLine($"Quantity: {model.Quantity}");
            System.Diagnostics.Debug.WriteLine($"ProductPresentationId: {model.ProductPresentationId}");
            System.Diagnostics.Debug.WriteLine($"ExpiryDate: {model.ExpiryDate}");
            System.Diagnostics.Debug.WriteLine($"\n \n \n \n \n \n \n \n ");
            System.Diagnostics.Debug.WriteLine($"\n \n \n \n \n \n \n \n ");

            if (!ModelState.IsValid) {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                var errorMessage = string.Join(" | ", errors);
                // ESTO VA A APARECER EN TU ALERTA ROJA

                // 🔥 Log específico para ver el error real
                _logger.LogError($"ModelState inválido: {errorMessage}");
                return Json(AjaxFunctions.GenerateJsonError("ERROR REAL: " + errorMessage));
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