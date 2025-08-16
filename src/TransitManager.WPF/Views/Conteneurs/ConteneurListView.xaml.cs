using System.Windows.Controls;
using System.Windows.Input;
using TransitManager.Core.Entities;
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Conteneurs
{
    /// <summary>
    /// Logique d'interaction pour ConteneurListView.xaml
    /// </summary>
    public partial class ConteneurListView : System.Windows.Controls.UserControl
    {
        public ConteneurListView()
        {
            InitializeComponent();
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is Conteneur conteneur)
            {
                if (DataContext is ConteneurViewModel viewModel)
                {
                    if (viewModel.ViewDetailsCommand.CanExecute(conteneur))
                    {
                        viewModel.ViewDetailsCommand.Execute(conteneur);
                    }
                }
            }
        }
    }
}