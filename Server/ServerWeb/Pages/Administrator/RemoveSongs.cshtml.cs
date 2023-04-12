using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.Administrator
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class RemoveSongsModel : PageModel
    {
        public List<Song> Songs { get; set; } = new List<Song>(0);
        public void OnGet()
        {
        }

        public string SecondsToString(double seconds)
        {
            int minutes = (int)(seconds / 60);
            int leftSeconds = (int)(seconds % 60);
            return $"{minutes:D2}:{leftSeconds:D2}";
        }
    }
}
