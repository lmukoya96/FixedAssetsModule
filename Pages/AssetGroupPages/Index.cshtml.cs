using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetGroupPages
{
    public class IndexModel : PageModel
    {
        private readonly AssetGroupRepository _repo = new AssetGroupRepository();

        public List<AssetGroup> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _repo.GetAllAsync();
        }
    }
}