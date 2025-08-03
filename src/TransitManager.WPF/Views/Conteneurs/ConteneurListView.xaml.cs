using System.Windows.Controls;
using System.Windows.Input; // N'oubliez pas d'ajouter ce using !
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Conteneurs
{
    /// <summary>
    /// Logique d'interaction pour ConteneurListView.xaml
    /// </summary>
    public partial class ConteneurListView : System.Windows.Controls.UserControl
    {
        public ConteneurListView(ConteneurViewModel viewModel)
        {
            InitializeComponent();
        }

        // === MÉTHODE AJOUTÉE POUR CORRIGER L'ERREUR DE COMPILATION ===
        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // La logique pour cet événement sera ajoutée plus tard.
            // Par exemple, on pourrait récupérer le ViewModel et appeler une commande pour
            // naviguer vers la vue de détail du conteneur sélectionné.

            // if (DataContext is ConteneurViewModel viewModel && viewModel.ViewDetailsCommand.CanExecute(null))
            // {
            //     viewModel.ViewDetailsCommand.Execute(null);
            // }
        }
    }
}