using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using TransitManager.WPF.ViewModels;
using TransitManager.WPF.Views.Conteneurs;

namespace TransitManager.WPF.Helpers
{
    public interface INavigationService
    {
        BaseViewModel? CurrentView { get; }
        event Action<BaseViewModel>? CurrentViewChanged;
        bool CanGoBack { get; }
        void GoBack();
        void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
        void NavigateTo(string viewName);
        void NavigateTo(string viewName, object? parameter);
    }

    public class NavigationService : ObservableObject, INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private BaseViewModel? _currentView;
        private readonly Stack<BaseViewModel> _history = new Stack<BaseViewModel>();

        public BaseViewModel? CurrentView
        {
            get => _currentView;
            private set => SetProperty(ref _currentView, value);
        }

        public bool CanGoBack => _history.Count > 0;

        public event Action<BaseViewModel>? CurrentViewChanged;

        private readonly Dictionary<string, Type> _viewModelMappings = new();

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            RegisterViewModels();
        }

        private void RegisterViewModels()
        {
            _viewModelMappings["Dashboard"] = typeof(DashboardViewModel);
            _viewModelMappings["Clients"] = typeof(ClientViewModel);
            _viewModelMappings["ClientDetail"] = typeof(ClientDetailViewModel);
            _viewModelMappings["Colis"] = typeof(ColisViewModel);
			_viewModelMappings["ColisDetail"] = typeof(ColisDetailViewModel);
			_viewModelMappings["Vehicules"] = typeof(VehiculeViewModel);
            _viewModelMappings["Conteneurs"] = typeof(ConteneurViewModel);
            _viewModelMappings["ConteneurDetail"] = typeof(ConteneurDetailViewModel);
            _viewModelMappings["Notifications"] = typeof(NotificationsViewModel);
        }

        public void NavigateTo(string viewName)
        {
            NavigateTo(viewName, null);
        }

        public void NavigateTo(string viewName, object? parameter)
        {
            if (!_viewModelMappings.TryGetValue(viewName, out var viewModelType))
            {
                throw new ArgumentException($"ViewModel pour la vue '{viewName}' non trouvé.");
            }

            // On empile la vue actuelle dans l'historique avant de changer
            if (CurrentView != null)
            {
                _history.Push(CurrentView);
            }

            var viewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);

            if (parameter != null)
            {
                var methodInfo = viewModel.GetType().GetMethod("InitializeAsync", new[] { parameter.GetType() });
                if (methodInfo != null)
                {
                    methodInfo.Invoke(viewModel, new[] { parameter });
                }
                else
                {
                    _ = viewModel.InitializeAsync();
                }
            }
            else
            {
                _ = viewModel.InitializeAsync();
            }

            CurrentView = viewModel;
            CurrentViewChanged?.Invoke(CurrentView);
        }

        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
             if (CurrentView != null)
            {
                _history.Push(CurrentView);
            }
            
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            CurrentView = viewModel;
            _ = CurrentView.InitializeAsync();
            CurrentViewChanged?.Invoke(CurrentView);
        }

		public void GoBack()
		{
			if (CanGoBack)
			{
				CurrentView = _history.Pop();
				// On ne recharge PAS automatiquement. La vue précédente est simplement réaffichée.
				CurrentViewChanged?.Invoke(CurrentView!);
			}
		}
    }
}