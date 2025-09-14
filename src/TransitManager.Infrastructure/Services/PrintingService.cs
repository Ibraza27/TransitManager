using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Interfaces;

namespace TransitManager.Infrastructure.Services
{
    public class PrintingService : IPrintingService
    {
        public Task<IEnumerable<string>> GetAvailablePrintersAsync()
        {
            return Task.Run(() =>
            {
                return PrinterSettings.InstalledPrinters.Cast<string>().ToList().AsEnumerable();
            });
        }

        public async Task<bool> PrintPdfAsync(byte[] pdfData, string printerName, int copies, string jobName = "TransitManager_Ticket")
        {
            // Vérifier si l'imprimante existe
            var printers = await GetAvailablePrintersAsync();
            if (!printers.Any(p => p.Equals(printerName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"L'imprimante '{printerName}' n'a pas été trouvée.");
            }

            // Créer un fichier temporaire pour le PDF
			 var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");

			try
			{
				await File.WriteAllBytesAsync(tempFilePath, pdfData);

				// La boucle pour les copies n'est plus nécessaire,
				// car l'utilisateur la choisira dans la boîte de dialogue d'impression.
				
				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = tempFilePath,
						// MODIFICATION : On utilise le verbe "Print" qui est plus fiable
						Verb = "Print", 
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden,
						UseShellExecute = true
					}
				};
				process.Start();

				// On n'a plus besoin d'attendre ici, le spouleur d'impression gère la suite.
				return true;
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				// On attend un peu avant de supprimer pour laisser le temps au spouleur de lire le fichier
				await Task.Delay(5000); 
				if (File.Exists(tempFilePath))
				{
					File.Delete(tempFilePath);
				}
			}
		}
    }
}