using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestModule.Data;
using TestModule.Models;
using TestModule.Services;
using System.Text;

namespace TestModule.Pages.AssetPages
{
    public class IndexModel : PageModel
    {
        private readonly AssetRepository _repo = new AssetRepository();
        private readonly ReportingService _reportingService = new ReportingService();

        public List<Asset> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _repo.GetAllAsync();
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            Items = await _repo.GetAllAsync();

            if (Items.Count == 0)
            {
                TempData["Message"] = "No data available to export.";
                return RedirectToPage();
            }

            // Build CSV
            var csv = new StringBuilder();
            csv.AppendLine("Code,Description,Asset Group,Asset Category,Department,Location,Purchase Date,Depreciation Start Date,Transaction Date,Purchase Amount,Cost,Tracking Code,Serial Number");

            foreach (var item in Items)
            {
                csv.AppendLine($"\"{item.Code}\",\"{item.Description?.Replace("\"", "\"\"")}\",\"{item.AssetGroup}\",\"{item.AssetCategory}\",\"{item.Department}\",\"{item.Location}\",\"{item.PurchaseDate:yyyy-MM-dd}\",\"{item.DepreciationStartDate:yyyy-MM-dd}\",\"{item.TransactionDate:yyyy-MM-dd}\",\"{item.PurchaseAmount}\",{item.Cost},\"{item.TrackingCode}\",\"{item.SerialNumber}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"Asset_Report_{DateTime.Now:yyyy-MM-dd}.csv";

            return File(bytes, "text/csv", fileName);
        }

        public async Task<IActionResult> OnPostExportAndSendAsync(string RecipientEmail)
        {
            var fileBytes = await _reportingService.ExportAssetsToExcelAsync();
            var fileName = $"Asset Report - {DateTime.Now:yyyy-MM-dd}.xlsx";

            var body = $@"
            <html>
              <body style='font-family: Arial, sans-serif; color: #333;'>
                <h3 style='color:#007bff;'>Asset Report</h3>
                <p>Hello,</p>
                <p>Please find attached the latest asset report.</p>
                <p style='font-size:12px; color:#777;'>
                  Sent automatically on {DateTime.Now:dd MMM yyyy}.
                </p>
              </body>
            </html>";

            var emailService = new EmailService();
            var success = await emailService.SendReportAsync(RecipientEmail, "Asset Report", body, fileBytes, fileName);

            if (success)
                TempData["Message"] = $"Report sent successfully to {RecipientEmail}!";
            else
                TempData["Message"] = $"Failed to send report to {RecipientEmail}. Please try again later.";

            return RedirectToPage();
        }
    }
}