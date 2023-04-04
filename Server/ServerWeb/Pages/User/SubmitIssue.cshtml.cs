using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace ServerWeb.Pages.User
{
    public class IssuesModel : PageModel
    {

        private readonly IssueManagerService _issueManagerService;
        [BindProperty]
        public IssueInput IssueInput { get; set; }

        public IssuesModel(IssueManagerService issueManagerService)
        {
            _issueManagerService = issueManagerService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if(ModelState.IsValid) 
            {
                await _issueManagerService.CreateIssueAsync(IssueInput.Title, IssueInput.Type, IssueInput.Description, User);
                TempData["Display"] = "block";
                TempData["Message"] = "The operation was successful!";
            }
            return RedirectToPage();
        }
    }
}
