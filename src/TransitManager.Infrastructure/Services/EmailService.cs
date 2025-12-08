using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using TransitManager.Core.Interfaces;

namespace TransitManager.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_configuration["EmailSettings:FromName"], _configuration["EmailSettings:FromEmail"]));
            email.To.Add(new MailboxAddress("", to));
            email.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };
            email.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            // Accepter les certificats SSL auto-signés si nécessaire (dev)
            smtp.CheckCertificateRevocation = false; 
            
            await smtp.ConnectAsync(
                _configuration["EmailSettings:SmtpHost"], 
                int.Parse(_configuration["EmailSettings:SmtpPort"]!), 
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _configuration["EmailSettings:SmtpUsername"], 
                _configuration["EmailSettings:SmtpPassword"]
            );

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}