using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using WendlandtVentas.Core.Interfaces;
using WendlandtVentas.Core;
using System.IO;
using System;

public class EmailSender : IEmailSender
{
    private readonly BrandSettings _brandSettings;
    private readonly IOptionsSnapshot<EmailSettings> _emailSettingsSnapshot;
    private readonly IHostEnvironment _environment;

    public EmailSender(IHostEnvironment environment,
        IOptionsSnapshot<EmailSettings> emailSettingsSnapshot,
        IOptions<BrandSettings> brandSettings)
    {
        _environment = environment;
        _emailSettingsSnapshot = emailSettingsSnapshot;
        _brandSettings = brandSettings.Value;
    }

    public async Task<bool> SendEmailAsync(string email,
    string subject,
    string message,
    string file = null,
    byte[] attachmentBytes = null,
    string attachmentName = null,
    string perfil = "Email")
    {
        var settings = _emailSettingsSnapshot.Get(perfil);

        using var client = new SmtpClient(settings.Server, settings.Port)
        {
            Credentials = new NetworkCredential(settings.UserName, settings.Password),
            EnableSsl = settings.UseSsl
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(settings.From, _brandSettings.Name),
            Subject = subject,
            Body = message,
            IsBodyHtml = true
        };

        mail.To.Add(email);

        // Adjuntar archivo físico (si aplica)
        if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
        {
            mail.Attachments.Add(new Attachment(file));
        }

        // Adjuntar PDF desde memoria
        Attachment attachment = null;
        MemoryStream ms = null;

        if (attachmentBytes != null && !string.IsNullOrWhiteSpace(attachmentName))
        {
            ms = new MemoryStream(attachmentBytes);
            ms.Position = 0; // <-- muy importante, por si el stream no está al inicio
            attachment = new Attachment(ms, attachmentName, "application/pdf");
            mail.Attachments.Add(attachment);
        }

        try
        {
            await client.SendMailAsync(mail);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando correo: {ex}");
            return false;
        }
        finally
        {
            // Ahora sí, liberar todo manualmente y en orden
            attachment?.Dispose();
            ms?.Dispose();
        }
    }



}


public class EmailModel
        {
            public string Email { get; set; }
            public string Subject { get; set; }
            public string Message { get; set; }
            public string By { get; set; }
            public string Host { get; set; }
            public string Logo { get; set; }
            public string ReplyTo { get; set; }
        }