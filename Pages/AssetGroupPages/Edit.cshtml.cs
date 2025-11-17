using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetGroupPages
{
    public class EditModel : PageModel
    {
        private readonly AssetGroupRepository _repo = new AssetGroupRepository();
        private readonly LedgerRepository _ledgerRepo = new LedgerRepository();

        [BindProperty]
        public AssetGroup Item { get; set; } = new();

        [BindProperty]
        public string OriginalCode { get; set; } = string.Empty;

        public List<SelectListItem> LedgerOptions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return NotFound();

            var found = await _repo.GetByCodeAsync(code);
            if (found == null) return NotFound();

            Item = found;
            OriginalCode = found.GroupCode;

            // load ledger list
            var ledgers = await _ledgerRepo.GetAllAsync();
            LedgerOptions = ledgers
                .Select(l => new SelectListItem
                {
                    Value = l.DisplayValue,
                    Text = l.Description,
                }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var result = await _repo.UpdateAsync(Item, OriginalCode);

            if (result > 0)
                return RedirectToPage("Index");

            if (result == -1)
                ModelState.AddModelError("Item.GroupCode", "This code already exists.");
            else if (result == -2)
                ModelState.AddModelError("Item.GroupDescription", "This description already exists.");
            else
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");

            // reload options if validation fails
            var ledgers = await _ledgerRepo.GetAllAsync();
            LedgerOptions = ledgers
                .Select(l => new SelectListItem
                {
                    Value = l.DisplayValue,
                    Text = l.DisplayValue
                }).ToList();

            return Page();
        }
    }
}
