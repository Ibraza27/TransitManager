using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

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
            
            // Validation simple
            if (string.IsNullOrWhiteSpace(Conteneur.NumeroDossier) || string.IsNullOrWhiteSpace(Conteneur.Destination))
            {
                await Shell.Current.DisplayAlert("Champs requis", "Le numéro de dossier et la destination sont obligatoires.", "OK");
                return;
            }

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
                await Shell.Current.GoToAsync(".."); // Revenir à la page précédente
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
    }
}