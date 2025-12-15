using TransitManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using TransitManager.Core.Interfaces;

namespace TransitManager.Infrastructure.Data.Uow;

public class UnitOfWork : IUnitOfWork
{
    private readonly TransitContext _context;

    public UnitOfWork(TransitContext context)
    {
        _context = context;
        Clients = new ClientRepository(_context);
        Colis = new ColisRepository(_context);
        Conteneurs = new ConteneurRepository(_context);
		Paiements = new PaiementRepository(_context);
		Utilisateurs = new UserRepository(_context);
        Barcodes = new BarcodeRepository(_context);
        // Initialise les autres repos ici
    }

    public IClientRepository Clients { get; }
    public IColisRepository Colis { get; }
    public IConteneurRepository Conteneurs { get; }
	public IPaiementRepository Paiements { get; }
	public IUserRepository Utilisateurs { get; }
    public IBarcodeRepository Barcodes { get; }
    // Ajoutez ici les autres repos...

    public async Task<int> CommitAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}