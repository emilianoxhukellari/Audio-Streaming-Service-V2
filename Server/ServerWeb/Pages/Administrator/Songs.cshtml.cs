using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace ServerWeb.Pages.Administrator
{
    public class SongsModel : PageModel
    {
        private readonly SongManagerService _songManagerService;

        [BindProperty]
        public string Pattern { get; set; } = string.Empty;
        public List<Song> Songs { get; set; } = new List<Song>(0);

        public SongsModel(SongManagerService songManagerService)
        {
            _songManagerService = songManagerService;
        }

        public async Task OnGet()
        {
            Songs = await _songManagerService.GetSongsForWebAppAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Pattern != string.Empty)
            {
                Songs = await _songManagerService.GetSongsForWebAppAsync(Pattern);
            }
            return Page();
        }

        public IActionResult OnPostUpdate(int songId)
        {
            return RedirectToPage("/Administrator/SongDetails", new { Id = songId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int songId)
        {
            await _songManagerService.DeleteSongAsync(songId);
            if(Pattern != string.Empty)
            {
                Songs = await _songManagerService.GetSongsForWebAppAsync(Pattern);
            }
            else
            {
                Songs = await _songManagerService.GetSongsForWebAppAsync();
            }
            ModelState.Clear();
            return Page();
        }

        public string SecondsToString(double seconds)
        {
            int minutes = (int)(seconds / 60);
            int leftSeconds = (int)(seconds % 60);
            return $"{minutes:D2}:{leftSeconds:D2}";
        }
    }
}
