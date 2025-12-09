// src/TransitManager.Core/Interfaces/IUserService.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;

namespace TransitManager.Core.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<Utilisateur>> GetAllAsync();
        Task<Utilisateur?> GetByIdAsync(Guid id);
        Task<Utilisateur> CreateAsync(Utilisateur user, string password);
        Task<Utilisateur> UpdateAsync(Utilisateur user);
        Task<bool> DeleteAsync(Guid id);
        Task<string?> ResetPasswordAsync(Guid id);
        Task<IEnumerable<Client>> GetUnlinkedClientsAsync();
		Task<bool> UnlockAccountAsync(Guid id);
		Task<bool> ChangePasswordManualAsync(Guid id, string newPassword);
		Task<int> DeleteUnconfirmedAccountsAsync(int hoursOld);
        Task<bool> ToggleEmailConfirmationAsync(Guid userId, bool isConfirmed);
        Task<bool> ResendConfirmationEmailAdminAsync(Guid userId);
    }
}