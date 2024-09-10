using System.Text.Encodings.Web;
using System.Threading.Tasks;
using WendlandtVentas.Core.Interfaces;

namespace WendlandtVentas.Web.Extensions
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link)
        {
            return emailSender.SendEmailAsync(email, "Confirma tu correo electrónico",
                $"Por favor confirma tu cuenta haciendo clic en el siguiente enlace: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
        }
    }
}
