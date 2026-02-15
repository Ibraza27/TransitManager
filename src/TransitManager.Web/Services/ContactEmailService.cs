using System.Net;
using System.Net.Mail;

namespace TransitManager.Web.Services;

public class ContactEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ContactEmailService> _logger;

    public ContactEmailService(IConfiguration config, ILogger<ContactEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendContactEmailAsync(string typeMarchandise, string prenom, string nom, string email, string telephone, string message)
    {
        try
        {
            var smtpHost = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
            var smtpUser = _config["EmailSettings:SmtpUsername"] ?? "";
            var smtpPass = _config["EmailSettings:SmtpPassword"] ?? "";
            var toEmail = "contact@hippocampeimportexport.com";

            var subject = $"[Devis] {typeMarchandise} - {prenom} {nom}";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; padding: 20px;'>
<h2 style='color: #E8752A;'>Nouvelle demande de devis</h2>
<table style='border-collapse: collapse; width: 100%;'>
<tr><td style='padding: 8px; font-weight: bold; border-bottom: 1px solid #eee;'>Type de marchandise</td><td style='padding: 8px; border-bottom: 1px solid #eee;'>{typeMarchandise}</td></tr>
<tr><td style='padding: 8px; font-weight: bold; border-bottom: 1px solid #eee;'>Prénom</td><td style='padding: 8px; border-bottom: 1px solid #eee;'>{prenom}</td></tr>
<tr><td style='padding: 8px; font-weight: bold; border-bottom: 1px solid #eee;'>Nom</td><td style='padding: 8px; border-bottom: 1px solid #eee;'>{nom}</td></tr>
<tr><td style='padding: 8px; font-weight: bold; border-bottom: 1px solid #eee;'>Email</td><td style='padding: 8px; border-bottom: 1px solid #eee;'><a href='mailto:{email}'>{email}</a></td></tr>
<tr><td style='padding: 8px; font-weight: bold; border-bottom: 1px solid #eee;'>Téléphone</td><td style='padding: 8px; border-bottom: 1px solid #eee;'>{telephone}</td></tr>
</table>
<h3 style='color: #0A1628; margin-top: 20px;'>Message</h3>
<div style='background: #f5f5f5; padding: 15px; border-radius: 8px; white-space: pre-wrap;'>{message}</div>
<hr style='margin-top: 30px; border: none; border-top: 1px solid #eee;' />
<p style='color: #999; font-size: 12px;'>Envoyé depuis le formulaire de contact Hippocampe Transit Manager</p>
</body>
</html>";

            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(smtpUser, "Hippocampe Transit Manager"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);
            mail.ReplyToList.Add(new MailAddress(email, $"{prenom} {nom}"));

            await smtp.SendMailAsync(mail);
            _logger.LogInformation("Contact email sent from {Email} - {Type}", email, typeMarchandise);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact email");
            return false;
        }
    }
}
