using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.PeriodPages
{
    public class DeleteModel : PageModel
    {
        private readonly PeriodRepository _repo = new PeriodRepository();
        public Period? Item { get; set; }

        public async Task<IActionResult> OnGetAsync(byte period, short year)
        {
            Item = await _repo.GetByPeriodAndYearAsync(period, year);
            if (Item == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(byte period, short year)
        {
            await _repo.DeleteAsync(period, year);
            return RedirectToPage("Index");
        }
    }
}