using System.Threading.Tasks;
using WendlandtVentas.Core.Entities;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IBitacoraService
    {
        Task AddAsync(Bitacora bitacoraEntry);
        // Agrega otros métodos según sea necesario, como obtener entradas de la bitácora
    }
}