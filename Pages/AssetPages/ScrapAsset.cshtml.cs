using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TestModule.Data;
using TestModule.Models;
using TestModule.Services;
using TestModule.Validators;

namespace TestModule.Pages.AssetPages
{
    public class ScrapAssetModel : PageModel
    {
        private readonly AssetRepository _assetRepo = new AssetRepository();
        private readonly DepreciationService _depreciationService = new DepreciationService();
        private readonly PeriodRepository _periodRepo = new PeriodRepository();

        [BindProperty]
        public string AssetCode { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please select a period")]
        [Display(Name = "Scrapping Period")]
        public string SelectedPeriod { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "Confirmation")]
        [MustBeTrue(ErrorMessage = "You must confirm the scrapping.")]
        public bool ConfirmScrapping { get; set; }

        public Asset? Asset { get; set; }
        public List<SelectListItem> PeriodOptions { get; set; } = new List<SelectListItem>();
        public string CurrentPeriodString { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
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

            var currentPeriod = await _periodRepo.GetCurrentPeriodAsync();
            CurrentPeriodString = currentPeriod != null
                ? $"{currentPeriod.PeriodNum:D2}-{currentPeriod.Year}"
                : "Current Period";

            await LoadPeriodOptions();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadPageDataAsync();
                return Page();
            }

            try
            {
                await _depreciationService.ScrapAssetAsync(AssetCode, SelectedPeriod);
                TempData["SuccessMessage"] = $"Asset {AssetCode} has been successfully scrapped for period {SelectedPeriod}.";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error scrapping asset: {ex.Message}";
                await ReloadPageDataAsync();
                return Page();
            }
        }

        private async Task LoadPeriodOptions()
        {
            // Use the method to get periods
            var periods = await _depreciationService.GetAssetCostPeriodsAsync(AssetCode);

            PeriodOptions = periods
                .Select(period => new SelectListItem
                {
                    Value = period,
                    Text = period
                })
                .ToList();

            // Set default selection if no period is selected yet and we have options
            if (string.IsNullOrEmpty(SelectedPeriod) && PeriodOptions.Any())
            {
                SelectedPeriod = PeriodOptions.Last().Value; // Default to the latest period
            }
        }

        private async Task ReloadPageDataAsync()
        {
            Asset = await _assetRepo.GetByCodeAsync(AssetCode);
            var currentPeriod = await _periodRepo.GetCurrentPeriodAsync();
            CurrentPeriodString = currentPeriod != null
                ? $"{currentPeriod.PeriodNum:D2}-{currentPeriod.Year}"
                : "Current Period";

            await LoadPeriodOptions();
        }
    }
}