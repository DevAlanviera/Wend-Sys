using System.Threading.Tasks;

namespace WendlandtVentas.Core.Interfaces
{
   public interface IHttpIdentityServerService
    {  
        Task<string> GetToken();
    }
}
