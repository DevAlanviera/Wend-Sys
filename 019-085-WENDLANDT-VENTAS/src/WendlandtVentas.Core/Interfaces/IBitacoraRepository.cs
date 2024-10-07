using System.Collections.Generic;
using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IBitacoraRepository
    {
        Task AddAsync(Bitacora bitacora);
        Task<IEnumerable<Bitacora>> GetBitacorasByOrderIdAsync(int orderId);
        // Puedes agregar otros métodos si es necesario (por ejemplo, para obtener registros).

        Task AddEditLogAsync(int orderId, string userName,string Accion);
    }
}