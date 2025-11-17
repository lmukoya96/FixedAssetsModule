using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetGroupPages
{
    public class DeleteModel : PageModel
    {
        private readonly AssetGroupRepository _repo = new AssetGroupRepository();
        public AssetGroup? Item { get; set; }

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return NotFound();
            Item = await _repo.GetByCodeAsync(code);
            if (Item == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return NotFound();
            await _repo.DeleteAsync(code);
            return RedirectToPage("Index");
        }
    }
}