using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.EngineAdministrator
{
    [Authorize(Policy = "RequireEngineAdministratorRole")]
    public class MoreModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
