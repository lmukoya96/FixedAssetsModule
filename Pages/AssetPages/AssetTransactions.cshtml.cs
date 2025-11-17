using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetPages
{
    public class AssetTransactionsModel : PageModel
    {
        private readonly TransactionRepository _repo = new TransactionRepository();

        public string Code { get; set; } = string.Empty;
        public List<Transaction> Transactions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return NotFound();

            Code = code;
            Transactions = await _repo.GetByCodeAsync(code);

            return Page();
        }
    }
}