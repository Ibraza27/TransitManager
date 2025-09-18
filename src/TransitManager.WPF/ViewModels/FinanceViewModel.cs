using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.Generic;
using System;
using System.Linq;
using TransitManager.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using TransitManager.WPF.Views;
using CommunityToolkit.Mvvm.Messaging; // <-- AJOUTER CE USING
using TransitManager.Core.Messages; // <-- CHANGER DE .WPF.Messages À .Core.Messages


namespace TransitManager.WPF.ViewModels
{
    public class FinanceViewModel : BaseViewModel
    {
        private readonly IPaiementService _paiementService;
        private readonly IClientService _clientService;
		private readonly IServiceProvider _serviceProvider;
		private readonly IMessenger _messenger;

        #region Propriétés Tableau de Bord
        private decimal _chiffreAffaireMois;
        public decimal ChiffreAffaireMois { get => _chiffreAffaireMois; set => SetProperty(ref _chiffreAffaireMois, value); }
        private decimal _totalImpayes;
        public decimal TotalImpayes { get => _totalImpayes; set => SetProperty(ref _totalImpayes, value); }
        private decimal _paiementsRecusAujourdhui;
        public decimal PaiementsRecusAujourdhui { get => _paiementsRecusAujourdhui; set => SetProperty(ref _paiementsRecusAujourdhui, value); }
        private int _paiementsEnRetard;
        public int PaiementsEnRetard { get => _paiementsEnRetard; set => SetProperty(ref _paiementsEnRetard, value); }

        public ISeries[] SeriesChiffreAffaires { get; set; } = new ISeries[0];
        public Axis[] XAxes { get; set; } = new Axis[0];
        #endregion

        #region Propriétés Historique
        private List<Paiement> _allPaiements = new();
        private DateTime? _dateDebut;
        private DateTime? _dateFin;
        private Client? _selectedClient;
        private string _selectedStatut = "Tous";
        private string _searchText = string.Empty;

        public DateTime? DateDebut { get => _dateDebut; set { if (SetProperty(ref _dateDebut, value)) ApplyFilters(); } }
        public DateTime? DateFin { get => _dateFin; set { if (SetProperty(ref _dateFin, value)) ApplyFilters(); } }
        public Client? SelectedClient { get => _selectedClient; set { if (SetProperty(ref _selectedClient, value)) ApplyFilters(); } }
        public string SelectedStatut { get => _selectedStatut; set { if (SetProperty(ref _selectedStatut, value)) ApplyFilters(); } }
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) ApplyFilters(); } }

        public ObservableCollection<Client> ClientsList { get; } = new();
        public ObservableCollection<string> StatutsList { get; } = new();

        public ObservableCollection<Paiement> HistoriquePaiements { get; } = new();
        #endregion

        #region Propriétés Impayés
        public ObservableCollection<Client> ClientsAvecImpayes { get; } = new();
        #endregion

		#region Commandes
		public IAsyncRelayCommand RefreshCommand { get; }
		public IRelayCommand ClearFiltersCommand { get; }
		public IAsyncRelayCommand<Client> ViewClientDetailsCommand { get; } // AJOUTER CETTE LIGNE
		public IAsyncRelayCommand<Client> AddNewPaymentForClientCommand { get; } // AJOUTER CETTE LIGNE
		#endregion

		public FinanceViewModel(IPaiementService paiementService, IClientService clientService, IServiceProvider serviceProvider, IMessenger messenger) // AJOUTER IMessenger
		{
			_paiementService = paiementService;
			_clientService = clientService;
			_serviceProvider = serviceProvider;
			_messenger = messenger; // AJOUTER cette ligne
			Title = "Gestion Financière";

			RefreshCommand = new AsyncRelayCommand(LoadAsync);
			ClearFiltersCommand = new RelayCommand(ClearFilters);
			ViewClientDetailsCommand = new AsyncRelayCommand<Client>(ViewClientDetailsAsync); // AJOUTER CETTE LIGNE
			AddNewPaymentForClientCommand = new AsyncRelayCommand<Client>(AddNewPaymentForClientAsync); // AJOUTER CETTE LIGNE
			
			_messenger.RegisterAll(this); // S'abonner aux messages

			StatutsList = new ObservableCollection<string>(Enum.GetNames(typeof(StatutPaiement)));
			StatutsList.Insert(0, "Tous");
		}
		
		// AJOUTER CETTE MÉTHODE pour recevoir le message
		public async void Receive(PaiementUpdatedMessage message)
		{
			// Quand un paiement change, on recharge tout
			await LoadAsync();
		}		

		public override async Task InitializeAsync()
        {
            await LoadAsync();
        }

		public override async Task LoadAsync()
		{
			await ExecuteBusyActionAsync(async () =>
			{
				StatusMessage = "Chargement des données financières...";
				
				var tasks = new List<Task>
				{
					LoadDashboardDataAsync(),
					LoadHistoriqueDataAsync(),
					LoadImpayesDataAsync() // AJOUTER CETTE LIGNE
				};

				await Task.WhenAll(tasks);

				StatusMessage = "";
			});
		}

        private async Task LoadDashboardDataAsync()
        {
            var today = DateTime.UtcNow;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfToday = today.Date;
            var startOfTomorrow = startOfToday.AddDays(1);

            ChiffreAffaireMois = await _paiementService.GetMonthlyRevenueAsync(startOfMonth);
            TotalImpayes = await _clientService.GetTotalUnpaidBalanceAsync();

            var paiementsDuJour = await _paiementService.GetByPeriodAsync(startOfToday, startOfTomorrow);
            PaiementsRecusAujourdhui = paiementsDuJour.Where(p => p.Statut == StatutPaiement.Paye).Sum(p => p.Montant);

            var paiementsEnRetardList = await _paiementService.GetOverduePaymentsAsync();
            PaiementsEnRetard = paiementsEnRetardList.Count();

            var revenues = new List<decimal>();
            var labels = new List<string>();
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.UtcNow.AddMonths(-i);
                var firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
                revenues.Add(await _paiementService.GetMonthlyRevenueAsync(firstDayOfMonth));
                labels.Add(firstDayOfMonth.ToString("MMM yy"));
            }

            SeriesChiffreAffaires = new ISeries[]
            {
                new ColumnSeries<decimal>
                {
                    Values = revenues,
                    Name = "Revenus",
                    Fill = new SolidColorPaint(SKColors.DodgerBlue)
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 0,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
                }
            };
            OnPropertyChanged(nameof(SeriesChiffreAffaires));
            OnPropertyChanged(nameof(XAxes));
        }

		private async Task LoadHistoriqueDataAsync()
		{
			// Charger les données de base pour les filtres et la liste
			_allPaiements = (await _paiementService.GetAllAsync()).ToList();
			
			var clients = await _clientService.GetActiveClientsAsync();
			ClientsList.Clear();
			// On n'ajoute pas de client "Tous" car la ComboBox est éditable,
			// l'utilisateur peut simplement effacer le texte pour désélectionner.

			foreach (var client in clients.OrderBy(c => c.NomComplet))
			{
				ClientsList.Add(client);
			}

			// Appliquer les filtres pour la première fois
			ApplyFilters();
		}

        private void ApplyFilters()
        {
            IEnumerable<Paiement> filtered = _allPaiements;

            if (DateDebut.HasValue)
            {
                filtered = filtered.Where(p => p.DatePaiement.Date >= DateDebut.Value.Date);
            }
            if (DateFin.HasValue)
            {
                filtered = filtered.Where(p => p.DatePaiement.Date <= DateFin.Value.Date);
            }
            if (SelectedClient != null)
            {
                filtered = filtered.Where(p => p.ClientId == SelectedClient.Id);
            }
            if (!string.IsNullOrEmpty(SelectedStatut) && SelectedStatut != "Tous" && Enum.TryParse<StatutPaiement>(SelectedStatut, out var statut))
            {
                filtered = filtered.Where(p => p.Statut == statut);
            }
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTextLower = SearchText.ToLower();
                filtered = filtered.Where(p =>
                    p.NumeroRecu.ToLower().Contains(searchTextLower) ||
                    (p.Client?.NomComplet.ToLower().Contains(searchTextLower) ?? false)
                );
            }

            HistoriquePaiements.Clear();
            foreach (var paiement in filtered.OrderByDescending(p => p.DatePaiement))
            {
                HistoriquePaiements.Add(paiement);
            }
        }

        private void ClearFilters()
        {
            SetProperty(ref _dateDebut, null, nameof(DateDebut));
            SetProperty(ref _dateFin, null, nameof(DateFin));
            SetProperty(ref _selectedClient, null, nameof(SelectedClient));
            SetProperty(ref _selectedStatut, "Tous", nameof(SelectedStatut));
            SetProperty(ref _searchText, string.Empty, nameof(SearchText));

            ApplyFilters();
        }
		
		private async Task LoadImpayesDataAsync()
		{
			var clientsImpayes = await _clientService.GetClientsWithUnpaidBalanceAsync();
			ClientsAvecImpayes.Clear();
			foreach(var client in clientsImpayes)
			{
				ClientsAvecImpayes.Add(client);
			}
		}

		private async Task ViewClientDetailsAsync(Client? client)
		{
			if (client == null) return;

			// On réutilise la même logique que dans les autres vues
			using var scope = _serviceProvider.CreateScope();
			var clientDetailViewModel = scope.ServiceProvider.GetRequiredService<ClientDetailViewModel>();
			
			clientDetailViewModel.SetModalMode();
			await clientDetailViewModel.InitializeAsync(client.Id);

			var window = new DetailHostWindow
			{
				DataContext = clientDetailViewModel,
				Owner = System.Windows.Application.Current.MainWindow,
				Title = $"Détails du Client - {client.NomComplet}"
			};

			clientDetailViewModel.CloseAction = () => window.Close();
			window.ShowDialog();
			
			// Après fermeture, on rafraîchit les données des impayés
			await LoadImpayesDataAsync();
		}

		private async Task AddNewPaymentForClientAsync(Client? client)
		{
			if (client == null) return;
			
			// Ici, nous pourrions ouvrir une fenêtre de paiement générique pour le client.
			// Pour l'instant, naviguons vers la fiche client où l'utilisateur peut voir les détails.
			await ViewClientDetailsAsync(client);
		}
		
    }
}