using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TransitManager.Core.Entities
{
    public abstract class BaseEntity : INotifyPropertyChanged
    {
        // Implémentation de INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        /// <summary>
        /// Identifiant unique de l'entité
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Date de création de l'enregistrement
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime? DateModification { get; set; }

        /// <summary>
        /// Utilisateur ayant créé l'enregistrement
        /// </summary>
        public string? CreePar { get; set; }

        /// <summary>
        /// Utilisateur ayant modifié l'enregistrement
        /// </summary>
        public string? ModifiePar { get; set; }

        /// <summary>
        /// Indique si l'enregistrement est actif
        /// </summary>
        public bool Actif { get; set; } = true;

        /// <summary>
        /// Version pour la gestion de la concurrence optimiste
        /// </summary>
        public byte[]? RowVersion { get; set; }

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            DateCreation = DateTime.UtcNow;
        }
    }
}