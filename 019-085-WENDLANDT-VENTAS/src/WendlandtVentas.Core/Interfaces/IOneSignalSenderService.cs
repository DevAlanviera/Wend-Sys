using System.Threading.Tasks;
using WendlandtVentas.Core.Models.Enums;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IOneSignalService
    {
        Task<bool> SendNotificationAsync(Tag tag, string tagValue, string title, string message);
    }
}
