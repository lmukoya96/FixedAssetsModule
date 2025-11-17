using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.DepreciationPages
{
    public class CreateModel : PageModel
    {
        private readonly DepreciationRepository _repo = new DepreciationRepository();

        [BindProperty]
        public Depreciation Item { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var result = await _repo.CreateAsync(Item);

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