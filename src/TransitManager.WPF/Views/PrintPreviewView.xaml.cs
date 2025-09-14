using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.ComponentModel;
using System.Windows;
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views
{
    public partial class PrintPreviewView : Window
    {
        public PrintPreviewView(PrintPreviewViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseAction = () => DialogResult = true;

            // S'abonner à l'événement du ViewModel
            viewModel.PrintRequested += OnPrintRequested;
        }

        // Gestionnaire d'événement qui exécute l'impression
        private async void OnPrintRequested()
        {
            if (PdfBrowser != null && PdfBrowser.CoreWebView2 != null)
            {
                // C'est la commande magique qui ouvre la boîte de dialogue d'impression
                await PdfBrowser.CoreWebView2.ExecuteScriptAsync("window.print();");
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is PrintPreviewViewModel vm)
            {
                // Se désabonner de l'événement pour éviter les fuites de mémoire
                vm.PrintRequested -= OnPrintRequested;
                vm.Cleanup();
            }
        }
    }
}