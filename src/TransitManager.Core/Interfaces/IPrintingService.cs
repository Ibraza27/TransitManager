using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitManager.Core.Interfaces
{
    public interface IPrintingService
    {
        Task<IEnumerable<string>> GetAvailablePrintersAsync();
        Task<bool> PrintPdfAsync(byte[] pdfData, string printerName, int copies, string jobName = "TransitManager_Ticket");
    }
}