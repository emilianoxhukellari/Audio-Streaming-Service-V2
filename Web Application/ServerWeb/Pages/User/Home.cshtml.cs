using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.User
{
    [Authorize(Policy = "RequireUserRole")]
    public class HomeModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IIssueManagerService _issueManagerService;
        private readonly IPlaylistManagerService _playlistManagerService;

        public string UserEmail { get; set; } = string.Empty;
        public int NumberOfPlaylists { get; set; }
        public int NumberOfDeletedPlaylists { get; set; }
        public int NumberOfSubmittedIssues { get; set; }
        public HomeModel(
            UserManager<IdentityUser> userManager,
            IPlaylistManagerService playlistManagerService,
            IIssueManagerService issueManagerService)
        {
            _userManager = userManager;
            _playlistManagerService = playlistManagerService;
            _issueManagerService = issueManagerService;
        }
        public async Task OnGetAsync()
        {
            NumberOfPlaylists = await _playlistManagerService.GetNumberOfPlaylistsAsync(User);
            NumberOfDeletedPlaylists = await _playlistManagerService.GetNumberOfDeletedPlaylistsAsync(User);
            NumberOfSubmittedIssues = await _issueManagerService.GetNumberOfSubmittedIssuesByUserAsync(User);
            var user = await _userManager.GetUserAsync(User);
            UserEmail = user?.Email ?? string.Empty;
        }
    }
}
