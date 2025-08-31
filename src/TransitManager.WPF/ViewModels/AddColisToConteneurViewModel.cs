using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using TransitManager.WPF.Views;
using TransitManager.WPF.Views.Inventaire;
using System.Text.Json;
using System.Windows;
using TransitManager.WPF.Models;
using Microsoft.Extensions.DependencyInjection;

namespace TransitManager.WPF.ViewModels
{
    public class AddColisToConteneurViewModel : BaseViewModel
    {
        private readonly IColisService _colisService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;
        private Guid _conteneurId;

        public ObservableCollection<SelectableColisWrapper> UnassignedColis { get; } = new();


        private string _searchText = string.Empty;
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) { _ = LoadUnassignedColisAsync(); } } }

        public bool HasMadeChanges { get; private set; } = false;

        public IAsyncRelayCommand SearchCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand AddSelectedCommand { get; }
		public IAsyncRelayCommand<SelectableColisWrapper> AddSingleColisCommand { get; }
		public IAsyncRelayCommand<SelectableColisWrapper> ViewInventaireCommand { get; }
		public IAsyncRelayCommand<SelectableColisWrapper> ViewColisDetailsCommand { get; }
		public IAsyncRelayCommand<SelectableColisWrapper> ViewClientDetailsCommand { get; }

        public AddColisToConteneurViewModel(IColisService colisService, IServiceProvider serviceProvider, IDialogService dialogService)
        {
            _colisService = colisService;
            _serviceProvider = serviceProvider;
            _dialogService = dialogService;
            Title = "Ajouter des Colis au Conteneur";

            SearchCommand = new AsyncRelayCommand(LoadUnassignedColisAsync);
            RefreshCommand = new AsyncRelayCommand(LoadUnassignedColisAsync);
            AddSelectedCommand = new AsyncRelayCommand(AddSelectedAsync, () => UnassignedColis.Any(w => w.IsSelected));
			AddSingleColisCommand = new AsyncRelayCommand<SelectableColisWrapper>(AddSingleColisAsync);
			ViewInventaireCommand = new AsyncRelayCommand<SelectableColisWrapper>(ViewInventaireAsync);
			ViewColisDetailsCommand = new AsyncRelayCommand<SelectableColisWrapper>(ViewColisDetailsAsync);
			ViewClientDetailsCommand = new AsyncRelayCommand<SelectableColisWrapper>(ViewClientDetailsAsync);

        }

        public async Task InitializeAsync(Guid conteneurId)
        {
            _conteneurId = conteneurId;
            await LoadUnassignedColisAsync();
        }

        private async Task LoadUnassignedColisAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var allColis = await _colisService.GetAllAsync();
				var assignableStatuses = new[] { 
					StatutColis.EnAttente, 
					StatutColis.Probleme, 
					StatutColis.Perdu, 
					StatutColis.Retourne 
				};
				
				var unassigned = allColis.Where(c => c.ConteneurId == null && assignableStatuses.Contains(c.Statut)).ToList();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTextLower = SearchText.ToLower();
                    unassigned = unassigned.Where(c => 
                        c.AllBarcodes.ToLower().Contains(searchTextLower) ||
                        c.NumeroReference.ToLower().Contains(searchTextLower) ||
                        (c.Client?.NomComplet.ToLower().Contains(searchTextLower) == true) ||
                        c.Designation.ToLower().Contains(searchTextLower) ||
                        c.DestinationFinale.ToLower().Contains(searchTextLower)
                    ).ToList();
                }

                UnassignedColis.Clear();
                foreach (var colis in unassigned.OrderByDescending(c => c.DateArrivee))
                {
					var wrapper = new SelectableColisWrapper(colis);
					// S'abonner au changement de la propriété IsSelected pour activer/désactiver le bouton
					wrapper.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(SelectableColisWrapper.IsSelected)) AddSelectedCommand.NotifyCanExecuteChanged(); };
                    UnassignedColis.Add(wrapper);
                }
            });
        }

		private async Task AddSelectedAsync()
		{
			// On récupère les wrappers cochés, puis on extrait l'objet Colis.
			var colisToAdd = UnassignedColis
				.Where(w => w.IsSelected)
				.Select(w => w.Colis)
				.ToList();

			if (!colisToAdd.Any()) return;

			await AddColisListToConteneurAsync(colisToAdd);
		}

		private async Task AddSingleColisAsync(SelectableColisWrapper? wrapper)
		{
			if (wrapper?.Colis == null) return;
			await AddColisListToConteneurAsync(new List<Colis> { wrapper.Colis });
		}

        private async Task AddColisListToConteneurAsync(List<Colis> colisList)
        {
            foreach (var colis in colisList)
            {
                await _colisService.AssignToConteneurAsync(colis.Id, _conteneurId);
            }
            HasMadeChanges = true;
            await _dialogService.ShowInformationAsync("Succès", $"{colisList.Count} colis ajouté(s) au conteneur.");
            await LoadUnassignedColisAsync(); // Refresh the list
        }

        // --- Fonctions pour le menu contextuel ---

		private async Task ViewInventaireAsync(SelectableColisWrapper? wrapper)
		{
			var colis = wrapper?.Colis; // Extraire le colis
			if (colis == null) return;
			var inventaireViewModel = new InventaireViewModel(colis.InventaireJson);
			var inventaireWindow = new InventaireView(inventaireViewModel)
			{
				Owner = App.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
			};
			inventaireWindow.ShowDialog();
			await Task.CompletedTask; // Ajouter cette ligne pour satisfaire le 'async'
		}

		private async Task ViewColisDetailsAsync(SelectableColisWrapper? wrapper)
		{
			var colis = wrapper?.Colis; // Extraire le colis
			if (colis == null) return;
			using var scope = _serviceProvider.CreateScope();
			var vm = scope.ServiceProvider.GetRequiredService<ColisDetailViewModel>();
			vm.SetModalMode();
			await vm.InitializeAsync(colis.Id);
			var window = new DetailHostWindow { DataContext = vm, Owner = App.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) };
			vm.CloseAction = () => window.Close();
			window.ShowDialog();
		}

		private async Task ViewClientDetailsAsync(SelectableColisWrapper? wrapper)
		{
			var colis = wrapper?.Colis; // Extraire le colis
			if (colis?.Client == null) return;
			using var scope = _serviceProvider.CreateScope();
			var vm = scope.ServiceProvider.GetRequiredService<ClientDetailViewModel>();
			vm.SetModalMode();
			await vm.InitializeAsync(colis.ClientId);
			var window = new DetailHostWindow { DataContext = vm, Owner = App.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) };
			vm.CloseAction = () => window.Close();
			window.ShowDialog();
		}
    }
}