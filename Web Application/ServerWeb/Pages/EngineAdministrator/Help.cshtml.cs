using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.EngineAdministrator
{
    [Authorize(Policy = "RequireEngineAdministratorRole")]
    public class HelpModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
