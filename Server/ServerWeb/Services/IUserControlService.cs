using Microsoft.AspNetCore.Identity;

namespace ServerWeb.Services
{
    public interface IUserControlService
    {
        Task<IdentityResult> ChangeUserRoleToAsync(IdentityUser identityUser, string newRole);
        Task<IdentityResult> ChangeUserRoleToAsync(string Email, string newRole);
        Task<List<IdentityUser>> GetAllUsersInRole(string roleName);
        Task<List<IdentityUser>> GetAllUsersNotInRole(string roleName);
        Task<List<IdentityUser>> GetUsersInRoleMatch(string roleName, string pattern);
        Task<List<IdentityUser>> GetUsersNotInRoleMatch(string roleName, string pattern);
        Task<IdentityResult> RemoveUserFromRoleAsync(IdentityUser identityUser);
        Task<IdentityResult> RemoveUserFromRoleAsync(string Email);
    }
}