using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq; // <-- AJOUTER CETTE LIGNE
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;

namespace TransitManager.WPF.ViewModels
{
    public class PrintPreviewViewModel : BaseViewModel
    {
        private readonly IPrintingService _printingService; // On le garde pour lister les imprimantes
        private readonly IDialogService _dialogService;

        private byte[]? _pdfData;
        private string? _tempPdfPath;

        public ObservableCollection<string> AvailablePrinters { get; } = new();

        private string? _selectedPrinter;
        public string? SelectedPrinter
        {
            get => _selectedPrinter;
            set => SetProperty(ref _selectedPrinter, value);
        }

        private int _numberOfCopies = 1;
        public int NumberOfCopies
        {
            get => _numberOfCopies;
            set => SetProperty(ref _numberOfCopies, value);
        }

        private Uri? _pdfSource;
        public Uri? PdfSource
        {
            get => _pdfSource;
            set => SetProperty(ref _pdfSource, value);
        }
		
		public event Action? PrintRequested;

        // CORRECTION 1 : Le type de la commande est maintenant IRelayCommand
        public IRelayCommand PrintCommand { get; }
        public Action? CloseAction { get; set; }

        // On remet IPrintingService dans le constructeur car LoadPrintersAsync en a besoin
        public PrintPreviewViewModel(IPrintingService printingService, IDialogService dialogService)
        {
            _printingService = printingService;
            _dialogService = dialogService;
            Title = "Aperçu avant impression";
            // 2. La commande appelle maintenant une méthode simple
            PrintCommand = new RelayCommand(Print); 
        }

        public override async Task InitializeAsync()
        {
            await LoadPrintersAsync();
        }

        public void LoadPdf(byte[] pdfData)
        {
            _pdfData = pdfData;
            
            _tempPdfPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
            File.WriteAllBytes(_tempPdfPath, _pdfData);
            PdfSource = new Uri(_tempPdfPath);
        }

		private async Task LoadPrintersAsync()
		{
			await ExecuteBusyActionAsync(async () =>
			{
                // CORRECTION 2 : _printingService est de nouveau disponible
				var printers = await _printingService.GetAvailablePrintersAsync();
				
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailablePrinters.Clear();
                    foreach (var printer in printers)
                    {
                        AvailablePrinters.Add(printer);
                    }
                });
				
				SelectedPrinter = AvailablePrinters.FirstOrDefault();
			});
		}

        private void Print()
        {
            // On déclenche simplement l'événement. La vue fera le reste.
            PrintRequested?.Invoke();
        }

        public void Cleanup()
        {
            if (!string.IsNullOrEmpty(_tempPdfPath) && File.Exists(_tempPdfPath))
            {
                try 
                {
                    File.Delete(_tempPdfPath);
                }
                catch 
                {
                    // Ignorer les erreurs si le fichier est encore utilisé par le lecteur PDF
                }
            }
        }
    }
}