using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using TransitManager.Core.Interfaces;
using System;

namespace TransitManager.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage, List<(string Name, byte[] Content)>? attachments = null, List<string>? ccEmails = null)
        {
            try
            {
                Console.WriteLine($"[EmailService] Tentative d'envoi à {to}...");

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_configuration["EmailSettings:FromName"], _configuration["EmailSettings:FromEmail"]));
                email.To.Add(new MailboxAddress("", to));
                if (ccEmails != null)
                {
                    foreach(var cc in ccEmails)
                    {
                         if(!string.IsNullOrWhiteSpace(cc)) email.Cc.Add(new MailboxAddress("", cc));
                    }
                }
                email.Subject = subject;
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };

                if (attachments != null)
                {
                    foreach (var att in attachments)
                    {
                        if (att.Content != null && att.Content.Length > 0 && !string.IsNullOrEmpty(att.Name))
                        {
                            bodyBuilder.Attachments.Add(att.Name, att.Content);
                        }
                    }
                }

                email.Body = bodyBuilder.ToMessageBody();

                using var smtp = new SmtpClient();
                smtp.CheckCertificateRevocation = false;

                // Log des paramètres (masquez le mot de passe !)
                var host = _configuration["EmailSettings:SmtpHost"];
                var port = int.Parse(_configuration["EmailSettings:SmtpPort"]!);
                Console.WriteLine($"[EmailService] Connexion SMTP: {host}:{port}");

                await smtp.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_configuration["EmailSettings:SmtpUsername"], _configuration["EmailSettings:SmtpPassword"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                Console.WriteLine("[EmailService] ✅ Email envoyé avec succès !");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] 💥 ERREUR SMTP : {ex.Message}");
                // Important : Ne pas throw l'erreur en prod pour ne pas bloquer l'utilisateur,
                // mais en dev c'est utile de savoir.
                throw;
            }
        }
    }
}
