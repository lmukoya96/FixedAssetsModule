using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TestModule.Services;
using TestModule.Data;

namespace TestModule.Pages.AssetPages
{
    public class AllAssetsDepreciationModel : PageModel
    {
        private readonly DepreciationService _depreciationService = new DepreciationService();
        private readonly AssetCategoryRepository _categoryRepo = new AssetCategoryRepository();

        public List<SelectListItem> AssetCategories { get; set; } = new();
        public List<Dictionary<string, object>> DepreciationData { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty]
        public string? EmailAddress { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAssetCategoriesAsync();

            // Load data based on filters
            if (!string.IsNullOrEmpty(SelectedCategory) && StartDate.HasValue && EndDate.HasValue)
            {
                // Filter by category and date range
                DepreciationData = await _depreciationService.GetTotalDepreciationInRangeAsync(SelectedCategory, StartDate.Value, EndDate.Value);
            }
            else if (!string.IsNullOrEmpty(SelectedCategory))
            {
                // Filter by category only
                DepreciationData = await _depreciationService.GetTotalDepreciationByCategoryAsync(SelectedCategory);
            }
            else if (StartDate.HasValue && EndDate.HasValue)
            {
                // Filter by date range only
                DepreciationData = await _depreciationService.GetTotalDepreciationInRangeAsync(null, StartDate.Value, EndDate.Value);
            }
            else
            {
                // Load all assets (no filters)
                DepreciationData = await _depreciationService.GetTotalDepreciationAsync();
            }
        }

        public async Task<IActionResult> OnGetExportCsv()
        {
            // Check if category is selected
            if (string.IsNullOrEmpty(SelectedCategory))
            {
                TempData["Error"] = "Please select a category before exporting.";
                return RedirectToPage();
            }

            await LoadAssetCategoriesAsync();

            // Get data based on current filters
            List<Dictionary<string, object>> exportData;

            if (!string.IsNullOrEmpty(SelectedCategory) && StartDate.HasValue && EndDate.HasValue)
            {
                exportData = await _depreciationService.GetTotalDepreciationInRangeAsync(SelectedCategory, StartDate.Value, EndDate.Value);
            }
            else if (!string.IsNullOrEmpty(SelectedCategory))
            {
                exportData = await _depreciationService.GetTotalDepreciationByCategoryAsync(SelectedCategory);
            }
            else if (StartDate.HasValue && EndDate.HasValue)
            {
                exportData = await _depreciationService.GetTotalDepreciationInRangeAsync(null, StartDate.Value, EndDate.Value);
            }
            else
            {
                exportData = await _depreciationService.GetTotalDepreciationAsync();
            }

            if (exportData.Count == 0)
            {
                // If no data, redirect back to the page with an error message
                TempData["Error"] = "No data available to export.";
                return RedirectToPage();
            }

            // Build CSV
            var csv = "Asset Code,Description,Total Depreciation" + Environment.NewLine;

            foreach (var row in exportData)
            {
                var code = row.ContainsKey("AssetCode") ? row["AssetCode"]?.ToString() : "";
                var desc = row.ContainsKey("Description") ? row["Description"]?.ToString()?.Replace("\"", "\"\"") : "";
                var total = row.ContainsKey("TotalDepreciation") ? row["TotalDepreciation"]?.ToString() : "0";

                csv += $"{code},\"{desc}\",{total}" + Environment.NewLine;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var fileName = $"AssetsDepreciation_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }

        public async Task<IActionResult> OnPostExportAndSend()
        {
            if (string.IsNullOrEmpty(EmailAddress))
            {
                TempData["Error"] = "Email address is required.";
                return RedirectToPage();
            }

            // Check if category is selected
            if (string.IsNullOrEmpty(SelectedCategory))
            {
                TempData["Error"] = "Please select a category before sending the report.";
                return RedirectToPage();
            }

            // Get data based on current filters
            List<Dictionary<string, object>> exportData;

            if (!string.IsNullOrEmpty(SelectedCategory) && StartDate.HasValue && EndDate.HasValue)
            {
                exportData = await _depreciationService.GetTotalDepreciationInRangeAsync(SelectedCategory, StartDate.Value, EndDate.Value);
            }
            else if (!string.IsNullOrEmpty(SelectedCategory))
            {
                exportData = await _depreciationService.GetTotalDepreciationByCategoryAsync(SelectedCategory);
            }
            else if (StartDate.HasValue && EndDate.HasValue)
            {
                exportData = await _depreciationService.GetTotalDepreciationInRangeAsync(null, StartDate.Value, EndDate.Value);
            }
            else
            {
                exportData = await _depreciationService.GetTotalDepreciationAsync();
            }

            if (exportData.Count == 0)
            {
                TempData["Error"] = "No data available to export.";
                return RedirectToPage();
            }

            // Create Excel report using ReportingService
            var reportingService = new ReportingService();
            var fileBytes = await reportingService.ExportDepreciationReportToExcelAsync(exportData);
            var fileName = $"AssetsDepreciationReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            var body = $@"
            <html>
              <body style='font-family: Arial, sans-serif; color: #333;'>
                <h3 style='color:#007bff;'>Assets Depreciation Report</h3>
                <p>Hello,</p>
                <p>Please find attached the assets depreciation report.</p>
                <p style='font-size:12px; color:#777;'>
                  Sent automatically on {DateTime.Now:dd MMM yyyy}.
                </p>
              </body>
            </html>";

            var emailService = new EmailService();
            var success = await emailService.SendReportAsync(EmailAddress, "Assets Depreciation Report", body, fileBytes, fileName);

            if (success)
                TempData["Message"] = $"Depreciation report sent successfully to {EmailAddress}!";
            else
                TempData["Message"] = $"Failed to send depreciation report to {EmailAddress}. Please try again later.";

            return RedirectToPage();
        }

        private async Task LoadAssetCategoriesAsync()
        {
            AssetCategories = (await _categoryRepo.GetAllAsync())
                .Select(c => new SelectListItem { Value = c.CategoryCode, Text = c.CategoryDescription })
                .ToList();
        }
    }
}