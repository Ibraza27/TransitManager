using System.Windows.Controls;
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Dashboard
{
    /// <summary>
    /// Logique d'interaction pour DashboardView.xaml
    /// </summary>
    public partial class DashboardView : System.Windows.Controls.UserControl
    {
        public DashboardView(DashboardViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}