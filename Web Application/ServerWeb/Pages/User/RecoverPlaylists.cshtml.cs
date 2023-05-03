using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.User
{
    [Authorize(Policy = "RequireUserRole")]
    public class RecoverPlaylistsModel : PageModel
    {
        private readonly IPlaylistManagerService _playlistManagerService;
        public List<Playlist> Playlists { get; set; } = new List<Playlist>(0);

        public string SecondsToString(double seconds)
        {
            int minutes = (int)(seconds / 60);
            int hours = minutes / 60;
            return $"{hours:D2}:{minutes:D2}";
        }

        public RecoverPlaylistsModel(IPlaylistManagerService playlistManagerService)
        {
            _playlistManagerService = playlistManagerService;
        }
        public async Task OnGetAsync()
        {
            Playlists = await _playlistManagerService.GetRemovedPlaylistAsync(User);
        }

        public async Task<IActionResult> OnPostRecoverAsync(int id)
        {
            await _playlistManagerService.RecoverPlaylist(id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPermanentlyDeleteAsync(int id)
        {
            await _playlistManagerService.PermanentlyDeletePlaylistAsync(id);
            return RedirectToPage();
        }
    }
}
