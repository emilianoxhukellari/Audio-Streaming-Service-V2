using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace ServerWeb.Pages.Administrator
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class IssueDetailsModel : PageModel
    {
        private readonly IIssueManagerService _issueManagerService;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }
        [BindProperty]
        public string? SolutionDescription { get; set; }
        public Issue Issue { get; set; } 

        public IssueDetailsModel(IIssueManagerService issueManagerService)
        {
            _issueManagerService = issueManagerService;
            Issue = new Issue();
        }

        public async Task OnGet()
        {
            var issue = await _issueManagerService.GetIssueAsync(Id);
            if(issue is not null)
            {
                Issue = issue;
                SolutionDescription = issue.SolutionDescription;
            }
        }

        public async Task<IActionResult> OnPostSolveAsync()
        {
            if (ModelState.IsValid)
            {
                var issue = await _issueManagerService.GetIssueAsync(Id);
                bool success = false;
                if (issue is not null)
                {
                    Issue = issue;
                    if (SolutionDescription is not null && Issue.SolutionDescription != SolutionDescription)
                    {
                        success = await _issueManagerService.SolveIssueAsync(Issue.Id, SolutionDescription, User);
                    }
                    else
                    {
                        success = false;
                    }
                }
                else
                {
                    success = false;
                }

                if (success)
                {
                    this.SetTempData(WebApplicationExtensions.Status.Success, "Issue solved successfully.");
                }
                else
                {
                    this.SetTempData(WebApplicationExtensions.Status.Fail, "Could not solve issue.");
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostUnresolveAsync()
        {
            if (ModelState.IsValid)
            {
                bool success = await _issueManagerService.UnresolveIssueAsync(Id);
                if(success)
                {
                    this.SetTempData(WebApplicationExtensions.Status.Success, "Issue unresolved successfully.");
                }
                else
                {
                    this.SetTempData(WebApplicationExtensions.Status.Fail, "Could not unresolve issue.");
                }
            }
            Issue = await _issueManagerService.GetIssueAsync(Id) ?? new Issue();
            SolutionDescription = Issue.SolutionDescription;
            ModelState.Clear();
            return Page();
        }
    }
}
