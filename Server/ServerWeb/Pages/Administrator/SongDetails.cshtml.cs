using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Extensions;
using System.ComponentModel.DataAnnotations;

namespace ServerWeb.Pages.Administrator
{
    [RequestFormLimits(MultipartBodyLengthLimit = 3000000000)]
    [RequestSizeLimit(3000000000)]
    [Authorize(Policy = "RequireAdministratorRole")]
    public class SongDetailsModel : PageModel
    {
        private readonly ISongManagerService _songManagerService;
        private readonly IAudioStoringService _audioStoringService;
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }
        [BindProperty]
        [Display(Name = "Song File")]
        public IFormFile? SongFile { get; set; }
        public Song Song { get; set; }

        public SongDetailsModel(ISongManagerService songManagerService, IAudioStoringService audioStoringService)
        {
            _songManagerService = songManagerService;
            _audioStoringService = audioStoringService;
            Song = new Song();
        }

        public void OnGet()
        {
            Song = _songManagerService.GetSongFromDatabase(Id) ?? new Song();
        }

        public async Task<IActionResult> OnPost()
        {
            if (SongFile != null)
            {
                bool success = await _audioStoringService.UpdateSongFileAsync(Id, SongFile);

                if (success)
                {
                    this.SetTempData(WebApplicationExtensions.Status.Success, "Song updated successfully.");
                }
                else
                {
                    this.SetTempData(WebApplicationExtensions.Status.Fail, "Could not update song. Files with different durations are not accepted.");
                }
            }
            else
            {
                this.SetTempData(WebApplicationExtensions.Status.Fail, "Could not update song.");
            }
            Song = _songManagerService.GetSongFromDatabase(Id) ?? new Song();
            return Page();
        }
    }
}
