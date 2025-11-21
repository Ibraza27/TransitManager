using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using System.Linq;
using System;
using TransitManager.Core.Enums;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.Core.Messages;

namespace TransitManager.WPF.ViewModels
{
    public class UserViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IUserService _userService;
		private readonly IDialogService _dialogService;

        public ObservableCollection<Utilisateur> Users { get; } = new();
        public ObservableCollection<string> RolesList { get; } = new();

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand<Utilisateur> EditUserCommand { get; }
        public IAsyncRelayCommand NewUserCommand { get; }
        public ICommand ClearFiltersCommand { get; }
		public IAsyncRelayCommand<Utilisateur> DeleteUserCommand { get; }

        private string _selectedRole = "Tous";
        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    LoadUsersAsync();
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    LoadUsersAsync();
                }
            }
        }

        private readonly IMessenger _messenger;

        public UserViewModel(INavigationService navigationService, IUserService userService, IDialogService dialogService, IMessenger messenger)
        {
            _userService = userService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _messenger = messenger; // Assurez-vous que IMessenger est injecté

            Title = "Gestion des Utilisateurs";

            RefreshCommand = new AsyncRelayCommand(LoadUsersAsync);
            EditUserCommand = new AsyncRelayCommand<Utilisateur>(EditUserAsync);
            NewUserCommand = new AsyncRelayCommand(NewUserAsync);
            DeleteUserCommand = new AsyncRelayCommand<Utilisateur>(DeleteUserAsync);
            ClearFiltersCommand = new RelayCommand(() => {
                SearchText = string.Empty;
                SelectedRole = "Tous";
            });

            RolesList.Add("Tous");
            foreach (var role in Enum.GetNames(typeof(RoleUtilisateur)))
            {
                RolesList.Add(role);
            }

            // === AJOUTER CETTE LIGNE DANS LE CONSTRUCTEUR ===
            // S'abonne à tous les messages pour lesquels cette classe a une interface IRecipient
            _messenger.RegisterAll(this);
        }

        // === AJOUTER CETTE MÉTHODE ===
        /// <summary>
        /// Reçoit le message UserUpdatedMessage et déclenche le rechargement de la liste.
        /// </summary>
        public async void Receive(UserUpdatedMessage message)
        {
            // Nous sommes sur le thread de l'UI grâce au Messenger, on peut appeler directement.
            await LoadUsersAsync();
        }

        private async Task DeleteUserAsync(Utilisateur? user)
        {
            if (user == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync("Confirmation", $"Voulez-vous vraiment désactiver l'utilisateur {user.NomUtilisateur} ?");
            if (confirm)
            {
                await _userService.DeleteAsync(user.Id);
                // On n'a plus besoin de rafraîchir manuellement ici, on envoie un message.
                // Le Receive s'occupera du rafraîchissement.
                _messenger.Send(new UserUpdatedMessage());
            }
        }		

        public override Task InitializeAsync()
        {
            return LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var users = await _userService.GetAllAsync();

                // Appliquer les filtres
                if (SelectedRole != "Tous" && Enum.TryParse<RoleUtilisateur>(SelectedRole, out var selectedRole))
                {
                    users = users.Where(u => u.Role == selectedRole);
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.ToLower();
                    users = users.Where(u =>
                        u.NomUtilisateur.ToLower().Contains(search) ||
                        u.NomComplet.ToLower().Contains(search) ||
                        (u.Email?.ToLower().Contains(search) ?? false) ||
                        (u.Telephone?.Contains(search) ?? false));
                }

                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            });
        }

		private async Task EditUserAsync(Utilisateur? user)
		{
			if (user == null) return;
			// Naviguer vers la vue de détail
			_navigationService.NavigateTo("UserDetail", user.Id);
			await Task.CompletedTask; // Garder la méthode async pour la signature
		}

		private async Task NewUserAsync()
		{
			_navigationService.NavigateTo("UserDetail", "new");
			await Task.CompletedTask;
		}
    }
}
