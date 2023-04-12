using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.SU
{
    [Authorize(Policy = "RequireSURole")]
    public class AdministratorRoleModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
