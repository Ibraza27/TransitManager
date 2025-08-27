using System.Windows.Controls;
using System.Windows.Input; // Gardez celui-ci pour MouseButtonEventArgs
using TransitManager.WPF.ViewModels; // <-- LIGNE AJOUTÉE pour résoudre CS0246

namespace TransitManager.WPF.Views.Clients
{
    public partial class ClientListView : System.Windows.Controls.UserControl // <-- On spécifie System.Windows.Controls.UserControl
    {
        public ClientListView()
        {
            InitializeComponent();
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Logique à venir
        }
		
		private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            // On spécifie explicitement qu'on veut le ComboBox de WPF
			if (DataContext is ClientViewModel viewModel && sender is System.Windows.Controls.ComboBox comboBox)
			{
				if (comboBox.SelectedItem is ComboBoxItem item)
				{
                    // On s'assure que le contenu n'est pas null avant de l'utiliser
					if (item.Content != null)
                    {
                        viewModel.SelectedStatus = item.Content.ToString() ?? "Tous";
                    }
				}
			}
		}
    }
}