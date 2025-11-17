using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.DepreciationPages
{
    public class IndexModel : PageModel
    {
        private readonly DepreciationRepository _repo = new DepreciationRepository();

        public List<Depreciation> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _repo.GetAllAsync();
        }
    }
}
