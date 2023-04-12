using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Formats.Asn1.AsnWriter;

namespace ServerWeb.Pages.SU
{
    [Authorize(Policy = "RequireSURole")]
    public class HomeModel : PageModel
    {
        private readonly ILogger<HomeModel> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        public int NumberOfSuperUsers { get; set; }
        public int NumberOfAdministrators { get; set; }
        public int NumberOfEngineAdministrators { get; set; }
        public int NumberOfUsers { get; set; }
        public string UserEmail { get; set; } = string.Empty;

        public HomeModel(ILogger<HomeModel> logger, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task OnGetAsync()
        {
            var superUsers = await _userManager.GetUsersInRoleAsync("SU");
            var administrators = await _userManager.GetUsersInRoleAsync("Administrator");
            var engineAdministrators = await _userManager.GetUsersInRoleAsync("EngineAdministrator");
            var users = await _userManager.GetUsersInRoleAsync("User");
            NumberOfSuperUsers = superUsers.Count;
            NumberOfAdministrators = administrators.Count;
            NumberOfEngineAdministrators = engineAdministrators.Count;
            NumberOfUsers = users.Count;
            var user = await _userManager.GetUserAsync(User);
            UserEmail = user?.Email ?? string.Empty;
        }
    }
}