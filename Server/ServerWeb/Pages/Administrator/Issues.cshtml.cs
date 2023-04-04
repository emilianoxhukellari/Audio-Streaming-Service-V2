using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.Administrator
{
    public class IssuesModel : PageModel
    {
        [BindProperty]
        public string Pattern { get; set; } = string.Empty;
        public List<Issue> Issues { get; set; } = new List<Issue>(0);

        private readonly IssueManagerService _issueManagerService;

        public IssuesModel(IssueManagerService issueManagerService)
        {
            _issueManagerService = issueManagerService;
        }
        public async Task OnGet()
        {
            Issues = await _issueManagerService.GetRecentIssuesAsync(100, IssueRetrieveMode.NonSolved);
        }
    }
}
