using TransitManager.Infrastructure.Repositories; // <-- AJOUTER CETTE LIGNE
using TransitManager.Core.Interfaces;
namespace TransitManager.Infrastructure.Data.Uow;

public interface IUnitOfWork : IDisposable
{
    IClientRepository Clients { get; }
    IColisRepository Colis { get; }
    IConteneurRepository Conteneurs { get; }
	IPaiementRepository Paiements { get; }
	IUserRepository Utilisateurs { get; }
    IBarcodeRepository Barcodes { get; }
    // Ajoutez ici les autres repos...

    Task<int> CommitAsync();
}