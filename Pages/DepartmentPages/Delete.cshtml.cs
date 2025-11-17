using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.DepartmentPages
{
    public class DeleteModel : PageModel
    {
        private readonly DepartmentRepository _repo = new DepartmentRepository();
        public Department? Item { get; set; }

        public async Task<IActionResult> OnGetAsync(string departmentCode)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
                return NotFound();

            Item = await _repo.GetByCodeAsync(departmentCode);
            if (Item == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string departmentCode)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
                return NotFound();

            await _repo.DeleteAsync(departmentCode);
            return RedirectToPage("Index");
        }
    }
}