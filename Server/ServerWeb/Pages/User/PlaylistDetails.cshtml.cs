using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.User
{
    [Authorize(Policy = "RequireUserRole")]
    public class PlaylistDetailsModel : PageModel
    {
        private readonly IPlaylistManagerService _playlistManagerService;
        public Playlist Playlist { get; set; } = new Playlist();
        public List<Song> Songs { get; set; } = new List<Song>(0);
        public string SecondsToString(double seconds)
        {
            int minutes = (int)(seconds / 60);
            int leftSeconds = (int)(seconds % 60);
            return $"{minutes:D2}:{leftSeconds:D2}";
        }

        public (string hours, string minutes) GetPlaylistDuration()
        {
            double seconds = Playlist.Duration;
            double minutes = seconds / 60;
            int hours = (int)(minutes / 60);
            int leftMinutes = (int)(minutes % 60);
            return (Convert.ToString(hours), Convert.ToString(leftMinutes));
        }

        public PlaylistDetailsModel(IPlaylistManagerService playlistManagerService)
        {
            _playlistManagerService = playlistManagerService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {

            if (!await _playlistManagerService.UserHasPlaylist(User, id))
            {
                return NotFound();
            }

            Songs = await _playlistManagerService.GetSongsAsync(id);
            var playlist = await _playlistManagerService.GetPlaylistAsync(id);

            if (playlist == null)
            {
                return NotFound();
            }

            Playlist = playlist;

            return Page();
        }
    }
}
