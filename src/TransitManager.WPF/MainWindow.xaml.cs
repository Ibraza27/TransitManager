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
		// On enlève _serviceProvider et _isMenuExpanded qui ne sont plus utilisés
		
		public MainWindow(MainViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
			// L'animation n'est plus dans le XAML, on peut la retirer ou la recréer si besoin
			// StartSyncAnimation(); 
		}



		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			System.Windows.Application.Current.Shutdown();
		}
    }
}