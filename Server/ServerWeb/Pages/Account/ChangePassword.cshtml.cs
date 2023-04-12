using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.Administrator
{
    public class ChangePasswordModel : PageModel
    {
        [BindProperty]
        public ChangePassword ChangePassword { get; set; }
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public ChangePasswordModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                var result = await _userManager.ChangePasswordAsync(user, ChangePassword.CurrentPassword, ChangePassword.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }

                await _signInManager.RefreshSignInAsync(user);
                return RedirectToPage("/Account/ChangePasswordConfirmation");
            }
            return Page();
        }
    }
}
