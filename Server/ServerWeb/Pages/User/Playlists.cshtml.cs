using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.User
{
    public class PlaylistsModel : PageModel
    {
        private readonly PlaylistManagerService _playlistManagerService;
        public List<Playlist> Playlists { get; set; } = new List<Playlist>(0);
        public PlaylistsModel(PlaylistManagerService playlistManagerService)
        {
            _playlistManagerService = playlistManagerService;
        }

        public string SecondsToString(double seconds)
        {
            int minutes = (int)(seconds / 60);
            int hours = minutes / 60;
            return $"{hours:D2}:{minutes:D2}";
        }

        public async Task OnGetAsync()
        {
            Playlists = await _playlistManagerService.GetPlaylistsAsync(User);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _playlistManagerService.SoftDeletePlaylistAsync(id);
            return RedirectToPage();
        }
    }
}
