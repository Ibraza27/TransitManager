using Microsoft.EntityFrameworkCore;
using TransitManager.Core.Entities;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Repositories
{
    public interface IUserRepository : IGenericRepository<Utilisateur>
    {
        Task<Utilisateur?> GetByUsernameAsync(string username);
		Task<Utilisateur?> GetByClientIdAsync(Guid clientId);
    }

    public class UserRepository : GenericRepository<Utilisateur>, IUserRepository
    {
        public UserRepository(TransitContext context) : base(context) { }

        public async Task<Utilisateur?> GetByUsernameAsync(string username)
        {
            return await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.NomUtilisateur == username && u.Actif);
        }
		
		public async Task<Utilisateur?> GetByClientIdAsync(Guid clientId)
		{
			return await _context.Utilisateurs
				.FirstOrDefaultAsync(u => u.ClientId == clientId && u.Actif);
		}
		
    }
}