using MailKit.Net.Smtp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using WendlandtVentas.Core;
using WendlandtVentas.Core.Interfaces;

namespace WendlandtVentas.Infrastructure.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly BrandSettings _brandSettings;
        private readonly EmailSettings _emailSettings;
        private readonly IHostEnvironment _environment;

        public EmailSender(IHostEnvironment environment,
            IOptions<EmailSettings> emailSettings, IOptions<BrandSettings> brandSettings)
        {
            _environment = environment;
            _emailSettings = emailSettings.Value;
            _brandSettings = brandSettings.Value;
        }

        public async Task<bool> SendEmailAsync(string email, string subject, string message, string file = null)
        {
            var emailPath = Path.Combine(_environment.ContentRootPath, "wwwroot/resources/Email.html");
            try
            {
                var model = new EmailModel
                {
                    Email = email,
                    Subject = subject,
                    Message = message
                };

                var mimeMessage = new MimeMessage();
                var bodyBuilder = new BodyBuilder();

                var logo = Path.Combine(_environment.ContentRootPath, "wwwroot/images/logo.png");

                var image = bodyBuilder.LinkedResources.Add(logo);
                image.ContentId = MimeUtils.GenerateMessageId();
                model.Logo = $"cid:{image.ContentId}";
                model.By = _brandSettings.Name;
                model.Host = _brandSettings.Host;

                bodyBuilder.HtmlBody = File.ReadAllText(emailPath)
                    .Replace("{{by}}", model.By)
                    .Replace("{{host}}", model.Host)
                    .Replace("{{subject}}", model.Subject)
                    .Replace("{{message}}", model.Message)
                    .Replace("{{logo}}", model.Logo);

                mimeMessage.From.Add(new MailboxAddress(_brandSettings.ShortName, _emailSettings.From));
                mimeMessage.To.Add(new MailboxAddress(model.Email, model.Email));
                if (!string.IsNullOrEmpty(model.ReplyTo))
                    mimeMessage.ReplyTo.Add(new MailboxAddress(model.ReplyTo, model.ReplyTo));

                mimeMessage.Subject = model.Subject;
                mimeMessage.Body = bodyBuilder.ToMessageBody();

                if (!string.IsNullOrEmpty(file))
                    bodyBuilder.Attachments.Add(file);

                using (var client = new SmtpClient())
                {
                    client.Connect(_emailSettings.Server, _emailSettings.Port, _emailSettings.UseSsl);
                    await client.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);
                    client.Send(mimeMessage);
                    await client.DisconnectAsync(true);
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
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
}