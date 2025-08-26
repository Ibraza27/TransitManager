using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using TransitManager.WPF.Models;
using TransitManager.WPF.Views;

namespace TransitManager.WPF.ViewModels
{
    public class AddVehiculeToConteneurViewModel : BaseViewModel
    {
        private readonly IVehiculeService _vehiculeService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;
        private Guid _conteneurId;

        public ObservableCollection<SelectableVehiculeWrapper> UnassignedVehicules { get; } = new();
        
        private string _searchText = string.Empty;
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) { _ = LoadUnassignedVehiculesAsync(); } } }

        public bool HasMadeChanges { get; private set; } = false;

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand AddSelectedCommand { get; }
        public IAsyncRelayCommand<SelectableVehiculeWrapper> AddSingleVehiculeCommand { get; }
        public IAsyncRelayCommand<SelectableVehiculeWrapper> ViewVehiculeDetailsCommand { get; }
        public IAsyncRelayCommand<SelectableVehiculeWrapper> ViewClientDetailsCommand { get; }

        public AddVehiculeToConteneurViewModel(IVehiculeService vehiculeService, IServiceProvider serviceProvider, IDialogService dialogService)
        {
            _vehiculeService = vehiculeService;
            _serviceProvider = serviceProvider;
            _dialogService = dialogService;
            Title = "Ajouter des Véhicules au Conteneur";

            RefreshCommand = new AsyncRelayCommand(LoadUnassignedVehiculesAsync);
            AddSelectedCommand = new AsyncRelayCommand(AddSelectedAsync, () => UnassignedVehicules.Any(w => w.IsSelected));
            AddSingleVehiculeCommand = new AsyncRelayCommand<SelectableVehiculeWrapper>(AddSingleVehiculeAsync);
            ViewVehiculeDetailsCommand = new AsyncRelayCommand<SelectableVehiculeWrapper>(ViewVehiculeDetailsAsync);
            ViewClientDetailsCommand = new AsyncRelayCommand<SelectableVehiculeWrapper>(ViewClientDetailsAsync);
        }

        public async Task InitializeAsync(Guid conteneurId)
        {
            _conteneurId = conteneurId;
            await LoadUnassignedVehiculesAsync();
        }

        private async Task LoadUnassignedVehiculesAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var allVehicules = await _vehiculeService.GetAllAsync();
                var assignableStatuses = new[] { StatutVehicule.EnAttente, StatutVehicule.Probleme, StatutVehicule.Retourne };
                var unassigned = allVehicules.Where(v => v.ConteneurId == null && assignableStatuses.Contains(v.Statut)).ToList();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTextLower = SearchText.ToLower();
                    unassigned = unassigned.Where(v => 
                        v.Immatriculation.ToLower().Contains(searchTextLower) ||
                        (v.Client?.NomComplet.ToLower().Contains(searchTextLower) == true) ||
                        v.Marque.ToLower().Contains(searchTextLower) ||
                        v.Modele.ToLower().Contains(searchTextLower)
                    ).ToList();
                }

                UnassignedVehicules.Clear();
                foreach (var vehicule in unassigned.OrderByDescending(v => v.DateCreation))
                {
                    var wrapper = new SelectableVehiculeWrapper(vehicule);
                    wrapper.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(SelectableVehiculeWrapper.IsSelected)) AddSelectedCommand.NotifyCanExecuteChanged(); };
                    UnassignedVehicules.Add(wrapper);
                }
            });
        }

        private async Task AddSelectedAsync()
        {
            var vehiculesToAdd = UnassignedVehicules.Where(w => w.IsSelected).Select(w => w.Vehicule).ToList();
            if (!vehiculesToAdd.Any()) return;
            await AddVehiculesListToConteneurAsync(vehiculesToAdd);
        }

        private async Task AddSingleVehiculeAsync(SelectableVehiculeWrapper? wrapper)
        {
            if (wrapper?.Vehicule == null) return;
            await AddVehiculesListToConteneurAsync(new List<Vehicule> { wrapper.Vehicule });
        }

        private async Task AddVehiculesListToConteneurAsync(List<Vehicule> vehiculeList)
        {
            foreach (var vehicule in vehiculeList)
            {
                await _vehiculeService.AssignToConteneurAsync(vehicule.Id, _conteneurId);
            }
            HasMadeChanges = true;
            await _dialogService.ShowInformationAsync("Succès", $"{vehiculeList.Count} véhicule(s) ajouté(s) au conteneur.");
            await LoadUnassignedVehiculesAsync();
        }

        private async Task ViewVehiculeDetailsAsync(SelectableVehiculeWrapper? wrapper)
        {
            if (wrapper?.Vehicule == null) return;
            using var scope = _serviceProvider.CreateScope();
            var vm = scope.ServiceProvider.GetRequiredService<VehiculeDetailViewModel>();
            vm.SetModalMode();
            await vm.InitializeAsync(wrapper.Vehicule.Id);
            var window = new DetailHostWindow { DataContext = vm, Owner = App.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) };
            vm.CloseAction = () => window.Close();
            window.ShowDialog();
        }

        private async Task ViewClientDetailsAsync(SelectableVehiculeWrapper? wrapper)
        {
            if (wrapper?.Vehicule?.Client == null) return;
            using var scope = _serviceProvider.CreateScope();
            var vm = scope.ServiceProvider.GetRequiredService<ClientDetailViewModel>();
            vm.SetModalMode();
            await vm.InitializeAsync(wrapper.Vehicule.ClientId);
            var window = new DetailHostWindow { DataContext = vm, Owner = App.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) };
            vm.CloseAction = () => window.Close();
            window.ShowDialog();
        }
    }
}