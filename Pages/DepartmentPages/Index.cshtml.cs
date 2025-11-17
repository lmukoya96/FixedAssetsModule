using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.DepartmentPages
{
    public class IndexModel : PageModel
    {
        private readonly DepartmentRepository _repo = new DepartmentRepository();

        public List<Department> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _repo.GetAllAsync();
        }
    }
}