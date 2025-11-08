using Microsoft.AspNetCore.Authorization;

namespace TransitManager.API.Authorization
{
    public class InternalOrJwtRequirement : IAuthorizationRequirement
    {
    }
}