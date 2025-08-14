using System;
using System.ComponentModel.DataAnnotations;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
	public class Vehicule : BaseEntity
	{
		private Guid _clientId;
		private string _immatriculation = string.Empty;
		private string _marque = string.Empty;
		private string _modele = string.Empty;
		private int _annee;
		private int _kilometrage;
		private string _destinationFinale = string.Empty;
		private decimal _valeurDeclaree;
		private string? _destinataire;
		private string? _telephoneDestinataire;
		private TypeVehicule _type;
		private string? _commentaires;
		private decimal _prixTotal;
		private decimal _sommePayee;

		public Guid ClientId { get => _clientId; set => SetProperty(ref _clientId, value); }

		[Required]
		[StringLength(50)]
		public string Immatriculation { get => _immatriculation; set => SetProperty(ref _immatriculation, value); }

		[Required]
		[StringLength(100)]
		public string Marque { get => _marque; set => SetProperty(ref _marque, value); }

		[Required]
		[StringLength(100)]
		public string Modele { get => _modele; set => SetProperty(ref _modele, value); }

		public int Annee { get => _annee; set => SetProperty(ref _annee, value); }
		public int Kilometrage { get => _kilometrage; set => SetProperty(ref _kilometrage, value); }

		[Required]
		[StringLength(200)]
		public string DestinationFinale { get => _destinationFinale; set => SetProperty(ref _destinationFinale, value); }

		public decimal ValeurDeclaree { get => _valeurDeclaree; set => SetProperty(ref _valeurDeclaree, value); }

		[StringLength(200)]
		public string? Destinataire { get => _destinataire; set => SetProperty(ref _destinataire, value); }
		
		[StringLength(20)]
		public string? TelephoneDestinataire { get => _telephoneDestinataire; set => SetProperty(ref _telephoneDestinataire, value); }

		public TypeVehicule Type { get => _type; set => SetProperty(ref _type, value); }
		public string? Commentaires { get => _commentaires; set => SetProperty(ref _commentaires, value); }
		
		public decimal PrixTotal { get => _prixTotal; set { if (SetProperty(ref _prixTotal, value)) OnPropertyChanged(nameof(RestantAPayer)); } }
		
		public decimal SommePayee { get => _sommePayee; set { if (SetProperty(ref _sommePayee, value)) OnPropertyChanged(nameof(RestantAPayer)); } }

		public decimal RestantAPayer => PrixTotal - SommePayee;

		// Relation de navigation vers le client
		public virtual Client? Client { get; set; }
    }
}