using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Pages.AssetPages
{
    public class EditModel : PageModel
    {
        private readonly AssetRepository _repo = new AssetRepository();
        private readonly AssetCategoryRepository _categoryRepo = new AssetCategoryRepository();
        private readonly AssetGroupRepository _groupRepo = new AssetGroupRepository();
        private readonly DepartmentRepository _deptRepo = new DepartmentRepository();

        [BindProperty]
        public Asset Item { get; set; } = new();

        [BindProperty]
        public string OriginalCode { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string code)
        {
            var found = await _repo.GetByCodeAsync(code);
            if (found == null) return NotFound();

            Item = found;
            OriginalCode = code;

            await LoadDropdownsAsync();

            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            // Get the existing asset before update
            var existingAsset = await _repo.GetByCodeAsync(OriginalCode);
            if (existingAsset == null) return NotFound();

            var result = await _repo.UpdateAsync(Item, OriginalCode);

            if (result > 0)
                return RedirectToPage("Index");
            else if (result == -1)
                ModelState.AddModelError("Item.Code", "Another Asset with this Code already exists.");
            else if (result == -2)
                ModelState.AddModelError("Item.TrackingCode", "Another Asset with this Tracking Code already exists.");
            else if (result == -3)
                ModelState.AddModelError("Item.SerialNumber", "Another Asset with this Serial Number already exists.");
            else
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");

            await LoadDropdownsAsync();
            return Page();
        }

        private async Task LoadDropdownsAsync()
        {
            var categories = await _categoryRepo.GetAllAsync() ?? new List<AssetCategory>();
            var groups = await _groupRepo.GetAllAsync() ?? new List<AssetGroup>();
            var departments = await _deptRepo.GetAllAsync() ?? new List<Department>();

            ViewData["AssetGroups"] = new SelectList(
                groups, "GroupCode", "GroupDescription", Item?.AssetGroup
            );

            ViewData["AssetCategories"] = new SelectList(
                categories, "CategoryCode", "CategoryDescription", Item?.AssetCategory
            );

            ViewData["Departments"] = new SelectList(
                departments, "DepartmentName", "DepartmentName", Item?.Department
            );
        }

    }
}