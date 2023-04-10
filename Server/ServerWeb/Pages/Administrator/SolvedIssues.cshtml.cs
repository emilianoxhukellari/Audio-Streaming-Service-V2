using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.Administrator
{
    public class SolvedIssuesModel : PageModel
    {
        private readonly IssueManagerService _issueManagerService;
        [BindProperty]
        public string Pattern { get; set; } = string.Empty;
        public List<Issue> SolvedIssues { get; set; } = new List<Issue>(0);
        public SolvedIssuesModel(IssueManagerService issueManagerService)
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
