using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerWeb.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace ServerWeb.Pages.Administrator
{
    [RequestFormLimits(MultipartBodyLengthLimit = 3000000000)]
    [RequestSizeLimit(3000000000)]
    public class AddSongsModel : PageModel
    {
        [BindProperty]
        public SongInput SongInput { get; set; }
        private readonly IAudioStoringService _audioStoringService;

        public AddSongsModel(IAudioStoringService audioStoringService)
        {
            _audioStoringService = audioStoringService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                this.SetTempData(WebApplicationExtensions.Status.Fail, "Could not store song.");
                return Page();
            }

            await _audioStoringService.StoreSongAsync(SongInput);
            this.SetTempData(WebApplicationExtensions.Status.Success, "Song stored successfully.");
            return RedirectToPage();
        }
    }
}
