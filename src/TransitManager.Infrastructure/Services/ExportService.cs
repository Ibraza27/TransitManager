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
					
					foreach(var vehicule in vehiculesList)
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
		
		
    }

}