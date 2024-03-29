﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ServerWeb.Services
{
    public class IndexRedirectionService : IIndexRedirectionService
    {
        private readonly UserManager<IdentityUser> _userManager;
        public IndexRedirectionService(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        private async Task<IActionResult> ToIndexAsync(IdentityUser? user)
        {
            if (user != null)
            {
                if (await _userManager.IsInRoleAsync(user, "User"))
                {
                    return new RedirectToPageResult("/User/Home");
                }

                else if (await _userManager.IsInRoleAsync(user, "SU"))
                {
                    return new RedirectToPageResult("/SU/Home");
                }

                else if (await _userManager.IsInRoleAsync(user, "Administrator"))
                {
                    return new RedirectToPageResult("/Administrator/Home");
                }

                else if (await _userManager.IsInRoleAsync(user, "EngineAdministrator"))
                {
                    return new RedirectToPageResult("/EngineAdministrator/EngineControl");
                }
            }
            return new RedirectToPageResult("/Index");
        }

        /// <inheritdoc/>
        public async Task<IActionResult> RedirectToIndexAsync(IdentityUser? user)
        {
            return await ToIndexAsync(user);
        }

        /// <inheritdoc/>
        public async Task<IActionResult> RedirectToIndexAsync(ClaimsPrincipal claimsPrincipal)
        {
            var user = await _userManager.GetUserAsync(claimsPrincipal);
            return await ToIndexAsync(user);
        }
    }
}
