using Microsoft.AspNetCore.Identity;

namespace ServerWeb.Services
{
    public interface IRegistrationService
    {
        Task<(IdentityUser?, IdentityResult)> Register(string email, string password);
    }
}