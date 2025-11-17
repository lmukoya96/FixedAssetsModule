using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetPages
{
    public class InfoModel : PageModel
    {
        private readonly AssetRepository _repo = new AssetRepository();
        public Asset? Asset { get; set; }

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return NotFound();

            Asset = await _repo.GetByCodeAsync(code);
            if (Asset == null) return NotFound();

            return Page();
        }
    }
}