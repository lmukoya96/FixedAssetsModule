using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;
using TestModule.Services;

namespace TestModule.Pages.AssetPages
{
    public class AssetDepreciationModel : PageModel
    {
        private readonly DepreciationService _depreciationService = new DepreciationService();
        private readonly AssetRepository _assetRepo = new AssetRepository();

        public List<(string Period, decimal DepreciationRate, decimal DepreciationAmount, decimal BookValue)>
            DepreciationHistory
        { get; set; } = new List<(string, decimal, decimal, decimal)>();

        public Asset? Asset { get; set; }
        public string? AssetCode { get; set; }

        public async Task<IActionResult> OnGetAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                return RedirectToPage("/AssetPages/Index");

            AssetCode = code;
            Asset = await _assetRepo.GetByCodeAsync(code);

            if (Asset == null)
                return NotFound();

            // Get the depreciation history
            DepreciationHistory = await _depreciationService.GetAssetDepreciationHistoryAsync(code);

            return Page();
        }
    }
}