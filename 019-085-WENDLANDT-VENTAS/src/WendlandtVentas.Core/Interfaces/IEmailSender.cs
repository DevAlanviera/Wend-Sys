using System.Collections.Generic;
using System.Threading.Tasks;
using WendlandtVentas.Core.DTO;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string email,
    string subject,
    string message,
    string file = null,
    byte[] attachmentBytes = null,
    string attachmentName = null,
    string perfil = "Email");
    }
}