using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ServerWeb.Services
{
    public interface IIndexRedirection
    {
        Task<IActionResult> RedirectToIndexAsync(ClaimsPrincipal claimsPrincipal);
        Task<IActionResult> RedirectToIndexAsync(IdentityUser? user);
    }
}