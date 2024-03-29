using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IAccountService _registrationService;

        [BindProperty]
        public Register Register { get; set; }

        public RegisterModel(
            IAccountService registrationService,
            SignInManager<IdentityUser> signInManager)
        {
            _registrationService = registrationService;
            _signInManager = signInManager;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                IdentityUser? user;
                IdentityResult registerResult;

                (user, registerResult) = await _registrationService.RegisterAsync(Register.Email, Register.Password);

                if (user != null)
                {
                    await _signInManager.SignInAsync(user, false);

                    return RedirectToPage("/User/Home");
                }
                foreach (var error in registerResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return Page();
        }
    }
}
