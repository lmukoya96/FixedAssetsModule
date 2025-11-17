using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.PeriodPages
{
    public class EditModel : PageModel
    {
        private readonly PeriodRepository _repo = new PeriodRepository();

        [BindProperty]
        public Period Item { get; set; } = new();

        [BindProperty]
        public byte OriginalPeriod { get; set; }

        [BindProperty]
        public short OriginalYear { get; set; }

        public async Task<IActionResult> OnGetAsync(byte period, short year)
        {
            var found = await _repo.GetByPeriodAndYearAsync(period, year);
            if (found == null) return NotFound();

            Item = found;
            OriginalPeriod = period;
            OriginalYear = year;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Validation failed. Please review your input.");
                return Page();
            }

            var result = await _repo.UpdateAsync(Item, OriginalPeriod, OriginalYear);

            if (result > 0)
            {
                //await _repo.RunDepreciationAsync();

                return RedirectToPage("Index");
            }
            else if (result == 0)
                ModelState.AddModelError("", "No record was updated. Check that OriginalPeriod/Year match an existing record.");
            else if (result == -1)
                ModelState.AddModelError("", "Another Period with the same Period/Year already exists.");
            else if (result == -2)
                ModelState.AddModelError("", "You are not allowed to skip over periods.");
            else if (result == -3)
                ModelState.AddModelError("", "This period has already been closed. You cannot re-open closed periods.");
            else
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");

            return Page();
        }
    }
}