// src/TransitManager.WPF/ViewModels/UserDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.Core.Messages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using TransitManager.WPF.ViewModels;
using System.ComponentModel;
using System.Windows;
using TransitManager.Core.Exceptions;
using System.Collections.Generic;

namespace TransitManager.WPF.ViewModels
{
    public class UserDetailViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IMessenger _messenger;
        private Utilisateur? _user;

        public Utilisateur? User
        {
            get => _user;
            set
            {
                if (_user != null) _user.PropertyChanged -= OnUserPropertyChanged;

                if (SetProperty(ref _user, value))
                {
                    OnPropertyChanged(nameof(IsClientRoleSelected));
                    OnPropertyChanged(nameof(CanEditDetails));
                }

                if (_user != null) _user.PropertyChanged += OnUserPropertyChanged;
            }
        }

        public ObservableCollection<RoleUtilisateur> RolesList { get; } = new();
        private Client? _selectedClient;
		public Client? SelectedClient
		{
			get => _selectedClient;
			set
			{
				if (SetProperty(ref _selectedClient, value))
				{
					// 1. Lier l'ID
					if (User != null)
					{
						User.ClientId = value?.Id;

						// 2. Remplissage automatique si un client est sélectionné
						if (value != null)
						{
							// On copie les infos du client vers l'utilisateur
							User.Nom = value.Nom;
							User.Prenom = value.Prenom;
							User.Email = value.Email ?? string.Empty;
							User.Telephone = value.TelephonePrincipal;
							
							// Optionnel : Générer un nom d'utilisateur par défaut (ex: p.nom)
							if (string.IsNullOrEmpty(User.NomUtilisateur))
							{
								string baseUser = $"{value.Prenom.FirstOrDefault()}{value.Nom}".ToLower().Replace(" ", "");
								User.NomUtilisateur = baseUser;
							}
						}
					}
				}
			}
		}

		public Action? CloseAction { get; set; }
		private bool _isModal = false;

		// Nouvelle méthode pour le mode modal
		public void SetModalMode()
		{
			_isModal = true;
		}		

        public bool IsClientRoleSelected => User?.Role == RoleUtilisateur.Client;
        public bool CanEditDetails => !IsClientRoleSelected || SelectedClient == null;
        private string? _temporaryPassword;
		public string? TemporaryPassword 
		{ 
			get => _temporaryPassword; 
			set 
			{
				if (SetProperty(ref _temporaryPassword, value))
				{
					// === CORRECTION : Notifier la commande ===
					CopyTemporaryPasswordCommand.NotifyCanExecuteChanged();
				}
			} 
		}

        private string _newPassword = string.Empty;
		public string NewPassword 
		{ 
			get => _newPassword; 
			set 
			{
				if (SetProperty(ref _newPassword, value))
				{
					// === VÉRIFIEZ QUE CETTE LIGNE EST BIEN LÀ ===
					SaveCommand.NotifyCanExecuteChanged();
				}
			}
		}
		
		private List<Client> _allUnlinkedClients = new(); // Stocker la liste complète
		public ObservableCollection<Client> UnlinkedClients { get; } = new();

		private bool _isClientDropDownOpen;
        public bool IsClientDropDownOpen
        {
            get => _isClientDropDownOpen;
            set => SetProperty(ref _isClientDropDownOpen, value);
        }
		
        private string _clientSearchText = string.Empty;
        public string ClientSearchText
        {
            get => _clientSearchText;
            set
            {
                if (SetProperty(ref _clientSearchText, value))
                {
                    // Déclenche le filtre à chaque frappe
                    FilterClients(value);
                }
            }
        }
		
		
        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand ResetPasswordCommand { get; }
        public IRelayCommand CopyTemporaryPasswordCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public UserDetailViewModel(IUserService userService, INavigationService navigationService, IDialogService dialogService, IMessenger messenger)
        {
            _userService = userService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _messenger = messenger;
            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync, CanResetPassword);
            CopyTemporaryPasswordCommand = new RelayCommand(CopyTemporaryPassword, () => !string.IsNullOrEmpty(TemporaryPassword));
            CancelCommand = new RelayCommand(Cancel);

            foreach (RoleUtilisateur role in Enum.GetValues(typeof(RoleUtilisateur)))
            {
                RolesList.Add(role);
            }
        }


        public override async Task InitializeAsync()
        {
            // On charge tous les clients disponibles dans la liste tampon
            _allUnlinkedClients = (await _userService.GetUnlinkedClientsAsync()).ToList();
            
            // On initialise la liste affichée
            FilterClients(string.Empty);
        }

        private void FilterClients(string searchText)
        {
            var selected = SelectedClient; // Garder la sélection actuelle si possible

            UnlinkedClients.Clear();
            
            IEnumerable<Client> filtered;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filtered = _allUnlinkedClients;
            }
            else
            {
                filtered = _allUnlinkedClients.Where(c => 
                    c.NomComplet.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.CodeClient.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                );
            }

            foreach (var client in filtered)
            {
                UnlinkedClients.Add(client);
            }

            // Si on a des résultats et qu'on est en train de taper, on ouvre la liste
            if (UnlinkedClients.Any() && !string.IsNullOrEmpty(searchText))
            {
                IsClientDropDownOpen = true;
            }
            
            // Restaurer la sélection si elle est toujours dans la liste filtrée
            if (selected != null && UnlinkedClients.Contains(selected))
            {
                SelectedClient = selected;
            }
        }


		public async Task InitializeAsync(string newMarker)
		{
			if (newMarker == "new")
			{
				Title = "Nouvel Utilisateur";
				
				// IMPORTANT : On crée une NOUVELLE instance.
				// Par défaut, Guid est Guid.Empty (0000...), ce qui est parfait pour notre test SaveAsync.
				User = new Utilisateur(); 
				
				// On s'assure que l'ID est bien vide (par précaution, même si le constructeur le fait souvent)
				User.Id = Guid.Empty; 

				await InitializeAsync(); // Charge les listes déroulantes (rôles, clients...)
			}
		}

        public async Task InitializeAsync(Guid userId)
        {
            await ExecuteBusyActionAsync(async () =>
            {
                Title = "Modifier l'Utilisateur";
                User = await _userService.GetByIdAsync(userId);
                
                // Charger les clients disponibles
                await InitializeAsync();

                // Si l'utilisateur a déjà un client lié, on s'assure qu'il est dans la liste
                if (User?.Client != null && !_allUnlinkedClients.Any(c => c.Id == User.ClientId))
                {
                    _allUnlinkedClients.Insert(0, User.Client);
                    // Rafraîchir la liste filtrée pour inclure ce client
                    FilterClients(string.Empty);
                }

                // Sélectionner le client actuel
                if (User?.Client != null)
                {
                    SelectedClient = UnlinkedClients.FirstOrDefault(c => c.Id == User.ClientId);
                    // Mettre le nom dans le champ de recherche pour l'affichage
                    _clientSearchText = SelectedClient?.NomComplet ?? string.Empty;
                    OnPropertyChanged(nameof(ClientSearchText));
                }

                OnPropertyChanged(nameof(IsClientRoleSelected));
                OnPropertyChanged(nameof(CanEditDetails));
                ResetPasswordCommand.NotifyCanExecuteChanged();
            });
        }

        private void OnUserPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();
            if (e.PropertyName == nameof(User.Role))
            {
                // Notifie la vue que l'état (Actif/Inactif) du champ client doit changer
                OnPropertyChanged(nameof(IsClientRoleSelected));
                
                // Si le rôle n'est plus Client, on vide la sélection pour éviter les incohérences
                if (!IsClientRoleSelected && User != null)
                {
                    User.ClientId = null;
                    SelectedClient = null;
                }
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(TemporaryPassword))
            {
                CopyTemporaryPasswordCommand.NotifyCanExecuteChanged();
            }
        }

		private bool CanSave()
		{
			if (User == null) return false;

			// Champs de base toujours requis
			bool baseFieldsValid = !string.IsNullOrWhiteSpace(User.NomUtilisateur) &&
								   !string.IsNullOrWhiteSpace(User.Nom) &&
								   !string.IsNullOrWhiteSpace(User.Prenom) &&
								   !string.IsNullOrWhiteSpace(User.Email);

			// Si c'est un NOUVEL utilisateur, le mot de passe est aussi requis
			if (User.Id == Guid.Empty)
			{
				return baseFieldsValid && !string.IsNullOrWhiteSpace(NewPassword);
			}

			// Pour un utilisateur existant, les champs de base suffisent
			return baseFieldsValid;
		}

		private async Task SaveAsync()
		{
			if (!CanSave() || User == null) return;
			await ExecuteBusyActionAsync(async () =>
			{
				try
				{
					// --- CAS CRÉATION (inchangé) ---
					if (User.Id == Guid.Empty)
					{
						if (string.IsNullOrWhiteSpace(NewPassword))
						{
							await _dialogService.ShowWarningAsync("Mot de passe requis", "Pour un nouvel utilisateur, le mot de passe est obligatoire.");
							return;
						}
						await _userService.CreateAsync(User, NewPassword);
					}
					// --- CAS MODIFICATION (Corrigé) ---
					else
					{
						// SI un nouveau mot de passe a été saisi manuellement
						if (!string.IsNullOrWhiteSpace(NewPassword))
						{
							// On hache le mot de passe et on met à jour l'entité
							// Note : BCrypt est déjà inclus dans le projet via les packages
							User.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
							
							// Optionnel : On peut forcer l'utilisateur à le changer à la prochaine connexion
							// User.DoitChangerMotDePasse = true; 
						}

						// On sauvegarde l'utilisateur (avec le nouveau hash si modifié)
						await _userService.UpdateAsync(User);
					}

					await _dialogService.ShowInformationAsync("Succès", "Utilisateur enregistré.");
					_messenger.Send(new UserUpdatedMessage());
					
					// Gestion de la fermeture
					if (_isModal)
					{
						CloseAction?.Invoke();
					}
					else
					{
						_navigationService.GoBack();
					}
				}
				catch (ConcurrencyException cex)
				{
					var refresh = await _dialogService.ShowConfirmationAsync(
						"Conflit de Données",
						$"{cex.Message}\n\nVoulez-vous rafraîchir les données pour voir les dernières modifications ? (Vos changements actuels seront perdus)");
					if (refresh && User != null)
					{
						await InitializeAsync(User.Id); // Recharge les données
					}
				}
				catch (Exception ex)
				{
					await _dialogService.ShowErrorAsync("Erreur", ex.Message);
				}
			});
		}


        private bool CanResetPassword() => User != null && User.Id != Guid.Empty;

		private async Task ResetPasswordAsync()
		{
			if (!CanResetPassword() || User == null) return;
			var tempPassword = await _userService.ResetPasswordAsync(User.Id);

			// On stocke le mot de passe en clair TEMPORAIREMENT
			TemporaryPassword = tempPassword; 

			// On affiche un message différent
			await _dialogService.ShowInformationAsync("Mot de passe réinitialisé", "Un nouveau mot de passe a été généré. Cliquez sur le bouton 'Copier' pour le voir.");
		}

		private void CopyTemporaryPassword()
		{
			if (!string.IsNullOrEmpty(TemporaryPassword))
			{
				System.Windows.Clipboard.SetText(TemporaryPassword);
				// On affiche le mot de passe dans le dialogue
				_dialogService.ShowInformationAsync("Mot de passe copié", $"Le mot de passe suivant a été copié dans le presse-papiers :\n\n{TemporaryPassword}");
			}
		}
		
		private void Cancel()
		{
			// === CORRECTION ICI ===
			if (_isModal)
			{
				// Si on est en mode modal, on appelle l'action pour fermer la fenêtre
				CloseAction?.Invoke();
			}
			else
			{
				// Sinon, on utilise la navigation standard
				_navigationService.GoBack();
			}
		}
	}
}