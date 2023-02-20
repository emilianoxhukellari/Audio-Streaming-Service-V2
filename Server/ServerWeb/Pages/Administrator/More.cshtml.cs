using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.Administrator
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class MoreModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
