using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Monobits.SharedKernel.Interfaces;
using Syncfusion.EJ2.Base;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Specifications.MovementSpecifications;
using WendlandtVentas.Core.Specifications.ProductPresentationSpecifications;
using WendlandtVentas.Web.Extensions;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.MovementViewModels;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial")]
    public class MovementController : Controller
    {
        private readonly IAsyncRepository _repository;
        private readonly SfGridOperations _sfGridOperations;
        private readonly UserManager<ApplicationUser> _userManager;

        public MovementController(IAsyncRepository repository, SfGridOperations sfGridOperations, UserManager<ApplicationUser> userManager)
        {
            _repository = repository;
            _sfGridOperations = sfGridOperations;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(FilterViewModel filter)
        {
            var operations = Enum.GetValues(typeof(Operation)).Cast<Operation>().AsEnumerable();
            var productPresentation = await _repository.GetAsync<ProductPresentation>(new ProductPresentationWithProductSpecification(filter.ProductPresentationId));
            var users = _userManager.Users;
            ViewData["Title"] = $"Movimientos de {productPresentation.Product.Name}";
          
            filter.Users = new SelectList(users.Select(x => new { Value = x.Id, Text = $"{x.Name} ({x.Email})" }), "Value", "Text");
            filter.Operations = new SelectList(operations.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text");

            return View(filter);
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataManagerRequest dm, FilterViewModel model)
        {
            var filters = new Dictionary<string, string>
            {
                {
                    nameof(model.ProductPresentationId),
                    model.ProductPresentationId.ToString()
                },
                {
                    nameof(model.DateStart),
                    model.DateStart
                },
                {
                    nameof(model.DateEnd),
                    model.DateEnd
                },
                 {
                    nameof(model.UserId),
                    model.UserId
                },
                {
                    nameof(model.Operation),
                    model.Operation
                }
            };
            var movements = await _repository.ListExistingAsync<Movement>(new ListByFiltersMovementSpecification(filters));
            var users = _userManager.Users.ToDictionary(c => c.Id, c => c.Name);
            var dataSource = movements.OrderByDescending(c => c.CreatedAt)
                .Select(c =>
                    new MovementTableModel
                    {
                        Id = c.Id,
                        Quantity = c.Quantity,
                        QuantityCurrent = c.QuantityCurrent,
                        QuantityOld = c.QuantityOld,
                        Comment = c.Comment,
                        Operation = c.Operation.Humanize(),
                        User = users[c.UserId],
                        CreatedAt = c.CreatedAt.FormatDateLongMx()
                    });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }
    }
}