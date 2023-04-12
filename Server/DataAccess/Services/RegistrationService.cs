using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;

namespace DataAccess.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly StreamingDbContext _streamingDbContext;
        public RegistrationService(UserManager<IdentityUser> userManager, StreamingDbContext streamingDbContext)
        {
            _userManager = userManager;
            _streamingDbContext = streamingDbContext;
        }

        /// <inheritdoc/>
        public async Task<(IdentityUser?, IdentityResult)> Register(string email, string password)
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
