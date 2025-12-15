namespace TransitManager.Infrastructure.Data.Uow;

public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> CreateAsync();
}