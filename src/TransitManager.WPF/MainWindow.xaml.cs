using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using TransitManager.WPF.Helpers;
using TransitManager.WPF.ViewModels;
using TransitManager.Core.Interfaces;
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
        private bool _isMenuExpanded = true;


        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }


        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // La navigation vers le tableau de bord est maintenant gérée par le MainViewModel.InitializeAsync()
            // _navigationService.NavigateTo("Dashboard"); // Cette ligne n'est plus nécessaire
            StartSyncAnimation();
        }


		private void ToggleMenuButton_Click(object sender, RoutedEventArgs e)
		{
			/*
			if (_isMenuExpanded)
			{
				// ... TOUT LE CODE EXISTANT ICI ...
			}
			else
			{
				// ... TOUT LE CODE EXISTANT ICI ...
			}
			
			_isMenuExpanded = !_isMenuExpanded;
			
			var icon = ToggleMenuButton.Content as MaterialDesignThemes.Wpf.PackIcon;
			if (icon != null)
			{
				icon.Kind = _isMenuExpanded 
					? MaterialDesignThemes.Wpf.PackIconKind.MenuOpen 
					: MaterialDesignThemes.Wpf.PackIconKind.Menu;
			}
			*/
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