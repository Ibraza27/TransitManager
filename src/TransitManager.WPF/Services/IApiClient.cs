// src/TransitManager.WPF/Services/IApiClient.cs

using System;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;

namespace TransitManager.WPF.Services
{
    public class PortalAccessResult
    {
        public string Message { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string TemporaryPassword { get; set; }
    }

    public interface IApiClient
    {
        Task<PortalAccessResult> CreateOrResetPortalAccess(Guid clientId);
    }
}