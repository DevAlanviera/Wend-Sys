using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core.Services;

namespace WendlandtVentas.Core.Services
{
    public class BitacoraService : IBitacoraService
    {
        private readonly IBitacoraRepository _bitacoraRepository;

        public BitacoraService(IBitacoraRepository bitacoraRepository)
        {
            _bitacoraRepository = bitacoraRepository;
        }

        //Metodo para agregar registro a la bitacora con el id de la orden, nombre del usuario y fecha que se agrego
        public async Task AddAsync(Bitacora log)
        {
            await _bitacoraRepository.AddAsync(log);
        }

        //Metodo para registrar cambios a la bitacora con el id de la orden y el usuario
       


    }
}