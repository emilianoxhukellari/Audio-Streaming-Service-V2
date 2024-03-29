using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Services;

namespace ServerWeb.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IIndexRedirectionService _indexRedirection;
        [BindProperty]
        public Login Login { get; set; }

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IIndexRedirectionService indexRedirection)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _indexRedirection = indexRedirection;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Login.Email, Login.Password, Login.RememberMe, false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(Login.Email);
                    return await _indexRedirection.RedirectToIndexAsync(user);
                }
                ModelState.AddModelError(string.Empty, "Username or Password incorrect.");
            }
            return Page();
        }
    }
}
