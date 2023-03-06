using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerWeb.Services;
using System.Runtime.CompilerServices;

namespace ServerWeb.Pages
{ 
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IIndexRedirection _indexRedirection;
        public IndexModel(ILogger<IndexModel> logger, IIndexRedirection indexRedirection)
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