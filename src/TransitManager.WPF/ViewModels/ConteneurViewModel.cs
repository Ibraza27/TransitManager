using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using TransitManager.Core.Enums;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TransitManager.WPF.ViewModels
{
    public class ConteneurViewModel : BaseViewModel
    {
        private readonly IConteneurService _conteneurService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Conteneur> _conteneurs = new();
        public ObservableCollection<Conteneur> Conteneurs { get => _conteneurs; set => SetProperty(ref _conteneurs, value); }

        private Conteneur? _selectedConteneur;
        public Conteneur? SelectedConteneur { get => _selectedConteneur; set => SetProperty(ref _selectedConteneur, value); }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadConteneursAsync();
                }
            }
        }

        // Commandes
        public IAsyncRelayCommand NewConteneurCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand<Conteneur> EditCommand { get; }
        public IAsyncRelayCommand<Conteneur> DeleteCommand { get; }
        public IAsyncRelayCommand<Conteneur> ViewDetailsCommand { get; } // Commande ajoutée

        public ConteneurViewModel(
            IConteneurService conteneurService,
            INavigationService navigationService,
            IDialogService dialogService,
            IExportService exportService) // exportService est gardé pour plus tard
        {
            _conteneurService = conteneurService;
            _navigationService = navigationService;
            _dialogService = dialogService;

            Title = "Gestion des Conteneurs / Dossiers";
            
            NewConteneurCommand = new AsyncRelayCommand(NewConteneur);
            RefreshCommand = new AsyncRelayCommand(LoadConteneursAsync);
            // On fait pointer les deux commandes vers la même méthode
            EditCommand = new AsyncRelayCommand<Conteneur>(ViewDetails);
            ViewDetailsCommand = new AsyncRelayCommand<Conteneur>(ViewDetails); 
            DeleteCommand = new AsyncRelayCommand<Conteneur>(DeleteConteneur);
        }
        
        public override Task InitializeAsync()
        {
            return LoadConteneursAsync();
        }

        private async Task LoadConteneursAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                IEnumerable<Conteneur> conteneurs;
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    conteneurs = await _conteneurService.GetAllAsync();
                }
                else
                {
                    // La logique de recherche est maintenant dans le repository
                    conteneurs = await ((Infrastructure.Repositories.IConteneurRepository)_conteneurService).SearchAsync(SearchText);
                }
                Conteneurs = new ObservableCollection<Conteneur>(conteneurs.OrderByDescending(c => c.DateCreation));
            });
        }

        private Task NewConteneur()
        {
            _navigationService.NavigateTo("ConteneurDetail", "new");
            return Task.CompletedTask;
        }

        private Task ViewDetails(Conteneur? conteneur)
        {
            if (conteneur != null)
            {
                _navigationService.NavigateTo("ConteneurDetail", conteneur.Id);
            }
            return Task.CompletedTask;
        }

        private async Task DeleteConteneur(Conteneur? conteneur)
        {
            if (conteneur == null) return;
            var confirm = await _dialogService.ShowConfirmationAsync("Supprimer le Conteneur", 
                $"Êtes-vous sûr de vouloir supprimer le conteneur {conteneur.NumeroDossier}?");
            
            if (confirm)
            {
                try
                {
                    await _conteneurService.DeleteAsync(conteneur.Id);
                    await LoadConteneursAsync();
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur de suppression", ex.Message);
                }
            }
        }
    }
}