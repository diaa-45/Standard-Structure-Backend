using Application.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Application.Infrastructure.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            var smtpUser = _config["Smtp:User"];
            var smtpPass = _config["Smtp:Pass"];
            var from = _config["Smtp:From"] ?? smtpUser;
            var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            // HTML body
            message.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            using var client = new MailKit.Net.Smtp.SmtpClient();

            // Choose the correct SecureSocketOptions based on your provider
            var secureOptions = enableSsl
                ? (smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                : SecureSocketOptions.None;

            await client.ConnectAsync(smtpHost, smtpPort, secureOptions);

            if (!string.IsNullOrWhiteSpace(smtpUser))
            {
                await client.AuthenticateAsync(smtpUser, smtpPass);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}