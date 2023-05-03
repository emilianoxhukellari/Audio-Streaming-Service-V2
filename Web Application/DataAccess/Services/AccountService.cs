using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace DataAccess.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly StreamingDbContext _streamingDbContext;
        public AccountService(UserManager<IdentityUser> userManager, StreamingDbContext streamingDbContext)
        {
            _userManager = userManager;
            _streamingDbContext = streamingDbContext;
        }

        public async Task<bool> DeleteAccountAsync(ClaimsPrincipal user)
        {
            var identityUser = await _userManager.GetUserAsync(user);
            if (identityUser == null)
            {
                return false;
            }

            await _userManager.DeleteAsync(identityUser);

            var streamingUser = await _streamingDbContext.StreamingUsers.FindAsync(identityUser.Id);
            if (streamingUser == null)
            {
                return false;
            }

            _streamingDbContext.StreamingUsers.Remove(streamingUser);
            await _streamingDbContext.SaveChangesAsync();

            return true;
        }

        /// <inheritdoc/>
        public async Task<(IdentityUser?, IdentityResult)> RegisterAsync(string email, string password)
        {
            var user = new IdentityUser
            {
                UserName = email,
                Email = email
            };

            var registerResult = await _userManager.CreateAsync(user, password);

            if (registerResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User"); 

                var streamingUser = new StreamingUser
                {
                    UserId = user.Id,
                    UserName = email
                };

                await _streamingDbContext.StreamingUsers.AddAsync(streamingUser);
                await _streamingDbContext.SaveChangesAsync();
                return (user, registerResult);
            }

            return (null, registerResult);
        }
    }
}
