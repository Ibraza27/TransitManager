using System.Threading.Tasks;

namespace TransitManager.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage, List<(string Name, byte[] Content)>? attachments = null, List<string>? ccEmails = null, string? replyTo = null);
        Task SendNewMessageNotificationAsync(string to, string clientName, string portalLink, string companyName, string companyAddress, string companyPhone, string companyLogoUrl);
        Task SendMissingDocumentNotificationAsync(string to, string clientName, string documentType, string? note, string portalLink, string companyName, string companyAddress, string companyPhone, string companyLogoUrl);
    }
}