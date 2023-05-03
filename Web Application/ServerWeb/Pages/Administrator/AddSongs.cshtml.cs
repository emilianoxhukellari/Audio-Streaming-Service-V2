using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Extensions;

namespace ServerWeb.Pages.Administrator
{
    [RequestFormLimits(MultipartBodyLengthLimit = 3000000000)]
    [RequestSizeLimit(3000000000)]
    [Authorize(Policy = "RequireAdministratorRole")]
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

            await _audioStoringService.StoreSongAsync(SongInput, User);
            this.SetTempData(WebApplicationExtensions.Status.Success, "Song stored successfully.");
            return RedirectToPage();
        }
    }
}
