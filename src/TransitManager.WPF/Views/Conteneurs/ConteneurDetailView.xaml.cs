using System.Windows.Controls;
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Conteneurs
{
    /// <summary>
    /// Logique d'interaction pour ConteneurDetailView.xaml
    /// </summary>
    public partial class ConteneurDetailView : System.Windows.Controls.UserControl
    {
        public ConteneurDetailView(ConteneurDetailViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}