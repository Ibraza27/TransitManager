using System.Windows.Controls;
using System.Windows.Input;
using TransitManager.Core.Entities;
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Colis
{
    public partial class ColisListView : System.Windows.Controls.UserControl
    {
        public ColisListView()
        {
            InitializeComponent();
        }

		private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is DataGridRow row && row.DataContext is Core.Entities.Colis colis)
			{
				if (DataContext is ColisViewModel viewModel && viewModel.EditCommand.CanExecute(colis))
				{
					// La commande IAsyncRelayCommand gère l'exécution asynchrone elle-même.
					// Il suffit d'appeler Execute.
					viewModel.EditCommand.Execute(colis);
				}
			}
		}
    }
}