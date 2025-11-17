using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Services;
using TestModule.Models;
using TestModule.Data;

namespace TestModule.Pages.AssetPages
{
    public class ViewCostModel : PageModel
    {
        private readonly DepreciationService _depreciationService = new DepreciationService();
        private readonly AssetRepository assetRepository = new AssetRepository();

        [BindProperty(SupportsGet = true)]
        public string? AssetCode { get; set; }

        [BindProperty]
        public DateTime? StartDate { get; set; }

        [BindProperty]
        public DateTime? EndDate { get; set; }

        [BindProperty]
        public string? RecipientEmail { get; set; }

        public Asset Asset { get; set; }

        public List<(string Period, decimal Cost)> CostHistory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? code)
        {
            AssetCode = code;

            if (string.IsNullOrEmpty(AssetCode))
                return RedirectToPage("AllAssetsCostAndDepreciation");

            // Fetch full history if no date filters
            CostHistory = await _depreciationService.GetAssetCostHistoryAsync(AssetCode);

            return Page();
        }


        public async Task<IActionResult> OnPostPopulateAsync()
        {
            if (string.IsNullOrEmpty(AssetCode))
                return RedirectToPage("AllAssetsCostAndDepreciation");

            if (!StartDate.HasValue || !EndDate.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Please select both a start date and an end date.");

                CostHistory = await _depreciationService.GetAssetCostHistoryAsync(AssetCode);

                return Page();
            }

            var rawData = await _depreciationService.GetAssetCostInRangeAsync(AssetCode, StartDate.Value, EndDate.Value);

            CostHistory = rawData.Select(r =>
                (
                    Period: _depreciationService.FormatPeriodDisplay(r["Period"].ToString() ?? ""),
                    Cost: Convert.ToDecimal(r["Cost"])
                )).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            Asset = await assetRepository.GetByCodeAsync(AssetCode);

            if (Asset == null)
                return RedirectToPage("AllAssetsCostAndDepreciation");

            if (string.IsNullOrEmpty(AssetCode))
                return RedirectToPage("AllAssetsCostAndDepreciation");

            if (!StartDate.HasValue || !EndDate.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Please select both a start date and an end date before exporting.");

                CostHistory = await _depreciationService.GetAssetCostHistoryAsync(AssetCode);

                return Page();
            }

            var rawData = await _depreciationService.GetAssetCostInRangeAsync(AssetCode, StartDate.Value, EndDate.Value);

            CostHistory = rawData.Select(r =>
               (
                   Period: _depreciationService.FormatPeriodDisplay(r["Period"].ToString() ?? ""),
                   Cost: Convert.ToDecimal(r["Cost"])
               )).ToList();

            // Generate CSV file from currently displayed table
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Period,Cost");

            foreach (var item in CostHistory)
            {
                csv.AppendLine($"\"{item.Period}\",{item.Cost}");
            }

            var fileBytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"Cost for {Asset.Description} from {StartDate.Value:dd-MM-yyyy} to {EndDate.Value:dd-MM-yyyy}.csv";

            return File(fileBytes, "text/csv", fileName);
        }

        public async Task<IActionResult> OnPostExportAndSendAsync(string RecipientEmail)
        {
            if (string.IsNullOrEmpty(AssetCode))
                return RedirectToPage("AllAssetsCostAndDepreciation");

            if (!StartDate.HasValue || !EndDate.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Please select both a start date and an end date before exporting.");

                CostHistory = await _depreciationService.GetAssetCostHistoryAsync(AssetCode);

                return Page();
            }

            var reportingService = new ReportingService();
            var fileBytes = await reportingService.ExportCostToExcelAsync(AssetCode, CostHistory);
            var fileName = $"Asset Report - {DateTime.Now:yyyy-MM-dd}.xlsx";

            var body = $@"
            <html>
              <body style='font-family: Arial, sans-serif; color: #333;'>
                <h3 style='color:#007bff;'>Cost Report</h3>
                <p>Hello,</p>
                <p>Please find attached the cost report from {StartDate.Value:dd-MM-yyyy} to {EndDate.Value:dd-MM-yyyy}.</p>
                <p style='font-size:12px; color:#777;'>
                  Sent automatically on {DateTime.Now:dd MMM yyyy}.
                </p>
              </body>
            </html>";

            var emailService = new EmailService();
            var success = await emailService.SendReportAsync(RecipientEmail, "Cost Report", body, fileBytes, fileName);

            if (success)
                TempData["Message"] = $"Report sent successfully to {RecipientEmail}!";
            else
                TempData["Message"] = $"Failed to send report to {RecipientEmail}. Please try again later.";

            return RedirectToPage();
        }
    }
}