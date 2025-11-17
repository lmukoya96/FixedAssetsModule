using ClosedXML.Excel;
using TestModule.Data;
using TestModule.Models;

namespace TestModule.Services
{
    public class ReportingService
    {
        private readonly AssetRepository _assetRepo = new AssetRepository();

        public async Task<byte[]> ExportAssetsToExcelAsync()
        {
            var assets = await _assetRepo.GetAllAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Assets");

            // Add headers
            worksheet.Cell(1, 1).Value = "Code";
            worksheet.Cell(1, 2).Value = "Description";
            worksheet.Cell(1, 3).Value = "Asset Group";
            worksheet.Cell(1, 4).Value = "Asset Category";
            worksheet.Cell(1, 5).Value = "Department";
            worksheet.Cell(1, 6).Value = "Location";
            worksheet.Cell(1, 7).Value = "Purchase Date";
            worksheet.Cell(1, 8).Value = "Depreciation Start Date";
            worksheet.Cell(1, 9).Value = "Transaction Date";
            worksheet.Cell(1, 10).Value = "Purchase Amount";
            worksheet.Cell(1, 11).Value = "Cost";
            worksheet.Cell(1, 12).Value = "Tracking Code";
            worksheet.Cell(1, 13).Value = "Serial Number";

            // Style header row: bold
            var headerRange = worksheet.Range(1, 1, 1, 13);
            headerRange.Style.Font.Bold = true;

            // Add data
            int row = 2;
            foreach (var asset in assets)
            {
                worksheet.Cell(row, 1).Value = asset.Code;
                worksheet.Cell(row, 2).Value = asset.Description;
                worksheet.Cell(row, 3).Value = asset.AssetGroup;
                worksheet.Cell(row, 4).Value = asset.AssetCategory;
                worksheet.Cell(row, 5).Value = asset.Department;
                worksheet.Cell(row, 6).Value = asset.Location;
                worksheet.Cell(row, 7).Value = asset.PurchaseDate;
                worksheet.Cell(row, 8).Value = asset.DepreciationStartDate;
                worksheet.Cell(row, 9).Value = asset.TransactionDate;
                worksheet.Cell(row, 10).Value = asset.PurchaseAmount;
                worksheet.Cell(row, 10).Style.NumberFormat.Format = "0.00";
                worksheet.Cell(row, 11).Value = asset.Cost;
                worksheet.Cell(row, 11).Style.NumberFormat.Format = "0.00";
                worksheet.Cell(row, 12).Value = asset.TrackingCode;
                worksheet.Cell(row, 13).Value = asset.SerialNumber;

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        public async Task<byte[]> ExportDepreciationToExcelAsync(string assetCode, List<(string Period, decimal Rate, decimal Amount, decimal BookValue)> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Depreciation");

            // Headers
            worksheet.Cell(1, 1).Value = "Period";
            worksheet.Cell(1, 2).Value = "Rate (%)";
            worksheet.Cell(1, 3).Value = "Depreciation Amount";
            worksheet.Cell(1, 4).Value = "Book Value";

            worksheet.Range(1, 1, 1, 4).Style.Font.Bold = true;

            // Data
            int row = 2;
            foreach (var entry in data)
            {
                worksheet.Cell(row, 1).Value = entry.Period;
                worksheet.Cell(row, 2).Value = entry.Rate;
                worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
                worksheet.Cell(row, 3).Value = entry.Amount;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.0000";
                worksheet.Cell(row, 4).Value = entry.BookValue;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "0.0000";

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        public async Task<byte[]> ExportCostToExcelAsync(string assetCode, List<(string Period, decimal Cost)> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Depreciation");

            // Headers
            worksheet.Cell(1, 1).Value = "Period";
            worksheet.Cell(1, 2).Value = "Cost";

            worksheet.Range(1, 1, 1, 2).Style.Font.Bold = true;

            // Data
            int row = 2;
            foreach (var entry in data)
            {
                worksheet.Cell(row, 1).Value = entry.Period;
                worksheet.Cell(row, 2).Value = entry.Cost;
                worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.0000";

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportDepreciationReportToExcelAsync(List<Dictionary<string, object>> depreciationData)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Depreciation Report");

            // Add headers
            worksheet.Cell(1, 1).Value = "Asset Code";
            worksheet.Cell(1, 2).Value = "Description";
            worksheet.Cell(1, 3).Value = "Total Depreciation";

            // Style header row: bold
            var headerRange = worksheet.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;

            // Add data
            int row = 2;
            foreach (var data in depreciationData)
            {
                worksheet.Cell(row, 1).Value = data.ContainsKey("AssetCode") ? data["AssetCode"]?.ToString() : "";
                worksheet.Cell(row, 2).Value = data.ContainsKey("Description") ? data["Description"]?.ToString() : "";

                if (data.ContainsKey("TotalDepreciation") && decimal.TryParse(data["TotalDepreciation"]?.ToString(), out decimal depreciation))
                {
                    worksheet.Cell(row, 3).Value = depreciation;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    worksheet.Cell(row, 3).Value = 0;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.00";
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}