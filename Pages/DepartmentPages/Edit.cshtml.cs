using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.DepartmentPages
{
    public class EditModel : PageModel
    {
        private readonly DepartmentRepository _repo = new DepartmentRepository();

        [BindProperty]
        public Department Item { get; set; } = new();

        [BindProperty]
        public string OriginalCode { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string code)
        {
            var found = await _repo.GetByCodeAsync(code);
            if (found == null) return NotFound();

            Item = found;
            OriginalCode = code;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var result = await _repo.UpdateAsync(Item, OriginalCode);

            if (result > 0)
                return RedirectToPage("Index");

            if (result == -1)
                ModelState.AddModelError("Item.DepartmentCode", "Another Department with this Code already exists.");
            else if (result == -2)
                ModelState.AddModelError("Item.DepartmentName", "Another Department with this Name already exists.");
            else
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");

            return Page();
        }
    }
}