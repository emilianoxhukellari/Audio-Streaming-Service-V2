using Microsoft.AspNetCore.Identity;

namespace DataAccess.Services
{
    public interface IRegistrationService
    {
        /// <summary>
        /// Register a user with email and password. Role "User" is assigned to newly registered users.
        /// The user is added in two databases - User and Streaming.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<(IdentityUser?, IdentityResult)> Register(string email, string password);
    }
}