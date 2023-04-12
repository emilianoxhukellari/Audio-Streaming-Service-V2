using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.SU
{
    [Authorize(Policy = "RequireSURole")]
    public class SuperUserRoleModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
