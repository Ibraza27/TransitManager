using TransitManager.Core.Entities;

namespace TransitManager.Core.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Utilisateur user);
    }
}