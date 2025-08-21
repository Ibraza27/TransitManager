using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; 
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Colis
{
    public partial class ColisDetailView : System.Windows.Controls.UserControl
    {
        public ColisDetailView()
        {
            InitializeComponent();
        }

        private void InventaireTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ColisDetailViewModel viewModel)
            {
                if (sender is System.Windows.Controls.TextBox textBox)
                {
                    // Correction : Utilisation de TraversalRequest au lieu de FocusNavigationRequest
                    textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
                
                if (viewModel.CheckInventaireModificationCommand.CanExecute(null))
                {
                    viewModel.CheckInventaireModificationCommand.Execute(null);
                }
            }
        }
    }
}