using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Monobits.Core.Specifications;
using Monobits.SharedKernel;
using Monobits.SharedKernel.Interfaces;
using Syncfusion.EJ2.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Services;
using WendlandtVentas.Core.Specifications.ProductSpecifications;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Web.Extensions;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.ProductViewModels;
using WendlandtVentas.Web.Models.TableModels;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial, Storekeeper, Billing, BillingAssistant")]
    public class ProductController : Controller
    {
        private readonly CacheService _cacheService;
        private readonly IMemoryCache _memoryCache;
        private readonly IAsyncRepository _repository;
        private readonly ILogger<ProductController> _logger;
        private readonly SfGridOperations _sfGridOperations;

        public ProductController(IAsyncRepository repository, ILogger<ProductController> logger, SfGridOperations sfGridOperations,
            IMemoryCache memoryCache, CacheService cacheService)
        {
            _repository = repository;
            _logger = logger;
            _sfGridOperations = sfGridOperations;
            _memoryCache = memoryCache;
            _cacheService = cacheService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataManagerRequest dm, [FromQuery] int classificationId = 1)
        {
            // Si por alguna razón llega 0, forzamos a 1 (Cerveza)
            if (classificationId == 0) classificationId = 1;

            // Llave de caché dinámica por pestaña
            string cacheKey = $"ProductTableData_Class_{classificationId}";

            if (!_memoryCache.TryGetValue(cacheKey, out List<ProductTableModel> cachedData))
            {
                IEnumerable<Product> filteredProducts;

                // --- LÓGICA DE FILTRADO SEGÚN PESTAÑA ---
                if (classificationId == 3)
                {
                    // PESTAÑA DESACTIVADOS: 
                    // Usamos GetQueryable para ignorar el filtro automático de "ListExisting"
                    filteredProducts = await _repository.GetQueryable<Product>()
                        .Include(p => p.ProductPresentations).ThenInclude(pp => pp.Presentation)
                        .Include(p => p.InventorySource)
                        .Where(p => p.IsDeleted == true)
                        .ToListAsync();
                }
                else
                {
                    // PESTAÑAS ACTIVAS:
                    var allProducts = await _repository.ListExistingAsync(new ProductTableSpecification());

                    if (classificationId == 4)
                    {
                        // --- NUEVA PESTAÑA: PAQUETES ---
                        // Filtramos solo los que marcamos con el nuevo campo IsBundle
                        filteredProducts = allProducts.Where(p => p.IsBundle == true);
                    }
                    else if (classificationId == 2)
                    {
                        // PESTAÑA WELLEN: Excluimos paquetes para que no se dupliquen
                        filteredProducts = allProducts.Where(p => p.Distinction == Distinction.Wellen && p.IsBundle == false);
                    }
                    else
                    {
                        // PESTAÑA CERVEZA (ID 1): Excluimos Wellen y paquetes
                        filteredProducts = allProducts.Where(p => p.Distinction != Distinction.Wellen && p.IsBundle == false);
                    }
                }

                // --- LÓGICA DE MAPEO ---
                var dataSource = filteredProducts.Select(c =>
                {
                    // Para los precios, seguimos filtrando presentaciones que no estén borradas individualmente
                    var presentations = c.ProductPresentations.Where(p => !p.IsDeleted);

                    return new ProductTableModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Distinction = c.Distinction.Humanize(),
                        Season = c.Season,
                        IsDeleted = c.IsDeleted,
                        // --- NUEVOS CAMPOS PARA EL VÍNCULO ---
                        InventorySourceId = c.InventorySourceId,
                        // Si tiene un InventorySource, mostramos su nombre, si no, es "Propio"
                        InventorySourceName = c.InventorySource?.Name ?? "Inventario Maestro",
                        IsVariant = c.InventorySourceId.HasValue,
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

                        //Obtenemos los datos del Six Pack-
                        PriceSmallCanSix = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Small Can 6-Pack"))?.Price.FormatCurrency() ?? string.Empty,
                        PriceUsdSmallCanSix = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Small Can 6-Pack"))?.PriceUsd.FormatCurrency() ?? string.Empty,
                        WeightSmallCanSix = presentations.FirstOrDefault(c => c.Presentation.Name.Equals("Small Can 6-Pack"))?.Weight.FormatCommasNullableTwoDecimals() ?? string.Empty,
                    };
                }).ToList();



                // --- MODIFICADO: Guardamos en el caché específico ---
                _memoryCache.Set(cacheKey, dataSource, TimeSpan.FromMinutes(30)); // Es bueno poner un tiempo de expiración
                cachedData = dataSource;
            }

            // Filtrar los datos desde el caché
            var dataResult = _sfGridOperations.FilterDataSource(cachedData, dm);
            return dm.RequiresCounts
                ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count })
                : new JsonResult(dataResult.DataResult);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            _cacheService.ClearProductCache();

            ViewData["Action"] = nameof(Add);
            ViewData["ModalTitle"] = "Crear producto";

            // 1. Obtener datos base
            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var distinctions = Enum.GetValues(typeof(Distinction)).Cast<Distinction>().AsEnumerable();

            // 2. Traer productos "Maestros" (Para el vínculo de inventario B.C. / Nacional)
            var allExistingProducts = await _repository.ListAllExistingAsync<Product>();

            var masterProducts = allExistingProducts
                .Where(p => !p.InventorySourceId.HasValue &&
                            (p.Name.Contains("BC") || p.Name.Contains("B.C.")) &&
                            !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                }).ToList();

            // 3. NUEVO: Traer productos para componentes (Para armar el 12-pack)
            // Filtramos para que NO aparezcan otros paquetes (IsBundle == false)
            var componentProducts = allExistingProducts
                .Where(p => !p.IsBundle && !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                }).ToList();

            // 4. Construir el modelo
            var model = new ProductViewModel
            {
                Distinction = Distinction.Hops, // Valor por defecto
                Presentations = presentations.Select(x => new PresentationPrice
                {
                    PresentationId = x.Id,
                    PresentationName = x.NameExtended()
                }),
                Distinctions = new SelectList(distinctions.Select(x => new
                {
                    Value = x,
                    Text = x.Humanize()
                }), "Value", "Text"),

                MasterProducts = masterProducts,

                // Esta es la lista que alimentará tu 'bundleSearcher' en el front
                AvailableComponents = componentProducts
            };

            return PartialView("_AddEditModal", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Action"] = nameof(Edit);
            ViewData["ModalTitle"] = "Editar producto";

            var distinctions = Enum.GetValues(typeof(Distinction)).Cast<Distinction>().AsEnumerable();

            // 1. Obtenemos el producto con sus presentaciones y COMPONENTES (Asegúrate que la especificación incluya BundleComponents)
            var product = await _repository.GetAsync(new ProductExtendedSpecification(id));
            if (product == null) return NotFound();

            // 2. Cargar listas para dropdowns
            var allProducts = await _repository.ListAllExistingAsync<Product>();

            // Maestros para inventario B.C.
            var masterProducts = allProducts
                .Where(p => !p.InventorySourceId.HasValue &&
                            (p.Name.ToUpper().Contains("BC") || p.Name.ToUpper().Contains("B.C.")) &&
                            p.Id != id && !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name })
                .ToList();

            // Componentes disponibles para la pestaña de paquetes (Excluimos el mismo producto y otros bundles)
            var availableComponents = allProducts
                .Where(p => !p.IsBundle && !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name })
                .ToList();

            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var presentationsEdit = product.ProductPresentations.Where(c => !c.IsDeleted);

            // 3. Lógica para detectar precios de Bundle
            // Buscamos si tiene la presentación de 12-pack o 6-pack para sacar el precio
            var packPresentation = presentationsEdit.FirstOrDefault(p =>
                p.Presentation.Name.Contains("12-Pack") || p.Presentation.Name.Contains("6-Pack"));

            var vm = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Distinction = product.Distinction,
                Season = product.Season,
                IsBundle = product.IsBundle,
                InventorySourceId = product.InventorySourceId,
                MasterProducts = masterProducts,
                AvailableComponents = availableComponents,

                // --- DATOS DEL BUNDLE ---
                BundlePriceMXN = packPresentation?.Price,
                BundlePriceUSD = packPresentation?.PriceUsd,
                BundleQuantityTarget = packPresentation?.Presentation.Name.Contains("12") == true ? 12 : 6,

                // Cargamos la "Receta" actual desde la tabla puente
                Components = product.BundleComponents.Select(c => new BundleComponentViewModel
                {
                    ProductId = c.ComponentProductId,
                    Quantity = c.Quantity
                }).ToList(),
                // -----------------------

                Distinctions = new SelectList(distinctions.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                PresentationsEdit = presentationsEdit.Select(x => new PresentationPrice
                {
                    PresentationId = x.PresentationId,
                    PresentationName = x.NameExtended(),
                    Price = x.Price,
                    PriceUsd = x.PriceUsd,
                    Weight = x.Weight
                }),
                Presentations = presentations.Select(x => new PresentationPrice
                {
                    PresentationId = x.Id,
                    PresentationName = x.NameExtended()
                }),
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
            Product product = new Product();

            try
            {
                // 1. VALIDACIONES INICIALES
                var products = await _repository.ListAllExistingAsync<Product>();
                if (products.Any(c => c.Name.ToLower().Equals(model.Name.ToLower())))
                    return Json(AjaxFunctions.GenerateJsonError("El producto ya existe"));

                if (model.IsBundle)
                {
                    // Validación para Paquetes
                    if (model.Components == null || !model.Components.Any())
                        return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Es necesario agregar componentes al paquete"));

                    int totalQty = model.Components.Sum(c => c.Quantity);
                    if (totalQty != model.BundleQuantityTarget)
                        return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, $"La cantidad total ({totalQty}) no coincide con el objetivo del pack ({model.BundleQuantityTarget})"));
                }
                else
                {
                    // Validación para Productos Normales
                    if (model.PresentationPricesAdd == null || !model.PresentationPricesAdd.Any())
                        return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Es necesario agregar por lo menos una presentación"));
                }

                // 2. CREACIÓN DEL PRODUCTO PADRE
                // Agregamos IsBundle al constructor o propiedad
                product = new Product(model.Name, model.Distinction, model.Season, model.InventorySourceId)
                {
                    IsBundle = model.IsBundle
                };

                await _repository.AddAsync(product);

                // 3. LÓGICA DIFERENCIADA SEGÚN TIPO
                if (model.IsBundle)
                {
                    // --- A. GUARDAR COMPONENTES (RECETA) ---
                    var bundleComponents = model.Components.Select(c => new ProductBundleComponent
                    {
                        BundleProductId = product.Id,
                        ComponentProductId = c.ProductId,
                        Quantity = c.Quantity
                    }).ToList();

                    await _repository.AddRangeAsync(bundleComponents);

                    // --- B. ASIGNAR PRECIO ÚNICO A LA PRESENTACIÓN DEL PACK ---
                    // Buscamos el ID de la presentación (ej. "Botella 12-Pack")
                    string targetName = model.BundleQuantityTarget == 12 ? "botella 12-pack" : "small can 6-pack";
                    var presentations = await _repository.ListAllExistingAsync<Presentation>();
                    // Buscamos ignorando mayúsculas/minúsculas y espacios extras
                    var targetPres = presentations.FirstOrDefault(p =>
                        p.Name.Trim().ToLower().Contains(targetName.ToLower()));

                    if (targetPres != null)
                    {
                        var packPresentation = new ProductPresentation(
                            product.Id,
                            targetPres.Id,
                            model.BundlePriceMXN ?? 0,
                            model.BundlePriceUSD ?? 0,
                            0 // El peso puedes calcularlo o dejarlo en 0
                        );
                        await _repository.AddAsync(packPresentation);
                    }
                }
                else
                {
                    // --- C. GUARDAR PRESENTACIONES NORMALES ---
                    var productPresentations = new List<ProductPresentation>();
                    foreach (var pres in model.PresentationPricesAdd)
                    {
                        productPresentations.Add(new ProductPresentation(product.Id, pres.PresentationId, pres.Price, pres.PriceUsd, pres.Weight));
                    }
                    await _repository.AddRangeAsync(productPresentations);
                }

                // 4. LIMPIEZA Y RESPUESTA
                _cacheService.ClearProductCache();
                _cacheService.RemoveProductsCache();

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Producto guardado con éxito"));
            }
            catch (Exception e)
            {
                // Rollback manual (como lo tenías)
                if (product.Id > 0)
                    await _repository.DeleteAsync(product);

                _logger.LogError($"Error al agregar producto: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar el producto"));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            try
            {
                // 1. VALIDACIONES INICIALES
                if (!model.IsBundle && !model.PresentationPricesAdd.Any())
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Es necesario agregar por lo menos una presentación"));

                if (model.IsBundle && (model.Components == null || !model.Components.Any()))
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Un paquete debe tener componentes"));

                var product = await _repository.GetAsync<Product>(new ProductExtendedSpecification(model.Id));
                if (product == null)
                    return Json(AjaxFunctions.GenerateJsonError("El producto no existe"));

                var products = (await _repository.ListAllExistingAsync<Product>()).Where(c => c.Id != product.Id);
                if (products.Any(c => c.Name.ToLower().Equals(model.Name.ToLower())))
                    return Json(AjaxFunctions.GenerateJsonError("El producto ya existe"));

                // 2. LÓGICA DE ACTUALIZACIÓN SEGÚN TIPO
                if (model.IsBundle)
                {
                    // --- A. SINCRONIZAR COMPONENTES (RECETA) ---
                    // Limpiamos los componentes actuales (Borrado lógico usando tu método Delete si existe)
                    if (product.BundleComponents != null)
                    {
                        foreach (var oldComp in product.BundleComponents.ToList())
                        {
                            // Si tu repositorio soporta Delete físico o lógico, aplícalo aquí
                            await _repository.DeleteAsync(oldComp);
                        }
                    }

                    // Agregamos los nuevos componentes desde el modelo
                    foreach (var comp in model.Components)
                    {
                        var newComp = new ProductBundleComponent
                        {
                            BundleProductId = product.Id,
                            ComponentProductId = comp.ProductId,
                            Quantity = comp.Quantity
                        };
                        await _repository.AddAsync(newComp);
                    }

                    // --- B. ACTUALIZAR PRECIO DEL PACK EN PRESENTACIONES ---
                    string targetName = model.BundleQuantityTarget == 12 ? "Botella 12-Pack" : "Small Can 6-Pack";
                    var presentations = await _repository.ListAllExistingAsync<Presentation>();
                    var targetPres = presentations.FirstOrDefault(p => p.Name.Trim().ToLower().Contains(targetName.ToLower()));

                    if (targetPres != null)
                    {
                        var currentPrice = product.ProductPresentations.FirstOrDefault(p => p.PresentationId == targetPres.Id);
                        if (currentPrice != null)
                        {
                            currentPrice.Delete(false); // Aseguramos que esté activo
                            currentPrice.EditPrice(model.BundlePriceMXN ?? 0);
                            currentPrice.EditPriceUsd(model.BundlePriceUSD ?? 0);
                        }
                        else
                        {
                            await _repository.AddAsync(new ProductPresentation(product.Id, targetPres.Id, model.BundlePriceMXN ?? 0, model.BundlePriceUSD ?? 0, 0));
                        }
                    }
                }
                else
                {
                    // --- C. LÓGICA ORIGINAL PARA PRODUCTOS NORMALES ---
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
                }

                // 3. ACTUALIZAR CAMPOS GENERALES Y GUARDAR
                product.Edit(model.Name, model.Distinction, model.Season, model.InventorySourceId);
                product.IsBundle = model.IsBundle; // No olvides actualizar la bandera

                await _repository.UpdateAsync(product);

                _cacheService.ClearProductCache();
                _cacheService.RemoveProductsCache();

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Producto actualizado"));
            }
            catch (Exception e)
            {
                _logger.LogError($"Error al actualizar: {e.Message}");
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
                _cacheService.ClearProductCache();
                _cacheService.RemoveProductsCache();

                // 2. NUEVO: Eliminar específicamente la llave que usan los Pedidos
                // Asegúrate de usar el mismo nombre de string que usas al leerla
                _memoryCache.Remove("Products_For_Orders");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Producto eliminado"));
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Error: {e.Message}");
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo eliminar el producto"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Restore(int id)
        {
            // 1. Buscamos el producto por ID para mostrar su nombre en el modal
            // Usamos GetByIdAsync para traer la entidad directamente
            var product = await _repository.GetByIdAsync<Product>(id);

            if (product == null)
            {
                // Si por algo no existe, mandamos un 404
                return NotFound();
            }

            // 2. Retornamos la Vista Parcial
            // Asegúrate de que el nombre del archivo sea "_RestoreModal.cshtml" 
            // y que esté en la carpeta Views/Product/
            return PartialView("_RestoreModal", product);
        }

        [HttpPost]
        public async Task<IActionResult> Restore(int id, bool confirm = true)
        {
            // 1. Buscamos el producto en la base de datos
            // GetByIdAsync suele ignorar los filtros de 'IsDeleted' del repositorio
            var product = await _repository.GetByIdAsync<Product>(id);

            if (product != null)
            {
                // 2. Usamos el nuevo método que agregaste a BaseEntity
                // ¡Ya no necesitamos reflexión!
                product.Restore();

                // 3. Persistimos el cambio en la base de datos
                await _repository.UpdateAsync(product);

                // 4. LIMPIEZA DE CACHÉ
                // Borramos los 3 cajones para que el producto aparezca en 'Cerveza'/'Wellen'
                // y desaparezca de 'Desactivados'
                _memoryCache.Remove("ProductTableData_Class_1");
                _memoryCache.Remove("ProductTableData_Class_2");
                _memoryCache.Remove("ProductTableData_Class_3");

                return Json(new
                {
                    status = "ok",
                    body = $"El producto '{product.Name}' ha sido restaurado exitosamente."
                });
            }

            return Json(new
            {
                status = "error",
                body = "No se pudo encontrar el producto para restaurar."
            });
        }
    }
}