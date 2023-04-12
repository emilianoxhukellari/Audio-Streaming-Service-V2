using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.Administrator
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class SolvedIssuesModel : PageModel
    {
        private readonly IIssueManagerService _issueManagerService;
        [BindProperty]
        public string Pattern { get; set; } = string.Empty;
        public List<Issue> SolvedIssues { get; set; } = new List<Issue>(0);
        public SolvedIssuesModel(IIssueManagerService issueManagerService)
        {
            _issueManagerService = issueManagerService;
        }
        public async Task OnGetAsync()
        {
            SolvedIssues = await _issueManagerService.GetRecentIssuesAsync(100, IssueRetrieveMode.Solved);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Pattern != string.Empty)
            {
                SolvedIssues = await _issueManagerService.GetIssuesFromPatternAsync(Pattern, IssueRetrieveMode.Solved);
            }
            return Page();
        }
    }
}
