using System;

namespace TransitManager.Core.Entities
{
    /// <summary>
    /// Classe de base pour toutes les entités du domaine
    /// </summary>
    public abstract class BaseEntity
    {
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