using System.Windows;
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Paiements
{
    public partial class PaiementColisView : Window
    {
        public PaiementColisView(PaiementColisViewModel viewModel)
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