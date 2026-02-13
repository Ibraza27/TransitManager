using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using TransitManager.Core.Interfaces;
using System;
using System.Linq;

namespace TransitManager.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage, List<(string Name, byte[] Content)>? attachments = null, List<string>? ccEmails = null, string? replyTo = null)
        {
            try
            {
                Console.WriteLine($"[EmailService] Tentative d'envoi à {to}...");

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_configuration["EmailSettings:FromName"], _configuration["EmailSettings:FromEmail"]));
                
                // Parse multiple recipients (comma or semicolon separated)
                var recipientList = to.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e) && e.Contains("@"));
                
                foreach (var recipient in recipientList)
                {
                    email.To.Add(new MailboxAddress("", recipient));
                }
                
                if (!email.To.Any())
                {
                    throw new ArgumentException("Aucun destinataire valide trouvé.");
                }
                if (ccEmails != null)
                {
                    foreach(var cc in ccEmails)
                    {
                         if(!string.IsNullOrWhiteSpace(cc)) email.Cc.Add(new MailboxAddress("", cc));
                    }
                }
                
                // Set Reply-To header if provided
                if (!string.IsNullOrWhiteSpace(replyTo) && replyTo.Contains("@"))
                {
                    email.ReplyTo.Add(new MailboxAddress("", replyTo.Trim()));
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
        public async Task SendNewMessageNotificationAsync(string to, string clientName, string portalLink, string companyName, string companyAddress, string companyPhone, string companyLogoUrl)
        {
            var subject = "Nouveau message dans votre espace client";
            var htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background-color: #333333; color: #ffffff; padding: 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 20px; font-weight: 600; }}
        .logo {{ max-width: 150px; height: auto; margin-bottom: 10px; }}
        .content {{ padding: 30px; color: #333333; line-height: 1.6; }}
        .btn {{ display: inline-block; background-color: #dc3545; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold; margin-top: 20px; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; font-size: 12px; color: #6c757d; text-align: center; border-top: 1px solid #dee2e6; }}
        .footer strong {{ color: #333333; }}
        .footer a {{ color: #6c757d; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <!-- Logo if available -->
            <img src='{companyLogoUrl}' alt='{companyName}' class='logo' onerror=""this.style.display='none'""/>
        </div>
        
        <div class='content'>
            <p>Bonjour,</p>
            <p>Vous venez de recevoir un nouveau message dans votre espace client.</p>
            <p>Pour le consulter, rendez-vous dans le centre de notification de votre site pour y accèder rapidement.</p>
            
            <div style='text-align: center;'>
                <a href='{portalLink}' class='btn'>Accéder à mon espace</a>
            </div>

            <p style='margin-top: 30px;'>Merci de votre confiance,</p>
            <p><strong>A très bientôt</strong></p>
            
            <p style='font-style: italic; font-size: 13px; color: #888; margin-top: 20px;'>Ce message règlementaire vous a été envoyé de manière automatique. Merci de ne pas y répondre.</p>
        </div>

        <div class='footer'>
            <p>
                <strong>{companyName}</strong><br/>
                {companyAddress}<br/>
                Tél: {companyPhone}
            </p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(to, subject, htmlMessage);
        }

        public async Task SendMissingDocumentNotificationAsync(string to, string clientName, string documentType, string? note, string portalLink, string companyName, string companyAddress, string companyPhone, string companyLogoUrl)
        {
            var subject = $"Document manquant : {documentType}";
            var noteHtml = !string.IsNullOrEmpty(note) ? $"<div style='background-color: #fff3cd; color: #856404; padding: 10px; border-radius: 4px; margin-top: 10px; border: 1px solid #ffeeba;'><strong>Note :</strong> {note}</div>" : "";

            var htmlMessage = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background-color: #ffffff; padding: 20px; text-align: center; border-bottom: 2px solid #f0f0f0; }}
        .logo {{ max-width: 150px; height: auto; }}
        .content {{ padding: 30px; color: #333333; line-height: 1.6; }}
        .btn-container {{ text-align: center; margin-top: 20px; margin-bottom: 20px; }}
        .btn {{ display: inline-block; background-color: #dc3545; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; font-size: 12px; color: #6c757d; text-align: center; border-top: 1px solid #dee2e6; }}
        .footer strong {{ color: #333333; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
             <img src='{companyLogoUrl}' alt='{companyName}' class='logo' onerror=""this.style.display='none'"" />
        </div>
        
        <div class='content'>
            <p>Bonjour {clientName},</p>
            <p>Nous avons besoin d'un document complémentaire pour votre dossier.</p>
            <p><strong>Document requis :</strong> <span style='color: #dc3545; font-weight: bold;'>{documentType}</span></p>
            
            {noteHtml}

            <p>Merci de le téléverser dès que possible via votre espace client.</p>
            
            <div class='btn-container'>
                <a href='{portalLink}' class='btn'>Accéder à mon espace</a>
            </div>

            <p style='margin-top: 30px;'>Merci de votre confiance,</p>
            <p><strong>{companyName}</strong></p>
            
            <p style='font-style: italic; font-size: 13px; color: #888;'>Ce message automatique ne nécessite pas de réponse.</p>
        </div>

        <div class='footer'>
            <p>
                <strong>{companyName}</strong><br/>
                {companyAddress}<br/>
                Tél: {companyPhone}
            </p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(to, subject, htmlMessage);
        }
    }
}
