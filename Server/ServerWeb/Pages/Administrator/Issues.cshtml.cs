using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.Administrator
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class IssuesModel : PageModel
    {
        [BindProperty]
        public string Pattern { get; set; } = string.Empty;
        public List<Issue> Issues { get; set; } = new List<Issue>(0);

        private readonly IIssueManagerService _issueManagerService;

        public IssuesModel(IIssueManagerService issueManagerService)
        {
            _issueManagerService = issueManagerService;
        }
        public async Task OnGetAsync()
        {
            Issues = await _issueManagerService.GetRecentIssuesAsync(50, IssueRetrieveMode.NonSolved);
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (Pattern != string.Empty)
            {
                Issues = await _issueManagerService.GetIssuesFromPatternAsync(Pattern, IssueRetrieveMode.NonSolved);
            }
            return Page();
        }
    }
}
