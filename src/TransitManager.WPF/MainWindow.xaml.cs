using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using TransitManager.WPF.Helpers;
using TransitManager.WPF.ViewModels;
using System.Windows.Controls; // Pour Descendants<TextBlock>
using System.Windows.Media;      // Pour RotateTransform

namespace TransitManager.WPF
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly INavigationService _navigationService;
        private bool _isMenuExpanded = true;


        public MainWindow(IServiceProvider serviceProvider, MainViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            
            _serviceProvider = serviceProvider;
            _navigationService = navigationService;
            
            DataContext = viewModel;
            
            // Initialiser le service de navigation avec le Frame
            _navigationService.Initialize(MainFrame);
            
            // Naviguer vers le tableau de bord par défaut
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _navigationService.NavigateTo("Dashboard");
            StartSyncAnimation();
        }

        private void ToggleMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMenuExpanded)
            {
                // Réduire le menu
                var animation = FindResource("MenuCloseAnimation") as Storyboard;
                animation?.Begin(SideMenu);
                
                // Masquer les textes
                foreach (var item in SideMenu.Descendants<System.Windows.Controls.TextBlock>())
                {
                    if (item.Name != "VersionText")
                        item.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Agrandir le menu
                var animation = FindResource("MenuOpenAnimation") as Storyboard;
                animation?.Begin(SideMenu);
                
                // Afficher les textes après l'animation
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };
                timer.Tick += (s, args) =>
                {
                    foreach (var item in SideMenu.Descendants<System.Windows.Controls.TextBlock>())
                    {
                        item.Visibility = Visibility.Visible;
                    }
                    timer.Stop();
                };
                timer.Start();
            }
            
            _isMenuExpanded = !_isMenuExpanded;
            
            // Changer l'icône
            var icon = ToggleMenuButton.Content as MaterialDesignThemes.Wpf.PackIcon;
            if (icon != null)
            {
                icon.Kind = _isMenuExpanded 
                    ? MaterialDesignThemes.Wpf.PackIconKind.MenuOpen 
                    : MaterialDesignThemes.Wpf.PackIconKind.Menu;
            }
        }

        private void StartSyncAnimation()
        {
            var rotation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            SyncRotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotation);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            System.Windows.Application.Current.Shutdown();
        }
    }
}