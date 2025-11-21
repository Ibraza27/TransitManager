// src/TransitManager.API/Authorization/HybridRequirement.cs

using Microsoft.AspNetCore.Authorization;

namespace TransitManager.API.Authorization
{
    /// <summary>
    /// Une exigence simple qui déclenche notre handler d'autorisation hybride.
    /// </summary>
    public class HybridRequirement : IAuthorizationRequirement
    {
    }
}