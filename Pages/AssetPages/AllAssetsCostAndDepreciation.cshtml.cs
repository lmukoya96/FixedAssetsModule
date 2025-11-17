using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetPages
{
    public class AllAssetsCostAndDepreciationModel : PageModel
    {
        private readonly AssetRepository _assetRepo = new AssetRepository();
        private readonly AssetCategoryRepository _categoryRepo = new AssetCategoryRepository();

        public List<SelectListItem> AssetCategories { get; set; } = new();
        public List<Asset> Assets { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedCategory { get; set; }

        public async Task OnGetAsync()
        {
            // Load categories for dropdown
            AssetCategories = (await _categoryRepo.GetAllAsync())
                .Select(c => new SelectListItem { Value = c.CategoryCode, Text = c.CategoryDescription })
                .ToList();

            // If a category is selected, load assets for that category; otherwise load all assets
            if (!string.IsNullOrEmpty(SelectedCategory))
            {
                // Trim to guard against whitespace mismatch
                var assetsByCategory = await _assetRepo.GetByAssetCategoryAsync(SelectedCategory.Trim());
                Assets = assetsByCategory ?? new List<Asset>();
            }
            else
            {
                Assets = await _assetRepo.GetAllAsync();
            }
        }
    }
}