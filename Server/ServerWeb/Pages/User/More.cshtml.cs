using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.User
{
    [Authorize(Policy = "RequireUserRole")]
    public class MoreModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
