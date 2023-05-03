using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ServerWeb.Services
{
    public class UserControlService : IUserControlService
    {
        private readonly UserManager<IdentityUser> _userManager;
        public UserControlService(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        /// <inheritdoc/>
        public async Task<List<IdentityUser>> GetAllUsersNotInRole(string roleName)
        {
            var roleUsersIds = (await _userManager.GetUsersInRoleAsync(roleName)).Select(x => x.Id).ToArray();
            return await _userManager.Users.Where(x => !roleUsersIds.Contains(x.Id)).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<IdentityUser>> GetAllUsersInRole(string roleName)
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return users.ToList();
        }

        /// <inheritdoc/>
        public async Task<List<IdentityUser>> GetUsersNotInRoleMatch(string roleName, string pattern)
        {
            var allUsers = await GetAllUsersNotInRole(roleName);
            var users = allUsers.Where(u => u.UserName!.Contains(pattern));
            return users.ToList();
        }

        /// <inheritdoc/>
        public async Task<List<IdentityUser>> GetUsersInRoleMatch(string roleName, string pattern)
        {
            var allUsers = await GetAllUsersInRole(roleName);
            var users = allUsers.Where(u => u.UserName!.Contains(pattern));
            return users.ToList();
        }

        private async Task<IdentityResult> ChangeUserRoleAsync(IdentityUser identityUser, string oldRole, string newRole)
        {
            var removeFromOldRoleResult = await _userManager.RemoveFromRoleAsync(identityUser, oldRole);
            if (!removeFromOldRoleResult.Succeeded)
            {
                return removeFromOldRoleResult;
            }

            var addToNewRoleResult = await _userManager.AddToRoleAsync(identityUser, newRole);
            return addToNewRoleResult;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> ChangeUserRoleToAsync(IdentityUser identityUser, string newRole)
        {
            var roles = await _userManager.GetRolesAsync(identityUser);
            var oldRole = roles.ElementAt(0);
            if (oldRole != newRole)
            {
                return await ChangeUserRoleAsync(identityUser, oldRole, newRole);
            }
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> ChangeUserRoleToAsync(string Email, string newRole)
        {
            var identityUser = await _userManager.FindByEmailAsync(Email);
            var roles = await _userManager.GetRolesAsync(identityUser);
            var oldRole = roles.ElementAt(0);
            if (oldRole != newRole)
            {
                return await ChangeUserRoleAsync(identityUser, oldRole, newRole);
            }
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> RemoveUserFromRoleAsync(IdentityUser identityUser)
        {
            var roles = await _userManager.GetRolesAsync(identityUser);
            var oldRole = roles.ElementAt(0);
            return await ChangeUserRoleAsync(identityUser, oldRole, "User");
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> RemoveUserFromRoleAsync(string Email)
        {
            var identityUser = await _userManager.FindByEmailAsync(Email);
            var roles = await _userManager.GetRolesAsync(identityUser);
            var oldRole = roles.ElementAt(0);
            return await ChangeUserRoleAsync(identityUser, oldRole, "User");
        }
    }
}
