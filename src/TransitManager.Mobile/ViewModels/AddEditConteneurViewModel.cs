using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;
using TransitManager.Mobile.Views;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ConteneurId), "conteneurId")]
    public partial class AddEditConteneurViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private string _conteneurId = string.Empty;

        [ObservableProperty]
        private Conteneur? _conteneur;

        [ObservableProperty]
        private string _pageTitle = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public AddEditConteneurViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        async partial void OnConteneurIdChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                PageTitle = "Nouveau Conteneur";
                Conteneur = new Conteneur { DateReception = DateTime.Now };
            }
            else
            {
                PageTitle = "Modifier le Conteneur";
                IsBusy = true;
                try
                {
                    var id = Guid.Parse(value);
                    Conteneur = await _transitApi.GetConteneurByIdAsync(id);
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger le conteneur : {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (Conteneur == null) return;
            
            if (string.IsNullOrWhiteSpace(Conteneur.NumeroDossier) || string.IsNullOrWhiteSpace(Conteneur.Destination))
            {
                await Shell.Current.DisplayAlert("Champs requis", "Le numéro de dossier et la destination sont obligatoires.", "OK");
                return;
            }

            // --- DÉBUT DE L'AJOUT : Gérer les dates désactivées ---
            // Si une checkbox est décochée, la propriété correspondante dans le binding ne met pas la date à null.
            // Il faut le faire manuellement avant d'envoyer les données.
            if (Conteneur.DateChargement.HasValue && !IsDateChecked("HasDateChargement")) Conteneur.DateChargement = null;
            if (Conteneur.DateDepart.HasValue && !IsDateChecked("HasDateDepart")) Conteneur.DateDepart = null;
            if (Conteneur.DateArriveeDestination.HasValue && !IsDateChecked("HasDateArrivee")) Conteneur.DateArriveeDestination = null;
            if (Conteneur.DateDedouanement.HasValue && !IsDateChecked("HasDateDedouanement")) Conteneur.DateDedouanement = null;
            // --- FIN DE L'AJOUT ---

            IsBusy = true;
            try
            {
                if (string.IsNullOrEmpty(ConteneurId))
                {
                    await _transitApi.CreateConteneurAsync(Conteneur);
                }
                else
                {
                    await _transitApi.UpdateConteneurAsync(Conteneur.Id, Conteneur);
                }
                await Shell.Current.GoToAsync(".."); 
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur de Sauvegarde", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        // --- DÉBUT DE L'AJOUT : Méthode helper ---
        private bool IsDateChecked(string checkBoxName)
        {
            if (Shell.Current.CurrentPage is AddEditConteneurPage page)
            {
                var checkBox = page.FindByName<CheckBox>(checkBoxName);
                return checkBox?.IsChecked ?? false;
            }
            return false;
        }
        // --- FIN DE L'AJOUT ---
    }
}