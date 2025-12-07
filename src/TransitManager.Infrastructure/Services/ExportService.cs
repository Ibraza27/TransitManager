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
using System.Text.Json;
using QuestPDF.Elements;
using SkiaSharp;
using Microsoft.Extensions.Configuration;

namespace TransitManager.Infrastructure.Services
{
    public class ExportService : IExportService
    {
        private readonly string _storageRootPath;

        static ExportService()
        {
            // Configuration QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ExportService(IConfiguration configuration)
        {
            // On récupère le chemin de stockage défini dans appsettings.json
            // ou on utilise un chemin par défaut relatif à l'exécution.
            _storageRootPath = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "Storage");
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
                    worksheet.Cell(row, 9).Value = client.NombreConteneursUniques;
                    worksheet.Cell(row, 10).Value = client.VolumeTotalExpedié;
                    worksheet.Cell(row, 11).Value = client.Impayes;
                    worksheet.Cell(row, 12).Value = client.DateInscription.ToString("dd/MM/yyyy");
                    // Colorer les lignes avec balance positive
                    if (client.Impayes > 0)
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
                // worksheet.Cell(1, 6).Value = "Poids (kg)"; // SUPPRIMÉ
                worksheet.Cell(1, 6).Value = "Volume (m³)"; // MODIFIÉ (indice 6 au lieu de 7)
                worksheet.Cell(1, 7).Value = "Statut"; // MODIFIÉ (indice 7 au lieu de 8)
                worksheet.Cell(1, 8).Value = "Date d'arrivée"; // MODIFIÉ (indice 8 au lieu de 9)
                worksheet.Cell(1, 9).Value = "Valeur déclarée (€)"; // MODIFIÉ (indice 9 au lieu de 10)
                worksheet.Cell(1, 10).Value = "Fragile"; // MODIFIÉ (indice 10 au lieu de 11)
                worksheet.Cell(1, 11).Value = "Localisation"; // MODIFIÉ (indice 11 au lieu de 12)
                // Style des en-têtes
                var headerRange = worksheet.Range(1, 1, 1, 12);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.DarkGreen;
                headerRange.Style.Font.FontColor = XLColor.White;
                // Données
                int row = 2;
                foreach (var item in colis)
                {
                    worksheet.Cell(row, 1).Value = string.Join(", ", item.Barcodes.Select(b => b.Value));
                    worksheet.Cell(row, 2).Value = item.NumeroReference;
                    worksheet.Cell(row, 3).Value = item.Client?.NomComplet ?? "N/A";
                    worksheet.Cell(row, 4).Value = item.Conteneur?.NumeroDossier ?? "Non affecté";
                    worksheet.Cell(row, 5).Value = item.Designation;
                    // worksheet.Cell(row, 6).Value = item.Poids; // SUPPRIMÉ
                    worksheet.Cell(row, 6).Value = item.Volume; // MODIFIÉ (indice 6)
                    worksheet.Cell(row, 7).Value = item.Statut.ToString(); // MODIFIÉ (indice 7)
                    worksheet.Cell(row, 8).Value = item.DateArrivee.ToString("dd/MM/yyyy"); // MODIFIÉ (indice 8)
                    worksheet.Cell(row, 9).Value = item.ValeurDeclaree; // MODIFIÉ (indice 9)
                    worksheet.Cell(row, 10).Value = item.EstFragile ? "Oui" : "Non"; // MODIFIÉ (indice 10)
                    worksheet.Cell(row, 11).Value = item.LocalisationActuelle ?? ""; // MODIFIÉ (indice 11)
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
                                    column.Item().Text($"Date de départ: {conteneur.DateDepart?.ToString("dd/MM/yyyy") ?? "Non définie"}");
                                    column.Item().Text($"Compagnie: {conteneur.NomCompagnie ?? "Non défini"}");
                                    column.Item().Text($"N° Plomb: {conteneur.NumeroPlomb ?? "Non défini"}");
                                });
                                // Statistiques
                                x.Item().Row(row =>
                                {
                                    row.RelativeItem().Border(1).Padding(5).Text($"Nombre de colis: {conteneur.NombreColis}");
                                    row.RelativeItem().Border(1).Padding(5).Text($"Nombre de véhicules: {conteneur.NombreVehicules}");
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
                                    });
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("#");
                                        header.Cell().Element(CellStyle).Text("Code-barres");
                                        header.Cell().Element(CellStyle).Text("Client");
                                        header.Cell().Element(CellStyle).Text("Désignation");
                                        header.Cell().Element(CellStyle).Text("Volume");
                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                        }
                                    });
                                    var index = 1;
                                    foreach (var colis in conteneur.Colis.OrderBy(c => c.Client?.Nom))
                                    {
                                        table.Cell().Element(CellStyle).Text(index++);
                                        table.Cell().Element(CellStyle).Text(colis.AllBarcodes);
                                        table.Cell().Element(CellStyle).Text(colis.Client?.NomComplet ?? "N/A");
                                        table.Cell().Element(CellStyle).Text(colis.Designation);
                                        // CORRECTION : On affiche le Volume à la place du Poids
                                        table.Cell().Element(CellStyle).Text($"{colis.Volume:N3} m³");
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
                                        // var tarif = item.PoidsFacturable * 2.5m; // ANCIENNE LIGNE
                                        var tarif = item.PrixTotal; // NOUVELLE LIGNE
                                        sousTotal += tarif;
                                        table.Cell().Text(item.NumeroReference);
                                        table.Cell().Text(item.Designation);
                                        // table.Cell().Text($"{item.Poids:N2} kg"); // SUPPRIMER CETTE LIGNE
                                        table.Cell().Text(""); // On laisse une cellule vide pour le poids
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

        public async Task<byte[]> ExportConteneurDetailToPdfAsync(Conteneur conteneur)
        {
            return await Task.Run(() =>
            {
                var document = PdfDocument.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        // MODIFICATION 1 : Police par défaut légèrement réduite
                        page.DefaultTextStyle(x => x.FontSize(9));
                        page.Header().Element(ComposeHeader);
                        page.Content().Element(container => ComposeContent(container, conteneur));
                        page.Footer().AlignCenter().Text(x =>
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

            // Fonctions d'aide pour la composition du document
            void ComposeHeader(IContainer container)
            {
                container.Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text($"Dossier Conteneur : {conteneur.NumeroDossier}")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"Destination : {conteneur.Destination}");
                    });
                    row.ConstantItem(150).AlignRight().Column(column =>
                    {
                        column.Item().Text($"Exporté le :").FontSize(8);
                        column.Item().Text($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}").SemiBold();
                    });
                });
            }

            void ComposeContent(IContainer container, Conteneur data)
            {
                container.PaddingVertical(20).Column(column =>
                {
                    column.Spacing(20);
                    // Section Infos générales et Dates
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(Block).Column(col => ComposeInfoGenerales(col, data));
                        row.ConstantItem(20); // Espace
                        row.RelativeItem().Element(Block).Column(col => ComposeDatesCles(col, data));
                    });
                    // Section Commentaires
                    if (!string.IsNullOrWhiteSpace(data.Commentaires))
                    {
                        column.Item().Element(Block).Column(col =>
                        {
                            col.Item().PaddingBottom(5).Text("Commentaires").SemiBold().FontSize(12);
                            col.Item().Text(data.Commentaires);
                        });
                    }
                    // Section Colis
                    if (data.Colis.Any())
                    {
                        column.Item().Element(container => ComposeTableColis(container, data.Colis));
                    }

                    // Section Véhicules
                    if (data.Vehicules.Any())
                    {
                        column.Item().Element(container => ComposeTableVehicules(container, data.Vehicules));
                    }
                });
            }

            void ComposeInfoGenerales(ColumnDescriptor column, Conteneur data)
            {
                column.Item().PaddingBottom(5).Text("Informations Générales").SemiBold().FontSize(12);
                column.Item().Grid(grid =>
                {
                    grid.Columns(2);
                    grid.Item(1).Text("N° Plomb:"); grid.Item(1).Text(data.NumeroPlomb ?? "N/A").SemiBold();
                    grid.Item(1).Text("Compagnie:"); grid.Item(1).Text(data.NomCompagnie ?? "N/A").SemiBold();
                    grid.Item(1).Text("Transitaire:"); grid.Item(1).Text(data.NomTransitaire ?? "N/A").SemiBold();
                    grid.Item(1).Text("Pays Dest.:"); grid.Item(1).Text(data.PaysDestination).SemiBold();
                });
            }

            void ComposeDatesCles(ColumnDescriptor column, Conteneur data)
            {
                column.Item().PaddingBottom(5).Text("Dates Clés").SemiBold().FontSize(12);
                column.Item().Grid(grid =>
                {
                    grid.Columns(2);
                    grid.Item(1).Text("Réception:"); grid.Item(1).Text(data.DateReception?.ToString("dd/MM/yyyy") ?? "-").SemiBold();
                    grid.Item(1).Text("Chargement:"); grid.Item(1).Text(data.DateChargement?.ToString("dd/MM/yyyy") ?? "-").SemiBold();
                    grid.Item(1).Text("Départ:"); grid.Item(1).Text(data.DateDepart?.ToString("dd/MM/yyyy") ?? "-").SemiBold();
                    grid.Item(1).Text("Arrivée:"); grid.Item(1).Text(data.DateArriveeDestination?.ToString("dd/MM/yyyy") ?? "-").SemiBold();
                    grid.Item(1).Text("Dédouanement:"); grid.Item(1).Text(data.DateDedouanement?.ToString("dd/MM/yyyy") ?? "-").SemiBold();
                    grid.Item(1).Text("Clôture:"); grid.Item(1).Text(data.DateCloture?.ToString("dd/MM/yyyy") ?? "-").SemiBold();
                });
            }

            void ComposeTableColis(IContainer container, ICollection<Colis> colisList)
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(1);
                        // MODIFICATION 2 : Ajout de la colonne pour le restant
                        columns.RelativeColumn(1);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Référence");
                        header.Cell().Element(CellStyle).Text("Client");
                        header.Cell().Element(CellStyle).Text("Désignation");
                        header.Cell().Element(CellStyle).AlignCenter().Text("Pièces");
                        header.Cell().Element(CellStyle).AlignRight().Text("Prix");
                        // MODIFICATION 2 : Ajout de l'en-tête
                        header.Cell().Element(CellStyle).AlignRight().Text("Restant");
                    });
                    foreach (var colis in colisList)
                    {
                        table.Cell().Element(CellStyle).Text(colis.NumeroReference);
                        table.Cell().Element(CellStyle).Text(colis.Client?.NomComplet ?? "N/A");
                        table.Cell().Element(CellStyle).Text(colis.Designation);
                        table.Cell().Element(CellStyle).AlignCenter().Text(colis.NombrePieces.ToString());
                        table.Cell().Element(CellStyle).AlignRight().Text($"{colis.PrixTotal:C}");

                        // MODIFICATION 2 : Ajout de la cellule avec couleur conditionnelle
                        var restant = colis.RestantAPayer;
                        var color = restant > 0 ? Colors.Red.Medium : Colors.Black;
                        table.Cell().Element(CellStyle).AlignRight().Text(text =>
                        {
                            text.Span($"{restant:C}").FontColor(color);
                        });
                        // Logique pour l'inventaire
                        if (!string.IsNullOrWhiteSpace(colis.InventaireJson) && colis.InventaireJson != "[]")
                        {
                            try
                            {
                                var items = JsonSerializer.Deserialize<List<InventaireItem>>(colis.InventaireJson);
                                if (items != null && items.Any())
                                {
                                    // On étend sur 6 colonnes maintenant
                                    table.Cell().ColumnSpan(6).PaddingLeft(20).PaddingTop(5).PaddingBottom(10)
                                        .Element(container => ComposeTableInventaire(container, items));
                                }
                            }
                            catch { /* Ignorer JSON invalide */ }
                        }
                    }
                });
            }

            void ComposeTableInventaire(IContainer container, List<InventaireItem> items)
            {
                container.Table(table =>
                {
                    // MODIFICATION 3 : On retire la 3ème colonne pour la valeur
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(50);
                    });

                    foreach (var item in items)
                    {
                        table.Cell().Padding(1).Text(item.Designation).FontSize(8).Italic();
                        table.Cell().Padding(1).AlignCenter().Text(item.Quantite.ToString()).FontSize(8).Italic();
                        // MODIFICATION 3 : On retire la cellule de la valeur
                    }
                });
            }

            void ComposeTableVehicules(IContainer container, ICollection<Vehicule> vehiculesList)
            {
                container.PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        // MODIFICATION 2 : Ajout de la colonne pour le restant
                        columns.RelativeColumn(1);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Immatriculation");
                        header.Cell().Element(CellStyle).Text("Client");
                        header.Cell().Element(CellStyle).Text("Marque & Modèle");
                        header.Cell().Element(CellStyle).AlignRight().Text("Prix");
                        // MODIFICATION 2 : Ajout de l'en-tête
                        header.Cell().Element(CellStyle).AlignRight().Text("Restant");
                    });

                    foreach (var vehicule in vehiculesList)
                    {
                        table.Cell().Element(CellStyle).Text(vehicule.Immatriculation);
                        table.Cell().Element(CellStyle).Text(vehicule.Client?.NomComplet ?? "N/A");
                        table.Cell().Element(CellStyle).Text($"{vehicule.Marque} {vehicule.Modele}");
                        table.Cell().Element(CellStyle).AlignRight().Text($"{vehicule.PrixTotal:C}");
                        // MODIFICATION 2 : Ajout de la cellule avec couleur conditionnelle
                        var restant = vehicule.RestantAPayer;
                        var color = restant > 0 ? Colors.Red.Medium : Colors.Black;
                        table.Cell().Element(CellStyle).AlignRight().Text(text =>
                        {
                            text.Span($"{restant:C}").FontColor(color);
                        });
                    }
                });
            }

            // Styles réutilisables
            IContainer Block(IContainer container) => container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10);

            // MODIFICATION 1 : Ajout du PaddingHorizontal
            IContainer CellStyle(IContainer container) => container
                .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5).PaddingHorizontal(5);
        }

        public async Task<byte[]> GenerateColisTicketPdfAsync(Colis colis)
        {
            var barcodeService = new BarcodeService();
            var firstBarcode = colis.Barcodes.FirstOrDefault()?.Value ?? colis.NumeroReference;
            var barcodeImage = await barcodeService.GenerateBarcodeImageAsync(firstBarcode, width: 350, height: 80);
            return await Task.Run(() =>
            {
                return PdfDocument.Create(container =>
                {
                    // Boucle pour créer autant de pages que nécessaire
                    for (int i = 0; i < colis.NombrePieces; i += 2)
                    {
                        container.Page(page =>
                        {
                            page.Size(100, 150, Unit.Millimetre);
                            page.Margin(0);
                            page.Content().Column(column =>
                            {
                                // Ticket 1 (pièce i + 1)
                                var pieceNumber1 = i + 1;
                                column.Item().Height(75, Unit.Millimetre).Element(cont => ComposeTicket(cont, colis, barcodeImage, pieceNumber1));

                                // Ticket 2 (pièce i + 2), seulement s'il existe
                                if (i + 2 <= colis.NombrePieces)
                                {
                                    var pieceNumber2 = i + 2;
                                    column.Item().Height(75, Unit.Millimetre).Element(cont => ComposeTicket(cont, colis, barcodeImage, pieceNumber2));
                                }
                            });
                        });
                    }
                }).GeneratePdf();
            });
        }

        // La méthode ComposeTicket accepte maintenant le numéro de la pièce
        static void ComposeTicket(IContainer container, Colis colis, byte[] barcodeImage, int pieceNumber)
        {
            container
                .Border(1)
                .PaddingVertical(2, Unit.Millimetre)
                .PaddingHorizontal(5, Unit.Millimetre)
                .Column(column =>
                {
                    column.Spacing(2); // Augmenter l'espacement entre les lignes
                    column.Item().AlignCenter().Text(colis.Destinataire).FontSize(22).ExtraBold();
                    column.Item().AlignCenter().Text($"Tel : {colis.TelephoneDestinataire}").FontSize(12);
                    if (colis.LivraisonADomicile)
                    {
                        column.Item().AlignCenter().PaddingTop(2).Background(Colors.Grey.Lighten2).PaddingHorizontal(5).Text("À LIVRER À DOMICILE").Bold().FontSize(12);
                    }
                    if (!string.IsNullOrWhiteSpace(colis.AdresseLivraison))
                    {
                        column.Item().PaddingTop(2).Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(9));
                            text.Span("Adresse : ").SemiBold();
                            text.Span(colis.AdresseLivraison);
                        });
                    }

                    column.Item().PaddingTop(2).Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9));
                        text.Span("Destination : ").SemiBold();
                        text.Span(colis.DestinationFinale);
                    });

                    column.Item().PaddingTop(2).AlignCenter().Text($"Colis: {pieceNumber} / {colis.NombrePieces}").FontSize(11).SemiBold();
                    column.Item().PaddingTop(2).ExtendHorizontal().AlignCenter().Image(barcodeImage, ImageScaling.FitWidth);

                    // Réduire l'espacement au-dessus du nom de l'entreprise
                    column.Item().AlignCenter().PaddingTop(1).Background(Colors.Grey.Lighten2).PaddingHorizontal(5).Text("HIPPOCAMPE IMPORT-EXPORT").Bold().FontSize(12);
                });
        }

        public async Task<byte[]> GenerateContainerPdfAsync(Conteneur conteneur, bool includeFinancials)
        {
            return await Task.Run(() =>
            {
                // 1. Chemin de l'image
                var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.jpg");
                // 2. Préparation des données pour le récapitulatif (LINQ)
                // On récupère tous les clients distincts qui ont soit un colis, soit un véhicule dans ce conteneur
                var clientsIds = conteneur.Colis.Select(c => c.ClientId)
                    .Union(conteneur.Vehicules.Select(v => v.ClientId))
                    .Distinct()
                    .ToList();
                var statsClients = clientsIds.Select(clientId =>
                {
                    // On cherche l'objet client (soit dans colis, soit dans véhicules)
                    var clientInfo = conteneur.Colis.FirstOrDefault(c => c.ClientId == clientId)?.Client
                                  ?? conteneur.Vehicules.FirstOrDefault(v => v.ClientId == clientId)?.Client;
                    var colisClient = conteneur.Colis.Where(c => c.ClientId == clientId).ToList();
                    var vehiculesClient = conteneur.Vehicules.Where(v => v.ClientId == clientId).ToList();
                    return new
                    {
                        Nom = clientInfo?.NomComplet ?? "Inconnu",
                        Telephone = clientInfo?.TelephonePrincipal ?? "-",
                        NbColis = colisClient.Count,
                        NbVehicules = vehiculesClient.Count,
                        TotalPrix = colisClient.Sum(x => x.PrixTotal) + vehiculesClient.Sum(x => x.PrixTotal),
                        TotalPaye = colisClient.Sum(x => x.SommePayee) + vehiculesClient.Sum(x => x.SommePayee),
                        Reste = colisClient.Sum(x => x.RestantAPayer) + vehiculesClient.Sum(x => x.RestantAPayer)
                    };
                }).OrderBy(x => x.Nom).ToList();
                // 3. Création du PDF
                var document = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.0f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                        page.Header().Element(c => ComposeHeader(c, logoPath));
                        page.Content().Element(ComposeContent);
                        page.Footer().Element(ComposeFooter);
                    });
                });
                return document.GeneratePdf();

                // --- COMPOSANTS ---
                void ComposeHeader(IContainer container, string imagePath)
                {
                    container.Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            // Logo aligné à gauche avec correction
                            if (File.Exists(imagePath))
                            {
                                try {
                                    column.Item().AlignLeft().Element(e => e.Height(60).Image(imagePath).FitHeight());
                                } catch { }
                            }

                            column.Item().Text("HIPPOCAMPE IMPORT-EXPORT").ExtraBold().FontSize(20).FontColor(Colors.Blue.Darken2);
                            column.Item().Text("7 Rue Pascal, 33370 Tresses").FontSize(9);
                            column.Item().Text("Tél: 06 99 56 93 58 / 09 81 72 45 40").FontSize(9);
                        });
                        row.ConstantItem(250).AlignRight().Column(column =>
                        {
                            column.Item().Text("DOSSIER DE TRANSIT").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken2);
                            column.Item().Text(conteneur.NumeroDossier).FontSize(18).Bold();

                            column.Item().PaddingTop(5).Text(text =>
                            {
                                text.Span("Date: ").SemiBold();
                                text.Span(DateTime.Now.ToString("dd/MM/yyyy"));
                            });

                            if (includeFinancials)
                            {
                                column.Item().PaddingTop(5).Background(Colors.Red.Lighten4).Padding(5)
                                    .Text("CONFIDENTIEL - PRIX INCLUS").FontSize(8).FontColor(Colors.Red.Darken2).AlignCenter();
                            }
                        });
                    });
                }

                void ComposeContent(IContainer container)
                {
                    container.PaddingVertical(15).Column(column =>
                    {
                        column.Spacing(15);
                        // A. LOGISTIQUE
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("INFOS GÉNÉRALES").FontSize(9).SemiBold().Underline();
                                c.Item().Text($"Destination: {conteneur.Destination}, {conteneur.PaysDestination}");
                                c.Item().Text($"Compagnie: {conteneur.NomCompagnie ?? "-"}");
                                c.Item().Text($"N° Plomb: {conteneur.NumeroPlomb ?? "-"}");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("STATUT & DATES").FontSize(9).SemiBold().Underline();
                                c.Item().Text($"Statut: {conteneur.Statut}");
                                c.Item().Text($"Départ: {conteneur.DateDepart:dd/MM/yyyy}");
                                c.Item().Text($"Arrivée: {conteneur.DateArriveeDestination:dd/MM/yyyy}");
                            });
                        });
                        // B. RÉCAPITULATIF CLIENTS (NOUVEAU !)
                        if (statsClients.Any())
                        {
                            column.Item().Text("RÉCAPITULATIF PAR CLIENT").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); // Nom
                                    columns.RelativeColumn(2); // Tél
                                    columns.ConstantColumn(40); // Nb Colis
                                    columns.ConstantColumn(40); // Nb Véhicules

                                    if (includeFinancials)
                                    {
                                        columns.RelativeColumn(2); // Total
                                        columns.RelativeColumn(2); // Payé
                                        columns.RelativeColumn(2); // Reste
                                    }
                                });
                                // En-tête Récap
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle).Text("Client");
                                    header.Cell().Element(HeaderStyle).Text("Téléphone");
                                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Colis");
                                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Véh.");

                                    if (includeFinancials)
                                    {
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Total");
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Payé");
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Reste");
                                    }
                                });
                                // Lignes Récap
                                foreach (var stat in statsClients)
                                {
                                    table.Cell().Element(c => CellStyle(c, false)).Text(stat.Nom).SemiBold();
                                    table.Cell().Element(c => CellStyle(c, false)).Text(stat.Telephone);
                                    table.Cell().Element(c => CellStyle(c, false)).AlignCenter().Text(stat.NbColis.ToString());
                                    table.Cell().Element(c => CellStyle(c, false)).AlignCenter().Text(stat.NbVehicules.ToString());

                                    if (includeFinancials)
                                    {
                                        table.Cell().Element(c => CellStyle(c, false)).AlignRight().Text($"{stat.TotalPrix:N0}");
                                        table.Cell().Element(c => CellStyle(c, false)).AlignRight().Text($"{stat.TotalPaye:N0}");

                                        var couleur = stat.Reste > 0 ? Colors.Red.Medium : Colors.Green.Medium;
                                        table.Cell().Element(c => CellStyle(c, false)).AlignRight().Text($"{stat.Reste:N0}").FontColor(couleur).Bold();
                                    }
                                }
                                // Pied du Récap (Totaux globaux)
                                table.Footer(footer =>
                                {
                                    footer.Cell().Element(FooterStyle).Text("TOTAUX").Bold();
                                    footer.Cell().Element(FooterStyle); // Vide (Tél)
                                    footer.Cell().Element(FooterStyle).AlignCenter().Text(statsClients.Sum(s => s.NbColis).ToString()).Bold();
                                    footer.Cell().Element(FooterStyle).AlignCenter().Text(statsClients.Sum(s => s.NbVehicules).ToString()).Bold();
                                    if (includeFinancials)
                                    {
                                        footer.Cell().Element(FooterStyle).AlignRight().Text($"{statsClients.Sum(s => s.TotalPrix):N0} €").Bold();
                                        footer.Cell().Element(FooterStyle).AlignRight().Text($"{statsClients.Sum(s => s.TotalPaye):N0} €").Bold();
                                        footer.Cell().Element(FooterStyle).AlignRight().Text($"{statsClients.Sum(s => s.Reste):N0} €").Bold().FontColor(Colors.Red.Medium);
                                    }
                                });
                            });
                        }
                        // C. LISTE DÉTAILLÉE DES COLIS (Existante, mais améliorée)
                        if (conteneur.Colis.Any())
                        {
                            column.Item().PaddingTop(10).Text($"DÉTAILS DES COLIS ({conteneur.Colis.Count})").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(70); // Réf
                                    columns.RelativeColumn(2);  // Client
                                    columns.RelativeColumn(3);  // Désignation
                                    columns.ConstantColumn(40); // Qté
                                    columns.RelativeColumn(1);  // Dest.

                                    if (includeFinancials)
                                    {
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(60);
                                    }
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle).Text("Réf.");
                                    header.Cell().Element(HeaderStyle).Text("Client");
                                    header.Cell().Element(HeaderStyle).Text("Désignation");
                                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Qté");
                                    header.Cell().Element(HeaderStyle).Text("Dest.");

                                    if (includeFinancials)
                                    {
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Prix");
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Payé");
                                        header.Cell().Element(HeaderStyle).AlignRight().Text("Dû");
                                    }
                                });
                                foreach (var item in conteneur.Colis)
                                {
                                    // Ligne Colis
                                    table.Cell().Element(c => CellStyle(c, true)).Text(item.NumeroReference).SemiBold();
                                    table.Cell().Element(c => CellStyle(c, true)).Text(item.Client?.NomComplet ?? "N/A").FontSize(8);
                                    table.Cell().Element(c => CellStyle(c, true)).Text(item.Designation).Bold();
                                    table.Cell().Element(c => CellStyle(c, true)).AlignCenter().Text(item.NombrePieces.ToString());
                                    table.Cell().Element(c => CellStyle(c, true)).Text(item.DestinationFinale).FontSize(8);

                                    if (includeFinancials)
                                    {
                                        table.Cell().Element(c => CellStyle(c, true)).AlignRight().Text($"{item.PrixTotal:N0}");
                                        table.Cell().Element(c => CellStyle(c, true)).AlignRight().Text($"{item.SommePayee:N0}");
                                        var reste = item.RestantAPayer;
                                        table.Cell().Element(c => CellStyle(c, true)).AlignRight().Text($"{reste:N0}").FontColor(reste > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                                    }
                                    // Lignes Inventaire
                                    if (!string.IsNullOrWhiteSpace(item.InventaireJson) && item.InventaireJson != "[]")
                                    {
                                        try
                                        {
                                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                            var inventaire = JsonSerializer.Deserialize<List<InventaireItem>>(item.InventaireJson, options);
                                            if (inventaire != null && inventaire.Any())
                                            {
                                                foreach (var invItem in inventaire)
                                                {
                                                    table.Cell().Element(SubCellStyle);
                                                    table.Cell().Element(SubCellStyle);
                                                    table.Cell().Element(SubCellStyle).PaddingLeft(15).Text($"- {invItem.Designation}").Italic().FontColor(Colors.Grey.Darken2);
                                                    table.Cell().Element(SubCellStyle).AlignCenter().Text(invItem.Quantite.ToString()).FontSize(8);
                                                    table.Cell().Element(SubCellStyle);
                                                    if (includeFinancials)
                                                    {
                                                        table.Cell().Element(SubCellStyle).AlignRight().Text($"{invItem.Valeur:N0}").FontSize(8).Italic();
                                                        table.Cell().Element(SubCellStyle);
                                                        table.Cell().Element(SubCellStyle);
                                                    }
                                                }
                                            }
                                        } catch { }
                                    }
                                }
                            });
                        }

                        // D. LISTE VÉHICULES
                        if (conteneur.Vehicules.Any())
                        {
                             column.Item().PaddingTop(10).Text($"DÉTAILS DES VÉHICULES ({conteneur.Vehicules.Count})").FontSize(12).Bold().FontColor(Colors.Green.Darken2);
                             // ... (Code similaire pour la table véhicules, simplifiée pour la réponse) ...
                             column.Item().Table(table =>
                             {
                                 table.ColumnsDefinition(c => {
                                     c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(1);
                                     if(includeFinancials) { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); }
                                 });
                                 table.Header(h => {
                                     h.Cell().Element(HeaderStyle).Text("Immat"); h.Cell().Element(HeaderStyle).Text("Client"); h.Cell().Element(HeaderStyle).Text("Véhicule"); h.Cell().Element(HeaderStyle).Text("Dest.");
                                     if(includeFinancials) { h.Cell().Element(HeaderStyle).AlignRight().Text("Prix"); h.Cell().Element(HeaderStyle).AlignRight().Text("Payé"); h.Cell().Element(HeaderStyle).AlignRight().Text("Dû"); }
                                 });
                                 foreach(var v in conteneur.Vehicules) {
                                     table.Cell().Element(c => CellStyle(c, true)).Text(v.Immatriculation).SemiBold();
                                     table.Cell().Element(c => CellStyle(c, true)).Text(v.Client?.NomComplet ?? "N/A");
                                     table.Cell().Element(c => CellStyle(c, true)).Text($"{v.Marque} {v.Modele}");
                                     table.Cell().Element(c => CellStyle(c, true)).Text(v.DestinationFinale);
                                     if(includeFinancials) {
                                         table.Cell().Element(c => CellStyle(c, true)).AlignRight().Text($"{v.PrixTotal:N0}");
                                         table.Cell().Element(c => CellStyle(c, true)).AlignRight().Text($"{v.SommePayee:N0}");
                                         table.Cell().Element(c => CellStyle(c, true)).AlignRight().Text($"{v.RestantAPayer:N0}").FontColor(v.RestantAPayer > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                                     }
                                 }
                             });
                        }
                    });
                }

                void ComposeFooter(IContainer container)
                {
                    container.Column(c =>
                    {
                        c.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Généré par Transit Manager").FontSize(7).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                        });
                    });
                }

                // --- STYLES ---
                static IContainer HeaderStyle(IContainer container) =>
                    container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(2).Background(Colors.Grey.Lighten3).PaddingHorizontal(2);
                static IContainer FooterStyle(IContainer container) =>
                    container.BorderTop(1).BorderColor(Colors.Black).PaddingVertical(2).PaddingHorizontal(2).Background(Colors.Grey.Lighten4);
                static IContainer CellStyle(IContainer container, bool isMainRow) =>
                    container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2)
                    .Background(isMainRow ? Colors.Grey.Lighten5 : Colors.White);
                static IContainer SubCellStyle(IContainer container) =>
                    container.PaddingVertical(1).PaddingHorizontal(2);
            });
        }

		public async Task<byte[]> GenerateColisPdfAsync(Colis colis, bool includeFinancials, bool includePhotos)
		{
			return await Task.Run(() =>
			{
				// 1. Chemins
				var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.jpg");

				// 2. Désérialisation inventaire
				List<InventaireItem> inventaire = new();
				if (!string.IsNullOrWhiteSpace(colis.InventaireJson) && colis.InventaireJson != "[]")
				{
					try
					{
						var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
						inventaire = JsonSerializer.Deserialize<List<InventaireItem>>(colis.InventaireJson, options) ?? new();
					}
					catch { }
				}

				// 3. Création PDF
				var document = QuestPDF.Fluent.Document.Create(container =>
				{
					container.Page(page =>
					{
						page.Size(PageSizes.A4);
						page.Margin(1.5f, Unit.Centimetre);
						page.PageColor(Colors.White);
						page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

						page.Header().Element(ComposeHeader);
						page.Content().Element(ComposeContent);
						page.Footer().Element(ComposeFooter);
					});
				});

				return document.GeneratePdf();

				// --- COMPOSANTS ---

				void ComposeHeader(IContainer container)
				{
					 // (Gardez votre code existant pour le Header, identique à avant)
					 // ...
					 container.Row(row =>
					 {
						row.ConstantItem(250).Column(column =>
						{
							if (File.Exists(logoPath)) try { column.Item().Height(50).AlignLeft().Image(logoPath).FitHeight(); } catch { }
							column.Item().Text("HIPPOCAMPE IMPORT-EXPORT").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
							column.Item().Text("7 Rue Pascal, 33370 Tresses").FontSize(9);
							column.Item().Text("Tél: 06 99 56 93 58").FontSize(9);
							column.Item().Text("bordeaux@h-import-export.com").FontSize(9);
						});

						// DROITE : Info Client Propriétaire
                        row.RelativeItem().AlignRight().Column(column =>
                        {
                            column.Item().AlignRight().Text("PROPRIÉTAIRE").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                            column.Item().AlignRight().Text(colis.Client?.NomComplet ?? "Inconnu").FontSize(12).Bold();
                            column.Item().AlignRight().Text(colis.Client?.TelephonePrincipal ?? "-");
                            
                            // Ajout/Vérification de l'Email en dessous du numéro
                            if (!string.IsNullOrWhiteSpace(colis.Client?.Email))
                                column.Item().AlignRight().Text(colis.Client.Email).FontSize(9).Italic();
                            
                            // Adresse à la fin
                            if (!string.IsNullOrWhiteSpace(colis.Client?.AdressePrincipale))
                                column.Item().AlignRight().Text($"{colis.Client.AdressePrincipale}, {colis.Client.Ville}").FontSize(9);
                        });
					 });
				}

				void ComposeContent(IContainer container)
				{
					container.PaddingVertical(20).Column(column =>
					{
						column.Spacing(15);

						// A. INFO & DESTINATAIRE (Identique à avant)
						// ... (Je ne remets pas tout le code pour abréger, gardez votre bloc existant) ...
						// Si vous avez besoin du code complet, dites-le moi, mais c'est le bloc avec "DESTINATAIRE & LIVRAISON" et "INFOS GÉNÉRALES"
						
						// --- POUR RAPPEL, LE DÉBUT DU BLOC EST : ---
						bool destinataireDifferent = colis.Destinataire?.Trim().ToLower() != colis.Client?.NomComplet?.Trim().ToLower();
						column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
						{
							// ... Code Destinataire ...
							 row.RelativeItem().Column(c =>
							{
								c.Item().Text("DESTINATAIRE & LIVRAISON").FontSize(9).SemiBold().Underline();
								if (destinataireDifferent)
								{
									c.Item().Text(colis.Destinataire ?? "Même que propriétaire").Bold();
									c.Item().Text(colis.TelephoneDestinataire ?? "-");
									if(!string.IsNullOrWhiteSpace(colis.AdresseLivraison)) c.Item().Text(colis.AdresseLivraison).FontSize(9);
								}
								else c.Item().Text("Identique au propriétaire").Italic();
							});
							// ... Code Destination ...
							 row.RelativeItem().AlignRight().Column(c =>
							{
								c.Item().Text("DESTINATION FINALE").FontSize(9).SemiBold().Underline();
								c.Item().Text(colis.DestinationFinale).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
								c.Item().Text($"Type: {colis.Type} | {colis.TypeEnvoi}");
							});
						});
						
						// B. INFOS GÉNÉRALES
						column.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(c => 
						{
							c.Item().Row(r => 
							{
								r.RelativeItem().Text(t => { t.Span("Réf: ").SemiBold(); t.Span(colis.NumeroReference); });
								r.RelativeItem().Text(t => { t.Span("Code-barres: ").SemiBold(); t.Span(colis.Barcodes.FirstOrDefault()?.Value ?? "-"); });
								r.RelativeItem().Text(t => { t.Span("Date: ").SemiBold(); t.Span(colis.DateArrivee.ToString("dd/MM/yyyy")); });
							});
							if (!string.IsNullOrWhiteSpace(colis.InstructionsSpeciales))
								c.Item().PaddingTop(5).Text(t => { t.Span("Instructions: ").SemiBold().FontColor(Colors.Red.Medium); t.Span(colis.InstructionsSpeciales); });
						});

						// C. INVENTAIRE (Identique à avant)
						column.Item().Text("DÉTAIL DE L'INVENTAIRE").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
						// ... (Tableau inventaire inchangé) ...
						column.Item().Table(table =>
						{
							table.ColumnsDefinition(columns => { columns.ConstantColumn(80); columns.RelativeColumn(); columns.ConstantColumn(50); columns.ConstantColumn(70); columns.ConstantColumn(70); });
							table.Header(header => { header.Cell().Element(HeaderStyle).Text("Date"); header.Cell().Element(HeaderStyle).Text("Désignation"); header.Cell().Element(HeaderStyle).AlignCenter().Text("Qté"); header.Cell().Element(HeaderStyle).AlignRight().Text("Total"); header.Cell().Element(HeaderStyle).AlignRight().Text("P.U."); });
							foreach (var item in inventaire)
							{
								table.Cell().Element(CellStyle).Text(item.Date.ToString("dd/MM/yy"));
								table.Cell().Element(CellStyle).Text(item.Designation);
								table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantite.ToString());
								table.Cell().Element(CellStyle).AlignRight().Text($"{item.Valeur:N2}");
								var unitPrice = item.Quantite > 0 ? item.Valeur / item.Quantite : 0;
								table.Cell().Element(CellStyle).AlignRight().Text($"{unitPrice:N2}").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
							}
							table.Footer(footer => {
								footer.Cell().ColumnSpan(2).Element(FooterStyle).AlignRight().Text("TOTAUX :").Bold();
								footer.Cell().Element(FooterStyle).AlignCenter().Text(inventaire.Sum(i => i.Quantite).ToString()).Bold();
								footer.Cell().Element(FooterStyle).AlignRight().Text($"{inventaire.Sum(i => i.Valeur):N2} €").Bold();
								footer.Cell().Element(FooterStyle);
							});
						});

						// D. VALIDATION & SIGNATURE (NOUVEAU !)
						// On affiche ce bloc tout le temps, que les prix soient là ou non.
						column.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
						{
							// Infos Lieu/Date
							row.RelativeItem().Column(c =>
							{
								c.Item().Text("VALIDATION CLIENT").FontSize(10).SemiBold().Underline();
								
								if (!string.IsNullOrEmpty(colis.LieuSignatureInventaire))
									c.Item().Text($"Fait à : {colis.LieuSignatureInventaire}");
								
								if (colis.DateSignatureInventaire.HasValue)
									c.Item().Text($"Le : {colis.DateSignatureInventaire.Value:dd/MM/yyyy}");
								else
									c.Item().Text("Le : __/__/____");
									
								c.Item().PaddingTop(5).Text("Le client atteste de l'exactitude de l'inventaire ci-dessus.").FontSize(8).Italic();
							});

							// Image Signature
							row.RelativeItem().AlignRight().Column(c =>
							{
								c.Item().Text("Signature").FontSize(9);
								
								if (!string.IsNullOrEmpty(colis.SignatureClientInventaire))
								{
									try 
									{
										var bytes = Convert.FromBase64String(colis.SignatureClientInventaire.Split(',')[1]); 
										c.Item().Height(40).AlignRight().Image(bytes).FitHeight();
									} 
									catch 
									{
										c.Item().Text("[Erreur image]").FontSize(8).FontColor(Colors.Red.Medium);
									}
								}
								else 
								{ 
									c.Item().Height(40).AlignRight().AlignMiddle().Text("Non signé").FontSize(8).Italic().FontColor(Colors.Grey.Medium); 
								}
							});
						});

						// E. RÉCAPITULATIF FINANCIER (Conditionnel)
						if (includeFinancials)
						{
							column.Item().PaddingTop(5).AlignRight().Border(1).BorderColor(Colors.Black).Padding(10).Column(c => 
							{
								c.Item().Text("SITUATION FINANCIÈRE").FontSize(10).Underline().Bold();
								c.Item().Row(r => { r.RelativeItem().Text("Prix Total :"); r.RelativeItem().AlignRight().Text($"{colis.PrixTotal:N2} €").Bold(); });
								c.Item().Row(r => { r.RelativeItem().Text("Déjà Payé :"); r.RelativeItem().AlignRight().Text($"{colis.SommePayee:N2} €").FontColor(Colors.Green.Medium); });
								c.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(r => 
								{
									r.RelativeItem().Text("RESTE À PAYER :").Bold();
									r.RelativeItem().AlignRight().Text($"{colis.RestantAPayer:N2} €").ExtraBold().FontColor(Colors.Red.Medium);
								});
							});
						}
						
						// F. PHOTOS (NOUVEAU !)
						if (includePhotos && colis.Documents != null && colis.Documents.Any())
						{
							 // On filtre pour ne prendre que les images
							 // Vous pouvez adapter le TypeDocument si vous avez créé "PhotoColis" spécifique
							 var photos = colis.Documents
								 .Where(d => d.Extension == ".jpg" || d.Extension == ".png" || d.Extension == ".jpeg")
								 .ToList();

							 if (photos.Any())
							 {
								 column.Item().PageBreak();
								 column.Item().Text("PHOTOS DU COLIS").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
								 
								 column.Item().Grid(grid =>
								 {
									 grid.Columns(2);
									 grid.Spacing(10);
									 
									 foreach (var photo in photos)
									 {
										 var fullPath = Path.Combine(_storageRootPath, photo.CheminFichier);
										 if (File.Exists(fullPath))
										 {
											 grid.Item().Border(1).BorderColor(Colors.Grey.Lighten3).Column(imgCol => 
											 {
												 imgCol.Item().Height(200).Image(fullPath).FitArea();
												 imgCol.Item().Padding(5).Text(photo.Nom).FontSize(8).AlignCenter();
											 });
										 }
									 }
								 });
							 }
						}
					});
				}

				void ComposeFooter(IContainer container)
				{
					// (Identique à avant)
					 container.Column(c =>
					{
						c.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
						{
							row.RelativeItem().Text($"Réf: {colis.NumeroReference} - Généré par Transit Manager").FontSize(7).FontColor(Colors.Grey.Medium);
							row.RelativeItem().AlignRight().Text(x => { x.Span("Page "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
						});
					});
				}
				
				// Styles (Identiques à avant)
				static IContainer HeaderStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(2).Background(Colors.Grey.Lighten3).PaddingHorizontal(2).DefaultTextStyle(x => x.SemiBold());
				static IContainer FooterStyle(IContainer container) => container.BorderTop(1).BorderColor(Colors.Black).PaddingVertical(2).Background(Colors.Grey.Lighten4).PaddingHorizontal(2);
				static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(2).PaddingHorizontal(2);
			});
		}

        public async Task<byte[]> GenerateVehiculePdfAsync(Vehicule vehicule, bool includeFinancials, bool includePhotos)
        {
            return await Task.Run(() =>
            {
                // 1. Chemins des ressources
                var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.jpg");

                // Détermination de l'image du plan selon le type
                string planImageName = vehicule.Type switch
                {
                    TypeVehicule.Moto => "moto_plan.png",
                    TypeVehicule.Camion => "camion_plan.png",
                    TypeVehicule.Quad => "quad_plan.png",
                    TypeVehicule.Van => "van_plan.png",
                    _ => "vehicule_plan.png"
                };
                // Note: Assurez-vous que ces images sont bien copiées dans le dossier de sortie de l'API (comme pour le logo)
                // ou adaptez le chemin vers wwwroot si elles sont stockées là-bas.
                // Ici je suppose qu'elles sont dans Resources/Plans/ pour l'exemple backend
                var planPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Plans", planImageName);
                // Fallback si le dossier Plans n'est pas copié, essayez la racine Resources
                if (!File.Exists(planPath)) planPath = Path.Combine(AppContext.BaseDirectory, "Resources", planImageName);
                // 2. Désérialisation des données JSON
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                List<SerializablePoint> points = new();
                if (!string.IsNullOrWhiteSpace(vehicule.EtatDesLieux))
                    try { points = JsonSerializer.Deserialize<List<SerializablePoint>>(vehicule.EtatDesLieux, options) ?? new(); } catch { }
                List<List<SerializablePoint>> strokes = new();
                if (!string.IsNullOrWhiteSpace(vehicule.EtatDesLieuxRayures))
                    try { strokes = JsonSerializer.Deserialize<List<List<SerializablePoint>>>(vehicule.EtatDesLieuxRayures, options) ?? new(); } catch { }
                VehiculeAccessoiresDto accessoires = new();
                if (!string.IsNullOrWhiteSpace(vehicule.AccessoiresJson))
                    try { accessoires = JsonSerializer.Deserialize<VehiculeAccessoiresDto>(vehicule.AccessoiresJson, options) ?? new(); } catch { }
                // 3. Création du PDF
                var document = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                        page.Header().Element(ComposeHeader);
                        page.Content().Element(ComposeContent);
                        page.Footer().Element(ComposeFooter);
                    });
                });
                return document.GeneratePdf();

                // --- COMPOSANTS ---
                void ComposeHeader(IContainer container)
                {
                    container.Row(row =>
                    {
                        // GAUCHE : Logo et Entreprise
                        row.ConstantItem(250).Column(column =>
                        {
                            if (File.Exists(logoPath))
                            {
                                try {
                                    column.Item().AlignLeft().Element(e => e.Height(50).Image(logoPath).FitHeight());
                                } catch { }
                            }

                            column.Item().Text("HIPPOCAMPE IMPORT-EXPORT").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            column.Item().Text("7 Rue Pascal, 33370 Tresses").FontSize(9);
                            column.Item().Text("Tél: 06 99 56 93 58").FontSize(9);
                            column.Item().Text("bordeaux@h-import-export.com").FontSize(9);
                        });
						
						// DROITE : Propriétaire
						row.RelativeItem().AlignRight().Column(column =>
						{
							column.Item().AlignRight().Text("PROPRIÉTAIRE").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
							column.Item().AlignRight().Text(vehicule.Client?.NomComplet ?? "Inconnu").FontSize(12).Bold();
							column.Item().AlignRight().Text(vehicule.Client?.TelephonePrincipal ?? "-");
							
							// Email
							if (!string.IsNullOrWhiteSpace(vehicule.Client?.Email))
								column.Item().AlignRight().Text(vehicule.Client.Email).FontSize(9).Italic();

							// AJOUT : Adresse en dessous de l'émail
							if (!string.IsNullOrWhiteSpace(vehicule.Client?.AdressePrincipale))
								column.Item().AlignRight().Text($"{vehicule.Client.AdressePrincipale}, {vehicule.Client.Ville}").FontSize(9);
						});
                    });
                }

                void ComposeContent(IContainer container)
                {
                    container.PaddingVertical(20).Column(column =>
                    {
                        column.Spacing(15);
                        // A. INFO VÉHICULE & LOGISTIQUE
                        bool destDiff = vehicule.Destinataire != vehicule.Client?.NomComplet;

                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("IDENTIFICATION").FontSize(9).SemiBold().Underline();
                                c.Item().Text($"Immat: {vehicule.Immatriculation}").Bold();
                                c.Item().Text($"Marque: {vehicule.Marque}");
                                c.Item().Text($"Modèle: {vehicule.Modele}");
                                c.Item().Text($"Année: {vehicule.Annee}  |  Km: {vehicule.Kilometrage:N0}");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("LOGISTIQUE").FontSize(9).SemiBold().Underline();
                                c.Item().Text($"Statut: {vehicule.Statut}");
                                c.Item().Text($"Destination: {vehicule.DestinationFinale}");
                                if (destDiff)
                                {
                                    c.Item().Text($"Destinataire: {vehicule.Destinataire}").FontSize(9);
                                    c.Item().Text($"Tél: {vehicule.TelephoneDestinataire}").FontSize(9);
                                }
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(vehicule.Commentaires))
                        {
                            column.Item().Background(Colors.Grey.Lighten4).Padding(5).Text($"Notes: {vehicule.Commentaires}").Italic();
                        }
                        // B. ÉTAT DES LIEUX (SCHÉMA GRAPHIQUE)
                        column.Item().PaddingTop(10).Text("ÉTAT DES LIEUX ET CARROSSERIE").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                        
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Element(box => 
                        {
                            // On génère l'image AVANT de construire le PDF
                            var inspectionImageBytes = GenerateInspectionImage(planPath, points, strokes);

                            if (inspectionImageBytes.Length > 0)
                            {
                                // On affiche simplement l'image générée
                                box.Height(300).AlignMiddle().AlignCenter().Image(inspectionImageBytes, ImageScaling.FitHeight);
                            }
                            else
                            {
                                // Fallback si image pas trouvée ou erreur
                                box.Height(50).AlignCenter().AlignMiddle().Text("Schéma non disponible (Image source manquante)").FontColor(Colors.Red.Medium);
                            }
                        });
						
                        // C. ÉQUIPEMENTS ET ACCESSOIRES
                        column.Item().Row(row =>
                        {
                            // Liste 1
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ÉQUIPEMENTS PRÉSENTS").FontSize(10).Bold().Underline();
                                c.Item().Text($"• Carte Grise : {(accessoires.CarteGrise ? "OUI" : "NON")}");
                                c.Item().Text($"• Clés : {accessoires.NombreClefs}");
                                c.Item().Text($"• Radio/Façade : {(accessoires.Radio ? "OUI" : "NON")}");
                                c.Item().Text($"• Antenne : {(accessoires.Antenne ? "OUI" : "NON")}");
                            });

                            // Liste 2
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(""); // Spacer
                                c.Item().Text($"• Roue Secours : {(accessoires.RoueSecours ? "OUI" : "NON")}");
                                c.Item().Text($"• Cric/Manivelle : {(accessoires.Cric ? "OUI" : "NON")}");
                                c.Item().Text($"• Enjoliveurs : {accessoires.NombreEnjoliveurs} / 4");
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(accessoires.AutresObservations))
                        {
                            column.Item().Text($"Autres observations : {accessoires.AutresObservations}").FontSize(9).Italic();
                        }

                        // --- AJOUT : SECTION DÉCLARATION ET RESPONSABILITÉ ---
                        column.Item().PaddingTop(15).Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
                        {
                            // Valeur du véhicule
                            c.Item().Row(r => 
                            {
                                r.RelativeItem().Text("Valeur du véhicule en euros :").Bold().FontSize(10);
                                
                                // --- MODIFICATION : Affichage conditionnel ---
                                if (vehicule.ValeurDeclaree > 0)
                                {
                                    // Si une valeur est saisie, on l'affiche avec le sigle €
                                    r.RelativeItem().Text($"{vehicule.ValeurDeclaree:N0} €").AlignRight().FontSize(10).Bold();
                                }
                                else
                                {
                                    // Sinon, on affiche les pointillés pour remplissage manuel
                                    r.RelativeItem().Text("........................................").AlignRight().FontSize(10);
                                }
                            });

                            c.Item().PaddingTop(10).Text("Je certifie que mes effets personnels m'appartiennent").Bold().FontSize(10);

                            c.Item().PaddingTop(5).Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(8)); // Texte légal un peu plus petit
                                text.Span("Important : ").Bold();
                                text.Span("en tant que responsable du chargement de mes effets personnels dans le conteneur et/ou véhicule, je certifie l'exactitude de ma liste et que mes colis ne contiennent aucun produit dangereux au sens du code IMDG qui peut comprendre des produits tels que : ");
                                text.Span("ARTICLES EXPLOSIFS, GAZ COMPRIMÉ, RECHARGE DE GAZ, AÉROSOLS, PRODUITS CORROSIFS, PRODUITS TOXIQUES OU MATIÈRES RADIOACTIVES.").Bold();
                            });

                            c.Item().PaddingTop(2).Text("La non observation de cette règle de sécurité engagera ma responsabilité civile et pénale en cas de litige.").FontSize(8);
                            c.Item().PaddingTop(2).Text("L'entreprise ne peut pas être tenue responsable en cas de dommage causé par la surcharge de votre véhicule.").FontSize(8);
                        });
						
                        // D. SIGNATURES
                        column.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Signature Agent").FontSize(8).SemiBold();
                                if (!string.IsNullOrEmpty(vehicule.SignatureAgent))
                                {
                                    try
                                    {
                                        var bytes = Convert.FromBase64String(vehicule.SignatureAgent.Split(',')[1]);
                                        c.Item().Height(40).AlignLeft().Element(e => e.Image(bytes).FitHeight());
                                    } catch {}
                                }
                                else { c.Item().Height(40).Text("Non signé").FontSize(8).Italic().FontColor(Colors.Grey.Medium); }
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignRight().Text($"Fait à {vehicule.LieuEtatDesLieux}, le {vehicule.DateEtatDesLieux:dd/MM/yyyy}").FontSize(8);
                                c.Item().AlignRight().Text("Signature Client").FontSize(8).SemiBold();
                                if (!string.IsNullOrEmpty(vehicule.SignatureClient))
                                {
                                    try
                                    {
                                        var bytes = Convert.FromBase64String(vehicule.SignatureClient.Split(',')[1]);
                                        c.Item().Height(40).AlignRight().Image(bytes).FitHeight();
                                    } catch {}
                                }
                                 else { c.Item().Height(40).AlignRight().Text("Non signé").FontSize(8).Italic().FontColor(Colors.Grey.Medium); }
                            });
                        });
                        // E. FINANCE (Optionnel)
                        if (includeFinancials)
                        {
                            column.Item().PaddingTop(10).AlignRight().Border(1).BorderColor(Colors.Black).Padding(10).Column(c =>
                            {
                                c.Item().Text("SITUATION FINANCIÈRE").FontSize(10).Underline().Bold();
                                c.Item().Row(r => { r.RelativeItem().Text("Valeur Déclarée :"); r.RelativeItem().AlignRight().Text($"{vehicule.ValeurDeclaree:N2} €"); });
                                c.Item().Row(r => { r.RelativeItem().Text("Prix Total :"); r.RelativeItem().AlignRight().Text($"{vehicule.PrixTotal:N2} €").Bold(); });
                                c.Item().Row(r => { r.RelativeItem().Text("Déjà Payé :"); r.RelativeItem().AlignRight().Text($"{vehicule.SommePayee:N2} €").FontColor(Colors.Green.Medium); });
                                c.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(r =>
                                {
                                    r.RelativeItem().Text("RESTE À PAYER :").Bold();
                                    r.RelativeItem().AlignRight().Text($"{vehicule.RestantAPayer:N2} €").ExtraBold().FontColor(Colors.Red.Medium);
                                });
                            });
                        }

                        // F. PHOTOS (Optionnel)
                        if (includePhotos && vehicule.Documents != null && vehicule.Documents.Any())
                        {
                             // Filtrer pour ne prendre que les images
                             var photos = vehicule.Documents
                                 .Where(d => d.Type == TypeDocument.PhotoConstatVehicule ||
                                             d.Type == TypeDocument.Autre && (d.Extension == ".jpg" || d.Extension == ".png" || d.Extension == ".jpeg"))
                                 .ToList();
                             if (photos.Any())
                             {
                                 column.Item().PageBreak();
                                 column.Item().Text("PHOTOS DU VÉHICULE").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                                 column.Item().Grid(grid =>
                                 {
                                     grid.Columns(2); // 2 photos par ligne
                                     grid.Spacing(10);

                                     foreach (var photo in photos)
                                     {
                                         // On doit charger le fichier physique
                                         var fullPath = Path.Combine(_storageRootPath, photo.CheminFichier);
                                         if (File.Exists(fullPath))
                                         {
                                             grid.Item().Border(1).BorderColor(Colors.Grey.Lighten3).Column(imgCol =>
                                             {
                                                 imgCol.Item().Height(200).Image(fullPath).FitArea();
                                                 imgCol.Item().Padding(5).Text(photo.Nom).FontSize(8).AlignCenter();
                                             });
                                         }
                                     }
                                 });
                             }
                        }
                    });
                }

                void ComposeFooter(IContainer container)
                {
                    container.Column(c =>
                    {
                        c.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Document généré par Transit Manager").FontSize(7).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                        });
                    });
                }
            });
        }

        // Helper interne pour désérialiser les accessoires
        private class VehiculeAccessoiresDto
        {
            public int NombreClefs { get; set; }
            public bool Antenne { get; set; }
            public bool Radio { get; set; }
            public int NombreEnjoliveurs { get; set; }
            public bool RoueSecours { get; set; }
            public bool Cric { get; set; }
            public bool CarteGrise { get; set; }
            public string? AutresObservations { get; set; }
        }
		
		// Méthode helper pour générer l'image de l'état des lieux en mémoire
        private byte[] GenerateInspectionImage(string planPath, List<SerializablePoint> points, List<List<SerializablePoint>> strokes)
        {
            if (!File.Exists(planPath)) return Array.Empty<byte>();

            try
            {
                using var planBitmap = SKBitmap.Decode(planPath);
                if (planBitmap == null) return Array.Empty<byte>();

                // On crée une surface de dessin de la même taille que l'image d'origine
                var info = new SKImageInfo(planBitmap.Width, planBitmap.Height);
                using var surface = SKSurface.Create(info);
                var canvas = surface.Canvas;

                // 1. Dessiner le plan
                canvas.DrawBitmap(planBitmap, 0, 0);

                // 2. Dessiner les rayures
                // On adapte l'épaisseur du trait à la taille de l'image (ex: 0.5% de la largeur)
                float strokeWidth = planBitmap.Width * 0.005f; 
                if (strokeWidth < 2) strokeWidth = 2;

                using var paintStroke = new SKPaint
                {
                    Color = SKColors.Red,
                    IsStroke = true,
                    StrokeWidth = strokeWidth,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke
                };

                foreach (var stroke in strokes)
                {
                    if (stroke.Count < 2) continue;
                    using var path = new SKPath();
                    
                    // Les points sont en % (0-1), on multiplie par la taille de l'image
                    var start = stroke[0];
                    path.MoveTo((float)start.X * planBitmap.Width, (float)start.Y * planBitmap.Height);

                    for (int i = 1; i < stroke.Count; i++)
                    {
                        var p = stroke[i];
                        path.LineTo((float)p.X * planBitmap.Width, (float)p.Y * planBitmap.Height);
                    }
                    canvas.DrawPath(path, paintStroke);
                }

                // 3. Dessiner les impacts
                float radius = planBitmap.Width * 0.01f; // 1% de la largeur
                if (radius < 4) radius = 4;

                using var paintPointFill = new SKPaint { Color = SKColors.Red.WithAlpha(150), IsAntialias = true, Style = SKPaintStyle.Fill };
                using var paintPointBorder = new SKPaint { Color = SKColors.Red, IsStroke = true, StrokeWidth = strokeWidth / 2, IsAntialias = true, Style = SKPaintStyle.Stroke };

                foreach (var p in points)
                {
                    float cx = (float)p.X * planBitmap.Width;
                    float cy = (float)p.Y * planBitmap.Height;
                    canvas.DrawCircle(cx, cy, radius, paintPointFill);
                    canvas.DrawCircle(cx, cy, radius, paintPointBorder);
                }

                // 4. Exporter en PNG (byte array)
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
		
		public async Task<byte[]> GenerateAttestationValeurPdfAsync(Vehicule vehicule)
		{
			return await Task.Run(() =>
			{
				var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.jpg");

				var document = QuestPDF.Fluent.Document.Create(container =>
				{
					container.Page(page =>
					{
						page.Size(PageSizes.A4);
						page.Margin(2, Unit.Centimetre);
						page.PageColor(Colors.White);
						page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

						page.Header().Element(ComposeHeader);
						page.Content().Element(ComposeContent);
					});
				});

				return document.GeneratePdf();

				void ComposeHeader(IContainer container)
				{
					container.Column(column =>
					{
						if (File.Exists(logoPath))
						{
							try { column.Item().Height(60).AlignLeft().Image(logoPath).FitHeight(); } catch { }
						}
						column.Item().Text("HIPPOCAMPE IMPORT-EXPORT").Bold().FontSize(10).FontColor(Colors.Blue.Darken2);
					});
				}

				void ComposeContent(IContainer container)
				{
					// On réduit le padding vertical global pour gagner de la place
					container.PaddingVertical(10).Column(column =>
					{
						// TITRE
						column.Item().AlignCenter().Text("ATTESTATION DE VALEUR").FontSize(18).Bold().Underline();
						column.Item().AlignCenter().Text($"({vehicule.Type})").FontSize(14);
						
						column.Item().Height(20); // Espace réduit

						// DÉCLARATION
						column.Item().Text(text =>
						{
							text.Span("Je soussigné ");
							text.Span($"{vehicule.Client?.NomComplet}").Bold();
							text.Span($" déclare vouloir expédier à {vehicule.DestinationFinale} mon véhicule personnel décrit ci-dessous :");
						});

						column.Item().Height(15);

						column.Item().Text("Je certifie les informations suivantes :").Underline().Bold();
						
						column.Item().Height(10);

						// TABLEAU 1 : INFOS VÉHICULE
						column.Item().Table(table =>
						{
							table.ColumnsDefinition(columns =>
							{
								columns.ConstantColumn(150);
								columns.RelativeColumn();
							});

							static IContainer CellStyle(IContainer c) => c.Border(1).BorderColor(Colors.Black).Padding(5);
							
							void AddRow(string label, string value)
							{
								table.Cell().Element(CellStyle).Text(label).Bold();
								table.Cell().Element(CellStyle).Text(value);
							}

							AddRow("Marque :", vehicule.Marque);
							AddRow("Modèle :", vehicule.Modele);
							AddRow("Immatriculation :", vehicule.Immatriculation);
							AddRow("Valeur :", $"{vehicule.ValeurDeclaree:N0} €");
							AddRow("N° de téléphone :", vehicule.Client?.TelephonePrincipal ?? "");
							AddRow("Adresse mail :", vehicule.Client?.Email ?? "");
						});

						column.Item().Height(15);

						// TABLEAU 2 : ADRESSE UNIQUE (Fusionnée)
						column.Item().Table(table =>
						{
							table.ColumnsDefinition(columns =>
							{
								columns.ConstantColumn(150);
								columns.RelativeColumn();
							});
							
							static IContainer CellStyle(IContainer c) => c.Border(1).BorderColor(Colors.Black).Padding(5).AlignMiddle();

							// Construction de l'adresse complète
							string adresseComplete = vehicule.Client?.AdressePrincipale ?? "";
							if (!string.IsNullOrEmpty(vehicule.Client?.CodePostal) || !string.IsNullOrEmpty(vehicule.Client?.Ville))
							{
								adresseComplete += $", {vehicule.Client?.CodePostal} {vehicule.Client?.Ville}";
							}
							if (!string.IsNullOrEmpty(vehicule.Client?.Pays))
							{
								adresseComplete += $", {vehicule.Client?.Pays}";
							}

							// Une seule ligne "Adresse :"
							table.Cell().Element(CellStyle).Height(40).Text("Adresse :");
							table.Cell().Element(CellStyle).Text(adresseComplete);
						});

						column.Item().Height(15);

						// DOCUMENTS À JOINDRE
						column.Item().Text("Documents à joindre :").Bold().Underline();
						column.Item().Text("- copie carte grise");
						column.Item().Text("- copie du certificat de cession (si la carte grise n'est pas au nom de l'acheteur)");
						column.Item().Text("- certificat de non-gage");
						column.Item().Text("- Facture d'achat (si achat dans un concessionnaire ou dans un garage)");
						column.Item().Text("- copie de la pièce d'identité de l'acheteur");

						column.Item().Height(25);

						// PIED DE PAGE
						column.Item().AlignCenter().Text("Attestation fait pour servir et valoir ce que de droit");

						column.Item().Height(25);

						// DATE ET SIGNATURE
						// Lieu et Date sur la même ligne
						string lieu = !string.IsNullOrEmpty(vehicule.LieuEtatDesLieux) ? vehicule.LieuEtatDesLieux : "Bordeaux";
						string date = vehicule.DateEtatDesLieux.HasValue ? vehicule.DateEtatDesLieux.Value.ToString("dd/MM/yyyy") : DateTime.Now.ToString("dd/MM/yyyy");

						column.Item().Text($"Fait à {lieu}, le {date}");
						
						column.Item().PaddingTop(5).Text("Signature :");
						
						// Insertion de la signature client
						if (!string.IsNullOrEmpty(vehicule.SignatureClient))
						{
							try
							{
								var bytes = Convert.FromBase64String(vehicule.SignatureClient.Split(',')[1]);
								// Syntaxe corrigée ici aussi
								column.Item().Height(50).AlignLeft().Image(bytes).FitHeight();
							}
							catch { }
						}
					});
				}
			});
		}				
		
    }
}
