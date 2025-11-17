using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetGroupPages
{
    public class CreateModel : PageModel
    {
        private readonly AssetGroupRepository _assetRepo = new AssetGroupRepository();
        private readonly LedgerRepository _ledgerRepo = new LedgerRepository();

        [BindProperty]
        public AssetGroup Item { get; set; } = new();

        public List<SelectListItem> LedgerOptions { get; set; } = new();

        public async Task OnGetAsync()
        {
            var ledgers = await _ledgerRepo.GetAllAsync();

            LedgerOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select --" } // Default unselected
            };

            LedgerOptions.AddRange(ledgers.Select(l => new SelectListItem
            {
                Value = l.DisplayValue,
                Text = l.Description
            }));
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var result = await _assetRepo.CreateAsync(Item);

            if (result > 0)
                return RedirectToPage("Index");

            if (result == -1)
                ModelState.AddModelError("Item.GroupCode", "This code already exists.");
            else if (result == -2)
                ModelState.AddModelError("Item.GroupDescription", "This description already exists.");
            else
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");

            return Page();
        }
    }
}
