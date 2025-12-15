using Microsoft.EntityFrameworkCore;

namespace TransitManager.Infrastructure.Data.Uow;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbContextFactory<TransitContext> _dbContextFactory;

    public UnitOfWorkFactory(IDbContextFactory<TransitContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IUnitOfWork> CreateAsync()
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        return new UnitOfWork(context);
    }
}