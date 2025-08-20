using System.Windows;
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Inventaire
{
    public partial class InventaireView : Window
    {
        public InventaireView(InventaireViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}