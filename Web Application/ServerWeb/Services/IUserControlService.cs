using Microsoft.AspNetCore.Identity;

namespace ServerWeb.Services
{
    public interface IUserControlService
    {
        /// <summary>
        /// Change the role of the user to newRole.
        /// </summary>
        /// <param name="identityUser"></param>
        /// <param name="newRole"></param>
        /// <returns></returns>
        Task<IdentityResult> ChangeUserRoleToAsync(IdentityUser identityUser, string newRole);

        /// <summary>
        /// Change the role of user with email to newRole.
        /// </summary>
        /// <param name="Email"></param>
        /// <param name="newRole"></param>
        /// <returns></returns>
        Task<IdentityResult> ChangeUserRoleToAsync(string email, string newRole);

        /// <summary>
        /// Returns a list of all users that are in the role.
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        Task<List<IdentityUser>> GetAllUsersInRole(string roleName);

        /// <summary>
        /// Returns a list of all users that are not in the role.
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        Task<List<IdentityUser>> GetAllUsersNotInRole(string roleName);

        /// <summary>
        /// Returns a list of all users in role roleName that match the pattern. The pattern must match
        /// with the user name of the user.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        Task<List<IdentityUser>> GetUsersInRoleMatch(string roleName, string pattern);

        /// <summary>
        /// Returns a list of all users not in role roleName that match the pattern. The pattern must match
        /// with the user name of the user.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="pattern"></param>
        Task<List<IdentityUser>> GetUsersNotInRoleMatch(string roleName, string pattern);

        /// <summary>
        /// Remove identityUser from their role. The role is set to User role.
        /// </summary>
        /// <param name="identityUser"></param>
        /// <returns></returns>
        Task<IdentityResult> RemoveUserFromRoleAsync(IdentityUser identityUser);

        /// <summary>
        /// Remove user with email from their role. The role is set to User role.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        Task<IdentityResult> RemoveUserFromRoleAsync(string email);
    }
}