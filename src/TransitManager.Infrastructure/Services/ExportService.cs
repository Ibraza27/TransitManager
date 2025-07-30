using ClosedXML.Excel;
using PdfDocument = QuestPDF.Fluent.Document;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using QuestPDF.Fluent;
using TransitManager.Core.Interfaces;

namespace TransitManager.Infrastructure.Services
{

    public class ExportService : IExportService
    {
        static ExportService()
        {
            // Configuration QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> ExportClientsToExcelAsync(IEnumerable<Client> clients)
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Clients");

                // En-têtes
                worksheet.Cell(1, 1).Value = "Code Client";
                worksheet.Cell(1, 2).Value = "Nom";
                worksheet.Cell(1, 3).Value = "Prénom";
                worksheet.Cell(1, 4).Value = "Téléphone Principal";
                worksheet.Cell(1, 5).Value = "Email";
                worksheet.Cell(1, 6).Value = "Ville";
                worksheet.Cell(1, 7).Value = "Pays";
                worksheet.Cell(1, 8).Value = "Client Fidèle";
                worksheet.Cell(1, 9).Value = "Nombre d'envois";
                worksheet.Cell(1, 10).Value = "Volume Total (m³)";
                worksheet.Cell(1, 11).Value = "Balance (€)";
                worksheet.Cell(1, 12).Value = "Date d'inscription";

                // Style des en-têtes
                var headerRange = worksheet.Range(1, 1, 1, 12);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.BlueGray;
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Données
                int row = 2;
                foreach (var client in clients)
                {
                    worksheet.Cell(row, 1).Value = client.CodeClient;
                    worksheet.Cell(row, 2).Value = client.Nom;
                    worksheet.Cell(row, 3).Value = client.Prenom;
                    worksheet.Cell(row, 4).Value = client.TelephonePrincipal;
                    worksheet.Cell(row, 5).Value = client.Email ?? "";
                    worksheet.Cell(row, 6).Value = client.Ville;
                    worksheet.Cell(row, 7).Value = client.Pays;
                    worksheet.Cell(row, 8).Value = client.EstClientFidele ? "Oui" : "Non";
                    worksheet.Cell(row, 9).Value = client.NombreTotalEnvois;
                    worksheet.Cell(row, 10).Value = client.VolumeTotalExpedié;
                    worksheet.Cell(row, 11).Value = client.BalanceTotal;
                    worksheet.Cell(row, 12).Value = client.DateInscription.ToString("dd/MM/yyyy");

                    // Colorer les lignes avec balance positive
                    if (client.BalanceTotal > 0)
                    {
                        worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.Red;
                        worksheet.Cell(row, 11).Style.Font.Bold = true;
                    }

                    row++;
                }

                // Auto-ajuster les colonnes
                worksheet.Columns().AdjustToContents();

                // Ajouter des filtres
                worksheet.RangeUsed().SetAutoFilter();

                // Totaux
                worksheet.Cell(row + 1, 8).Value = "TOTAUX:";
                worksheet.Cell(row + 1, 8).Style.Font.Bold = true;
                worksheet.Cell(row + 1, 9).FormulaA1 = $"=SUM(I2:I{row - 1})";
                worksheet.Cell(row + 1, 10).FormulaA1 = $"=SUM(J2:J{row - 1})";
                worksheet.Cell(row + 1, 11).FormulaA1 = $"=SUM(K2:K{row - 1})";

                // Formater les nombres
                worksheet.Range(2, 10, row + 1, 10).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Range(2, 11, row + 1, 11).Style.NumberFormat.Format = "#,##0.00 €";

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            });
        }

        public async Task<byte[]> ExportColisToExcelAsync(IEnumerable<Colis> colis)
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Colis");

                // En-têtes
                worksheet.Cell(1, 1).Value = "Code-barres";
                worksheet.Cell(1, 2).Value = "Référence";
                worksheet.Cell(1, 3).Value = "Client";
                worksheet.Cell(1, 4).Value = "Conteneur";
                worksheet.Cell(1, 5).Value = "Désignation";
                worksheet.Cell(1, 6).Value = "Poids (kg)";
                worksheet.Cell(1, 7).Value = "Volume (m³)";
                worksheet.Cell(1, 8).Value = "Statut";
                worksheet.Cell(1, 9).Value = "Date d'arrivée";
                worksheet.Cell(1, 10).Value = "Valeur déclarée (€)";
                worksheet.Cell(1, 11).Value = "Fragile";
                worksheet.Cell(1, 12).Value = "Localisation";

                // Style des en-têtes
                var headerRange = worksheet.Range(1, 1, 1, 12);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.DarkGreen;
                headerRange.Style.Font.FontColor = XLColor.White;

                // Données
                int row = 2;
                foreach (var item in colis)
                {
                    worksheet.Cell(row, 1).Value = item.CodeBarre;
                    worksheet.Cell(row, 2).Value = item.NumeroReference;
                    worksheet.Cell(row, 3).Value = item.Client?.NomComplet ?? "N/A";
                    worksheet.Cell(row, 4).Value = item.Conteneur?.NumeroDossier ?? "Non affecté";
                    worksheet.Cell(row, 5).Value = item.Designation;
                    worksheet.Cell(row, 6).Value = item.Poids;
                    worksheet.Cell(row, 7).Value = item.Volume;
                    worksheet.Cell(row, 8).Value = item.Statut.ToString();
                    worksheet.Cell(row, 9).Value = item.DateArrivee.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 10).Value = item.ValeurDeclaree;
                    worksheet.Cell(row, 11).Value = item.EstFragile ? "Oui" : "Non";
                    worksheet.Cell(row, 12).Value = item.LocalisationActuelle ?? "";

                    // Colorer selon le statut
                    var statusColor = item.Statut switch
                    {
                        StatutColis.EnAttente => XLColor.Orange,
                        StatutColis.EnTransit => XLColor.Blue,
                        StatutColis.Livre => XLColor.Green,
                        StatutColis.Probleme => XLColor.Red,
                        _ => XLColor.Gray
                    };
                    worksheet.Cell(row, 8).Style.Fill.BackgroundColor = statusColor;
                    worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.White;

                    row++;
                }

                worksheet.Columns().AdjustToContents();
                worksheet.RangeUsed().SetAutoFilter();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            });
        }

        public async Task<byte[]> ExportConteneurManifestAsync(Conteneur conteneur)
        {
            return await Task.Run(() =>
            {
                var document = PdfDocument.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .Text("MANIFESTE D'EXPÉDITION")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(x =>
                            {
                                x.Spacing(20);

                                // Informations du conteneur
                                x.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
                                {
                                    column.Item().Text($"Numéro de dossier: {conteneur.NumeroDossier}").FontSize(14).SemiBold();
                                    column.Item().Text($"Destination: {conteneur.Destination}, {conteneur.PaysDestination}");
                                    column.Item().Text($"Date de départ prévue: {conteneur.DateDepartPrevue?.ToString("dd/MM/yyyy") ?? "Non définie"}");
                                    column.Item().Text($"Transporteur: {conteneur.Transporteur ?? "Non défini"}");
                                    column.Item().Text($"Numéro de tracking: {conteneur.NumeroTracking ?? "Non défini"}");
                                });

                                // Statistiques
                                x.Item().Row(row =>
                                {
                                    row.RelativeItem().Border(1).Padding(5).Text($"Nombre de colis: {conteneur.NombreColis}");
                                    row.RelativeItem().Border(1).Padding(5).Text($"Poids total: {conteneur.PoidsUtilise:N2} kg");
                                    row.RelativeItem().Border(1).Padding(5).Text($"Volume total: {conteneur.VolumeUtilise:N2} m³");
                                });

                                // Liste des colis
                                x.Item().Text("LISTE DES COLIS").FontSize(14).SemiBold();
                                x.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(50);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(4);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    // En-têtes
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("#");
                                        header.Cell().Element(CellStyle).Text("Code-barres");
                                        header.Cell().Element(CellStyle).Text("Client");
                                        header.Cell().Element(CellStyle).Text("Désignation");
                                        header.Cell().Element(CellStyle).Text("Poids");
                                        header.Cell().Element(CellStyle).Text("Volume");
                                        header.Cell().Element(CellStyle).Text("Valeur");

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                        }
                                    });

                                    // Données
                                    var index = 1;
                                    foreach (var colis in conteneur.Colis.OrderBy(c => c.Client?.Nom))
                                    {
                                        table.Cell().Element(CellStyle).Text(index++);
                                        table.Cell().Element(CellStyle).Text(colis.CodeBarre);
                                        table.Cell().Element(CellStyle).Text(colis.Client?.NomComplet ?? "N/A");
                                        table.Cell().Element(CellStyle).Text(colis.Designation);
                                        table.Cell().Element(CellStyle).Text($"{colis.Poids:N2} kg");
                                        table.Cell().Element(CellStyle).Text($"{colis.Volume:N2} m³");
                                        table.Cell().Element(CellStyle).Text($"{colis.ValeurDeclaree:C}");

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                        }
                                    }
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                    });
                });

                return document.GeneratePdf();
            });
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(Client client, IEnumerable<Colis> colis, decimal montant)
        {
            return await Task.Run(() =>
            {
                var document = PdfDocument.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);

                        page.Header().Element(ComposeHeader);
                        page.Content().Element(ComposeContent);
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Transit Manager - ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });

                        void ComposeHeader(IContainer container)
                        {
                            container.Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().Text("FACTURE").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                                    column.Item().Text($"N° {DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}").FontSize(12);
                                    column.Item().Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
                                });

                                row.ConstantItem(150).Text("TRANSIT MANAGER\n123 Rue du Commerce\n75001 Paris\nTél: 01 23 45 67 89")
                                    .FontSize(10).AlignRight();
                            });
                        }

                        void ComposeContent(IContainer container)
                        {
                            container.PaddingVertical(20).Column(column =>
                            {
                                column.Spacing(10);

                                // Client
                                column.Item().Background(Colors.Grey.Lighten4).Padding(10).Text(text =>
                                {
                                    text.Span("Client: ").SemiBold();
                                    text.Span($"{client.NomComplet}\n{client.AdresseComplete}\nTél: {client.TelephonePrincipal}");
                                });

                                // Détails
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(4);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderStyle).Text("Référence");
                                        header.Cell().Element(HeaderStyle).Text("Description");
                                        header.Cell().Element(HeaderStyle).Text("Poids");
                                        header.Cell().Element(HeaderStyle).Text("Volume");
                                        header.Cell().Element(HeaderStyle).Text("Montant");

                                        static IContainer HeaderStyle(IContainer container)
                                        {
                                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                        }
                                    });

                                    decimal sousTotal = 0;
                                    foreach (var item in colis)
                                    {
                                        var tarif = item.PoidsFacturable * 2.5m;
                                        sousTotal += tarif;

                                        table.Cell().Text(item.NumeroReference);
                                        table.Cell().Text(item.Designation);
                                        table.Cell().Text($"{item.Poids:N2} kg");
                                        table.Cell().Text($"{item.Volume:N2} m³");
                                        table.Cell().AlignRight().Text($"{tarif:C}");
                                    }
                                });

                                // Totaux
                                column.Item().AlignRight().Column(col =>
                                {
                                    col.Item().Row(row =>
                                    {
                                        row.ConstantItem(100).Text("Total HT:");
                                        row.ConstantItem(100).AlignRight().Text($"{montant:C}");
                                    });
                                    col.Item().Row(row =>
                                    {
                                        row.ConstantItem(100).Text("TVA (20%):");
                                        row.ConstantItem(100).AlignRight().Text($"{montant * 0.2m:C}");
                                    });
                                    col.Item().Row(row =>
                                    {
                                        row.ConstantItem(100).Text("Total TTC:").SemiBold();
                                        row.ConstantItem(100).AlignRight().Text($"{montant * 1.2m:C}").SemiBold();
                                    });
                                });
                            });
                        }
                    });
                });

                return document.GeneratePdf();
            });
        }

        public async Task<byte[]> GenerateReceiptPdfAsync(Paiement paiement)
        {
            return await Task.Run(() =>
            {
                var document = PdfDocument.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A5.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);

                        page.Content().Column(column =>
                        {
                            column.Spacing(10);

                            // En-tête
                            column.Item().AlignCenter().Text("REÇU DE PAIEMENT").FontSize(18).SemiBold();
                            column.Item().AlignCenter().Text($"N° {paiement.NumeroRecu}").FontSize(12);

                            // Informations
                            column.Item().Border(1).Padding(10).Column(col =>
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Date: {paiement.DatePaiement:dd/MM/yyyy HH:mm}");
                                    row.RelativeItem().AlignRight().Text($"Montant: {paiement.Montant:C}").FontSize(14).SemiBold();
                                });

                                col.Item().Text($"Client: {paiement.Client?.NomComplet ?? "N/A"}");
                                col.Item().Text($"Mode de paiement: {GetPaymentTypeLabel(paiement.ModePaiement)}");
                                
                                if (!string.IsNullOrEmpty(paiement.Reference))
                                    col.Item().Text($"Référence: {paiement.Reference}");
                                
                                if (!string.IsNullOrEmpty(paiement.Description))
                                    col.Item().Text($"Description: {paiement.Description}");
                            });

                            // Signature
                            column.Item().PaddingTop(20).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Signature du client:");
                                    col.Item().Height(40).Border(1);
                                });
                                
                                row.ConstantItem(50);
                                
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Cachet et signature:");
                                    col.Item().Height(40).Border(1);
                                });
                            });
                        });
                    });
                });

                return document.GeneratePdf();
            });
        }

        public async Task<byte[]> GenerateDashboardReportAsync(DashboardData data)
        {
            // TODO: Implémenter la génération du rapport de tableau de bord
            return await Task.FromResult(Array.Empty<byte>());
        }

        public async Task<byte[]> ExportFinancialReportAsync(DateTime startDate, DateTime endDate, IEnumerable<Paiement> paiements)
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Rapport Financier");

                // Titre
                worksheet.Cell(1, 1).Value = $"RAPPORT FINANCIER - Du {startDate:dd/MM/yyyy} au {endDate:dd/MM/yyyy}";
                worksheet.Range(1, 1, 1, 8).Merge().Style.Font.Bold = true;
                worksheet.Range(1, 1, 1, 8).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // En-têtes
                var row = 3;
                worksheet.Cell(row, 1).Value = "Date";
                worksheet.Cell(row, 2).Value = "N° Reçu";
                worksheet.Cell(row, 3).Value = "Client";
                worksheet.Cell(row, 4).Value = "Conteneur";
                worksheet.Cell(row, 5).Value = "Mode";
                worksheet.Cell(row, 6).Value = "Référence";
                worksheet.Cell(row, 7).Value = "Montant";
                worksheet.Cell(row, 8).Value = "Statut";

                var headerRange = worksheet.Range(row, 1, row, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                headerRange.Style.Font.FontColor = XLColor.White;

                // Données
                row++;
                decimal total = 0;
                foreach (var paiement in paiements.OrderBy(p => p.DatePaiement))
                {
                    worksheet.Cell(row, 1).Value = paiement.DatePaiement.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 2).Value = paiement.NumeroRecu;
                    worksheet.Cell(row, 3).Value = paiement.Client?.NomComplet ?? "N/A";
                    worksheet.Cell(row, 4).Value = paiement.Conteneur?.NumeroDossier ?? "-";
                    worksheet.Cell(row, 5).Value = GetPaymentTypeLabel(paiement.ModePaiement);
                    worksheet.Cell(row, 6).Value = paiement.Reference ?? "-";
                    worksheet.Cell(row, 7).Value = paiement.Montant;
                    worksheet.Cell(row, 8).Value = paiement.Statut.ToString();

                    if (paiement.Statut == StatutPaiement.Paye)
                        total += paiement.Montant;

                    row++;
                }

                // Total
                worksheet.Cell(row + 1, 6).Value = "TOTAL:";
                worksheet.Cell(row + 1, 6).Style.Font.Bold = true;
                worksheet.Cell(row + 1, 7).Value = total;
                worksheet.Cell(row + 1, 7).Style.Font.Bold = true;

                // Formatage
                worksheet.Range(4, 7, row + 1, 7).Style.NumberFormat.Format = "#,##0.00 €";
                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            });
        }

        public async Task<string> ExportToCsvAsync<T>(IEnumerable<T> data, string fileName)
        {
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            
            await using var writer = new StreamWriter(filePath);
            var properties = typeof(T).GetProperties();
            
            // En-têtes
            await writer.WriteLineAsync(string.Join(";", properties.Select(p => p.Name)));
            
            // Données
            foreach (var item in data)
            {
                var values = properties.Select(p => p.GetValue(item)?.ToString() ?? "");
                await writer.WriteLineAsync(string.Join(";", values));
            }
            
            return filePath;
        }

        private string GetPaymentTypeLabel(TypePaiement type)
        {
            return type switch
            {
                TypePaiement.Especes => "Espèces",
                TypePaiement.Virement => "Virement",
                TypePaiement.Cheque => "Chèque",
                TypePaiement.CarteBancaire => "Carte bancaire",
                TypePaiement.MobileMoney => "Mobile Money",
                TypePaiement.EnLigne => "En ligne",
                _ => "Autre"
            };
        }
    }

}