using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetPages
{
    public class CreateModel : PageModel
    {
        private readonly AssetRepository _repo = new AssetRepository();
        private readonly AssetCategoryRepository _categoryRepo = new AssetCategoryRepository();
        private readonly AssetGroupRepository _groupRepo = new AssetGroupRepository();
        private readonly DepartmentRepository _deptRepo = new DepartmentRepository();

        [BindProperty]
        public Asset Item { get; set; } = new Asset
        {
            PurchaseDate = DateTime.Now,
            DepreciationStartDate = DateTime.Now,
            TransactionDate = DateTime.Now
        };

        public List<SelectListItem> AssetGroups { get; set; } = new();
        public List<SelectListItem> AssetCategories { get; set; } = new();
        public List<SelectListItem> Departments { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDropdownsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            var result = await _repo.CreateAsync(Item);

            if (result > 0)//Asset created successfully
            {
                // 1. Get the current period
                var _periodRepo = new PeriodRepository();
                var currentPeriod = await _periodRepo.GetCurrentPeriodAsync();
                if (currentPeriod == null) throw new Exception("No current period found.");

                string period = $"{currentPeriod.PeriodNum:D2}-{currentPeriod.Year}";

                // 2. Create the transaction.
                var transactionRepo = new TransactionRepository();
                var transaction = new Transaction
                {
                    AssetCode = Item.Code,
                    TransactionType = "Asset added", // keep simple, can extend later
                    TransactionDate = DateTime.Today,
                    Amount = Item.PurchaseAmount,
                    Cost = Item.PurchaseAmount
                };

                await transactionRepo.CreateAsync(transaction, period);

                return RedirectToPage("Index");
            }
            else if (result == -1)
                ModelState.AddModelError("Item.Code", "This Asset Code already exists.");
            else if (result == -2)
                ModelState.AddModelError("Item.TrackingCode", "This Tracking Code already exists.");
            else if (result == -3)
                ModelState.AddModelError("Item.SerialNumber", "This Serial Number already exists.");
            else
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");

            await LoadDropdownsAsync();
            return Page();
        }

        private async Task LoadDropdownsAsync()
        {
            AssetGroups = (await _groupRepo.GetAllAsync())
                .Select(g => new SelectListItem { Value = g.GroupCode, Text = g.GroupDescription })
                .ToList();

            AssetCategories = (await _categoryRepo.GetAllAsync())
                .Select(c => new SelectListItem { Value = c.CategoryCode, Text = c.CategoryDescription })
                .ToList();

            Departments = (await _deptRepo.GetAllAsync())
                .Select(d => new SelectListItem { Value = d.DepartmentName, Text = d.DepartmentName })
                .ToList();
        }
    }
}
