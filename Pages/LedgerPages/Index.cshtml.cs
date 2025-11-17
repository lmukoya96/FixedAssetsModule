using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.LedgerPages
{
    public class IndexModel : PageModel
    {
        private readonly LedgerRepository _repo = new LedgerRepository();

        public List<Ledger> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _repo.GetAllAsync();
        }
    }
}