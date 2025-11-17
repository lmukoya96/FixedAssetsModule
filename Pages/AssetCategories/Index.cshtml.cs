using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetCategories
{
    public class IndexModel : PageModel
    {
        private readonly AssetCategoryRepository _repo = new AssetCategoryRepository();

        public List<AssetCategory> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _repo.GetAllAsync();
        }
    }
}
