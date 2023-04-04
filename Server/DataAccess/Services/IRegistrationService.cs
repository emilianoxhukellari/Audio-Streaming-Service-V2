using Microsoft.AspNetCore.Identity;

namespace DataAccess.Services
{
    public interface IRegistrationService
    {
        Task<(IdentityUser?, IdentityResult)> Register(string email, string password);
    }
}