using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{

	[QueryProperty(nameof(ClientId), "clientId")]
	public partial class AddEditClientViewModel : ObservableObject
	{
		private readonly ITransitApi _transitApi;
		
		[ObservableProperty]
		private Client? _client;

		[ObservableProperty]
		private string? _clientId;
		
		[ObservableProperty]
		private string _pageTitle = string.Empty;

		public AddEditClientViewModel(ITransitApi transitApi)
		{
			_transitApi = transitApi;
		}

		async partial void OnClientIdChanged(string? value)
		{
			if (string.IsNullOrEmpty(value))
			{
				PageTitle = "Nouveau Client";
				Client = new Client();
			}
			else
			{
				PageTitle = "Modifier le Client";
				var id = Guid.Parse(value);
				Client = await _transitApi.GetClientByIdAsync(id);
			}
		}

		[RelayCommand]
		async Task SaveAsync()
		{
			if (Client == null) return;

			try
			{
				if (string.IsNullOrEmpty(ClientId))
				{
					await _transitApi.CreateClientAsync(Client);
				}
				else
				{
					await _transitApi.UpdateClientAsync(Client.Id, Client);
				}
				await Shell.Current.GoToAsync(".."); // Revenir à la page précédente
			}
			catch (System.Exception ex)
			{
				await Shell.Current.DisplayAlert("Erreur", $"Sauvegarde échouée : {ex.Message}", "OK");
			}
		}
	}
}