using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Services;

namespace ServerWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IIndexRedirectionService _indexRedirection;
        public IndexModel(ILogger<IndexModel> logger, IIndexRedirectionService indexRedirection)
        {
            _logger = logger;
            _indexRedirection = indexRedirection;
        }

        public async Task<IActionResult> OnGet()
        {
            return await _indexRedirection.RedirectToIndexAsync(User);
        }
    }
}