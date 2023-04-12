using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ServerWeb.Services
{
    public interface IIndexRedirection
    {
        /// <summary>
        /// Based on the user role, this method will redirect the user to the correct index.
        /// </summary>
        /// <param name="claimsPrincipal"></param>
        /// <returns></returns>
        Task<IActionResult> RedirectToIndexAsync(ClaimsPrincipal claimsPrincipal);

        /// <summary>
        /// Based on the user role, this method will redirect the user to the correct index.
        /// </summary>
        /// <param name="claimsPrincipal"></param>
        /// <returns></returns>
        Task<IActionResult> RedirectToIndexAsync(IdentityUser? user);
    }
}