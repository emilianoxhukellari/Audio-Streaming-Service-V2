using DataAccess.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Services;

namespace ServerWeb.Pages.Account
{
    public class DeleteAccountModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IIndexRedirectionService _indexRedirection;
        private readonly IAccountService _accountService;
        public DeleteAccountModel(
            SignInManager<IdentityUser> signInManager, 
            IIndexRedirectionService indexRedirection,
            IAccountService accountService)
        {
            _signInManager = signInManager;
            _indexRedirection = indexRedirection;
            _accountService = accountService;
        }

        public async Task<IActionResult> OnPostDeleteAccountConfirmAsync()
        {
            var deleteSuccess = await _accountService.DeleteAccountAsync(User);
            if(deleteSuccess)
            {
                await _signInManager.SignOutAsync();
                return RedirectToPage("/Account/Login");
            }
            return await _indexRedirection.RedirectToIndexAsync(User);
        }
        public async Task<IActionResult> OnPostDeleteAccountReject()
        {
            return await _indexRedirection.RedirectToIndexAsync(User);
        }
    }
}
