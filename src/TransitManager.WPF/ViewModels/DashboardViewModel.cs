using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;

namespace TransitManager.WPF.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IClientService _clientService;
        private readonly IColisService _colisService;
        private readonly IConteneurService _conteneurService;
        private readonly IPaiementService _paiementService;

        // Statistiques
        private int _totalClients;
        private int _nouveauxClients;
        private int _colisEnAttente;
        private int _colisEnTransit;
        private int _conteneursActifs;
        private decimal _chiffreAffaireMois;
        private decimal _paiementsEnAttente;
        private decimal _tauxRemplissageMoyen;

        // Collections pour les listes
        private ObservableCollection<Client> _derniersClients = new();
        private ObservableCollection<Colis> _colisRecents = new();
        private ObservableCollection<Conteneur> _conteneursProchainDepart = new();
        private ObservableCollection<DashboardAlert> _alertes = new();

        // Graphiques
        public ISeries[] SeriesChiffreAffaires { get; set; } = Array.Empty<ISeries>();
        public ISeries[] SeriesRepartitionColis { get; set; } = Array.Empty<ISeries>();
        public ISeries[] SeriesEvolutionClients { get; set; } = Array.Empty<ISeries>();
        public Axis[] AxesX { get; set; } = Array.Empty<Axis>();
        public Axis[] AxesY { get; set; } = Array.Empty<Axis>();

        #region Propriétés

        public int TotalClients
        {
            get => _totalClients;
            set => SetProperty(ref _totalClients, value);
        }

        public int NouveauxClients
        {
            get => _nouveauxClients;
            set => SetProperty(ref _nouveauxClients, value);
        }

        public int ColisEnAttente
        {
            get => _colisEnAttente;
            set => SetProperty(ref _colisEnAttente, value);
        }

        public int ColisEnTransit
        {
            get => _colisEnTransit;
            set => SetProperty(ref _colisEnTransit, value);
        }

        public int ConteneursActifs
        {
            get => _conteneursActifs;
            set => SetProperty(ref _conteneursActifs, value);
        }

        public decimal ChiffreAffaireMois
        {
            get => _chiffreAffaireMois;
            set => SetProperty(ref _chiffreAffaireMois, value);
        }

        public decimal PaiementsEnAttente
        {
            get => _paiementsEnAttente;
            set => SetProperty(ref _paiementsEnAttente, value);
        }

        public decimal TauxRemplissageMoyen
        {
            get => _tauxRemplissageMoyen;
            set => SetProperty(ref _tauxRemplissageMoyen, value);
        }

        public ObservableCollection<Client> DerniersClients
        {
            get => _derniersClients;
            set => SetProperty(ref _derniersClients, value);
        }

        public ObservableCollection<Colis> ColisRecents
        {
            get => _colisRecents;
            set => SetProperty(ref _colisRecents, value);
        }

        public ObservableCollection<Conteneur> ConteneursProchainDepart
        {
            get => _conteneursProchainDepart;
            set => SetProperty(ref _conteneursProchainDepart, value);
        }

        public ObservableCollection<DashboardAlert> Alertes
        {
            get => _alertes;
            set => SetProperty(ref _alertes, value);
        }

        #endregion

        // Commandes
        public ICommand RefreshCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand ExportDashboardCommand { get; }
		public ICommand NavigateCommand { get; }

        public DashboardViewModel(
            IClientService clientService,
            IColisService colisService,
            IConteneurService conteneurService,
            IPaiementService paiementService)
        {
            _clientService = clientService;
            _colisService = colisService;
            _conteneurService = conteneurService;
            _paiementService = paiementService;

            Title = "Tableau de bord";

            // Initialiser les commandes
            RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);
            ViewDetailsCommand = new RelayCommand<string>(ViewDetails);
            ExportDashboardCommand = new AsyncRelayCommand(ExportDashboardAsync);
			NavigateCommand = new RelayCommand<string>(ViewDetails);

            InitializeCharts();
        }

        public override async Task LoadAsync()
        {
            await RefreshDataAsync();
        }

        private async Task RefreshDataAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                StatusMessage = "Chargement des données...";

                // Charger toutes les données en parallèle
                var tasks = new List<Task>
                {
                    LoadStatisticsAsync(),
                    LoadRecentDataAsync(),
                    LoadChartsDataAsync(),
                    LoadAlertsAsync()
                };

                await Task.WhenAll(tasks);

                StatusMessage = "Données mises à jour";
            });
        }

        private async Task LoadStatisticsAsync()
        {
            // Statistiques clients
            TotalClients = await _clientService.GetTotalCountAsync();
            NouveauxClients = await _clientService.GetNewClientsCountAsync(DateTime.Now.AddDays(-30));

            // Statistiques colis
            ColisEnAttente = await _colisService.GetCountByStatusAsync(StatutColis.EnAttente);
            ColisEnTransit = await _colisService.GetCountByStatusAsync(StatutColis.EnTransit);

            // Statistiques conteneurs
            ConteneursActifs = await _conteneurService.GetActiveCountAsync();
            TauxRemplissageMoyen = await _conteneurService.GetAverageFillingRateAsync();

            // Statistiques financières
            ChiffreAffaireMois = await _paiementService.GetMonthlyRevenueAsync(DateTime.Now);
            PaiementsEnAttente = await _paiementService.GetPendingAmountAsync();
        }

        private async Task LoadRecentDataAsync()
        {
            // Derniers clients
            var clients = await _clientService.GetRecentClientsAsync(5);
            DerniersClients = new ObservableCollection<Client>(clients);

            // Colis récents
            var colis = await _colisService.GetRecentColisAsync(10);
            ColisRecents = new ObservableCollection<Colis>(colis);

            // Conteneurs prochain départ
            var conteneurs = await _conteneurService.GetUpcomingDeparturesAsync(5);
            ConteneursProchainDepart = new ObservableCollection<Conteneur>(conteneurs);
        }

        private async Task LoadChartsDataAsync()
        {
            await Task.Run(() =>
            {
                // Graphique chiffre d'affaires (12 derniers mois)
                var revenueData = new[] { 45000, 52000, 48000, 61000, 58000, 72000, 
                                         68000, 75000, 82000, 79000, 85000, 92000 };
                
                SeriesChiffreAffaires = new ISeries[]
                {
                    new ColumnSeries<decimal>
                    {
                        Values = revenueData.Select(x => (decimal)x).ToArray(),
                        Name = "Chiffre d'affaires",
                        Fill = new SolidColorPaint(SKColors.Blue)
                    }
                };

                // Graphique répartition des colis
                SeriesRepartitionColis = new ISeries[]
                {
                    new PieSeries<double> { Values = new double[] { 45 }, Name = "En attente", Fill = new SolidColorPaint(SKColors.Orange) },
                    new PieSeries<double> { Values = new double[] { 30 }, Name = "En transit", Fill = new SolidColorPaint(SKColors.Blue) },
                    new PieSeries<double> { Values = new double[] { 20 }, Name = "Livrés", Fill = new SolidColorPaint(SKColors.Green) },
                    new PieSeries<double> { Values = new double[] { 5 }, Name = "Problème", Fill = new SolidColorPaint(SKColors.Red) }
                };

                // Graphique évolution clients
                var clientsData = new[] { 150, 162, 175, 189, 205, 218, 235 };
                SeriesEvolutionClients = new ISeries[]
                {
                    new LineSeries<int>
                    {
                        Values = clientsData,
                        Name = "Nombre de clients",
                        GeometrySize = 10,
                        GeometryStroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
                        Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
                        Fill = null
                    }
                };

                // Configuration des axes
                AxesX = new Axis[]
                {
                    new Axis
                    {
                        Labels = new[] { "Jan", "Fév", "Mar", "Avr", "Mai", "Jun", 
                                       "Jul", "Aoû", "Sep", "Oct", "Nov", "Déc" },
                        LabelsRotation = 0,
                        SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
                    }
                };

                AxesY = new Axis[]
                {
                    new Axis
                    {
                        Labeler = value => value.ToString("C0"),
                        SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
                    }
                };
            });
        }

        private async Task LoadAlertsAsync()
        {
            var alerts = new List<DashboardAlert>();

            // Colis en attente depuis plus de 7 jours
            var colisEnAttenteLongue = await _colisService.GetColisWaitingLongTimeAsync(7);
            if (colisEnAttenteLongue.Any())
            {
                alerts.Add(new DashboardAlert
                {
                    Type = TypeNotification.Avertissement,
                    Message = $"{colisEnAttenteLongue.Count()} colis en attente depuis plus de 7 jours",
                    Action = "Voir les colis",
                    ActionParameter = "colis-attente"
                });
            }

            // Conteneurs presque pleins
            var conteneursPresquePleins = await _conteneurService.GetAlmostFullContainersAsync(90);
            if (conteneursPresquePleins.Any())
            {
                alerts.Add(new DashboardAlert
                {
                    Type = TypeNotification.Information,
                    Message = $"{conteneursPresquePleins.Count()} conteneurs sont remplis à plus de 90%",
                    Action = "Voir les conteneurs",
                    ActionParameter = "conteneurs-pleins"
                });
            }

            // Paiements en retard
            var paiementsRetard = await _paiementService.GetOverduePaymentsAsync();
            if (paiementsRetard.Any())
            {
                alerts.Add(new DashboardAlert
                {
                    Type = TypeNotification.Erreur,
                    Message = $"{paiementsRetard.Count()} paiements en retard",
                    Action = "Voir les paiements",
                    ActionParameter = "paiements-retard"
                });
            }

            Alertes = new ObservableCollection<DashboardAlert>(alerts);
        }

        private void InitializeCharts()
        {
            // Configuration initiale des graphiques
            SeriesChiffreAffaires = new ISeries[] 
            { 
                new ColumnSeries<decimal> 
                { 
                    Values = Array.Empty<decimal>(),
                    Fill = new SolidColorPaint(SKColors.Blue)
                } 
            };
        }


		private void ViewDetails(string? parameter)
		{
			if (string.IsNullOrEmpty(parameter)) return;
			
			// La navigation sera gérée par le MainViewModel.
			// Pour l'instant, on ne fait rien ici pour ne pas avoir d'erreur.
			// switch (parameter)
			// {
			//     case "clients":
			//         //TODO: Utiliser le vrai service de navigation
			//         break;
			//     // etc.
			// }
		}

        private async Task ExportDashboardAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                StatusMessage = "Export du tableau de bord...";
                
                // TODO: Implémenter l'export PDF du dashboard
                await Task.Delay(1000);
                
                StatusMessage = "Export terminé";
            });
        }
    }

    public class DashboardAlert
    {
        public TypeNotification Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Action { get; set; }
        public string? ActionParameter { get; set; }
		public ICommand NavigateCommand { get; set; }
    }
	
}
