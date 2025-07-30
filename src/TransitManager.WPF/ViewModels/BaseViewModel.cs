using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using TransitManager.Core.Enums;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TransitManager.WPF.ViewModels
{
    /// <summary>
    /// Classe de base pour tous les ViewModels
    /// </summary>
    public abstract class BaseViewModel : ObservableObject, IDisposable
    {
        private bool _isBusy;
        private string _title = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _disposed;

        /// <summary>
        /// Indique si le ViewModel est occupé
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsNotBusy));
                    // Mettre à jour l'état des commandes
                    RefreshCommands();
                }
            }
        }

        /// <summary>
        /// Indique si le ViewModel n'est pas occupé
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Titre de la vue
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Message de statut
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Dictionnaire pour stocker les commandes
        /// </summary>
        protected Dictionary<string, IRelayCommand> Commands { get; } = new();

        /// <summary>
        /// Constructeur
        /// </summary>
        protected BaseViewModel()
        {
            InitializeCommands();
        }

        /// <summary>
        /// Initialise les commandes du ViewModel
        /// </summary>
        protected virtual void InitializeCommands()
        {
            // À implémenter dans les classes dérivées
        }

        /// <summary>
        /// Rafraîchit l'état de toutes les commandes
        /// </summary>
        protected virtual void RefreshCommands()
        {
            foreach (var command in Commands.Values)
            {
                command.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Enregistre une commande
        /// </summary>
        protected void RegisterCommand(string name, IRelayCommand command)
        {
            Commands[name] = command;
        }

        /// <summary>
        /// Récupère une commande enregistrée
        /// </summary>
        protected IRelayCommand? GetCommand(string name)
        {
            return Commands.TryGetValue(name, out var command) ? command : null;
        }

        /// <summary>
        /// Exécute une action avec gestion du statut IsBusy
        /// </summary>
        protected async Task ExecuteBusyActionAsync(Func<Task> action, string? statusMessage = null)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                if (!string.IsNullOrEmpty(statusMessage))
                    StatusMessage = statusMessage;

                await action();
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// Méthode appelée lors de l'initialisation du ViewModel
        /// </summary>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Méthode appelée lors du chargement de la vue
        /// </summary>
        public virtual Task LoadAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Méthode appelée lors du déchargement de la vue
        /// </summary>
        public virtual Task UnloadAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Valide une propriété
        /// </summary>
        protected virtual bool ValidateProperty<T>(T value, [CallerMemberName] string? propertyName = null)
        {
            return true;
        }

        /// <summary>
        /// Déclenche un événement de changement de propriété pour plusieurs propriétés
        /// </summary>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// Libère les ressources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Libère les ressources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Libérer les ressources managées
                Commands.Clear();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Classe de base pour les ViewModels avec paramètre de navigation
    /// </summary>
    public abstract class BaseViewModel<T> : BaseViewModel where T : class
    {
        private T? _parameter;

        /// <summary>
        /// Paramètre de navigation
        /// </summary>
        public T? Parameter
        {
            get => _parameter;
            set => SetProperty(ref _parameter, value);
        }

        /// <summary>
        /// Initialise le ViewModel avec un paramètre
        /// </summary>
        public virtual Task InitializeAsync(T parameter)
        {
            Parameter = parameter;
            return InitializeAsync();
        }
    }
}