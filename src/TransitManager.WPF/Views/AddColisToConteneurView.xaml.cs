using System.Windows;
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views
{
    public partial class AddColisToConteneurView : Window
    {
        public AddColisToConteneurView(AddColisToConteneurViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}