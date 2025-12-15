using TransitManager.Core.Entities;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Repositories
{
    public interface IBarcodeRepository : IGenericRepository<Barcode> { }
    
    public class BarcodeRepository : GenericRepository<Barcode>, IBarcodeRepository
    {
        public BarcodeRepository(TransitContext context) : base(context) { }
    }
}