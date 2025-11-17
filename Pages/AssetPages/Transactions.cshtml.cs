using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;

namespace TestModule.Pages.AssetPages
{
    public class TransactionsModel : PageModel
    {
        private readonly TransactionRepository _repo = new TransactionRepository();

        public List<(string Description, string Period, string TransactionType, DateTime TransactionDate, decimal Amount, decimal Cost)> Items { get; set; } = new ();

        public async Task OnGetAsync()
        {
            Items = await _repo.GetAllWithAssetsAsync();
        }
    }
}