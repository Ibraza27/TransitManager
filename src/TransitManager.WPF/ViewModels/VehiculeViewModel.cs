using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;

namespace TransitManager.WPF.ViewModels
{
    public class VehiculeViewModel : BaseViewModel
    {
        private readonly IVehiculeService _vehiculeService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Vehicule> _vehicules = new();
        public ObservableCollection<Vehicule> Vehicules { get => _vehicules; set => SetProperty(ref _vehicules, value); }

        private Vehicule? _selectedVehicule;
        public Vehicule? SelectedVehicule { get => _selectedVehicule; set => SetProperty(ref _selectedVehicule, value); }

        private string _searchText = string.Empty;
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) { _ = LoadVehiculesAsync(); } } }

        private decimal _prixTotalGlobal;
        public decimal PrixTotalGlobal { get => _prixTotalGlobal; set => SetProperty(ref _prixTotalGlobal, value); }

        private decimal _totalPayeGlobal;
        public decimal TotalPayeGlobal { get => _totalPayeGlobal; set => SetProperty(ref _totalPayeGlobal, value); }

        private decimal _totalRestantGlobal;
        public decimal TotalRestantGlobal { get => _totalRestantGlobal; set => SetProperty(ref _totalRestantGlobal, value); }

        public IAsyncRelayCommand NewVehiculeCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand<Vehicule> EditCommand { get; }
        public IAsyncRelayCommand<Vehicule> DeleteCommand { get; }

        public VehiculeViewModel(IVehiculeService vehiculeService, INavigationService navigationService, IDialogService dialogService)
        {
            _vehiculeService = vehiculeService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            Title = "Gestion des Véhicules";

            NewVehiculeCommand = new AsyncRelayCommand(NewVehicule);
            RefreshCommand = new AsyncRelayCommand(LoadVehiculesAsync);
            EditCommand = new AsyncRelayCommand<Vehicule>(EditVehicule);
            DeleteCommand = new AsyncRelayCommand<Vehicule>(DeleteVehicule);
        }

        public override Task InitializeAsync() => LoadVehiculesAsync();

        private async Task LoadVehiculesAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var vehicules = string.IsNullOrWhiteSpace(SearchText)
                    ? await _vehiculeService.GetAllAsync()
                    : await _vehiculeService.SearchAsync(SearchText);
                
                Vehicules = new ObservableCollection<Vehicule>(vehicules);
                CalculateStatistics();
            });
        }

        private void CalculateStatistics()
        {
            PrixTotalGlobal = Vehicules.Sum(v => v.PrixTotal);
            TotalPayeGlobal = Vehicules.Sum(v => v.SommePayee);
            TotalRestantGlobal = Vehicules.Sum(v => v.RestantAPayer);
        }

        private Task NewVehicule()
        {
            _navigationService.NavigateTo("VehiculeDetail", "new");
            return Task.CompletedTask;
        }

        private Task EditVehicule(Vehicule? vehicule)
        {
            if (vehicule != null)
            {
                _navigationService.NavigateTo("VehiculeDetail", vehicule.Id);
            }
            return Task.CompletedTask;
        }

        private async Task DeleteVehicule(Vehicule? vehicule)
        {
            if (vehicule == null) return;
            var confirm = await _dialogService.ShowConfirmationAsync("Supprimer le Véhicule", 
                $"Êtes-vous sûr de vouloir supprimer le véhicule {vehicule.Marque} {vehicule.Modele} ({vehicule.Immatriculation})?");
            
            if (confirm)
            {
                await _vehiculeService.DeleteAsync(vehicule.Id);
                await LoadVehiculesAsync();
            }
        }
    }
}