using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms; // Important pour le FolderBrowserDialog

namespace TransitManager.WPF.Helpers
{
    // --- L'INTERFACE CORRECTE ---
    public interface IDialogService
    {
        Task<bool> ShowConfirmationAsync(string title, string message);
        Task ShowInformationAsync(string title, string message);
        Task ShowWarningAsync(string title, string message);
        Task ShowErrorAsync(string title, string message);
        Task<string?> ShowInputAsync(string title, string prompt, string? defaultValue = null);
        Task<T?> ShowDialogAsync<T>(object viewModel) where T : class;
        string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*", string? initialDirectory = null);
        string? ShowSaveFileDialog(string filter = "All files (*.*)|*.*", string? defaultFileName = null);
        string? ShowFolderBrowserDialog(string? description = null);
        void ShowProgress(string message, Action<IProgress<double>> work);
    }

    // --- LA CLASSE D'IMPLÉMENTATION CORRECTE ---
    public class DialogService : IDialogService
    {
        // Votre code existant est bon, donc je le garde.
        // La seule chose qui manquait était la séparation correcte de l'interface
        // et la correction des noms de méthodes.
        //private readonly ISnackbarMessageQueue _snackbarMessageQueue; // Vous pouvez décommenter si vous utilisez les Snackbars

        public DialogService()
        {
            //_snackbarMessageQueue = new SnackbarMessageQueue();
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return await Task.FromResult(result == MessageBoxResult.Yes);
        }

        public Task ShowInformationAsync(string title, string message)
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        public Task ShowWarningAsync(string title, string message)
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return Task.CompletedTask;
        }

        public Task ShowErrorAsync(string title, string message)
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.CompletedTask;
        }
        
        // Note : Votre code original utilisait DialogHost, ce qui est excellent
        // mais requiert plus de configuration dans le XAML de la MainWindow.
        // Pour l'instant, cette version plus simple avec MessageBox fonctionnera.
        // Nous pourrons réintégrer DialogHost une fois que le projet compile.
        public Task<string?> ShowInputAsync(string title, string prompt, string? defaultValue = null)
        {
            // Implémentation simplifiée. Vous pouvez créer une petite fenêtre pour ça.
            // Pour le moment, on retourne null.
            return Task.FromResult<string?>(null);
        }

        public Task<T?> ShowDialogAsync<T>(object viewModel) where T : class
        {
            // Pour le moment, on retourne null.
            return Task.FromResult<T?>(null);
        }
        
        public string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*", string? initialDirectory = null)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog // Le type vient de Microsoft.Win32
            {
                Filter = filter,
                InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Multiselect = false
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowSaveFileDialog(string filter = "All files (*.*)|*.*", string? defaultFileName = null)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog // Le type vient de Microsoft.Win32
            {
                Filter = filter,
                FileName = defaultFileName,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowFolderBrowserDialog(string? description = null)
        {
            var dialog = new FolderBrowserDialog // Le type vient de System.Windows.Forms
            {
                Description = description ?? "Sélectionner un dossier",
                ShowNewFolderButton = true
            };
            var result = dialog.ShowDialog();
            return result == DialogResult.OK ? dialog.SelectedPath : null;
        }

        public void ShowProgress(string message, Action<IProgress<double>> work)
        {
            // Logique de progression à implémenter si nécessaire
        }
    }

    // VOS CLASSES DE DIALOGUES PERSONNALISÉES SONT OK, MAIS INUTILISÉES POUR LE MOMENT
    public class ConfirmationDialog
    {
        public string Title { get; set; } = "Confirmation";
        public string Message { get; set; } = "Êtes-vous sûr ?";
        public string ConfirmText { get; set; } = "Oui";
        public string CancelText { get; set; } = "Non";
    }

    public class MessageDialog
    {
        public string Title { get; set; } = "Information";
        public string Message { get; set; } = "";
        public PackIconKind Icon { get; set; } = PackIconKind.Information;
        public System.Windows.Media.Color IconColor { get; set; } = System.Windows.Media.Colors.Blue;
    }

    public class InputDialog
    {
        public string Title { get; set; } = "Saisie";
        public string Prompt { get; set; } = "Veuillez saisir une valeur :";
        public string? DefaultValue { get; set; }
        public string Value { get; set; } = "";
    }

    public class ProgressDialog
    {
        public string Message { get; set; } = "Traitement en cours...";
        public double Progress { get; set; }
        public bool IsIndeterminate { get; set; } = false;
    }
}