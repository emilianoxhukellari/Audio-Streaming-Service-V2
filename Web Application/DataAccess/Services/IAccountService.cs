using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DataAccess.Services
{
    public interface IAccountService
    {
        /// <summary>
        /// Register a user with email and password. Role "User" is assigned to newly registered users.
        /// The user is added in two databases - User and Streaming.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<(IdentityUser?, IdentityResult)> Register(string email, string password);

        /// <summary>
        /// Delete a user from both databases.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> DeleteAccountAsync(ClaimsPrincipal user);
    }
}