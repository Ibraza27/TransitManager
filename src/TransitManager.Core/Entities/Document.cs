using System;
using System.ComponentModel.DataAnnotations;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    /// <summary>
    /// Représente un document dans le système
    /// </summary>
    public class Document : BaseEntity
    {
        /// <summary>
        /// Nom du document
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty;

        /// <summary>
        /// Type de document
        /// </summary>
        public TypeDocument Type { get; set; }

        /// <summary>
        /// Description du document
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Chemin du fichier
        /// </summary>
        [Required]
        [StringLength(500)]
        public string CheminFichier { get; set; } = string.Empty;

        /// <summary>
        /// Nom du fichier original
        /// </summary>
        [Required]
        [StringLength(255)]
        public string NomFichierOriginal { get; set; } = string.Empty;

        /// <summary>
        /// Extension du fichier
        /// </summary>
        [Required]
        [StringLength(10)]
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// Taille du fichier en octets
        /// </summary>
        public long TailleFichier { get; set; }

        /// <summary>
        /// Type MIME du fichier
        /// </summary>
        [StringLength(100)]
        public string? TypeMime { get; set; }

        /// <summary>
        /// Hash MD5 du fichier pour vérification d'intégrité
        /// </summary>
        [StringLength(32)]
        public string? HashMd5 { get; set; }

        /// <summary>
        /// ID du client associé (si applicable)
        /// </summary>
        public Guid? ClientId { get; set; }

        /// <summary>
        /// ID du colis associé (si applicable)
        /// </summary>
        public Guid? ColisId { get; set; }

        /// <summary>
        /// ID du conteneur associé (si applicable)
        /// </summary>
        public Guid? ConteneurId { get; set; }

        /// <summary>
        /// ID du paiement associé (si applicable)
        /// </summary>
        public Guid? PaiementId { get; set; }

        /// <summary>
        /// Date d'expiration du document
        /// </summary>
        public DateTime? DateExpiration { get; set; }

        /// <summary>
        /// Indique si le document est confidentiel
        /// </summary>
        public bool EstConfidentiel { get; set; }

        /// <summary>
        /// Indique si le document est archivé
        /// </summary>
        public bool EstArchive { get; set; }

        /// <summary>
        /// Tags/mots-clés (séparés par des virgules)
        /// </summary>
        [StringLength(500)]
        public string? Tags { get; set; }

        /// <summary>
        /// Numéro de version du document
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// ID du document parent (pour la gestion des versions)
        /// </summary>
        public Guid? DocumentParentId { get; set; }

        /// <summary>
        /// Nombre de téléchargements
        /// </summary>
        public int NombreTelechargements { get; set; }

        /// <summary>
        /// Date du dernier accès
        /// </summary>
        public DateTime? DateDernierAcces { get; set; }

        // Navigation properties
        /// <summary>
        /// Client associé
        /// </summary>
        public virtual Client? Client { get; set; }

        /// <summary>
        /// Colis associé
        /// </summary>
        public virtual Colis? Colis { get; set; }

        /// <summary>
        /// Conteneur associé
        /// </summary>
        public virtual Conteneur? Conteneur { get; set; }

        /// <summary>
        /// Paiement associé
        /// </summary>
        public virtual Paiement? Paiement { get; set; }

        /// <summary>
        /// Document parent (pour les versions)
        /// </summary>
        public virtual Document? DocumentParent { get; set; }

        /// <summary>
        /// Constructeur
        /// </summary>
        public Document()
        {
            DateCreation = DateTime.UtcNow;
        }

        /// <summary>
        /// Nom complet avec extension
        /// </summary>
        public string NomComplet => $"{Nom}{Extension}";

        /// <summary>
        /// Taille formatée (KB, MB, GB)
        /// </summary>
        public string TailleFormatee
        {
            get
            {
                if (TailleFichier < 1024)
                    return $"{TailleFichier} B";
                else if (TailleFichier < 1024 * 1024)
                    return $"{TailleFichier / 1024.0:F2} KB";
                else if (TailleFichier < 1024 * 1024 * 1024)
                    return $"{TailleFichier / (1024.0 * 1024.0):F2} MB";
                else
                    return $"{TailleFichier / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
        }

        /// <summary>
        /// Indique si le document est expiré
        /// </summary>
        public bool EstExpire => DateExpiration.HasValue && DateExpiration.Value < DateTime.UtcNow;

        /// <summary>
        /// Obtient l'icône associée au type de fichier
        /// </summary>
        public string GetFileIcon()
        {
            return Extension.ToLower() switch
            {
                ".pdf" => "FilePdf",
                ".doc" or ".docx" => "FileWord",
                ".xls" or ".xlsx" => "FileExcel",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "FileImage",
                ".zip" or ".rar" or ".7z" => "FolderZip",
                ".txt" => "FileDocument",
                _ => "File"
            };
        }
    }
}