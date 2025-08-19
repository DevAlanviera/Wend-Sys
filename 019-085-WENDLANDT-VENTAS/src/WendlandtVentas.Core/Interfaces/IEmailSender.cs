using System.Threading.Tasks;

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