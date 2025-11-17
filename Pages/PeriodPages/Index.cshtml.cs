using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.PeriodPages
{
    public class IndexModel : PageModel
    {
        private readonly PeriodRepository _repo = new PeriodRepository();

        public List<Period> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _repo.GetAllAsync();
        }
    }
}