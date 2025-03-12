using System;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Monobits.SharedKernel.Interfaces;
using Syncfusion.EJ2.Base;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Entities.Enums;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Specifications.ClientSpecifications;
using WendlandtVentas.Infrastructure.Commons;
using WendlandtVentas.Web.Libs;
using WendlandtVentas.Web.Models.ClientViewModels;
using WendlandtVentas.Web.Models.TableModels;

namespace WendlandtVentas.Web.Controllers
{
    [Authorize(Roles = "Administrator, AdministratorCommercial, AdministratorAssistant, Storekeeper, Billing, Sales, Distributor")]
    public class ClientController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAsyncRepository _repository;
        private readonly ILogger<ProductController> _logger;
        private readonly SfGridOperations _sfGridOperations;
        private readonly IExcelReadService _excelReadService;

        public ClientController(IAsyncRepository repository, ILogger<ProductController> logger, SfGridOperations sfGridOperations,
            UserManager<ApplicationUser> userManager, IExcelReadService excelReadService)
        {
            _repository = repository;
            _logger = logger;
            _sfGridOperations = sfGridOperations;
            _userManager = userManager;
            _excelReadService = excelReadService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DataManagerRequest dm)
        {
            var dataSource = (await _repository.ListExistingAsync(new ClientExtendedSpecification()))
                .Select(c =>
                {
                    var seller = "";//string.IsNullOrEmpty(c.SellerId) || c.SellerId == "0" ? "Ninguno" : (await _userManager.FindByIdAsync(c.SellerId)).Name;
                    return new ClientTableModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Classification = c.Classification == null ? "-" : c.Classification.Humanize(),
                        Channel = c.Channel == null ? "-" : c.Channel.Humanize(),
                        State = c.State == null ? "-" : c.State.Name,
                        RFC = string.IsNullOrEmpty(c.RFC) ? "-" : c.RFC,
                        Addresses = c.Addresses.Where(d => !d.IsDeleted).Count(),
                        City = string.IsNullOrEmpty(c.City) ? "-" : c.City,
                        CreationDate = $"{c.CreatedAt:dd MMM yyyy}",
                        PayType = c.PayType == null ? "-" : c.PayType.Humanize(),
                        Seller = seller,
                        Contacts = c.Contacts.Where(d => !d.IsDeleted).Count(),
                        CreditDays = c.CreditDays
                    };
                });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }

        /*public async Task<IActionResult> GetDistributorsData([FromBody] DataManagerRequest dm)
        {
            var clients = await _repository.ListExistingAsync(new ClientExtendedSpecification());
            var distributors = clients
                .Where(c => c.DiscountPercentage.HasValue) // Solo distribuidores con descuento
                .ToList(); // Convertir a lista para usar Where

            var dataSource = distributors.Select(c => new
            {
                c.Id,
                c.Name,
                c.DiscountPercentage
            });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, count = dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }*/

        [HttpPost]
        public async Task<IActionResult> GetDataContacts([FromBody] DataManagerRequest dm, int id)
        {
            var dataSource = (await _repository.ListAsync(new ContactsByClientIdSpecification(id)))
                .Where(c => !c.IsDeleted)
                .Select(c =>
                {
                    return new ContactTableModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Cellphone = c.Cellphone,
                        Comments = c.Comments,
                        Email = c.Email,
                        OfficePhone = c.OfficePhone
                    };
                });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }

        [HttpPost]
        public async Task<IActionResult> GetDataAddresses([FromBody] DataManagerRequest dm, int id)
        {
            var dataSource = (await _repository.ListAsync(new AddressesByClientIdSpecification(id)))
                .Where(c => !c.IsDeleted)
                .Select(c =>
                {
                    return new AddressTableModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Address = c.AddressLocation
                    };
                });

            var dataResult = _sfGridOperations.FilterDataSource(dataSource, dm);
            return dm.RequiresCounts ? new JsonResult(new { result = dataResult.DataResult, dataResult.Count }) : new JsonResult(dataResult.DataResult);
        }

        [HttpGet]
        public IActionResult UploadXlsxView()
        {
            ViewData["Action"] = nameof(AddXlsClients);
            ViewData["ModalTitle"] = "Importar clientes";
            return PartialView("_ImportClients");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddXlsClients(ImportClientModel model)
        {
            try
            {
                if (model.ExcelFile == null)
                {
                    return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "Favor de seleccionar un archivo de importación"));
                }
                var spreadsheetDocument = new XLWorkbook(model.ExcelFile.OpenReadStream());
                var clients = await _excelReadService.ExtractData(spreadsheetDocument);
                await _repository.AddRangeAsync(clients);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Clientes importados correctamente."));
            }
            catch (Exception err)
            {
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, err.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ContactsView(int id)
        {
            var client = await _repository.GetByIdAsync<Client>(id);
            ViewData["ModalTitle"] = $"Contactos de {client.Name}";
            return PartialView("_ContactsView", id);
        }

        [HttpGet]
        public IActionResult AddContactView(int id)
        {
            ViewData["ModalTitle"] = "Agregar contacto";
            ViewData["Action"] = nameof(AddContact);
            var model = new ContactViewModel
            {
                ClientId = id
            };
            return PartialView("_AddEditContactModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddContact(ContactViewModel model)
        {
            if (!ModelState.IsValid) return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));
            try
            {
                var contact = new Contact(model.Name, model.Cellphone, model.OfficePhone, model.Email, model.Comments, model.ClientId);
                await _repository.AddAsync(contact);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Contacto guardado"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en ClientController --> AddContact: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar el contacto"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditContactView(int id)
        {
            var contact = await _repository.GetByIdAsync<Contact>(id);
            ViewData["ModalTitle"] = "Editar contacto";
            ViewData["Action"] = nameof(EditContact);
            var model = new ContactViewModel
            {
                ClientId = contact.ClientId,
                Cellphone = contact.Cellphone,
                Comments = contact.Comments,
                Email = contact.Email,
                Id = contact.Id,
                Name = contact.Name,
                OfficePhone = contact.OfficePhone
            };
            return PartialView("_AddEditContactModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditContact(ContactViewModel model)
        {
            if (!ModelState.IsValid) return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));
            try
            {
                var contact = await _repository.GetByIdAsync<Contact>(model.Id);
                contact.Edit(model.Name, model.Cellphone, model.OfficePhone, model.Email, model.Comments, model.ClientId);
                await _repository.UpdateAsync(contact);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Contacto actualizado"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en ClientController --> EditContact: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo editar el contacto"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddressesView(int id)
        {
            var client = await _repository.GetByIdAsync<Client>(id);
            ViewData["ModalTitle"] = $"Direcciones de {client.Name}";
            return PartialView("_AddressesView", id);
        }

        [HttpGet]
        public IActionResult AddAddressView(int id)
        {
            ViewData["ModalTitle"] = "Agregar dirección";
            ViewData["Action"] = nameof(AddAddress);
            var model = new AddressViewModel
            {
                ClientId = id
            };
            return PartialView("_AddEditAddressModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(AddressViewModel model)
        {
            if (!ModelState.IsValid) return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));
            try
            {
                var address = new Address(model.Name, model.Address, model.ClientId);
                await _repository.AddAsync(address);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Dirección guardada"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en ClientController --> AddAddress: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar la dirección"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditAddressView(int id)
        {
            var address = await _repository.GetByIdAsync<Address>(id);
            ViewData["ModalTitle"] = "Editar dirección";
            ViewData["Action"] = nameof(EditAddress);

            var model = new AddressViewModel
            {
                ClientId = address.ClientId,
                Id = address.Id,
                Name = address.Name,
                Address = address.AddressLocation
            };

            return PartialView("_AddEditAddressModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(AddressViewModel model)
        {
            if (!ModelState.IsValid) return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));
            try
            {
                var address = await _repository.GetByIdAsync<Address>(model.Id);
                address.Edit(model.Name, model.Address, model.ClientId);
                await _repository.UpdateAsync(address);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Dirección actualizada"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en ClientController --> EditAddress: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo editar la dirección"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewData["Action"] = nameof(Add);
            ViewData["ModalTitle"] = "Crear cliente";
            var classifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().OrderBy(x => x).AsEnumerable();
            var channels = Enum.GetValues(typeof(Channel)).Cast<Channel>().OrderBy(x => x).AsEnumerable();
            var payTypes = Enum.GetValues(typeof(PayType)).Cast<PayType>().OrderBy(x => x).AsEnumerable();
            var states = (await _repository.ListAllExistingAsync<State>()).OrderBy(c => c.Name);
            var model = new ClientViewModel
            {
                Classifications = new SelectList(classifications.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                Channels = new SelectList(channels.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                PayTypes = new SelectList(payTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                States = new SelectList(states.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text"),
            };

            model.Sellers = _userManager.Users.Select(s => new SelectListItem
            {
                Text = s.Name,
                Value = s.Id
            }).ToList();

            model.Sellers.Insert(0, new SelectListItem
            {
                Text = "Ninguno",
                Value = "0"
            });

            return PartialView("_AddEditModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ClientViewModel model)
        {
            if (!ModelState.IsValid) return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));

            try
            {
                var client = new Client(model.Name);
                client.Channel = model.Channel;
                client.Classification = model.Classification;
                client.StateId = model.StateId;
                client.PayType = model.PayType;
                client.RFC = string.IsNullOrEmpty(model.RFC) ? model.RFC : model.RFC.ToUpper();
                //client.Address = model.Address;
                client.City = string.IsNullOrEmpty(model.City) ? model.City : model.City.Trim();
                client.SellerId = model.SellerId;
                client.CreditDays = model.CreditDays;

                await _repository.AddAsync(client);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Cliente guardado"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en ClientController --> Add: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo guardar el cliente"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Action"] = nameof(Edit);
            ViewData["ModalTitle"] = "Editar cliente";

            var client = await _repository.GetByIdAsync<Client>(id);

            if (client == null)
                return NotFound();

            var classifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().OrderBy(x => x).AsEnumerable();
            var channels = Enum.GetValues(typeof(Channel)).Cast<Channel>().OrderBy(x => x).AsEnumerable();
            var payTypes = Enum.GetValues(typeof(PayType)).Cast<PayType>().OrderBy(x => x).AsEnumerable();
            var states = await _repository.ListAllExistingAsync<State>();
            var model = new ClientViewModel
            {
                Id = client.Id,
                Name = client.Name,
                Classification = client.Classification,
                Classifications = new SelectList(classifications.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                Channel = client.Channel,
                Channels = new SelectList(channels.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                StateId = client.StateId,
                States = new SelectList(states.Select(x => new { Value = x.Id, Text = x.Name }), "Value", "Text"),
                PayType = client.PayType,
                PayTypes = new SelectList(payTypes.Select(x => new { Value = x, Text = x.Humanize() }), "Value", "Text"),
                RFC = client.RFC,
                //Address = client.Address,
                City = client.City,
                SellerId = client.SellerId,
                CreditDays = client.CreditDays
            };

            model.Sellers = _userManager.Users.Select(s => new SelectListItem
            {
                Text = s.Name,
                Value = s.Id
            }).ToList();

            model.Sellers.Insert(0, new SelectListItem
            {
                Text = "Ninguno",
                Value = "0"
            });

            return PartialView("_AddEditModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientViewModel model)
        {
            if (!ModelState.IsValid) return Json(AjaxFunctions.GenerateJsonError("Datos inválidos"));

            try
            {
                var client = await _repository.GetByIdAsync<Client>(model.Id);

                if (client == null)
                    return Json(AjaxFunctions.GenerateJsonError("El cliente no existe"));

                client.Edit(model.Name);
                client.Channel = model.Channel;
                client.Classification = model.Classification;
                client.StateId = model.StateId;
                client.PayType = model.PayType;
                client.RFC = string.IsNullOrEmpty(model.RFC) ? model.RFC : model.RFC.ToUpper();
                //client.Address = model.Address;
                client.City = model.City;
                client.SellerId = model.SellerId;
                client.CreditDays = model.CreditDays;

                await _repository.UpdateAsync(client);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Cliente actualizado"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en ClientController --> Edit: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo actualizar el cliente"));
            }
        }

        [HttpGet("{controller}/Delete/{id}")]
        public async Task<IActionResult> DeleteView(int id)
        {
            var client = await _repository.GetByIdAsync<Client>(id);

            if (client == null)
                return Json(AjaxFunctions.GenerateJsonError("El cliente no existe"));

            ViewData["Action"] = nameof(Delete);
            ViewData["ModalTitle"] = "Eliminar cliente";
            ViewData["ModalDescription"] = $" el cliente de {client.Name}";

            return PartialView("_DeleteModal", client.Id.ToString());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var client = await _repository.GetByIdAsync<Client>(id);

                if (client == null)
                    return Json(AjaxFunctions.GenerateJsonError("El cliente no existe"));

                client.Delete();
                await _repository.UpdateAsync(client);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Cliente eliminado"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en ClientController --> Delete: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo eliminar el cliente"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteContactView(int id)
        {
            var contact = await _repository.GetByIdAsync<Contact>(id);

            if (contact == null)
                return Json(AjaxFunctions.GenerateJsonError("El contacto no existe"));

            ViewData["Action"] = nameof(DeleteContact);
            ViewData["ModalTitle"] = "Eliminar Contacto";
            ViewData["ModalDescription"] = $" el contacto de {contact.Name}";

            return PartialView("_DeleteModal", contact.Id.ToString());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(int id)
        {
            try
            {
                var contact = await _repository.GetByIdAsync<Contact>(id);

                if (contact == null)
                    return Json(AjaxFunctions.GenerateJsonError("El contacto no existe"));

                contact.Delete();
                await _repository.UpdateAsync(contact);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Contacto eliminado"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en ClientController --> DeleteContact: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo eliminar el contacto"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAddressView(int id)
        {
            var address = await _repository.GetByIdAsync<Address>(id);

            if (address == null)
                return Json(AjaxFunctions.GenerateJsonError("La dirección no existe"));

            ViewData["Action"] = nameof(DeleteAddress);
            ViewData["ModalTitle"] = "Eliminar dirección";
            ViewData["ModalDescription"] = $" la dirección {address.Name}";

            return PartialView("_DeleteModal", address.Id.ToString());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            try
            {
                var address = await _repository.GetByIdAsync<Address>(id);

                if (address == null)
                    return Json(AjaxFunctions.GenerateJsonError("La dirección no existe"));

                address.Delete();
                await _repository.UpdateAsync(address);

                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Ok, "Dirección eliminada"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error en ClientController --> DeleteAddress: " + e.Message);
                return Json(AjaxFunctions.GenerateAjaxResponse(ResultStatus.Error, "No se pudo eliminar la dirección"));
            }
        }

        #region AJAX
        [HttpGet]
        public IActionResult GetClients(string states)
        {
            var clients = string.IsNullOrEmpty(states)
                ? _repository.GetQueryableExisting(new ClientByStateFilterSpecification(new string[] { }))
                : _repository.GetQueryableExisting(new ClientByStateFilterSpecification(states.Split(',')));
            var model = clients.OrderBy(c => c.Name).Select(c => new
            {
                Value = $"{c.Id}",
                Text = c.Name
            });

            return Json(model);
        }
        #endregion  
    }
}