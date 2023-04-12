using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.Administrator
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class HomeModel : PageModel
    {
        private readonly ISongManagerService _songManagerService;
        private readonly IIssueManagerService _issueManagerService;
        private readonly UserManager<IdentityUser> _userManager;
        public int NumberOfSongs { get; set; }
        public int NumberOfUnsolvedIssues { get; set; }
        public int NumberOfSolvedIssues { get; set; }
        public string UserEmail { get; set; } = string.Empty;

        public HomeModel(
            IIssueManagerService issueManagerService,
            ISongManagerService songManagerService,
            UserManager<IdentityUser> userManager
            )
        {
            _issueManagerService = issueManagerService;
            _songManagerService = songManagerService;
            _userManager = userManager;
        }
        public async Task OnGetAsync()
        {
            NumberOfSongs = await _songManagerService.GetNumberOfSongsAsync();
            NumberOfSolvedIssues = await _issueManagerService.GetNumberOfIssues(IssueType.Solved);
            NumberOfUnsolvedIssues = await _issueManagerService.GetNumberOfIssues(IssueType.Unsolved);
            var user = await _userManager.GetUserAsync(User);
            UserEmail = user?.Email ?? string.Empty;
        }
    }
}
