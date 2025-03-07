using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using Syncfusion.EJ2.Base;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Specifications.PromotionSpecifications;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.PromotionViewModels;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial, Storekeeper, Billing, BillingAssistant")]
    public class PromotionController : Controller
    {
        private readonly IAsyncRepository _repository;
        private readonly ILogger<ProductController> _logger;
        private readonly SfGridOperations _sfGridOperations;

        public PromotionController(IAsyncRepository repository, ILogger<ProductController> logger, SfGridOperations sfGridOperations)
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
            var presentationsAll = await _repository.ListAllExistingAsync<Presentation>();
            var dataSource = (await _repository.ListAsync(new PromotionExtendedSpecification()))
                .Select(c =>
                {
                    var presentationsPromotions = c.PresentationPromotions.Where(c => !c.IsDeleted);
                    var presentations = string.Empty;

                if (presentationsPromotions.All(c => c.IsDeleted))
                    presentations = "-";
                else
                if (presentationsAll.All(e => presentationsPromotions.Select(f => f.PresentationId).Contains(e.Id)))
                        presentations = "Todas";
                    else
                        presentations = string.Join(", ", presentationsPromotions.Select(c => $"{c.Presentation.Name} {c.Presentation.Liters} lts"));

                    return new PromotionTableModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Buy = c.Buy,
                        Present = c.Present,
                        Discount = c.DiscountFormat,
                        Type = c.Type.Humanize(),
                        IsActive = c.IsActive,
                        Classification = c.Classification != null ? c.Classification.Humanize() : "-",
                        Presentations = presentations,
                        Clients = c.ClientPromotions.Any(c => !c.IsDeleted) ? string.Join(", ", c.ClientPromotions.Where(c => !c.IsDeleted).Select(c => c.Client.Name)) : "-"
                    };
                });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewData["Action"] = nameof(Add);
            ViewData["ModalTitle"] = "Crear promoción";
            var classifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().OrderBy(x => x).AsEnumerable();
            var promotionTypes = Enum.GetValues(typeof(PromotionType)).Cast<PromotionType>().OrderBy(x => x).AsEnumerable();
            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var clients = await _repository.ListAllExistingAsync<Client>();
            var model = new PromotionViewModel
            {
                PresentationsAllSelected = true,
                Type = PromotionType.General,
                Presentations = new SelectList(presentations.Select(x => new { Value = x.Id, Text = $"{x.Name} {x.Liters} lts" }), "Value", "Text"),
                Classifications = new SelectList(classifications.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                Clients = new SelectList(clients.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text"),
                PromotionTypes = promotionTypes
            };

            return PartialView("_AddEditModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(PromotionViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));

            try
            {
                if (model.PresentationsAllSelected)
                    model.PresentationIds = (await _repository.ListAllExistingAsync<Presentation>()).Select(c => c.Id).ToList();

                if (!model.PresentationIds.Any())
                    return Json(AjaxFunctions.GenerateJsonError("Es necesario agregar al menos una presentación"));

                if (model.Type == PromotionType.Clients && !model.ClientIds.Any())
                    return Json(AjaxFunctions.GenerateJsonError("Es necesario agregar al menos un cliente"));

                var presentationPromotions = model.PresentationIds.Select(presentationId => new PresentationPromotion(presentationId));
                var clientPromotions = model.ClientIds.Select(clientId => new ClientPromotion(clientId));
                var promotion = new Promotion(model.Name, model.Buy, model.Present, model.Type, model.Classification, presentationPromotions, clientPromotions);

                await _repository.AddAsync(promotion);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Promoción guardada"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en PromotionController ---> Add: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar el cliente"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Action"] = nameof(Edit);
            ViewData["ModalTitle"] = "Editar promoción";

            var promotion = await _repository.GetAsync(new PromotionExtendedSpecification(id));

            if (promotion == null)
                return NotFound();

            var presentations = await _repository.ListAllExistingAsync<Presentation>();
            var clients = await _repository.ListAllExistingAsync<Client>();
            var classifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().OrderBy(x => x).AsEnumerable();
            var promotionTypes = Enum.GetValues(typeof(PromotionType)).Cast<PromotionType>().OrderBy(x => x).AsEnumerable();
            var promotionPresentations = promotion.PresentationPromotions.Where(c => !c.IsDeleted).Select(c => c.PresentationId).ToList();
            var model = new PromotionViewModel
            {
                Id = promotion.Id,
                Name = promotion.Name,
                Buy = promotion.Buy,
                Present = promotion.Present,
                Type = promotion.Type,
                PresentationsAllSelected = presentations.All(c => promotionPresentations.Contains(c.Id)),
                Classification = promotion.Classification,
                ClientIds = promotion.ClientPromotions.Where(c => !c.IsDeleted).Select(c => c.ClientId).ToList(),
                PresentationIds = promotionPresentations,
                Presentations = new SelectList(presentations.Select(x => new { Value = x.Id, Text = $"{x.Name} {x.Liters} lts" }), "Value", "Text"),
                Classifications = new SelectList(classifications.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                Clients = new SelectList(clients.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text"),
                PromotionTypes = promotionTypes
            };

            return PartialView("_AddEditModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PromotionViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));

            try
            {
                var presentationPromotionsAdd = new List<PresentationPromotion>();
                var clientsAdd = new List<ClientPromotion>();
                var promotion = await _repository.GetAsync(new PromotionExtendedSpecification(model.Id));

                if (promotion == null)
                    return Json(AjaxFunctions.GenerateJsonError("La promoción no existe"));

                if (model.PresentationsAllSelected)
                    model.PresentationIds = (await _repository.ListAllExistingAsync<Presentation>()).Select(c => c.Id).ToList();

                if (!model.PresentationIds.Any())
                    return Json(AjaxFunctions.GenerateJsonError("Es necesario agregar al menos una presentación"));


                promotion.PresentationPromotions.ToList().ForEach(c => c.Delete());

                foreach (var presentationId in model.PresentationIds)
                {
                    var presentationCurrent = promotion.PresentationPromotions.SingleOrDefault(c => c.PresentationId == presentationId);

                    if (presentationCurrent != null)
                        presentationCurrent.Delete(false);
                    else
                        presentationPromotionsAdd.Add(new PresentationPromotion(presentationId));
                }

                if (model.Type == PromotionType.Clients)
                {
                    if (!model.ClientIds.Any())
                        return Json(AjaxFunctions.GenerateJsonError("Es necesario agregar al menos un cliente"));

                    promotion.ClientPromotions.ToList().ForEach(c => c.Delete());

                    foreach (var clientId in model.ClientIds)
                    {
                        var clientCurrent = promotion.ClientPromotions.FirstOrDefault(c => c.ClientId == clientId);

                        if (clientCurrent != null)
                            clientCurrent.Delete(false);
                        else
                            clientsAdd.Add(new ClientPromotion(clientId));
                    }
                }
                promotion.Edit(model.Name, model.Buy, model.Present, model.Type, model.Classification, presentationPromotionsAdd, clientsAdd);

                await _repository.UpdateAsync(promotion);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Promoción actualizada"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en PromotionController ---> Edit: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo actualizar la promoción"));
            }
        }

        [HttpGet("{controller}/Delete/{id}")]
        public async Task<IActionResult> DeleteView(int id)
        {
            var promotion = await _repository.GetByIdAsync<Promotion>(id);

            if (promotion == null)
                return NotFound();

            ViewData["Action"] = nameof(Delete);
            ViewData["ModalTitle"] = "Eliminar promoción";
            ViewData["ModalDescription"] = $" la promoción";

            return PartialView("_DeleteModal", $"{promotion.Id}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var promotion = await _repository.GetAsync(new PromotionExtendedSpecification(id));

                if (promotion == null)
                    return Json(AjaxFunctions.GenerateJsonError("La promoción no existe"));

                promotion.Delete();
                await _repository.UpdateAsync(promotion);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Promoción eliminada"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en PromotionController ---> Delete: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo eliminar la promoción"));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var promotion = await _repository.GetByIdAsync<Promotion>(id);
            if (promotion == null)
                return Json(AjaxFunctions.GenerateJsonError("La promoción no existe"));

            promotion.ChangeStatus();
            await _repository.UpdateAsync(promotion);

            return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Usuario editado correctamente."));
        }
    }
}