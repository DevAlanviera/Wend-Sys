using Microsoft.AspNetCore.Mvc;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;
using System.Threading.Tasks;

namespace WendlandtVentas.Web.Controllers
{
   [ApiController]
    [Route("api/[controller]")]
    public class BitacoraController : ControllerBase
    {
        private readonly IBitacoraService _bitacoraService;

        public BitacoraController(IBitacoraService bitacoraService)
        {
            _bitacoraService = bitacoraService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBitacora(Bitacora bitacora)
        {
            await _bitacoraService.AddAsync(bitacora);
            return Ok();
        }
    }
}