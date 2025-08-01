using System.Windows;
using System.Windows.Controls; // <-- USING IMPORTANT pour PasswordBox
using TransitManager.WPF.ViewModels;

namespace TransitManager.WPF.Views.Auth
{
    public partial class LoginView : MahApps.Metro.Controls.MetroWindow // Assurez-vous que c'est bien Window ou MetroWindow
    {
        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.CloseAction = (result) =>
            {
                DialogResult = result;
                this.Close();
            };
        }

        // === MÉTHODE AJOUTÉE POUR LE MOT DE PASSE ===
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }

        // === MÉTHODE AJOUTÉE POUR LE BOUTON FERMER ===
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}