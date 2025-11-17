using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.DepreciationPages
{
    public class EditModel : PageModel
    {
        private readonly DepreciationRepository _repo = new DepreciationRepository();

        [BindProperty]
        public Depreciation Item { get; set; } = new();

        [BindProperty]
        public string OriginalCode { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return NotFound();
            var found = await _repo.GetByCodeAsync(code);
            if (found == null) return NotFound();
            Item = found;
            OriginalCode = found.Code; // keep track of original value
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var result = await _repo.UpdateAsync(Item, OriginalCode);

            if (result > 0)
                return RedirectToPage("Index");

            if (result == -1)
                ModelState.AddModelError("Item.Code", "This code already exists.");
            else if (result == -2)
                ModelState.AddModelError("Item.Description", "This description already exists.");
            else
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");

            return Page();
        }
    }
}