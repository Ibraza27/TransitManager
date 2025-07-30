using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Navigation;
using TransitManager.WPF.ViewModels;
using TransitManager.WPF.Views.Auth;
using TransitManager.WPF.Views.Clients;
using TransitManager.WPF.Views.Colis;
using TransitManager.WPF.Views.Conteneurs;
using TransitManager.WPF.Views.Dashboard;
using TransitManager.WPF.Views.Finance;
using System.Windows;

namespace TransitManager.WPF.Helpers
{
    public interface INavigationService
    {
        void Initialize(Frame frame);
        void NavigateTo(string viewName, object? parameter = null);
        void NavigateBack();
        //void Initialize(object mainFrame);

        bool CanNavigateBack { get; }
        event EventHandler<System.Windows.Navigation.NavigationEventArgs>? Navigated;
    }

    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private Frame? _frame;
        private readonly Stack<string> _navigationHistory = new();
        private readonly Dictionary<string, Type> _viewMappings = new();

        public bool CanNavigateBack => _navigationHistory.Count > 1;

        public event EventHandler<System.Windows.Navigation.NavigationEventArgs>? Navigated;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            RegisterViews();
        }

        public void Initialize(Frame frame)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
            _frame.Navigated += OnFrameNavigated;
        }

        private void RegisterViews()
        {
            // Enregistrer toutes les vues disponibles
            _viewMappings["Login"] = typeof(LoginView);
            _viewMappings["Dashboard"] = typeof(DashboardView);
            _viewMappings["Clients"] = typeof(ClientListView);
            _viewMappings["ClientDetail"] = typeof(ClientDetailView);
            _viewMappings["Colis"] = typeof(ColisListView);
            _viewMappings["Scanner"] = typeof(ColisScanView);
            _viewMappings["Conteneurs"] = typeof(ConteneurListView);
            _viewMappings["ConteneurDetail"] = typeof(ConteneurDetailView);
            _viewMappings["Finance"] = typeof(PaiementView);
            _viewMappings["Factures"] = typeof(FactureView);
            // Ajouter d'autres mappings selon les besoins
        }

        public void NavigateTo(string viewName, object? parameter = null)
        {
            if (_frame == null)
                throw new InvalidOperationException("NavigationService n'est pas initialisé. Appelez Initialize() d'abord.");

            if (!_viewMappings.TryGetValue(viewName, out var viewType))
                throw new ArgumentException($"Vue '{viewName}' non trouvée.", nameof(viewName));

            // Créer l'instance de la vue
            var view = _serviceProvider.GetRequiredService(viewType) as Page;
            if (view == null)
                throw new InvalidOperationException($"Impossible de créer une instance de {viewType.Name}");

            // Si la vue a un DataContext qui est un ViewModel, l'initialiser
            if (view.DataContext is BaseViewModel viewModel)
            {
                // Si c'est un ViewModel avec paramètre
                if (parameter != null && viewModel.GetType().IsGenericType)
                {
                    var initMethod = viewModel.GetType().GetMethod("InitializeAsync", new[] { parameter.GetType() });
                    if (initMethod != null)
                    {
                        initMethod.Invoke(viewModel, new[] { parameter });
                    }
                }
                else
                {
                    // Initialisation standard
                    _ = viewModel.InitializeAsync();
                }
            }

            // Naviguer vers la vue
            _frame.Navigate(view, parameter);
            
            // Ajouter à l'historique
            _navigationHistory.Push(viewName);
        }

        public void NavigateBack()
        {
            if (_frame?.CanGoBack == true)
            {
                _frame.GoBack();
                
                // Retirer de l'historique
                if (_navigationHistory.Count > 0)
                    _navigationHistory.Pop();
            }
        }

		private void OnFrameNavigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{
			// La nouvelle propriété est 'e.Content', pas e.Parameter.
			if (e.Content is FrameworkElement { DataContext: BaseViewModel viewModel })
			{
				_ = viewModel.LoadAsync();
			}

			Navigated?.Invoke(this, e);
		}
    }


}