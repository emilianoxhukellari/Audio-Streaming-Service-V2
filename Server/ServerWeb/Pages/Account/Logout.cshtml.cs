using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Services;

namespace ServerWeb.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IndexRedirection _indexRedirection;
        public LogoutModel(SignInManager<IdentityUser> signInManager, IndexRedirection indexRedirection) 
        { 
            _signInManager= signInManager;  
            _indexRedirection = indexRedirection;
        }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLogoutConfirmAsync()
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Account/Login");
        }
        public async Task<IActionResult> OnPostLogoutReject()
        {
            
            return await _indexRedirection.RedirectToIndexAsync(User);
        }
    }
}
