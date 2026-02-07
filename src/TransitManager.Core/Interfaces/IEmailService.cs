using System.Threading.Tasks;

namespace TransitManager.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage, List<(string Name, byte[] Content)>? attachments = null, List<string>? ccEmails = null, string? replyTo = null);
    }
}