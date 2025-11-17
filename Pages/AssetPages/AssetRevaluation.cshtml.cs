using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TestModule.Services;
using TestModule.Data;
using TestModule.Models;
using System.ComponentModel.DataAnnotations;

namespace TestModule.Pages.AssetPages
{
    public class AssetRevaluationModel : PageModel
    {
        private readonly AssetRepository _assetRepo = new AssetRepository();
        private readonly DepreciationService _depreciationService = new DepreciationService();

        [BindProperty]
        public string AssetCode { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please select a period")]
        [Display(Name = "Revaluation Period")]
        public string SelectedPeriod { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please enter the new cost")]
        [Display(Name = "New Cost")]
        public decimal NewCost { get; set; }
        public Asset? Asset { get; set; }
        public List<SelectListItem> PeriodOptions { get; set; } = new List<SelectListItem>();
        public decimal CurrentCost { get; set; }

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "No asset code provided.";
                return RedirectToPage("Index");
            }

            AssetCode = code;
            Asset = await _assetRepo.GetByCodeAsync(code);

            if (Asset == null)
            {
                TempData["ErrorMessage"] = $"Asset {code} not found.";
                return RedirectToPage("Index");
            }

            CurrentCost = Asset.Cost;
            NewCost = CurrentCost; // Set default value to current cost
            await LoadPeriodOptions();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadPageData();
                return Page();
            }

            try
            {
                // Get the asset to update
                var asset = await _assetRepo.GetByCodeAsync(AssetCode);
                if (asset == null)
                {
                    TempData["ErrorMessage"] = $"Asset {AssetCode} not found.";
                    return RedirectToPage("Index");
                }

                decimal oldCost = asset.Cost;
                asset.Cost = NewCost;

                // Update the asset valuation with the selected period
                await _depreciationService.UpdateAssetValuationAsync(asset, oldCost, SelectedPeriod);

                TempData["SuccessMessage"] = $"Asset {AssetCode} has been successfully revalued from {oldCost:C} to {NewCost:C} for period {SelectedPeriod}.";
                return RedirectToPage("Info", new { code = AssetCode });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error revaluing asset: {ex.Message}";
                await ReloadPageData();
                return Page();
            }
        }

        private async Task LoadPeriodOptions()
        {
            // Use the new method to get periods
            var periods = await _depreciationService.GetAssetCostPeriodsAsync(AssetCode);

            PeriodOptions = periods
                .Select(period => new SelectListItem
                {
                    Value = period,
                    Text = period
                })
                .ToList();

            SelectedPeriod = string.Empty;
        }

        private async Task ReloadPageData()
        {
            Asset = await _assetRepo.GetByCodeAsync(AssetCode);
            if (Asset != null)
            {
                CurrentCost = Asset.Cost;
            }
            await LoadPeriodOptions();
        }
    }
}