using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Monobits.Core.Specifications;
using Monobits.SharedKernel.Interfaces;
using Syncfusion.EJ2.Base;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Web.Extensions;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.ProductViewModels;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial, Storekeeper, Billing")]
    public class ProductController : Controller
    {
        private readonly IAsyncRepository _repository;
        private readonly ILogger<ProductController> _logger;
        private readonly SfGridOperations _sfGridOperations;

        public ProductController(IAsyncRepository repository, ILogger<ProductController> logger, SfGridOperations sfGridOperations)
        {
            _repository = repository;
            _logger = logger;
            _sfGridOperations = sfGridOperations;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataManagerRequest dm)
        {
            var dataSource = (await _repository.ListExistingAsync(new ProductExtendedSpecification()))
                .Select(c =>
                {
                    var presentations = c.ProductPresentations.Where(c => !c.IsDeleted);
                    return new ProductTableModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Distinction = c.Distinction.Humanize(),
                        Season = c.Season,

                        PriceBarrelPet20 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Pet") && c.Presentation.Liters == 20)?.Price.FormatCurrency() ?? string.Empty,
                        PriceUsdBarrelPet20 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Pet") && c.Presentation.Liters == 20)?.PriceUsd.FormatCurrency() ?? string.Empty,
                        WeightBarrelPet20 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Pet") && c.Presentation.Liters == 20)?.Weight.FormatCommasNullableTwoDecimals() ?? string.Empty,
                        // PriceBarrelPet30 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Pet") && c.Presentation.Liters == 30)?.Price.FormatCurrency() ?? string.Empty,
                        PriceBarrelInox20 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Inox") && c.Presentation.Liters == 20)?.Price.FormatCurrency() ?? string.Empty,
                        PriceUsdBarrelInox20 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Inox") && c.Presentation.Liters == 20)?.PriceUsd.FormatCurrency() ?? string.Empty,
                        WeightBarrelInox20 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Inox") && c.Presentation.Liters == 20)?.Weight.FormatCommasNullableTwoDecimals() ?? string.Empty,
                        //Comentado porque ya no se ocupael barril inoxidable 30 litros
                        //PriceBarrelInox30 = presentations.SingleOrDefault(c => c.Presentation.Name.Equals("Barril Inox") && c.Presentation.Liters == 30)?.Price.FormatCurrency() ?? string.Empty,
                        PriceBarrelInox60 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Inox") && c.Presentation.Liters == 60)?.Price.FormatCurrency() ?? string.Empty,
                        PriceUsdBarrelInox60 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Inox") && c.Presentation.Liters == 60)?.PriceUsd.FormatCurrency() ?? string.Empty,
                        WeightBarrelInox60 = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Barril Inox") && c.Presentation.Liters == 60)?.Weight.FormatCommasNullableTwoDecimals() ?? string.Empty,
                       
                        PriceBottle = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Botella"))?.Price.FormatCurrency() ?? string.Empty,
                        PriceUsdBottle = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Botella"))?.PriceUsd.FormatCurrency() ?? string.Empty,
                        WeightBottle = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Botella"))?.Weight.FormatCommasNullableTwoDecimals() ?? string.Empty,
                       
                        PriceCan = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Lata"))?.Price.FormatCurrency() ?? string.Empty,
                        PriceUsdCan = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Lata"))?.PriceUsd.FormatCurrency() ?? string.Empty,
                        WeightCan = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Lata"))?.Weight.FormatCommasNullableTwoDecimals() ?? string.Empty,
                       
                        Tasting = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Tasting"))?.Price.FormatCurrency() ?? string.Empty,
                        TastingUsd = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Tasting"))?.PriceUsd.FormatCurrency() ?? string.Empty,
                        TastingWeight = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Tasting"))?.Weight.FormatCommasNullableTwoDecimals() ?? string.Empty,
                    

                            //Obtenemos los datos del doce pack-
                        PriceBotellaDoce = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Botella 12-Pack"))?.Price.FormatCurrency() ?? string.Empty,
                        PriceUsdBotellaDoce = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Botella 12-Pack"))?.PriceUsd.FormatCurrency() ?? string.Empty,
                        WeightBotellaDoce = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Botella 12-Pack"))?.Weight.FormatCommasNullableTwoDecimals() ?? string.Empty,
                       
                       
                    
                    };
                });

                

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewData["Action"] = nameof(Add);
            ViewData["ModalTitle"] = "Crear producto";
            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var distinctions = Enum.GetValues(typeof(Distinction)).Cast<Distinction>().AsEnumerable();
            var model = new ProductViewModel
            {
                Distinction = Distinction.Hops,
                Presentations = presentations.Select(x => new PresentationPrice { PresentationId = x.Id, PresentationName = x.NameExtended() }),
                Distinctions = new SelectList(distinctions.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text")
            };

            return PartialView("_AddEditModal", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Action"] = nameof(Edit);
            ViewData["ModalTitle"] = "Editar producto";
            var distinctions = Enum.GetValues(typeof(Distinction)).Cast<Distinction>().AsEnumerable();
            var product = await _repository.GetAsync(new ProductExtendedSpecification(id));

            if (product == null)
                return NotFound();

            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var presentationsEdit = product.ProductPresentations.Where(c => !c.IsDeleted);
            var vm = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Distinction = product.Distinction,
                Season = product.Season,
                Distinctions = new SelectList(distinctions.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                PresentationsEdit = presentationsEdit.Select(x => new PresentationPrice { PresentationId = x.PresentationId, PresentationName = x.NameExtended(), Price = x.Price, PriceUsd = x.PriceUsd, Weight = x.Weight }),
                Presentations = presentations.Select(x => new PresentationPrice { PresentationId = x.Id, PresentationName = x.NameExtended() }),
            };

            return PartialView("_AddEditModal", vm);
        }

        [HttpGet("{controller}/Delete/{id}")]
        public async Task<IActionResult> DeleteView(int id)
        {
            var product = await _repository.GetByIdAsync<Product>(id);

            if (product == null)
                return Json(AjaxFunctions.GenerateJsonError("El producto no existe"));

            ViewData["Action"] = nameof(Delete);
            ViewData["ModalTitle"] = "Eliminar producto";
            ViewData["ModalDescription"] = $"el producto de {product.Name}";

            return PartialView("_DeleteModal", product.Id.ToString());
        }

        [HttpPost]
        public async Task<IActionResult> Add(ProductViewModel model)
        {
            var product = new Product();

            try
            {
                if (!model.PresentationPricesAdd.Any())
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Es necesario agregar por lo menos una presentación"));

                var products = await _repository.ListAllExistingAsync<Product>();
                if (products.Any(c => c.Name.ToLower().Equals(model.Name.ToLower())))
                    return Json(AjaxFunctions.GenerateJsonError("El producto ya existe"));

                var productPresentations = new List<ProductPresentation>();
                product = new Product(model.Name, model.Distinction, model.Season);

                await _repository.AddAsync(product);

                foreach (var pres in model.PresentationPricesAdd)
                    productPresentations.Add(new ProductPresentation(product.Id, pres.PresentationId, pres.Price, pres.PriceUsd, pres.Weight));

                await _repository.AddRangeAsync(productPresentations);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Producto guardado"));
            }
            catch (Exception e)
            {
                // Si falla al agregar las presentaciones, borramos el producto nuevo.
                if (product.Id > 0)
                    await _repository.DeleteAsync(product);

                _logger.LogInformation($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar el producto"));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            try
            {
                if (!model.PresentationPricesAdd.Any())
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Es necesario agregar por lo menos una presentación"));

                var product = await _repository.GetAsync<Product>(new ProductExtendedSpecification(model.Id));
                if (product == null)
                    return Json(AjaxFunctions.GenerateJsonError("El producto no existe"));

                var products = (await _repository.ListAllExistingAsync<Product>()).Where(c => c.Id != product.Id);
                if (products.Any(c => c.Name.ToLower().Equals(model.Name.ToLower())))
                    return Json(AjaxFunctions.GenerateJsonError("El producto ya existe"));

                product.ProductPresentations.ToList().ForEach(c => c.Delete());

                foreach (var pres in model.PresentationPricesAdd)
                {
                    var presentationCurrent = product.ProductPresentations.FirstOrDefault(c => c.PresentationId == pres.PresentationId);

                    if (presentationCurrent != null)
                    {
                        presentationCurrent.Delete(false);
                        presentationCurrent.EditPrice(pres.Price);
                        presentationCurrent.EditPriceUsd(pres.PriceUsd);
                        presentationCurrent.EditWeight(pres.Weight);
                    }
                    else
                        await _repository.AddAsync(new ProductPresentation(product.Id, pres.PresentationId, pres.Price, pres.PriceUsd, pres.Weight));
                }

                product.Edit(model.Name, model.Distinction, model.Season);
                await _repository.UpdateAsync(product);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Producto actualizado"));
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo actualizar el producto"));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _repository.GetAsync<Product>(new ProductExtendedSpecification(id));

                if (product == null)
                    return Json(AjaxFunctions.GenerateJsonError("El producto no existe"));

                product.ProductPresentations.ToList().ForEach(c => c.Delete());
                product.Delete();
                await _repository.UpdateAsync(product);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Producto eliminado"));
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo eliminar el producto"));
            }
        }
    }
}