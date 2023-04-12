using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Extensions;
using System.Diagnostics;

namespace ServerWeb.Pages.User
{
    [Authorize(Policy = "RequireUserRole")]
    public class IssuesModel : PageModel
    {

        private readonly IIssueManagerService _issueManagerService;
        [BindProperty]
        public IssueInput IssueInput { get; set; }
        public int NumberOfSubmittedIssues { get; set; }

        public IssuesModel(IIssueManagerService issueManagerService)
        {
            _issueManagerService = issueManagerService;
        }

        public async Task OnGetAsync()
        {
            NumberOfSubmittedIssues = await _issueManagerService.GetNumberOfSubmittedIssuesByUserAsync(User);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if(ModelState.IsValid) 
            {
                await _issueManagerService.CreateIssueAsync(IssueInput.Title, IssueInput.Type, IssueInput.Description, User);
                this.SetTempData(WebApplicationExtensions.Status.Success, "Issue created successfully.");
            }
            return RedirectToPage();
        }
    }
}
