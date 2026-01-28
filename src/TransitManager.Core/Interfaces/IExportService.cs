using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.DTOs.Commerce;

namespace TransitManager.Core.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> ExportClientsToExcelAsync(IEnumerable<Client> clients);
        Task<byte[]> ExportColisToExcelAsync(IEnumerable<Colis> colis);
        Task<byte[]> ExportConteneurManifestAsync(Conteneur conteneur);
        Task<byte[]> GenerateInvoicePdfAsync(Client client, IEnumerable<Colis> colis, decimal montant);
        Task<byte[]> GenerateInvoicePdfAsync(InvoiceDto invoice);
        Task<byte[]> GenerateReceiptPdfAsync(Paiement paiement);
        Task<byte[]> GenerateDashboardReportAsync(DashboardData data);
        Task<byte[]> ExportFinancialReportAsync(DateTime startDate, DateTime endDate, IEnumerable<Paiement> paiements);
        Task<string> ExportToCsvAsync<T>(IEnumerable<T> data, string fileName);
        Task<byte[]> GenerateQuotePdfAsync(QuoteDto quote);
		Task<byte[]> ExportConteneurDetailToPdfAsync(Conteneur conteneur);
		Task<byte[]> GenerateColisTicketPdfAsync(Colis colis, string format = "thermal");
		Task<byte[]> GenerateContainerPdfAsync(Conteneur conteneur, bool includeFinancials);
		Task<byte[]> GenerateVehiculePdfAsync(Vehicule vehicule, bool includeFinancials, bool includePhotos);
		Task<byte[]> GenerateColisPdfAsync(Colis colis, bool includeFinancials, bool includePhotos);
		Task<byte[]> GenerateAttestationValeurPdfAsync(Vehicule vehicule);
    }


}