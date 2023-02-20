using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServerWeb.Pages.SU
{
    [Authorize(Policy = "RequireSURole")]
    public class EngineAdministratorRoleModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
