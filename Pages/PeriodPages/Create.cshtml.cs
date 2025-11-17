using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.PeriodPages
{
    public class CreateModel : PageModel
    {
        private readonly PeriodRepository _repo = new PeriodRepository();

        [BindProperty]
        public Period Item { get; set; } = new Period
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(1).AddDays(0)
        };


        public void OnGet(){}

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var result = await _repo.CreateAsync(Item);

            if (result > 0)
                return RedirectToPage("Index");

            if (result == -1)
                ModelState.AddModelError("Item.PeriodNum", "This Period already exists for the given Year.");
            else
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");

            return Page();
        }
    }
}