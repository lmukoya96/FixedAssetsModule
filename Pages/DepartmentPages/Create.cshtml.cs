using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.DepartmentPages
{
    public class CreateModel : PageModel
    {
        private readonly DepartmentRepository _repo = new DepartmentRepository();

        [BindProperty]
        public Department Item { get; set; } = new Department();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var result = await _repo.CreateAsync(Item);

            if (result > 0)
                return RedirectToPage("Index");

            if (result == -1)
                ModelState.AddModelError("Item.DepartmentCode", "This Department Code already exists.");
            else if (result == -2)
                ModelState.AddModelError("Item.DepartmentName", "This Department Name already exists.");
            else
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");

            return Page();
        }
    }
}
